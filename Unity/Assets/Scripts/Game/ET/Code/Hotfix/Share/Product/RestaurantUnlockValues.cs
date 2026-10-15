#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    public static class RestaurantUnlockValues
    {
        public static SavedRestaurantUnlocks Initial(DRLevelConfig first)
        {
            return new SavedRestaurantUnlocks
            {
                DishIds = (first.UseInitialUnlockSet ? first.InitialUnlockedDishIds : first.AllowedDishIds.Concat(first.AllowedDrinkIds))
                    .Distinct().OrderBy(id => id).ToArray(),
                StationIds = (first.UseInitialUnlockSet ? first.InitialUnlockedStationIds : Tables.Instance.DTInitialLayoutConfig.DataList
                    .Where(row => ProductMenuCatalog.Includes(first.MenuVariant, row.MenuVariant)).Select(row => row.StationDefinitionId))
                    .Distinct().OrderBy(id => id).ToArray()
            };
        }

        public static SavedRestaurantUnlocks Legacy(SoloSaveHeader header, SoloSavePayload payload)
        {
            var level = Tables.Instance.DTLevelConfig.Get(header.CompletedLevelId);
            return new SavedRestaurantUnlocks
            {
                DishIds = level.AllowedDishIds.Concat(level.AllowedDrinkIds).Distinct().OrderBy(id => id).ToArray(),
                StationIds = payload.Stations.Select(row => row.DefinitionId).Distinct().OrderBy(id => id).ToArray()
            };
        }

        public static void AdaptLegacy(SoloSaveHeader header, SoloSavePayload payload)
        {
            if (header.Version is >= 1 and <= 7 && payload?.Unlocks == null)
            {
                payload.Unlocks = Legacy(header, payload);
            }
        }

        public static void Validate(SavedRestaurantUnlocks value)
        {
            if (value?.DishIds == null || value.StationIds == null || value.DishIds.Length == 0 ||
                value.DishIds.Length > 512 || value.StationIds.Length > 512 ||
                value.DishIds.Distinct().Count() != value.DishIds.Length || value.StationIds.Distinct().Count() != value.StationIds.Length ||
                value.DishIds.Any(id => Tables.Instance.DTDishConfig.GetOrDefault(id) == null) ||
                value.StationIds.Any(id => Tables.Instance.DTStationConfig.GetOrDefault(id) == null))
            {
                throw new InvalidOperationException("InvalidRestaurantUnlocks");
            }
        }
    }
}
#endif
