#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    [FriendOf(typeof(RestaurantRenovation))]
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(SupplyShipment))]
    [FriendOf(typeof(FrontTable))]
    public static partial class GridKitchenSystem
    {
        private static bool RestoreKitchenBaseline(this GridKitchenComponent self)
        {
            Match match = self.Match;
            SelectedSaveBaseline baseline = match.GetComponent<SoloSaveBinding>()?.Baseline ?? default;
            if (baseline == null) return false;
            SoloSavePayload payload = baseline.Payload;
            // All values were validated before ProductHost was created. No initial item path runs here.
            var stations = new Dictionary<int, GridStation>(); var items = new Dictionary<int, GridItem>();
            foreach (var saved in payload.Stations)
            {
                var station = self.RestoreSaveStation(saved.DefinitionId, saved.InitialLayoutId, new GridCell(saved.X, saved.Y), saved.Orientation);
                stations.Add(saved.Key, station);
                station.UpgradeDefinitionId = saved.UpgradeDefinitionId; station.UpgradeLevel = saved.UpgradeLevel;
                if (saved.TableDefinitionId != 0)
                {
                    var table = station.AddComponent<FrontTable, DRTableConfig>(Tables.Instance.DTTableConfig.Get(saved.TableDefinitionId));
                    table.State = saved.TableDirty ? FrontTableState.Dirty : FrontTableState.Free; table.CustomerId = 0;
                }
            }
            foreach (var saved in payload.Items)
                items.Add(saved.Key, self.CreateGridItem(saved.DefinitionId, (GridItemLocation)saved.Location, 0, new GridCell(saved.X, saved.Y)));
            foreach (var saved in payload.Items)
            {
                var item = items[saved.Key]; item.Hygiene = (ItemHygiene)saved.Hygiene;
                item.HolderId = saved.HolderKey == 0 ? 0 : item.Location == GridItemLocation.Container ? items[saved.HolderKey].Id : stations[saved.HolderKey].Id;
                item.Contents.AddRange(saved.ContentKeys.Select(key => items[key].Id));
            }
            foreach (var saved in payload.Stations)
            {
                var station = stations[saved.Key]; station.ItemId = saved.ItemKey == 0 ? 0 : items[saved.ItemKey].Id;
                station.PoolItemIds.AddRange(saved.PoolItemKeys.Select(key => items[key].Id));
            }
            foreach (var saved in payload.ManualWork)
            {
                var process = self.AddChild<GridManualProcess>();
                process.RecipeId = saved.RecipeId; process.RequiredTicks = saved.RequiredTicks; process.ProgressTicks = saved.ProgressTicks;
                process.InputItemId = items[saved.InputKey].Id; process.TargetItemId = items[saved.TargetKey].Id;
                process.StationId = saved.StationKey == 0 ? 0 : stations[saved.StationKey].Id;
                process.State = GridWorkState.Paused; process.StartedTick = -1;
                self.Processes.Add(process.Id, process);
            }
            foreach (var saved in payload.Batches)
            {
                var process = self.AddChild<GridAutoProcess>(); var pot = items[saved.PotKey];
                process.RecipeId = saved.RecipeId; process.RequiredTicks = saved.RequiredTicks; process.ProgressTicks = saved.ProgressTicks;
                process.PotItemId = pot.Id; process.StartStationId = stations[saved.StationKey].Id; process.State = (GridAutoState)saved.State;
                process.OutputDefinitionId = saved.OutputDefinitionId; process.TotalPortions = saved.TotalPortions; process.RemainingPortions = saved.RemainingPortions;
                process.ServingContainerDefinitionId = saved.ServingContainerDefinitionId; process.InputItemIds.AddRange(saved.InputKeys.Select(key => items[key].Id));
                process.RequiredCapability = saved.RequiredCapability == 0 ? (int)GridStationCapability.Cook : saved.RequiredCapability;
                process.LockContainer = saved.LockContainer;
                process.StartedTick = -1; pot.AutoProcessId = process.Id; self.AutoProcesses.Add(process.Id, process);
                if (process.State == GridAutoState.Cooking) stations[saved.StationKey].AutoProcessId = process.Id;
            }
            foreach (var saved in payload.Baking)
            {
                var process = self.AddChild<GridBakeProcess>(); var input = items[saved.InputKey]; var station = stations[saved.StationKey];
                process.RecipeId = saved.RecipeId; process.RequiredTicks = saved.RequiredTicks; process.ProgressTicks = saved.ProgressTicks;
                process.StationId = station.Id; process.InputItemId = input.Id; process.OutputDefinitionId = saved.OutputDefinitionId;
                process.State = GridBakeState.Baking; process.StartedTick = -1; input.BakeProcessId = process.Id; station.BakeProcessId = process.Id;
                self.BakeProcesses.Add(process.Id, process);
            }
            var supply = self.AddComponent<KitchenSupply>(); supply.RemainingPackages.Clear();
            foreach (var saved in payload.Supply) supply.RemainingPackages.Add(saved.SourceId, saved.RemainingPackages);
            foreach (var saved in payload.Shipments)
            {
                var shipment = supply.AddChild<SupplyShipment>(); shipment.SourceId = saved.SourceId;
                shipment.MaterialDefinitionId = saved.MaterialDefinitionId; shipment.PackageDefinitionId = saved.PackageDefinitionId; shipment.PackageUnits = saved.PackageUnits;
                shipment.RequiredTicks = saved.RequiredTicks; shipment.ProgressTicks = saved.ProgressTicks; shipment.StartedTick = -1;
                shipment.ReceivingStationId = stations[saved.ReceivingStationKey].Id; shipment.PackageItemId = saved.PackageKey == 0 ? 0 : items[saved.PackageKey].Id;
                shipment.Stage = (ShipmentStage)saved.Stage;
            }
            RestaurantRuntime restaurant = self.Restaurant; var renovation = restaurant.GetComponent<RestaurantRenovation>();
            foreach (var saved in payload.Renovations)
                renovation.AppliedStations.Add(saved.DefinitionId, Array.AsReadOnly(saved.StationKeys.Select(key => stations[key].Id).ToArray()));
            if (payload.PendingRenovationId != 0)
            {
                var definition = Tables.Instance.DTRenovationConfig.Get(payload.PendingRenovationId);
                if (definition.RewardKind == 2)
                {
                    var target = stations[payload.PendingUpgradeStationKey]; var upgrade = Tables.Instance.DTStationUpgradeConfig.Get(definition.UpgradeDefinitionId);
                    target.UpgradeDefinitionId = upgrade.Id; target.UpgradeLevel = upgrade.TargetLevel;
                    renovation.AppliedStations.TryGetValue(definition.Id, out var previous);
                    renovation.AppliedStations[definition.Id] = Array.AsReadOnly((previous ?? Array.Empty<long>()).Append(target.Id).Distinct().ToArray());
                }
                else
                {
                    var ids = RenovationConfig.Placements(definition).Select(p => self.RestoreSaveStation(p.DefinitionId, 0, p.Cell, p.Orientation).Id).ToArray();
                    renovation.AppliedStations.Add(definition.Id, Array.AsReadOnly(ids));
                }
            }
            self.Blocked.UnionWith(GridStationConfig.ValidateTerrain(GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList),
                self.Stations.Values.Select(value => (GridStation)value).Select(s => new GridStationPlacement(s.DefinitionId, s.Cell, s.Orientation)),
                Tables.Instance.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY))));
            return true;
        }

        private static GridStation RestoreSaveStation(this GridKitchenComponent self, int definitionId, int initialId, GridCell cell, int orientation)
        {
            var definition = Tables.Instance.DTStationConfig.Get(definitionId); var station = self.AddChild<GridStation>();
            station.InitialLayoutId = initialId; station.DefinitionId = definitionId; station.Cell = cell; station.Orientation = orientation;
            station.Name = initialId == 0 ? definition.Name : Tables.Instance.DTInitialLayoutConfig.Get(initialId).Name;
            station.Kind = (GridStationKind)definition.Kind; station.Capabilities = (GridStationCapability)definition.Capabilities;
            station.ItemSlotCapacity = definition.ItemSlotCapacity; self.Stations.Add(station.Id, station); return station;
        }
    }
}
#endif
