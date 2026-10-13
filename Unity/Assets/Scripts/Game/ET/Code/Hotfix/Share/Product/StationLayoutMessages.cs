#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class LayoutPeerHandler : ProductActorRequestHandler<HostPeer, StationLayoutRequest, StationLayoutResponse>
    {
        protected override async UniTask Run(HostPeer peer, StationLayoutRequest request, StationLayoutResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            if (!peer.CanControlRestaurant()) { response.Error = ProductError.InvalidRequest; response.Message = "HostControlOnly"; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<StationLayoutSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.BindingGeneration = request.BindingGeneration;
                value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration; value.CommandId = request.CommandId;
                value.StationId = request.StationId; value.ExpectedLayoutVersion = request.ExpectedLayoutVersion;
                value.X = request.X; value.Y = request.Y; value.Orientation = request.Orientation;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (StationLayoutSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Match))]
    public sealed class LayoutSubmitHandler : ProductActorRequestHandler<HostSessionComponent, StationLayoutSubmitRequest, StationLayoutSubmitResponse>
    {
        protected override UniTask Run(HostSessionComponent authority, StationLayoutSubmitRequest request, StationLayoutSubmitResponse response)
        {
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId); Match match = authority.Match;
            string reason = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : match.ProductKitchen().QueueStationLayout(peer, new ProductCommandContext(request.MatchId, request.WorldId,
                    request.WorldGeneration, request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId),
                    new GridStationLayoutIntent(request.StationId, request.ExpectedLayoutVersion, new GridCell(request.X, request.Y), request.Orientation));
            response.CommandId = request.CommandId; response.Error = reason == null ? 0 : ProductError.InvalidRequest;
            response.Message = reason ?? peer.LastCommandReason; response.Status = (int)(reason == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class LayoutClientCommands
    {
        public static StationLayoutRequest CreateStationLayout(this GridClientComponent self, long station, GridCell cell,
            int orientation, long? expectedVersion = null)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            var context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.Layout;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<StationLayoutRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.BindingGeneration = context.BindingGeneration;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.CommandId = context.CommandId;
                value.StationId = station; value.ExpectedLayoutVersion = expectedVersion ?? self.LayoutVersion;
                value.X = cell.X; value.Y = cell.Y; value.Orientation = orientation;
            });
        }
    }
}
#endif
