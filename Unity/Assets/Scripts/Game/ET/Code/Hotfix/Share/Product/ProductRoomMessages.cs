#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProductRoomPeerHandler : ProductActorRequestHandler<HostPeer, ProductRoomControlRequest, ProductRoomControlResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductRoomControlRequest request, ProductRoomControlResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.CanControlRestaurant())
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "HostControlOnly";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            ProductRoomSubmitRequest inner = ProductMessages.Create<ProductRoomSubmitRequest>(message =>
            {
                message.PeerId = peer.Id;
                message.PeerInstanceId = peer.InstanceId;
                message.BindingGeneration = request.BindingGeneration;
                message.ProtocolVersion = request.ProtocolVersion;
                message.MatchId = request.MatchId;
                message.CommandId = request.CommandId;
                message.ExpectedRoomVersion = request.ExpectedRoomVersion;
                message.Action = request.Action;
                message.MenuVariant = request.MenuVariant;
                message.ContentChapterId = request.ContentChapterId;
            });
            HostSessionComponent authority = peer.Authority;
            using var reply = (ProductRoomSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = reply.Error;
            response.Message = reply.Message;
            response.Status = reply.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProductRoomSubmitHandler : ProductActorRequestHandler<Entity, ProductRoomSubmitRequest, ProductRoomSubmitResponse>
    {
        protected override UniTask Run(Entity target, ProductRoomSubmitRequest request, ProductRoomSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "InternalOnly";
                response.Status = (int)GridResultStatus.Rejected;
                return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration ||
                    !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : authority.CommitRoom(peer, new ProductCommandContext(request.MatchId, string.Empty, 0, 0, 0,
                        request.BindingGeneration, request.CommandId),
                    new ProductRoomIntent(request.ExpectedRoomVersion, (ProductRoomAction)request.Action, (ProductMenuVariant)request.MenuVariant, request.ContentChapterId));
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }
}
#endif
