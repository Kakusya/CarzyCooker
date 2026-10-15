#if !DOTNET
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProgressCloseMatchPeerHandler : ProductActorRequestHandler<HostPeer, ProgressCloseMatchRequest, ProgressCloseMatchResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProgressCloseMatchRequest request, ProgressCloseMatchResponse response)
        {
            if (!peer.CanControlRestaurant())
            {
                response.Error = ProductError.InvalidRequest; response.Message = "HostControlOnly";
                return;
            }
            HostSessionComponent authority = peer.Authority;
            using var reply = (ProgressCloseMatchSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(),
                ProductMessages.Create<ProgressCloseMatchSubmitRequest>(value =>
                {
                    value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId;
                    value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId;
                    value.BindingGeneration = request.BindingGeneration; value.ExpectedRoomVersion = request.ExpectedRoomVersion;
                }), false);
            response.Error = reply.Error; response.Message = reply.Message; response.Status = reply.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeCloseReceipt))]
    public sealed class ProgressCloseMatchSubmitHandler : ProductActorRequestHandler<Entity, ProgressCloseMatchSubmitRequest, ProgressCloseMatchSubmitResponse>
    {
        protected override UniTask Run(Entity target, ProgressCloseMatchSubmitRequest request, ProgressCloseMatchSubmitResponse response)
        {
            var authority = target as HostSessionComponent;
            Match match = authority?.Match ?? default;
            var peer = authority?.GetChild<HostPeer>(request.PeerId);
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : match?.MatchId != request.MatchId ? "WrongMatch"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration ||
                    !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : !peer.CanControlRestaurant() ? "HostControlOnly" : null;
            var state = match?.GetComponent<CooperativeClaimState>();
            if (error == null && state?.Closing != true && state != null &&
                state.Results.Values.Any(reference => ((CooperativeSaveResult)reference).Preparing))
            {
                error = "ProgressPreparing";
            }
            if (error == null && state?.Closing != true && match.RoomVersion != request.ExpectedRoomVersion)
            {
                error = "RoomChanged";
            }
            if (error != null)
            {
                response.Error = ProductError.InvalidRequest; response.Message = error;
                return UniTask.CompletedTask;
            }
            state ??= match.AddComponent<CooperativeClaimState>();
            if (!state.Closing)
            {
                state.Closing = true;
                state.CloseDeadline = TimeInfo.Instance.FrameTime + 4000;
                RestaurantRuntime restaurant = match.Restaurant;
                restaurant?.StopRestaurant();
                foreach (var reference in state.Results.Values)
                {
                    CooperativeSaveResult result = reference;
                    if (result.SignedSuccess == null)
                    {
                        continue;
                    }
                    var success = ProgressClaimCodec.Decode<CooperativeSuccessValue>(OfflineProfiles.Verify(result.SignedSuccess, OfflineProfiles.SuccessResult).Body);
                    foreach (var claimReference in result.Claims.Values)
                    {
                        CooperativeClaim claim = claimReference;
                        if (state.CloseReceipts.ContainsKey(claim.ProfileId))
                        {
                            continue;
                        }
                        var receipt = match.AddChild<CooperativeCloseReceipt>();
                        receipt.ReceiptId = Guid.NewGuid().ToString("N"); receipt.ProfileId = claim.ProfileId;
                        state.CloseReceipts.Add(receipt.ProfileId, receipt);
                        receipt.SignCloseReceipt(success.Authority);
                    }
                }
                state.CloseReady = state.CloseReceipts.Count == 0;
                state.NextPulse = 0;
            }
            response.Status = (int)(state.CloseReady ? GridResultStatus.Executed : GridResultStatus.Accepted);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(Match))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class CooperativeCloseReceiptSystem
    {
        public static void SignCloseReceipt(this CooperativeCloseReceipt self, byte[] authority)
        {
            self.Preparing = true;
            string matchId = self.GetParent<Match>().MatchId;
            long id = self.Id, instance = self.InstanceId;
            var document = MemoryPackHelper.Serialize(new ProgressCloseValue
            {
                ReceiptId = self.ReceiptId, MatchId = matchId, ProfileId = self.ProfileId, Authority = authority
            });
            var repositoryActor = self.Root().GetComponent<SoloSaveRepository>().GetActorId();
            var authorityActor = ((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId();
            EntityRef<CooperativeCloseReceipt> original = self;
            EntityRef<ProcessInnerSender> senderRef = self.Root().GetComponent<ProcessInnerSender>();
            self.GetAsyncOwner().Run(async context =>
            {
                int error = 0; string failure = null; byte[] signed = null;
                try
                {
                    using var reply = (ProgressRepositoryResponse)await ((ProcessInnerSender)senderRef).Call(repositoryActor,
                        ProductMessages.Create<ProgressRepositoryRequest>(value => { value.Operation = (int)ProgressRepositoryOperation.SignClose; value.MatchId = matchId; value.Document = document; }), false);
                    error = reply.Error; failure = reply.Message;
                    signed = reply.Document == null ? null : (byte[])reply.Document.Clone();
                }
                catch (Exception exception) { error = ProductError.InvalidRequest; failure = exception.Message; }
                context.ThrowIfStopped(); self = original;
                var sender = (ProcessInnerSender)senderRef;
                if (self == null || sender == null || sender.IsStopped)
                {
                    return;
                }
                using var replyReceipt = (ProgressClosePreparedResponse)await sender.Call(authorityActor, ProductMessages.Create<ProgressClosePreparedRequest>(value =>
                {
                    value.MatchId = matchId; value.ReceiptEntityId = id; value.ReceiptInstanceId = instance;
                    value.Error = error; value.Failure = failure; value.Document = signed;
                }), false);
            });
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(CooperativeCloseReceipt))]
    public sealed class ProgressClosePreparedHandler : ProductActorRequestHandler<Entity, ProgressClosePreparedRequest, ProgressClosePreparedResponse>
    {
        protected override UniTask Run(Entity target, ProgressClosePreparedRequest request, ProgressClosePreparedResponse response)
        {
            var authority = target as HostSessionComponent;
            Match match = authority?.Match ?? default;
            var receipt = match?.GetChild<CooperativeCloseReceipt>(request.ReceiptEntityId);
            if (match?.MatchId != request.MatchId || receipt == null || receipt.InstanceId != request.ReceiptInstanceId || !receipt.Preparing)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "StaleCloseReceipt";
                return UniTask.CompletedTask;
            }
            receipt.Preparing = false;
            if (request.Error == 0)
            {
                receipt.Document = (byte[])request.Document.Clone();
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
