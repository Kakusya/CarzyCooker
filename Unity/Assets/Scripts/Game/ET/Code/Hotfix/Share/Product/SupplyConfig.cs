#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class SupplyConfig
    {
        public static IEnumerable<int> ProducedDefinitions() => Tables.Instance.DTGridManualRecipeConfig.DataList.Select(row => row.OutputDefinitionId)
            .Concat(Tables.Instance.DTGridAutoRecipeConfig.DataList.Select(row => row.OutputDefinitionId))
            .Concat(Tables.Instance.DTGridBakeRecipeConfig.DataList.Select(row => row.OutputDefinitionId));

        public static Dictionary<int, DRSupplyConfig> Validate(IEnumerable<DRSupplyConfig> sourceRows,
            IEnumerable<DRGridItemConfig> itemRows, IEnumerable<DRStationConfig> stationRows,
            IEnumerable<DRInitialLayoutConfig> layoutRows, IEnumerable<int> producedDefinitions)
        {
            var items = itemRows.ToDictionary(row => row.Id); var stations = GridStationConfig.ValidateDefinitions(stationRows);
            var layouts = layoutRows.ToDictionary(row => row.Id); var produced = producedDefinitions.ToHashSet();
            var sources = new Dictionary<int, DRSupplyConfig>(); var raw = new HashSet<int>();
            bool Coexist(ProductMenuVariant one, ProductMenuVariant two) => Tables.Instance.DTLevelConfig.DataList.Any(level =>
                ProductMenuCatalog.Includes(level.MenuVariant, one) && ProductMenuCatalog.Includes(level.MenuVariant, two));
            foreach (var row in sourceRows)
            {
                bool sharedTea = row.MenuVariant == ProductMenuVariant.Tea &&
                    (row.MaterialDefinitionId == 44 && row.PackageDefinitionId == 51 ||
                     row.MaterialDefinitionId == GridKitchenRules.HotDrinkCup && row.PackageDefinitionId == 63);
                if (row.Id <= 0 || !sources.TryAdd(row.Id, row) || string.IsNullOrWhiteSpace(row.Name) || row.PackageUnits <= 0 ||
                    row.AvailablePackages <= 0 || row.TransitTicks <= 0 || (long)row.PackageUnits * row.AvailablePackages > int.MaxValue ||
                    sources.Values.Any(other => other.Id != row.Id &&
                        (other.MaterialDefinitionId == row.MaterialDefinitionId || other.PackageDefinitionId == row.PackageDefinitionId) && Coexist(other.MenuVariant, row.MenuVariant)) ||
                    !ProductMenuCatalog.IsValid(row.MenuVariant) || !items.TryGetValue(row.MaterialDefinitionId, out var material) ||
                    !(material.Kind == GridItemKind.Material && (material.Form == GridMaterialForm.Solid ||
                        row.MenuVariant == ProductMenuVariant.Tea && material.Id == GridKitchenRules.Syrup && material.Form == GridMaterialForm.Liquid) ||
                        material.Kind == GridItemKind.Container && material.Disposable && material.Form == GridMaterialForm.Container) ||
                    produced.Contains(material.Id) || !items.TryGetValue(row.PackageDefinitionId, out var package) ||
                    package.Kind != GridItemKind.Container || package.Form != GridMaterialForm.Container || !package.Discardable ||
                    package.ContainerCapacity < row.PackageUnits || !package.AcceptedDefinitionIds.SequenceEqual(new[] { material.Id }) ||
                    !sharedTea && (package.MenuVariant != row.MenuVariant || material.MenuVariant != row.MenuVariant) || material.Kind == GridItemKind.Container && !package.AllowsEmptyContainers ||
                    !layouts.TryGetValue(row.ReceivingLayoutId, out var receiving) ||
                    !stations.TryGetValue(receiving.StationDefinitionId, out var station) || station.Kind != GridStationKind.Counter || station.ItemSlotCapacity != 1)
                    throw new InvalidOperationException($"Invalid supply / package / receiving reference {row.Id}");
                raw.Add(row.MaterialDefinitionId);
            }
            foreach (int id in new[] { GridKitchenRules.Tomato, GridKitchenRules.Egg, GridKitchenRules.Bread })
                if (!raw.Contains(id)) throw new InvalidOperationException($"Missing finite raw supply {id}");
            return sources;
        }
    }
}
#endif
