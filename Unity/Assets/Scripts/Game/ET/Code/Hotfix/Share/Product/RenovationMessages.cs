#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class RenovationPeerHandler : ProductActorRequestHandler<HostPeer, ChooseRenovationRequest, ChooseRenovationResponse>
    {
        protected override async UniTask Run(HostPeer peer, ChooseRenovationRequest request, ChooseRenovationResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<ChooseRenovationSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.BindingGeneration = request.BindingGeneration;
                value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration; value.CommandId = request.CommandId;
                value.ChoiceVersion = request.ChoiceVersion; value.DefinitionId = request.DefinitionId;
                value.TargetStationId = request.TargetStationId; value.ExpectedUpgradeLevel = request.ExpectedUpgradeLevel;
            });
            HostSessionComponent authority = peer.Authority;
            using var reply = (ChooseRenovationSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = reply.Error; response.Message = reply.Message; response.Status = reply.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    public sealed class RenovationSubmitHandler : ProductActorRequestHandler<Entity, ChooseRenovationSubmitRequest, ChooseRenovationSubmitResponse>
    {
        protected override UniTask Run(Entity target, ChooseRenovationSubmitRequest request, ChooseRenovationSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly";
                response.Status = (int)GridResultStatus.Rejected; return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId); Match match = authority.Match; RestaurantRuntime restaurant = match.Restaurant;
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : restaurant == null ? "SoloRestaurantOnly"
                : restaurant.QueueRenovationChoice(peer,
                    new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration, request.LevelId,
                        request.LevelGeneration, request.BindingGeneration, request.CommandId),
                    new RenovationChoiceIntent(request.ChoiceVersion, request.DefinitionId, request.TargetStationId, request.ExpectedUpgradeLevel));
            response.Error = error == null ? 0 : ProductError.InvalidRequest; response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class RenovationClientCommands
    {
        public static ChooseRenovationRequest CreateRenovationChoice(this GridClientComponent self, int definition, long version, long station = 0, int expectedLevel = 0)
        {
            if (!self.Confirmed) throw new InvalidOperationException("Grid projection is not ready");
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            ProductCommandContext context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.Renovation;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<ChooseRenovationRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.BindingGeneration = context.BindingGeneration;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.CommandId = context.CommandId;
                value.DefinitionId = definition; value.ChoiceVersion = version;
                value.TargetStationId = station; value.ExpectedUpgradeLevel = expectedLevel;
            });
        }
    }
}
#endif
