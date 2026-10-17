#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(Customer))]
    public static partial class GridKitchenSystem
    {
        public static void ReleaseDrinkOrder(this GridKitchenComponent kitchen, Level level, long orderId)
        {
            foreach (long cup in level.DrinkBindings.Where(pair => pair.Value == orderId).Select(pair => pair.Key).ToArray())
            {
                level.DrinkBindings.Remove(cup);
            }
            kitchen.Dirty = true;
        }

        private static string BindDrink(this GridKitchenComponent kitchen, GridActor actor, GridStation station, GridIntent intent)
        {
            Level level = kitchen.ProductLevel();
            if (!ProductMenuCatalog.SupportsDrinks(level?.MenuVariant ?? ProductMenuVariant.Basic)) return "DrinkMenuRequired";
            if ((station.Capabilities & GridStationCapability.LabelDrink) == 0) return "RequiresLabelDesk";
            if (actor.HandItemId != 0) return "HandFull";
            GridItem cup = kitchen.Item(station.ItemId);
            if (cup == null || cup.Id != intent.ExpectedItemId || cup.Id != intent.ExpectedTargetItemId ||
                cup.Location != GridItemLocation.Counter || cup.HolderId != station.Id || cup.Hygiene != ItemHygiene.Clean)
                return "ItemChanged";
            int dishId = kitchen.IdentifyDish(cup.Id);
            DRDishConfig dish = Tables.Instance.DTDishConfig.GetOrDefault(dishId);
            if (dish == null || !dish.RequiresOrderBinding) return "RequiresFinishedDrink";
            if (intent.Action == GridAction.UnbindDrink)
            {
                if (!level.DrinkBindings.TryGetValue(cup.Id, out long oldOrder) || oldOrder != intent.ExpectedOrderId) return "BindingChanged";
                level.DrinkBindings.Remove(cup.Id);
                return null;
            }
            Order order = level.GetChild<Order>(intent.ExpectedOrderId);
            Customer customer = order == null ? null : level.GetChild<Customer>(order.CustomerId);
            if (order == null || order.LevelGeneration != level.Generation || order.State != OrderState.Waiting ||
                kitchen.Tick >= order.DeadlineTick || customer?.Phase != CustomerPhase.Ordered || customer.OrderId != order.Id)
                return "OrderEnded";
            if (order.DishId != dishId || !level.AllowedDishIds.Contains(dishId)) return "WrongDish";
            if (level.DrinkBindings.Any(pair => pair.Key != cup.Id && pair.Value == order.Id)) return "OrderAlreadyBound";
            // 所有新目标检查完成后，单次替换旧关系。
            level.DrinkBindings[cup.Id] = order.Id;
            return null;
        }
    }
}
#endif
