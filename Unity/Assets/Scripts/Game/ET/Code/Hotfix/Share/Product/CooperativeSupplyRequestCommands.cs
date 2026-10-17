#if !DOTNET
using System;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(CooperativeSupplyRequests))]
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class SupplyRequestSystem
    {
        public static string CommitSupplyRequest(this GridKitchenComponent kitchen, HostPeer peer, ProductCommandContext context, int action, int sourceId, long expectedRequestId, long expectedRevision)
        {
            Level level = kitchen.ProductLevel();
            if (kitchen.Stopped || level == null || peer == null || !peer.EntryOpen || !kitchen.HasBinding(peer.MemberId, peer.Generation))
            {
                return "EntryClosed";
            }
            string error = context.Validate(((Match)kitchen.Match).MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (error != null)
            {
                return error;
            }
            var intent = new SupplyRequestCommand((SupplyRequestAction)action, sourceId, expectedRequestId, expectedRevision);
            if (!Enum.IsDefined(typeof(SupplyRequestAction), action))
            {
                return "InvalidSupplyRequest";
            }
            error = peer.CheckProductCommand(context, ProductCommandKind.SupplyRequest, intent.Equals(peer.LastSupplyRequest), out bool replay);
            if (error != null || replay)
            {
                return error;
            }
            if (level.Paused)
            {
                return "Paused";
            }
            if (level.Phase is not (LevelPhase.Preparing or LevelPhase.Operating))
            {
                return "WrongPhase";
            }
            var state = level.GetComponent<CooperativeSupplyRequests>();
            state.Members.TryGetValue(peer.MemberId, out var previous);
            if (previous.Id != expectedRequestId || previous.Revision != expectedRevision)
            {
                return "SupplyRequestChanged";
            }
            if (intent.Action == SupplyRequestAction.Publish && !kitchen.GetComponent<KitchenSupply>().RemainingPackages.ContainsKey(sourceId))
            {
                return "InvalidSupply";
            }
            peer.RecordProductCommand(context, ProductCommandKind.SupplyRequest);
            peer.LastSupplyRequest = intent;
            if (intent.Action == SupplyRequestAction.Cancel)
            {
                state.Members.Remove(peer.MemberId);
            }
            else
            {
                state.Members[peer.MemberId] = new SupplyRequestValue(IdGenerater.Instance.GenerateId(), ++state.NextRevision, peer.MemberId, peer.Generation, sourceId, SupplyRequestState.Pending);
            }
            peer.RecordProductResult(context, ProductCommandKind.SupplyRequest, GridResultStatus.Executed, null);
            kitchen.Dirty = true;
            kitchen.CommitGridState();
            return null;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class SupplyRequestPeerHandler : ProductActorRequestHandler<HostPeer, SupplyRequestRequest, SupplyRequestResponse>
    {
        protected override async UniTask Run(HostPeer peer, SupplyRequestRequest request, SupplyRequestResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "EntryClosed";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            var inner = ProductMessages.Create<SupplyRequestSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration;
                value.BindingGeneration = request.BindingGeneration; value.CommandId = request.CommandId; value.Action = request.Action; value.SourceId = request.SourceId; value.ExpectedRequestId = request.ExpectedRequestId; value.ExpectedRevision = request.ExpectedRevision;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (SupplyRequestSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class SupplyRequestSubmitHandler : ProductActorRequestHandler<Entity, SupplyRequestSubmitRequest, SupplyRequestSubmitResponse>
    {
        protected override UniTask Run(Entity target, SupplyRequestSubmitRequest request, SupplyRequestSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            response.Status = (int)GridResultStatus.Rejected;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly";
                return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            GridKitchenComponent kitchen = ((Match)authority.Match).ProductKitchen();
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : kitchen == null ? "RestaurantUnavailable"
                : kitchen.CommitSupplyRequest(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId), request.Action, request.SourceId, request.ExpectedRequestId, request.ExpectedRevision);
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class SupplyRequestClientCommands
    {
        public static SupplyRequestRequest CreateSupplyRequest(this GridClientComponent self, SupplyRequestAction action, int sourceId = 0, long? expectedRequestId = null, long? expectedRevision = null)
        {
            if (!self.Confirmed)
            {
                throw new InvalidOperationException("Grid projection is not ready");
            }
            var current = self.SupplyRequests.FirstOrDefault(value => value.MemberId == self.MemberId);
            var context = self.GetParent<ClientSessionComponent>().CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.SupplyRequest;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<SupplyRequestRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration;
                value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId; value.Action = (int)action; value.SourceId = sourceId; value.ExpectedRequestId = expectedRequestId ?? current.Id; value.ExpectedRevision = expectedRevision ?? current.Revision;
            });
        }
    }
}
#endif
