#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class SoloSaveValidation
    {
        public const int FormatVersion = 4;
        public const int MaxEntries = 512;

        public static bool IsIdentifier(string value) => value != null && value.Length == 32 &&
            Guid.TryParseExact(value, "N", out _) && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

        public static void Header(SoloSaveHeader header, string profile)
        {
            Require(header != null && header.Version is 1 or 2 or 3 or FormatVersion, "UnsupportedVersion");
            Require(header.Platform == "PC", "WrongPlatform");
            Require(IsIdentifier(profile) && header.ProfileId == profile, "WrongProfile");
            Require(IsIdentifier(header.SlotId) && IsIdentifier(header.ResultId) && header.Revision == 1, "InvalidIdentity");
            Require(header.SourceSlotId == "" && header.SourceRevision == 0 ||
                IsIdentifier(header.SourceSlotId) && header.SourceSlotId != header.SlotId && header.SourceRevision == 1, "InvalidSource");
            Require(header.Score >= 0 && header.Stars is >= 0 and <= 3 && header.SavedUtcTicks > 0 &&
                header.SavedUtcTicks <= DateTime.MaxValue.Ticks, "InvalidResult");
            var completed = Tables.Instance.DTLevelConfig.GetOrDefault(header.CompletedLevelId);
            Require(completed != null && completed.NextLevelConfigId == header.NextLevelId, "InvalidNextLevel");
            Require(header.Version >= 4 || completed.MenuVariant == 0, "UnsupportedDrinkCatalog");
            if (header.Version == 1)
                Require(completed.ChapterId == 0 && header.RecordKind == SoloSaveRecordKind.Continue &&
                    Tables.Instance.DTLevelConfig.GetOrDefault(header.NextLevelId) != null, "InvalidLegacyNextLevel");
            else
            {
                Require(header.RecordKind is SoloSaveRecordKind.Continue or SoloSaveRecordKind.ChapterCompleted &&
                    header.ServedOrders >= 0 && header.UnfulfilledOrders >= 0, "InvalidChapterResult");
                Require(header.RecordKind == SoloSaveRecordKind.ChapterCompleted
                    ? completed.IsChapterTerminal && header.NextLevelId == 0 && header.ChapterId == completed.ChapterId && header.ChapterStage == 3
                    : !completed.IsChapterTerminal && Tables.Instance.DTLevelConfig.GetOrDefault(header.NextLevelId) != null,
                    "InvalidRecordKind");
            }
            Require(header.Stars == completed.StarThresholds.Count(threshold => header.Score >= threshold), "InvalidResultStars");
        }

        public static void Payload(SoloSaveHeader header, SoloSavePayload payload, string profile)
        {
            Header(header, profile);
            Require(payload != null, "MissingPayload");
            var stations = Index(payload.Stations, s => s.Key);
            var items = Index(payload.Items, i => i.Key);
            Index(payload.ManualWork, p => p.Key); Index(payload.Batches, p => p.Key); Index(payload.Baking, p => p.Key);
            var balances = Index(payload.Supply, s => s.SourceId);
            Index(payload.Shipments, s => s.Key);
            var renovations = Index(payload.Renovations, r => r.DefinitionId);
            var tables = Tables.Instance;
            var definitions = GridStationConfig.ValidateDefinitions(tables.DTStationConfig.DataList);
            int menuVariant = tables.DTLevelConfig.Get(header.CompletedLevelId).MenuVariant;
            var layouts = tables.DTInitialLayoutConfig.DataList.Where(row => row.MenuVariant == 0 || row.MenuVariant == menuVariant).ToDictionary(row => row.Id);
            var initialIds = new HashSet<int>();
            var required = tables.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY))
                .Concat(tables.DTCompanionConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY)))
                .Concat(tables.DTCustomerFlowConfig.DataList.Select(row => new GridCell(row.QueueX, row.QueueY))).ToList();
            foreach (var station in stations.Values)
            {
                Require(definitions.ContainsKey(station.DefinitionId), "UnknownStation");
                Require(station.InitialLayoutId == 0 || layouts.TryGetValue(station.InitialLayoutId, out var layout) &&
                    layout.StationDefinitionId == station.DefinitionId && initialIds.Add(station.InitialLayoutId), "InvalidInitialStation");
                var definition = definitions[station.DefinitionId];
                Require(station.UpgradeLevel == 0 && station.UpgradeDefinitionId == 0 || header.Version >= 3 && station.UpgradeLevel == 1 &&
                    StationUpgradeConfig.Matches(tables.DTStationUpgradeConfig.GetOrDefault(station.UpgradeDefinitionId), definition.Kind, (GridStationCapability)definition.Capabilities), "InvalidSavedUpgrade");
                Require(station.PoolItemKeys != null && station.PoolItemKeys.Length <= definition.ItemSlotCapacity &&
                    station.PoolItemKeys.Distinct().Count() == station.PoolItemKeys.Length, "PoolCapacity");
                bool pool = definition.Kind is (int)GridStationKind.DirtyPool or (int)GridStationKind.CleanPool;
                Require(pool ? station.ItemKey == 0 : station.PoolItemKeys.Length == 0, "InvalidStationSlot");
                if (station.ItemKey != 0)
                    Require(items.TryGetValue(station.ItemKey, out var item) && item.HolderKey == station.Key &&
                        item.Location == (int)(definition.Kind == (int)GridStationKind.Table ? GridItemLocation.Table : GridItemLocation.Counter), "StationItemReference");
                foreach (int key in station.PoolItemKeys)
                    Require(items.TryGetValue(key, out var item) && item.HolderKey == station.Key && item.ContentKeys != null && item.ContentKeys.Length == 0 &&
                        item.Location == (int)(definition.Kind == (int)GridStationKind.CleanPool ? GridItemLocation.CleanPool : GridItemLocation.DirtyPool) &&
                        item.Hygiene == (definition.Kind == (int)GridStationKind.CleanPool ? (int)ItemHygiene.Clean : (int)ItemHygiene.Dirty), "PoolItemReference");
                if (definition.Kind == (int)GridStationKind.Table)
                {
                    var table = tables.DTTableConfig.GetOrDefault(station.TableDefinitionId);
                    Require(table != null && table.InitialLayoutId == station.InitialLayoutId, "MissingFrontTable");
                    required.Add(GridStationGeometry.Cells(new[] { table.SeatOffsetX, table.SeatOffsetY }, new GridCell(station.X, station.Y), station.Orientation)[0]);
                    Require(!station.TableDirty || station.ItemKey != 0 && items[station.ItemKey].Hygiene == (int)ItemHygiene.Dirty, "InvalidTableHygiene");
                }
                else Require(station.TableDefinitionId == 0 && !station.TableDirty, "UnexpectedFrontTable");
            }
            Require(initialIds.SetEquals(layouts.Keys), "MissingInitialStation");
            var installedKeys = new HashSet<int>();
            var upgradedKeys = new HashSet<int>();
            foreach (var renovation in renovations.Values)
            {
                var definition = tables.DTRenovationConfig.GetOrDefault(renovation.DefinitionId);
                if (definition?.RewardKind == 2)
                {
                    Require(header.Version >= 3 && renovation.StationKeys != null && renovation.StationKeys.Length > 0, "InvalidUpgradeRenovation");
                    foreach (int key in renovation.StationKeys)
                        Require(stations.TryGetValue(key, out var upgraded) && upgraded.UpgradeLevel == 1 &&
                            upgraded.UpgradeDefinitionId == definition.UpgradeDefinitionId && upgradedKeys.Add(key), "UpgradeStationReference");
                    continue;
                }
                Require(definition != null && renovation.StationKeys != null && renovation.StationKeys.Length == definition.Quantity, "InvalidRenovation");
                foreach (int key in renovation.StationKeys)
                    Require(stations.TryGetValue(key, out var station) && station.InitialLayoutId == 0 && station.DefinitionId == definition.StationDefinitionId &&
                        installedKeys.Add(key), "RenovationStationReference");
            }
            Require(stations.Values.Where(s => s.InitialLayoutId == 0).All(s => installedKeys.Contains(s.Key)), "UnownedRenovationStation");
            Require(stations.Values.Where(s => s.UpgradeLevel != 0).All(s => upgradedKeys.Contains(s.Key)), "UnownedStationUpgrade");
            var pending = Array.Empty<GridStationPlacement>();
            if (payload.PendingRenovationId != 0)
            {
                var definition = tables.DTRenovationConfig.GetOrDefault(payload.PendingRenovationId);
                Require(definition != null && (definition.RewardKind == 2 || !renovations.ContainsKey(definition.Id)) &&
                    tables.DTLevelConfig.Get(header.CompletedLevelId).AllowedRenovationIds.Contains(definition.Id), "InvalidPendingRenovation");
                if (definition.RewardKind == 2)
                {
                    Require(header.Version >= 3 && payload.PendingUpgradeExpectedLevel == 0 &&
                        stations.TryGetValue(payload.PendingUpgradeStationKey, out var target) && target.UpgradeLevel == 0 && target.UpgradeDefinitionId == 0 &&
                        StationUpgradeConfig.Matches(tables.DTStationUpgradeConfig.GetOrDefault(definition.UpgradeDefinitionId), definitions[target.DefinitionId].Kind,
                            (GridStationCapability)definitions[target.DefinitionId].Capabilities), "InvalidPendingUpgrade");
                }
                else
                {
                    Require(payload.PendingUpgradeStationKey == 0 && payload.PendingUpgradeExpectedLevel == 0, "UnexpectedUpgradeTarget");
                    pending = RenovationConfig.Placements(definition).ToArray();
                }
            }
            else Require(payload.PendingUpgradeStationKey == 0 && payload.PendingUpgradeExpectedLevel == 0, "UnexpectedUpgradeTarget");
            var placements = stations.Values.Select(s => new GridStationPlacement(s.DefinitionId, new GridCell(s.X, s.Y), s.Orientation));
            var blocked = GridStationConfig.ValidateTerrain(definitions, placements.Concat(pending), required);
            foreach (var item in items.Values)
            {
                var definition = tables.DTGridItemConfig.GetOrDefault(item.DefinitionId);
                Require(definition != null && (definition.MenuVariant == 0 || definition.MenuVariant == menuVariant) && item.Hygiene is (int)ItemHygiene.Clean or (int)ItemHygiene.Dirty &&
                    GridStationGeometry.Inside(new GridCell(item.X, item.Y)), "InvalidItem");
                Require(item.ContentKeys != null && item.ContentKeys.Length <= definition.ContainerCapacity &&
                    item.ContentKeys.Distinct().Count() == item.ContentKeys.Length && (definition.Kind == 2 || item.ContentKeys.Length == 0), "ContainerCapacity");
                foreach (int key in item.ContentKeys)
                    Require(items.TryGetValue(key, out var content) && content.Location == (int)GridItemLocation.Container && content.HolderKey == item.Key &&
                        definition.AcceptedDefinitionIds.Contains(content.DefinitionId) && (tables.DTGridItemConfig.Get(content.DefinitionId).Kind == 1 ||
                            definition.AllowsEmptyContainers && tables.DTGridItemConfig.Get(content.DefinitionId).Disposable && content.ContentKeys.Length == 0 && content.Hygiene == (int)ItemHygiene.Clean), "ContentReference");
                switch ((GridItemLocation)item.Location)
                {
                    case GridItemLocation.Ground:
                        // Ground drops share a cell in the existing gameplay; uniqueness is by item key.
                        Require(item.HolderKey == 0 && !blocked.Contains(new GridCell(item.X, item.Y)), "InvalidGroundItem");
                        break;
                    case GridItemLocation.Container:
                        Require(items.TryGetValue(item.HolderKey, out var container) && container.ContentKeys.Contains(item.Key), "ContainerHolderReference"); break;
                    case GridItemLocation.Counter:
                    case GridItemLocation.Table:
                        Require(stations.TryGetValue(item.HolderKey, out var station) && station.ItemKey == item.Key, "StationHolderReference"); break;
                    case GridItemLocation.DirtyPool:
                    case GridItemLocation.CleanPool:
                        Require(stations.TryGetValue(item.HolderKey, out var pool) && pool.PoolItemKeys.Contains(item.Key), "PoolHolderReference"); break;
                    default: throw new InvalidOperationException("InvalidPersistentLocation");
                }
            }
            // A work input cannot be claimed by two persistent processes.
            var workItems = new HashSet<int>();
            foreach (var process in payload.ManualWork)
            {
                var recipe = tables.DTGridManualRecipeConfig.GetOrDefault(process.RecipeId);
                var input = items.GetValueOrDefault(process.InputKey);
                Require(recipe != null && InProgress(process.RequiredTicks, process.ProgressTicks) &&
                    input != null && input.DefinitionId == recipe.InputDefinitionId &&
                    items.ContainsKey(process.TargetKey) && workItems.Add(process.InputKey), "InvalidManualWork");
                var target = items[process.TargetKey];
                Require(recipe.Mode == 1 ? process.InputKey == process.TargetKey && stations.TryGetValue(process.StationKey, out var station) &&
                    (definitions[station.DefinitionId].Capabilities & (int)GridStationCapability.Chop) != 0 :
                    process.StationKey == 0 && target.DefinitionId == GridKitchenRules.Bowl && target.ContentKeys.SequenceEqual(new[] { process.InputKey }), "ManualTargetReference");
                Require(input.Hygiene == (int)ItemHygiene.Clean && target.Hygiene == (int)ItemHygiene.Clean, "DirtyWorkInput");
            }
            var workStations = new HashSet<int>();
            foreach (var process in payload.Batches)
            {
                var recipe = tables.DTGridAutoRecipeConfig.GetOrDefault(process.RecipeId);
                var pot = items.GetValueOrDefault(process.PotKey);
                Require(recipe != null && pot != null && pot.DefinitionId == recipe.ContainerDefinitionId &&
                    stations.ContainsKey(process.StationKey) && workItems.Add(process.PotKey), "InvalidBatchReference");
                int capability = header.Version < 4 ? (int)GridStationCapability.Cook : process.RequiredCapability;
                Require(capability == recipe.StationCapability && (header.Version < 4 ? !recipe.LockContainer && !process.LockContainer &&
                    process.RequiredCapability is 0 or (int)GridStationCapability.Cook : process.LockContainer == recipe.LockContainer), "InvalidBatchLock");
                Require(process.InputKeys != null && process.InputKeys.SequenceEqual(pot.ContentKeys) &&
                    process.OutputDefinitionId == recipe.OutputDefinitionId && process.TotalPortions == recipe.OutputPortions &&
                    process.ServingContainerDefinitionId == recipe.ServingContainerDefinitionId && process.RequiredTicks > 0, "InvalidBatchParameters");
                var counts = process.InputKeys.Select(key => items[key].DefinitionId).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                Require(counts.Count == recipe.InputDefinitionIds.Count && recipe.InputDefinitionIds.Select((id, index) =>
                    counts.GetValueOrDefault(id) == recipe.InputCounts[index]).All(value => value), "BatchInputRecipe");
                if (process.State == (int)GridAutoState.Cooking)
                    Require(InProgress(process.RequiredTicks, process.ProgressTicks) && process.RemainingPortions == 0 && workStations.Add(process.StationKey) &&
                        (definitions[stations[process.StationKey].DefinitionId].Capabilities & capability) != 0 &&
                        (!process.LockContainer || pot.Location == (int)GridItemLocation.Counter && pot.HolderKey == process.StationKey && stations[process.StationKey].ItemKey == pot.Key), "InvalidCookingState");
                else Require(process.State == (int)GridAutoState.Ready && process.ProgressTicks == process.RequiredTicks &&
                    process.RemainingPortions > 0 && process.RemainingPortions <= process.TotalPortions, "InvalidBatchPortions");
            }
            foreach (var process in payload.Baking)
            {
                var recipe = tables.DTGridBakeRecipeConfig.GetOrDefault(process.RecipeId);
                Require(recipe != null && InProgress(process.RequiredTicks, process.ProgressTicks) && process.OutputDefinitionId == recipe.OutputDefinitionId &&
                    items.TryGetValue(process.InputKey, out var input) && input.DefinitionId == recipe.InputDefinitionId &&
                    stations.TryGetValue(process.StationKey, out var station) && station.ItemKey == input.Key &&
                    input.Location == (int)GridItemLocation.Counter && input.HolderKey == station.Key &&
                    (definitions[station.DefinitionId].Capabilities & (int)GridStationCapability.Bake) != 0 && workItems.Add(input.Key) && workStations.Add(station.Key), "InvalidBakeWork");
            }
            var sources = tables.DTSupplyConfig.DataList.Where(row => row.MenuVariant == 0 || row.MenuVariant == menuVariant).ToDictionary(row => row.Id);
            Require(balances.Keys.ToHashSet().SetEquals(sources.Keys), "MissingSupplyBalance");
            foreach (var balance in balances.Values)
                Require(balance.RemainingPackages >= 0 && balance.RemainingPackages <= sources[balance.SourceId].AvailablePackages &&
                    payload.Shipments.Count(s => s.SourceId == balance.SourceId) == sources[balance.SourceId].AvailablePackages - balance.RemainingPackages, "SupplyConservation");
            var receivedPackages = new HashSet<int>();
            foreach (var shipment in payload.Shipments)
            {
                Require(sources.TryGetValue(shipment.SourceId, out var source) && shipment.MaterialDefinitionId == source.MaterialDefinitionId &&
                    shipment.PackageDefinitionId == source.PackageDefinitionId && shipment.PackageUnits == source.PackageUnits && shipment.RequiredTicks > 0 &&
                    shipment.ProgressTicks >= 0 && shipment.ProgressTicks <= shipment.RequiredTicks && stations.TryGetValue(shipment.ReceivingStationKey, out var receiving) &&
                    receiving.InitialLayoutId == source.ReceivingLayoutId, "InvalidShipment");
                if (shipment.Stage == (int)ShipmentStage.Received)
                    Require(shipment.ProgressTicks == shipment.RequiredTicks && (shipment.PackageKey == 0 || items.TryGetValue(shipment.PackageKey, out var package) &&
                        package.DefinitionId == shipment.PackageDefinitionId && receivedPackages.Add(package.Key)), "InvalidReceivedShipment");
                else Require(shipment.PackageKey == 0 && (shipment.Stage == (int)ShipmentStage.InTransit && shipment.ProgressTicks < shipment.RequiredTicks ||
                    shipment.Stage == (int)ShipmentStage.Ready && shipment.ProgressTicks == shipment.RequiredTicks), "InvalidTransitShipment");
            }
        }

        private static bool InProgress(int required, int progress) => required > 0 && progress >= 0 && progress < required;
        private static Dictionary<int, T> Index<T>(T[] values, Func<T, int> key) where T : class
        {
            Require(values != null && values.Length <= MaxEntries, "MissingOrOversizedCollection");
            var result = new Dictionary<int, T>();
            foreach (T value in values) Require(value != null && key(value) > 0 && result.TryAdd(key(value), value), "DuplicateOrInvalidKey");
            return result;
        }

        private static void Require(bool valid, string error)
        {
            if (!valid) throw new InvalidOperationException(error);
        }
    }
}
#endif
