#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class SupplyPeerHandler : ProductActorRequestHandler<HostPeer, SupplyCommandRequest, SupplyCommandResponse>
    {
        protected override async UniTask Run(HostPeer peer, SupplyCommandRequest request, SupplyCommandResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<SupplySubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.BindingGeneration = request.BindingGeneration;
                value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration; value.CommandId = request.CommandId;
                value.Action = request.Action; value.SourceId = request.SourceId; value.Quantity = request.Quantity;
                value.StationId = request.StationId; value.ShipmentId = request.ShipmentId; value.PackageId = request.PackageId;
                value.ContentItemId = request.ContentItemId; value.LayoutVersion = request.LayoutVersion;
            });
            HostSessionComponent authority = peer.Authority;
            using var reply = (SupplySubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = reply.Error; response.Message = reply.Message; response.Status = reply.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class SupplySubmitHandler : ProductActorRequestHandler<Entity, SupplySubmitRequest, SupplySubmitResponse>
    {
        protected override UniTask Run(Entity target, SupplySubmitRequest request, SupplySubmitResponse response)
        {
            response.CommandId = request.CommandId;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly";
                response.Status = (int)GridResultStatus.Rejected; return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId); Match match = authority.Match;
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : match.ProductKitchen().QueueSupplyCommand(peer,
                    new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration, request.LevelId,
                        request.LevelGeneration, request.BindingGeneration, request.CommandId),
                    new SupplyIntent((SupplyAction)request.Action, request.SourceId, request.Quantity, request.StationId,
                        request.ShipmentId, request.PackageId, request.ContentItemId, request.LayoutVersion));
            response.Error = error == null ? 0 : ProductError.InvalidRequest; response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class SupplyClientCommands
    {
        public static SupplyCommandRequest CreateSupplyCommand(this GridClientComponent self, SupplyIntent intent)
        {
            if (!self.Confirmed) throw new InvalidOperationException("Grid projection is not ready");
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            ProductCommandContext context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.Supply;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<SupplyCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.BindingGeneration = context.BindingGeneration;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.CommandId = context.CommandId;
                value.Action = (int)intent.Action; value.SourceId = intent.SourceId; value.Quantity = intent.Quantity;
                value.StationId = intent.StationId; value.ShipmentId = intent.ShipmentId; value.PackageId = intent.PackageId;
                value.ContentItemId = intent.ContentItemId; value.LayoutVersion = intent.LayoutVersion;
            });
        }
    }
}
#endif
