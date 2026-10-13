#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        public static void ValidateCleanupDefinitions(IEnumerable<DRGridItemConfig> itemRows, IEnumerable<DRStationConfig> stationRows)
        {
            var items = itemRows.ToDictionary(row => row.Id);
            var stations = GridStationConfig.ValidateDefinitions(stationRows);
            foreach (var item in items.Values)
                if (item.Kind == 1 ? item.Form is not ((int)GridMaterialForm.Solid or (int)GridMaterialForm.Liquid)
                    : item.Kind != 2 || item.Form != (int)GridMaterialForm.Container)
                    throw new InvalidOperationException($"Invalid material form {item.Id}");
            if (!items.TryGetValue(GridKitchenRules.Water, out var water) || water.Kind != 1 || water.Form != (int)GridMaterialForm.Liquid ||
                !items.TryGetValue(GridKitchenRules.Bowl, out var bowl) || bowl.Kind != 2 || !bowl.AcceptedDefinitionIds.Contains(water.Id))
                throw new InvalidOperationException("Missing water / compatible bowl definitions");
            foreach (var id in new[] { GridKitchenRules.Pot, GridKitchenRules.Bowl, GridKitchenRules.Plate })
                if (!items.TryGetValue(id, out var vessel) || vessel.Kind != 2 || vessel.Discardable)
                    throw new InvalidOperationException("Reusable cookware cannot be discarded");
            foreach (var station in stations.Values.Where(row => (row.Capabilities & (int)GridStationCapability.FillWater) != 0))
                if (station.WaterUnits != 1 || !items.TryGetValue(station.WaterDefinitionId, out var supplied) ||
                    supplied.Kind != 1 || supplied.Form != (int)GridMaterialForm.Liquid || supplied.Id != water.Id)
                    throw new InvalidOperationException($"Invalid sink water definition {station.Id}");
            if (!stations.Values.Any(row => (row.Capabilities & (int)GridStationCapability.FillWater) != 0) ||
                !stations.Values.Any(row => row.Kind == (int)GridStationKind.Trash))
                throw new InvalidOperationException("Missing sink / trash facilities");
        }
    }
}
#endif
