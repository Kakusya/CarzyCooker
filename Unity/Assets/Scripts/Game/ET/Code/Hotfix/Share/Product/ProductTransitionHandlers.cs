#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(Level))]
    public abstract class ProductTransitionHandler<TMessage> : HandlerObject, IMHandler where TMessage : MessageObject
    {
        protected abstract ProductTransitionIdentity Read(TMessage message);
        protected abstract void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, TMessage message, ProductTransitionIdentity identity);
        protected virtual bool IsLevelScoped() => true;

        public UniTask Handle(Entity entity, Address fromAddress, MessageObject input)
        {
            try
            {
                if (entity is not HostSessionComponent authority || input is not TMessage message || fromAddress != authority.Fiber().Address)
                {
                    return UniTask.CompletedTask;
                }
                Match match = authority.Match;
                RestaurantRuntime restaurant = match?.Restaurant ?? default;
                GridKitchenComponent kitchen = restaurant?.Kitchen ?? default;
                Level level = restaurant?.CurrentLevel ?? default;
                ProductTransitionIdentity identity = Read(message);
                if (restaurant == null || restaurant.Stopped || kitchen == null || level == null ||
                    identity.ProtocolVersion != ProductSessionRules.ProtocolVersion || identity.MatchId != match.MatchId ||
                    identity.WorldId != kitchen.WorldId || identity.WorldGeneration != kitchen.WorldGeneration ||
                    identity.SourceId <= 0 || identity.SourceInstanceId <= 0 || identity.Serial <= 0 ||
                    (IsLevelScoped() && (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation)))
                {
                    return UniTask.CompletedTask;
                }
                Enqueue(kitchen, restaurant, level, message, identity);
                return UniTask.CompletedTask;
            }
            finally
            {
                input.Dispose();
            }
        }

        public Type GetRequestType() => typeof(TMessage);
        public Type GetResponseType() => null;
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(GridKitchenComponent))]
    public sealed class KitchenWorkTransitionHandler : ProductTransitionHandler<KitchenWorkTransition>
    {
        protected override bool IsLevelScoped() => false;
        protected override ProductTransitionIdentity Read(KitchenWorkTransition message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, KitchenWorkTransition message, ProductTransitionIdentity identity)
        {
            KitchenProcessKind kind = (KitchenProcessKind)message.ProcessKind;
            Entity source = kind switch
            {
                KitchenProcessKind.Manual => kitchen.GetChild<GridManualProcess>(identity.SourceId),
                KitchenProcessKind.Soup or KitchenProcessKind.Juice => kitchen.GetChild<GridAutoProcess>(identity.SourceId),
                KitchenProcessKind.Bake => kitchen.GetChild<GridBakeProcess>(identity.SourceId),
                _ => null
            };
            if (source?.InstanceId == identity.SourceInstanceId && message.Action == (int)KitchenTransitionAction.Complete)
            {
                kitchen.WorkTransitions.Enqueue(new KitchenWorkTransitionValue(identity, kind, (KitchenTransitionAction)message.Action));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(FrontLevelState))]
    public sealed class CustomerFlowTransitionHandler : ProductTransitionHandler<CustomerFlowTransition>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(CustomerFlowTransition message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, CustomerFlowTransition message, ProductTransitionIdentity identity)
        {
            FrontLevelState front = level.GetComponent<FrontLevelState>();
            Entity source = message.Action == (int)CustomerFlowAction.Arrive ? front
                : message.Action == (int)CustomerFlowAction.OrderExpired ? level.GetChild<Order>(identity.SourceId)
                : level.GetChild<Customer>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(CustomerFlowAction), message.Action))
            {
                front.Transitions.Enqueue(new CustomerFlowTransitionValue(identity, (CustomerFlowAction)message.Action));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(FrontLevelState))]
    public sealed class CustomerFlowFinishedHandler : ProductTransitionHandler<CustomerFlowFinished>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(CustomerFlowFinished message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, CustomerFlowFinished message, ProductTransitionIdentity identity)
        {
            Customer source = level.GetChild<Customer>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(CustomerPhase), message.Phase))
            {
                level.GetComponent<FrontLevelState>().FinishedTransitions.Enqueue(new CustomerFlowFinishedValue(identity, (CustomerPhase)message.Phase, message.TableStationId));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(FrontWorkCommands))]
    public sealed class FrontWorkTransitionHandler : ProductTransitionHandler<FrontWorkTransition>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(FrontWorkTransition message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, FrontWorkTransition message, ProductTransitionIdentity identity)
        {
            FrontWork source = level.GetChild<FrontWork>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(FrontWorkTransitionAction), message.Action))
            {
                level.GetComponent<FrontWorkCommands>().Transitions.Enqueue(new FrontWorkTransitionValue(identity, (FrontWorkTransitionAction)message.Action, message.ClaimSerial));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(FrontWorkCommands))]
    public sealed class FrontWorkFinishedHandler : ProductTransitionHandler<FrontWorkFinished>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(FrontWorkFinished message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, FrontWorkFinished message, ProductTransitionIdentity identity)
        {
            FrontWork source = level.GetChild<FrontWork>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(FrontWorkKind), message.Kind))
            {
                level.GetComponent<FrontWorkCommands>().FinishedTransitions.Enqueue(new FrontWorkFinishedValue(identity, (FrontWorkKind)message.Kind,
                    message.StationId, message.CustomerId, message.ItemId, message.ClaimSerial));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(KitchenSupply))]
    public sealed class ShipmentTransitionHandler : ProductTransitionHandler<ShipmentTransition>
    {
        protected override bool IsLevelScoped() => false;
        protected override ProductTransitionIdentity Read(ShipmentTransition message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, ShipmentTransition message, ProductTransitionIdentity identity)
        {
            KitchenSupply supply = kitchen.GetComponent<KitchenSupply>();
            SupplyShipment source = supply.GetChild<SupplyShipment>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && message.Action == (int)ShipmentTransitionAction.Ready)
            {
                supply.Transitions.Enqueue(new ShipmentTransitionValue(identity, (ShipmentTransitionAction)message.Action));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(RestaurantRuntime))]
    public sealed class LevelTransitionHandler : ProductTransitionHandler<LevelTransition>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(LevelTransition message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, LevelTransition message, ProductTransitionIdentity identity)
        {
            if (level.Id == identity.SourceId && level.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(LevelTransitionAction), message.Action))
            {
                restaurant.Transitions.Enqueue(new LevelTransitionValue(identity, (LevelTransitionAction)message.Action));
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(LevelBuff))]
    public sealed class BuffPrepareHandler : ProductTransitionHandler<BuffPrepare>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(BuffPrepare message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, BuffPrepare message, ProductTransitionIdentity identity)
        {
            LevelBuff buff = level.GetComponent<LevelBuff>();
            if (buff.Id == identity.SourceId && buff.InstanceId == identity.SourceInstanceId)
            {
                buff.Preparations.Enqueue(identity);
            }
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(RestaurantRenovation))]
    public sealed class RenovationPrepareHandler : ProductTransitionHandler<RenovationPrepare>
    {
        protected override bool IsLevelScoped() => true;
        protected override ProductTransitionIdentity Read(RenovationPrepare message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, RenovationPrepare message, ProductTransitionIdentity identity)
        {
            if (level.Id == identity.SourceId && level.InstanceId == identity.SourceInstanceId)
            {
                restaurant.GetComponent<RestaurantRenovation>().Preparations.Enqueue(identity);
            }
        }
    }
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(FrontWorkCommands))]
    public sealed class FrontWorkCreateHandler : ProductTransitionHandler<FrontWorkCreateRequest>
    {
        protected override ProductTransitionIdentity Read(FrontWorkCreateRequest message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, FrontWorkCreateRequest message, ProductTransitionIdentity identity)
        {
            Entity source = message.Kind == (int)FrontWorkKind.Wash ? level.GetChild<FrontWork>(identity.SourceId) : level.GetChild<Customer>(identity.SourceId);
            if (source?.InstanceId == identity.SourceInstanceId && Enum.IsDefined(typeof(FrontWorkKind), message.Kind))
                level.GetComponent<FrontWorkCommands>().Creations.Enqueue(new FrontWorkCreateValue(identity, (FrontWorkKind)message.Kind));
        }
    }
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(RestaurantRuntime))]
    public sealed class LevelFinishedHandler : ProductTransitionHandler<LevelFinished>
    {
        protected override ProductTransitionIdentity Read(LevelFinished message) => new(message.ProtocolVersion, message.MatchId, message.WorldId,
            message.WorldGeneration, message.LevelId, message.LevelGeneration, message.SourceId, message.SourceInstanceId, message.TransitionSerial);
        protected override void Enqueue(GridKitchenComponent kitchen, RestaurantRuntime restaurant, Level level, LevelFinished message, ProductTransitionIdentity identity)
        {
            if (level.Id == identity.SourceId && level.InstanceId == identity.SourceInstanceId) restaurant.FinishedTransitions.Enqueue(identity);
        }
    }
}
#endif
