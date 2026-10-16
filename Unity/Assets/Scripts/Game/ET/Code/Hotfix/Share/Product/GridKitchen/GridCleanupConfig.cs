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
                if (item.Kind == GridItemKind.Material ? item.Form is not (GridMaterialForm.Solid or GridMaterialForm.Liquid)
                    : item.Kind != GridItemKind.Container || item.Form != GridMaterialForm.Container)
                    throw new InvalidOperationException($"Invalid material form {item.Id}");
            if (!items.TryGetValue(GridKitchenRules.Water, out var water) || water.Kind != GridItemKind.Material || water.Form != GridMaterialForm.Liquid ||
                !items.TryGetValue(GridKitchenRules.Bowl, out var bowl) || bowl.Kind != GridItemKind.Container || !bowl.AcceptedDefinitionIds.Contains(water.Id))
                throw new InvalidOperationException("Missing water / compatible bowl definitions");
            foreach (var id in new[] { GridKitchenRules.Pot, GridKitchenRules.Bowl, GridKitchenRules.Plate })
                if (!items.TryGetValue(id, out var vessel) || vessel.Kind != GridItemKind.Container || vessel.Discardable)
                    throw new InvalidOperationException("Reusable cookware cannot be discarded");
            foreach (var station in stations.Values.Where(row => (row.Capabilities & GridStationCapability.FillWater) != 0))
                if (station.WaterUnits != 1 || !items.TryGetValue(station.WaterDefinitionId, out var supplied) ||
                    supplied.Kind != GridItemKind.Material || supplied.Form != GridMaterialForm.Liquid || supplied.Id != water.Id)
                    throw new InvalidOperationException($"Invalid sink water definition {station.Id}");
            if (!stations.Values.Any(row => (row.Capabilities & GridStationCapability.FillWater) != 0) ||
                !stations.Values.Any(row => row.Kind == GridStationKind.Trash))
                throw new InvalidOperationException("Missing sink / trash facilities");
        }
    }
}
#endif
