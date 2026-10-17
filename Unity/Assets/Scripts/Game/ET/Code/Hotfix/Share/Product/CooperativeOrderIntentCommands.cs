#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(CooperativeOrderIntents))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class OrderIntentSystem
    {
        public static string CommitOrderIntent(this GridKitchenComponent kitchen, HostPeer peer, ProductCommandContext context, int action, long orderId, long customerId)
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
            var intent = new OrderIntentCommand((OrderIntentAction)action, orderId, customerId);
            if (!Enum.IsDefined(typeof(OrderIntentAction), action))
            {
                return "InvalidOrderIntent";
            }
            error = peer.CheckProductCommand(context, ProductCommandKind.OrderIntent, intent.Equals(peer.LastOrderIntent), out bool replay);
            if (error != null || replay)
            {
                return error;
            }
            if (level.Paused)
            {
                return "Paused";
            }
            if (level.Phase is not (LevelPhase.Operating or LevelPhase.Draining))
            {
                return "WrongPhase";
            }
            var state = level.GetComponent<CooperativeOrderIntents>();
            if (intent.Action == OrderIntentAction.Set)
            {
                Order order = level.GetChild<Order>(orderId);
                if (order == null || order.LevelGeneration != level.Generation || order.CustomerId != customerId ||
                    order.State != OrderState.Waiting || kitchen.Tick >= order.DeadlineTick)
                {
                    return "OrderEnded";
                }
            }
            peer.RecordProductCommand(context, ProductCommandKind.OrderIntent);
            peer.LastOrderIntent = intent;
            if (intent.Action == OrderIntentAction.Clear)
            {
                state.Members.Remove(peer.MemberId);
            }
            else
            {
                state.Members[peer.MemberId] = new OrderIntentValue(peer.MemberId, peer.Generation, orderId, customerId);
            }
            peer.RecordProductResult(context, ProductCommandKind.OrderIntent, GridResultStatus.Executed, null);
            kitchen.Dirty = true;
            kitchen.CommitGridState();
            return null;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class OrderIntentPeerHandler : ProductActorRequestHandler<HostPeer, OrderIntentRequest, OrderIntentResponse>
    {
        protected override async UniTask Run(HostPeer peer, OrderIntentRequest request, OrderIntentResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "EntryClosed";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            var inner = ProductMessages.Create<OrderIntentSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration;
                value.BindingGeneration = request.BindingGeneration; value.CommandId = request.CommandId; value.Action = request.Action; value.OrderId = request.OrderId; value.CustomerId = request.CustomerId;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (OrderIntentSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class OrderIntentSubmitHandler : ProductActorRequestHandler<Entity, OrderIntentSubmitRequest, OrderIntentSubmitResponse>
    {
        protected override UniTask Run(Entity target, OrderIntentSubmitRequest request, OrderIntentSubmitResponse response)
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
                : kitchen.CommitOrderIntent(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId), request.Action, request.OrderId, request.CustomerId);
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class OrderIntentClientCommands
    {
        public static OrderIntentRequest CreateOrderIntent(this GridClientComponent self, OrderIntentAction action, long orderId = 0, long customerId = 0)
        {
            if (!self.Confirmed)
            {
                throw new InvalidOperationException("Grid projection is not ready");
            }
            var context = self.GetParent<ClientSessionComponent>().CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.OrderIntent;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<OrderIntentRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration;
                value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId; value.Action = (int)action; value.OrderId = orderId; value.CustomerId = customerId;
            });
        }
    }
}
#endif
