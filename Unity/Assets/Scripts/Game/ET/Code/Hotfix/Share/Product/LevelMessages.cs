#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class LevelPeerHandler : ProductActorRequestHandler<HostPeer, LevelCommandRequest, LevelCommandResponse>
    {
        protected override async UniTask Run(HostPeer peer, LevelCommandRequest request, LevelCommandResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            if (!peer.CanControlRestaurant()) { response.Error = ProductError.InvalidRequest; response.Message = "HostControlOnly"; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<LevelSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.BindingGeneration = request.BindingGeneration; value.CommandId = request.CommandId;
                value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration;
                value.Action = request.Action; value.NextLevelConfigId = request.NextLevelConfigId;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (LevelSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Match))]
    public sealed class LevelSubmitHandler : ProductActorRequestHandler<HostSessionComponent, LevelSubmitRequest, LevelSubmitResponse>
    {
        protected override UniTask Run(HostSessionComponent authority, LevelSubmitRequest request, LevelSubmitResponse response)
        {
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId); Match match = authority.Match;
            RestaurantRuntime restaurant = match.Restaurant;
            string reason = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : restaurant == null ? "SoloRestaurantOnly"
                : restaurant.QueueLevelControl(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId),
                    new LevelControlIntent((LevelControlAction)request.Action, request.NextLevelConfigId));
            response.CommandId = request.CommandId; response.Error = reason == null ? 0 : ProductError.InvalidRequest;
            response.Message = reason ?? peer.LastCommandReason; response.Status = (int)(reason == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static class LevelClientCommands
    {
        public static LevelCommandRequest CreateLevelCommand(this GridClientComponent self, LevelControlAction action)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            ProductCommandContext context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            self.LastCommandKind = ProductCommandKind.Level;
            return ProductMessages.Create<LevelCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.Action = (int)action;
                value.NextLevelConfigId = action is LevelControlAction.Continue or LevelControlAction.SaveAndContinue ? self.NextLevelConfigId : 0;
            });
        }
    }
}
#endif
