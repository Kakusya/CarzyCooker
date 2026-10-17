#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(CooperativeTargetPings))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class TargetPingSystem
    {
        public static string CommitTargetPing(this GridKitchenComponent kitchen, HostPeer peer, ProductCommandContext context, int action, int targetKind, long targetId)
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
            var intent = new TargetPingCommand((TargetPingAction)action, (TargetPingKind)targetKind, targetId);
            if (!Enum.IsDefined(typeof(TargetPingAction), action))
            {
                return "InvalidTargetPing";
            }
            error = peer.CheckProductCommand(context, ProductCommandKind.TargetPing, intent.Equals(peer.LastTargetPing), out bool replay);
            if (error != null || replay)
            {
                return error;
            }
            if (level.Paused)
            {
                return "Paused";
            }
            if (level.Phase is not (LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining))
            {
                return "WrongPhase";
            }
            var state = level.GetComponent<CooperativeTargetPings>();
            if (intent.Action == TargetPingAction.Set)
            {
                if (!Enum.IsDefined(typeof(TargetPingKind), targetKind) || !kitchen.PingTargetExists((TargetPingKind)targetKind, targetId))
                {
                    return "PingTargetChanged";
                }
            }
            peer.RecordProductCommand(context, ProductCommandKind.TargetPing);
            peer.LastTargetPing = intent;
            if (intent.Action == TargetPingAction.Clear)
            {
                state.Members.Remove(peer.MemberId);
            }
            else
            {
                state.Members[peer.MemberId] = new TargetPingValue(peer.MemberId, peer.Generation, (TargetPingKind)targetKind, targetId, ++state.NextSerial, kitchen.Tick + (10000 + GridKitchenRules.TickMilliseconds - 1) / GridKitchenRules.TickMilliseconds);
            }
            peer.RecordProductResult(context, ProductCommandKind.TargetPing, GridResultStatus.Executed, null);
            kitchen.Dirty = true;
            kitchen.CommitGridState();
            return null;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class TargetPingPeerHandler : ProductActorRequestHandler<HostPeer, TargetPingRequest, TargetPingResponse>
    {
        protected override async UniTask Run(HostPeer peer, TargetPingRequest request, TargetPingResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "EntryClosed";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            var inner = ProductMessages.Create<TargetPingSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration;
                value.BindingGeneration = request.BindingGeneration; value.CommandId = request.CommandId; value.Action = request.Action; value.TargetKind = request.TargetKind; value.TargetId = request.TargetId;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (TargetPingSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class TargetPingSubmitHandler : ProductActorRequestHandler<Entity, TargetPingSubmitRequest, TargetPingSubmitResponse>
    {
        protected override UniTask Run(Entity target, TargetPingSubmitRequest request, TargetPingSubmitResponse response)
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
                : kitchen.CommitTargetPing(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId), request.Action, request.TargetKind, request.TargetId);
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class TargetPingClientCommands
    {
        public static TargetPingRequest CreateTargetPing(this GridClientComponent self, TargetPingAction action, TargetPingKind kind = TargetPingKind.Station, long targetId = 0)
        {
            if (!self.Confirmed)
            {
                throw new InvalidOperationException("Grid projection is not ready");
            }
            var context = self.GetParent<ClientSessionComponent>().CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.TargetPing;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<TargetPingRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration;
                value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId; value.Action = (int)action; value.TargetKind = (int)kind; value.TargetId = targetId;
            });
        }
    }
}
#endif
