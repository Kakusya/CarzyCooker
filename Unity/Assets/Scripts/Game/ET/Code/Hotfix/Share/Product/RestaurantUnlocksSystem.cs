#if !DOTNET
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(RestaurantUnlocks))]
    [FriendOf(typeof(RestaurantUnlocks))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class RestaurantUnlocksSystem
    {
        [EntitySystem]
        private static void Awake(this RestaurantUnlocks self, DRLevelConfig first)
        {
            self.PermissionVersion = 0; self.DishIds.Clear(); self.StationIds.Clear();
            self.RestoreRestaurantUnlocks(RestaurantUnlockValues.Initial(first));
        }

        public static void RestoreRestaurantUnlocks(this RestaurantUnlocks self, SavedRestaurantUnlocks value)
        {
            RestaurantUnlockValues.Validate(value);
            self.DishIds.Clear(); self.DishIds.UnionWith(value.DishIds);
            self.StationIds.Clear(); self.StationIds.UnionWith(value.StationIds);
        }

        public static SavedRestaurantUnlocks CaptureRestaurantUnlocks(this RestaurantUnlocks self) => new()
        {
            DishIds = self.DishIds.OrderBy(id => id).ToArray(),
            StationIds = self.StationIds.OrderBy(id => id).ToArray()
        };

        public static bool GrantRestaurantUnlocks(this RestaurantUnlocks self, DRRenovationConfig reward)
        {
            bool changed = false;
            foreach (int id in reward.UnlockDishIds)
            {
                changed |= self.DishIds.Add(id);
            }
            foreach (int id in reward.UnlockStationIds)
            {
                changed |= self.StationIds.Add(id);
            }
            return changed;
        }

        public static void RefreshProductionPermissions(this RestaurantRuntime self)
        {
            Level level = self.CurrentLevel;
            if (level == null)
            {
                return;
            }
            var unlocked = self.GetComponent<RestaurantUnlocks>();
            var definition = Tables.Instance.DTLevelConfig.Get(level.ConfigId);
            ProductionPermissionRules.Rebuild(level.ProductionPermit,
                definition.AllowedDishIds.Concat(definition.AllowedDrinkIds).Where(unlocked.DishIds.Contains), unlocked.StationIds);
            level.AllowedDishIds = level.ProductionPermit.DishIds.OrderBy(id => id).ToArray();
            ++unlocked.PermissionVersion;
            GridKitchenComponent kitchen = self.Kitchen;
            if (kitchen != null)
            {
                kitchen.Dirty = true;
            }
        }
    }
}
#endif
