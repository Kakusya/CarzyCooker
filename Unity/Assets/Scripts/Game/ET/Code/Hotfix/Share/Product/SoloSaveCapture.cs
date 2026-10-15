#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(GridManualProcess))]
    [FriendOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridBakeProcess))]
    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(SupplyShipment))]
    [FriendOf(typeof(RestaurantRenovation))]
    public static class SoloSaveCapture
    {
        public static SoloSavePayload Kitchen(RestaurantRuntime restaurant)
        {
            Level level = restaurant.CurrentLevel;
            if (restaurant.Stopped || level == null || level.Phase != LevelPhase.Completed)
                throw new InvalidOperationException("SuccessRequired");
            GridKitchenComponent kitchen = restaurant.Kitchen;
            var renovation = restaurant.GetComponent<RestaurantRenovation>();
            if (!level.IsChapterTerminal && (!renovation.Prepared || renovation.ResultLevelId != level.Id || renovation.ResultLevelGeneration != level.Generation))
                throw new InvalidOperationException("RenovationNotReady");
            if (!level.IsChapterTerminal && renovation.Candidates.Count > 0 && renovation.SelectedDefinitionId == 0 &&
                !(level.EligibleProfiles.Count > 0 && renovation.SelectionFinalized))
                throw new InvalidOperationException("RenovationRequired");
            var stationKeys = kitchen.Stations.Keys.OrderBy(id => id).Select((id, index) => (id, key: index + 1)).ToDictionary(p => p.id, p => p.key);
            var itemKeys = kitchen.Items.Keys.OrderBy(id => id).Select((id, index) => (id, key: index + 1)).ToDictionary(p => p.id, p => p.key);
            int StationKey(long id) => id == 0 ? 0 : stationKeys[id];
            int ItemKey(long id) => id == 0 ? 0 : itemKeys[id];
            var supply = kitchen.GetComponent<KitchenSupply>();
            return new SoloSavePayload
            {
                Unlocks = restaurant.GetComponent<RestaurantUnlocks>().CaptureRestaurantUnlocks(),
                Geometry = new SavedKitchenGeometry { Width = kitchen.Width, Height = kitchen.Height },
                Stations = kitchen.Stations.Values.Select(value => (GridStation)value).OrderBy(s => s.Id).Select(station =>
                {
                    var table = station.GetComponent<FrontTable>();
                    return new SoloSavedStation
                    {
                        Key = StationKey(station.Id), DefinitionId = station.DefinitionId, InitialLayoutId = station.InitialLayoutId,
                        X = station.Cell.X, Y = station.Cell.Y, Orientation = station.Orientation, ItemKey = ItemKey(station.ItemId),
                        PoolItemKeys = station.PoolItemIds.Select(ItemKey).ToArray(), TableDefinitionId = table?.DefinitionId ?? 0,
                        TableDirty = table?.State == FrontTableState.Dirty,
                        UpgradeDefinitionId = station.UpgradeDefinitionId, UpgradeLevel = station.UpgradeLevel
                    };
                }).ToArray(),
                Items = kitchen.Items.Values.Select(value => (GridItem)value).OrderBy(i => i.Id).Select(item =>
                {
                    var location = item.Location; var cell = item.Cell; long holder = item.HolderId;
                    if (location == GridItemLocation.Hand)
                    {
                        GridActor actor = kitchen.Actors.Values.Select(value => (GridActor)value).Single(a => a.MemberId == holder && a.HandItemId == item.Id);
                        location = GridItemLocation.Ground; cell = actor.Cell; holder = 0;
                    }
                    return new SoloSavedItem
                    {
                        Key = ItemKey(item.Id), DefinitionId = item.DefinitionId, Location = (int)location,
                        HolderKey = location == GridItemLocation.Ground ? 0 : location == GridItemLocation.Container ? ItemKey(holder) : StationKey(holder),
                        X = cell.X, Y = cell.Y, Hygiene = (int)item.Hygiene, ContentKeys = item.Contents.Select(ItemKey).ToArray()
                    };
                }).ToArray(),
                ManualWork = kitchen.Processes.Values.Select(value => (GridManualProcess)value).Where(p => p.State != GridWorkState.Completed)
                    .OrderBy(p => p.Id).Select((p, index) => new SoloSavedManual
                    {
                        Key = index + 1, RecipeId = p.RecipeId, RequiredTicks = p.RequiredTicks, ProgressTicks = p.ProgressTicks,
                        InputKey = ItemKey(p.InputItemId), TargetKey = ItemKey(p.TargetItemId), StationKey = StationKey(p.StationId)
                    }).ToArray(),
                Batches = kitchen.AutoProcesses.Values.Select(value => (GridAutoProcess)value).OrderBy(p => p.Id).Select((p, index) => new SoloSavedBatch
                {
                    Key = index + 1, RecipeId = p.RecipeId, RequiredTicks = p.RequiredTicks, ProgressTicks = p.ProgressTicks,
                    PotKey = ItemKey(p.PotItemId), StationKey = StationKey(p.StartStationId), State = (int)p.State,
                    OutputDefinitionId = p.OutputDefinitionId, TotalPortions = p.TotalPortions, RemainingPortions = p.RemainingPortions,
                    ServingContainerDefinitionId = p.ServingContainerDefinitionId, InputKeys = p.InputItemIds.Select(ItemKey).ToArray(),
                    RequiredCapability = p.RequiredCapability, LockContainer = p.LockContainer
                }).ToArray(),
                Baking = kitchen.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Where(p => p.State != GridBakeState.Completed)
                    .OrderBy(p => p.Id).Select((p, index) => new SoloSavedBake
                    {
                        Key = index + 1, RecipeId = p.RecipeId, RequiredTicks = p.RequiredTicks, ProgressTicks = p.ProgressTicks,
                        StationKey = StationKey(p.StationId), InputKey = ItemKey(p.InputItemId), OutputDefinitionId = p.OutputDefinitionId,
                        CarrierKey = ItemKey(p.CarrierItemId)
                    }).ToArray(),
                Supply = supply.RemainingPackages.OrderBy(pair => pair.Key).Select(pair => new SoloSavedSupplyBalance
                    { SourceId = pair.Key, RemainingPackages = pair.Value }).ToArray(),
                Shipments = supply.Children.Values.OfType<SupplyShipment>().OrderBy(s => s.Id).Select((s, index) => new SoloSavedShipment
                {
                    Key = index + 1, SourceId = s.SourceId, MaterialDefinitionId = s.MaterialDefinitionId,
                    PackageDefinitionId = s.PackageDefinitionId, PackageUnits = s.PackageUnits, RequiredTicks = s.RequiredTicks,
                    ProgressTicks = s.ProgressTicks, ReceivingStationKey = StationKey(s.ReceivingStationId), Stage = (int)s.Stage,
                    PackageKey = itemKeys.GetValueOrDefault(s.PackageItemId)
                }).ToArray(),
                Renovations = renovation.AppliedStations.OrderBy(pair => pair.Key).Select(pair => new SoloSavedRenovation
                    { DefinitionId = pair.Key, StationKeys = pair.Value.Select(StationKey).ToArray() }).ToArray(),
                PendingRenovationId = renovation.SelectedDefinitionId,
                PendingUpgradeStationKey = StationKey(((GridStation)renovation.PendingUpgradeStation)?.Id ?? 0),
                PendingUpgradeExpectedLevel = renovation.PendingUpgradeExpectedLevel
            };
        }
    }
}
#endif
