#if !DOTNET
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(GridKitchenComponent))]
    public sealed class ProgressPreparedHandler : ProductActorRequestHandler<Entity, ProgressPreparedRequest, ProgressPreparedResponse>
    {
        protected override UniTask Run(Entity target, ProgressPreparedRequest request, ProgressPreparedResponse response)
        {
            var authority = target as HostSessionComponent;
            Match match = authority?.Match ?? default;
            var result = match?.GetChild<CooperativeSaveResult>(request.ResultEntityId);
            if (match == null || match.MatchId != request.MatchId || result == null || result.InstanceId != request.ResultInstanceId || !result.Preparing)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "StaleSaveResult";
                return UniTask.CompletedTask;
            }
            result.Preparing = false;
            try
            {
                if (request.Error != 0)
                {
                    throw new InvalidOperationException(request.Failure);
                }
                if (request.Tickets.Count != result.Claims.Count)
                {
                    throw new InvalidOperationException("InvalidClaimIdentity");
                }
                // Root已验源授权并签名；返回仍按冻结的具体身份对应，拒绝迟到/错结果。
                foreach (byte[] document in request.Tickets)
                {
                    var signed = OfflineProfiles.Verify(document, OfflineProfiles.ClaimTicket);
                    var ticket = ProgressClaimCodec.Decode<ProgressTicketValue>(signed.Body);
                    if (!result.Claims.TryGetValue(ticket.EligibleProfileId, out var reference))
                    {
                        throw new InvalidOperationException("IneligibleProfile");
                    }
                    CooperativeClaim claim = reference;
                    if (claim.TicketId != ticket.TicketId || claim.KeyId != ticket.EligibleKeyId || !ticket.Success.SequenceEqual(request.Success))
                    {
                        throw new InvalidOperationException("InvalidClaimIdentity");
                    }
                }
                result.SignedSuccess = (byte[])request.Success.Clone();
                foreach (byte[] document in request.Tickets)
                {
                    var ticket = ProgressClaimCodec.Decode<ProgressTicketValue>(OfflineProfiles.Verify(document, OfflineProfiles.ClaimTicket).Body);
                    ((CooperativeClaim)result.Claims[ticket.EligibleProfileId]).Document = (byte[])document.Clone();
                }
                result.State = SoloSaveState.Ready; result.Failure = null;
                if (result.HostSaveRequired && result.PendingControl is LevelControlEnvelope control)
                {
                    HostPeer peer = control.Peer;
                    var member = peer == null ? null : match.GetChild<Participant>(peer.MemberId);
                    if (member?.ProfileId != null)
                    {
                        result.WriteCooperativeHostClaim(member.ProfileId);
                    }
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                result.State = SoloSaveState.Failed; result.Failure = exception.Message;
                if (result.PendingControl is LevelControlEnvelope control)
                {
                    ((HostPeer)control.Peer)?.RecordProductResult(control.Context, ProductCommandKind.Level, GridResultStatus.Rejected, "SaveFailed");
                }
                result.PendingControl = null;
            }
            RestaurantRuntime restaurant = match.Restaurant;
            if (restaurant != null)
            {
                ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProgressHostSavedHandler : ProductActorRequestHandler<Entity, ProgressHostSavedRequest, ProgressHostSavedResponse>
    {
        protected override UniTask Run(Entity target, ProgressHostSavedRequest request, ProgressHostSavedResponse response)
        {
            var authority = target as HostSessionComponent;
            Match match = authority?.Match ?? default;
            var result = match?.GetChild<CooperativeSaveResult>(request.ResultEntityId);
            if (match == null || match.MatchId != request.MatchId || result == null || result.InstanceId != request.ResultInstanceId || result.State != SoloSaveState.Writing)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "StaleSaveResult";
                return UniTask.CompletedTask;
            }
            result.State = request.Error == 0 ? SoloSaveState.Committed : SoloSaveState.Failed;
            result.Failure = request.Failure;
            if (request.Error == 0)
            {
                result.HostSavedReceipt = ProgressClaimCodec.Decode<ProgressCommitReceipt>(request.Receipt);
                result.HostSaveCommitted = true;
            }
            RestaurantRuntime restaurant = match.Restaurant;
            Level level = restaurant?.CurrentLevel ?? default;
            if (result.PendingControl is LevelControlEnvelope control && level != null && level.Id == result.LevelId &&
                level.Generation == result.LevelGeneration && level.Phase == LevelPhase.Completed)
            {
                HostPeer peer = control.Peer;
                GridKitchenComponent kitchen = restaurant.Kitchen;
                if (peer != null && kitchen.HasBinding(peer.MemberId, control.Context.BindingGeneration))
                {
                    if (result.HostSaveCommitted)
                    {
                        restaurant.Controls.Enqueue(control);
                    }
                    else peer.RecordProductResult(control.Context, ProductCommandKind.Level, GridResultStatus.Rejected, "SaveFailed");
                }
                kitchen.Dirty = true;
            }
            result.PendingControl = null;
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProgressAckPeerHandler : ProductActorRequestHandler<HostPeer, ProgressAckRequest, ProgressAckResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProgressAckRequest request, ProgressAckResponse response)
        {
            HostSessionComponent authority = peer.Authority;
            using var reply = (ProgressAckSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(),
                ProductMessages.Create<ProgressAckSubmitRequest>(value =>
                {
                    value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId;
                    value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.BindingGeneration = request.BindingGeneration;
                    value.Kind = request.Kind; value.Id = request.Id;
                }), false);
            response.Error = reply.Error; response.Message = reply.Message;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeCloseReceipt))]
    public sealed class ProgressAckSubmitHandler : ProductActorRequestHandler<Entity, ProgressAckSubmitRequest, ProgressAckSubmitResponse>
    {
        protected override UniTask Run(Entity target, ProgressAckSubmitRequest request, ProgressAckSubmitResponse response)
        {
            var authority = target as HostSessionComponent;
            Match match = authority?.Match ?? default;
            HostPeer peer = authority?.GetChild<HostPeer>(request.PeerId);
            Participant member = peer == null ? null : match?.GetChild<Participant>(peer.MemberId);
            var state = match?.GetComponent<CooperativeClaimState>();
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : match?.MatchId != request.MatchId ? "WrongMatch"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration ||
                    !peer.EntryOpen || peer.Closing || !peer.ProfileProved || member == null || (HostPeer)member.BoundPeer != peer ? "EntryClosed"
                : "UnknownProgressDelivery";
            if (error == "UnknownProgressDelivery" && state != null)
            {
                if ((ProgressDeliveryKind)request.Kind == ProgressDeliveryKind.Ticket)
                {
                    foreach (var reference in state.Results.Values)
                    {
                        CooperativeSaveResult result = reference;
                        if (result.Claims.TryGetValue(member.ProfileId, out var claimRef))
                        {
                            CooperativeClaim claim = claimRef;
                            if (claim.TicketId == request.Id && claim.KeyId == member.ProfileKeyId && claim.Document != null)
                            {
                                claim.AckedBindings.Add(peer.Generation); error = null; break;
                            }
                        }
                    }
                }
                else if ((ProgressDeliveryKind)request.Kind == ProgressDeliveryKind.NormalClose && state.CloseReceipts.TryGetValue(member.ProfileId, out var receiptRef))
                {
                    CooperativeCloseReceipt receipt = receiptRef;
                    if (receipt.ReceiptId == request.Id && receipt.Document != null)
                    {
                        receipt.AckedBindings.Add(peer.Generation); error = null;
                    }
                }
            }
            response.Error = error == null ? 0 : ProductError.InvalidRequest; response.Message = error;
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeCloseReceipt))]
    public sealed class ProgressPulseHandler : MessageHandler<Entity, ProgressPulse>
    {
        protected override UniTask Run(Entity target, ProgressPulse message)
        {
            try
            {
                var authority = target as HostSessionComponent;
                Match match = authority?.Match ?? default;
                var state = match?.GetComponent<CooperativeClaimState>();
                if (state == null || match.MatchId != message.MatchId || state.Id != message.StateId || state.InstanceId != message.StateInstanceId)
                {
                    return UniTask.CompletedTask;
                }
                state.PulsePending = false;
                foreach (var peer in authority.Children.Values.OfType<HostPeer>())
                {
                    var member = match.GetChild<Participant>(peer.MemberId);
                    if (!peer.EntryOpen || peer.Closing || !peer.ProfileProved || member?.Online != true || (HostPeer)member.BoundPeer != peer)
                    {
                        continue;
                    }
                    foreach (var reference in state.Results.Values)
                    {
                        CooperativeSaveResult result = reference;
                        if (!result.Claims.TryGetValue(member.ProfileId, out var claimRef))
                        {
                            continue;
                        }
                        CooperativeClaim claim = claimRef;
                        if (claim.KeyId == member.ProfileKeyId && claim.Document != null && !claim.AckedBindings.Contains(peer.Generation))
                        {
                            Send(peer, match.MatchId, ProgressDeliveryKind.Ticket, claim.TicketId, claim.Document);
                        }
                    }
                    if (state.CloseReceipts.TryGetValue(member.ProfileId, out var receiptRef))
                    {
                        CooperativeCloseReceipt receipt = receiptRef;
                        if (receipt.Document != null && !receipt.AckedBindings.Contains(peer.Generation))
                        {
                            Send(peer, match.MatchId, ProgressDeliveryKind.NormalClose, receipt.ReceiptId, receipt.Document);
                        }
                    }
                }
                if (state.Closing && !state.CloseReady)
                {
                    bool pending = false;
                    foreach (var peer in authority.Children.Values.OfType<HostPeer>())
                    {
                        var member = match.GetChild<Participant>(peer.MemberId);
                        if (!peer.EntryOpen || peer.Closing || !peer.ProfileProved || member?.Online != true || (HostPeer)member.BoundPeer != peer)
                        {
                            continue;
                        }
                        if (state.CloseReceipts.TryGetValue(member.ProfileId, out var receiptRef))
                        {
                            CooperativeCloseReceipt receipt = receiptRef;
                            if (receipt.Document == null || !receipt.AckedBindings.Contains(peer.Generation))
                            {
                                pending = true;
                            }
                        }
                    }
                    state.CloseReady = !pending || TimeInfo.Instance.FrameTime >= state.CloseDeadline;
                }
            }
            finally { message.Dispose(); }
            return UniTask.CompletedTask;
        }

        private static void Send(HostPeer peer, string match, ProgressDeliveryKind kind, string id, byte[] document)
        {
            ProductDelivery.Send(peer, ProductMessages.Create<ProgressDelivery>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = match; value.BindingGeneration = peer.Generation;
                value.Kind = (int)kind; value.Id = id; value.Document = (byte[])document.Clone();
            }));
        }
    }
}
#endif
