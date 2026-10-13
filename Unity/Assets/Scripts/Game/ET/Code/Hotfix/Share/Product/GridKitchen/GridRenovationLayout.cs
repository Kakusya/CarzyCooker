#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(FrontTable))]
    public static partial class GridKitchenSystem
    {
        public static string CheckRenovationInstallation(this GridKitchenComponent self, IReadOnlyList<GridStationPlacement> additions, out HashSet<GridCell> terrain)
        {
            terrain = null;
            if (additions.Count == 0 || self.LayoutVersion == long.MaxValue) return "InvalidRenovationPlan";
            var definitions = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            if (additions.Any(value => !definitions.ContainsKey(value.DefinitionId) ||
                definitions[value.DefinitionId].Kind is not ((int)GridStationKind.Counter or (int)GridStationKind.Chopping))) return "InvalidRenovationPlan";
            var footprint = additions.SelectMany(value => GridStationGeometry.Cells(definitions[value.DefinitionId].FootprintOffsets, value.Cell, value.Orientation)).ToHashSet();
            Level level = self.ProductLevel();
            CompanionRole[] roles = level?.Children.Values.OfType<CompanionRole>().ToArray() ?? Array.Empty<CompanionRole>();
            Customer[] customers = level?.Children.Values.OfType<Customer>().Where(value => value.Phase != CustomerPhase.Gone).ToArray() ?? Array.Empty<Customer>();
            if (self.OrderedActors().Any(actor => footprint.Contains(actor.Cell) || actor.Motion == GridMotion.Transit && footprint.Contains(actor.NextCell)) ||
                roles.Any(role => footprint.Contains(role.Cell) || role.Motion == GridMotion.Transit && footprint.Contains(role.NextCell)) ||
                customers.Any(customer => footprint.Contains(customer.Cell) || customer.Motion == GridMotion.Transit && footprint.Contains(customer.NextCell)) ||
                self.Items.Values.Select(value => (GridItem)value).Any(item => item.Location == GridItemLocation.Ground && footprint.Contains(item.Cell))) return "RenovationOccupied";
            foreach (GridStation station in self.Stations.Values.Select(value => (GridStation)value))
            {
                GridItem item = self.Item(station.ItemId);
                if (station.ItemId != 0 && (item == null || item.HolderId != station.Id || item.Location is not (GridItemLocation.Counter or GridItemLocation.Table)))
                    return "RenovationRelationshipsChanged";
                if (station.PoolItemIds.Count > station.ItemSlotCapacity || station.PoolItemIds.Distinct().Count() != station.PoolItemIds.Count ||
                    station.PoolItemIds.Any(id => self.Item(id) == null || self.Item(id).HolderId != station.Id || self.Item(id).Contents.Count != 0 ||
                        self.Item(id).Location != (station.Kind == GridStationKind.CleanPool ? GridItemLocation.CleanPool : GridItemLocation.DirtyPool))) return "RenovationRelationshipsChanged";
                if (station.AutoProcessId != 0 && (self.AutoProcess(station.AutoProcessId)?.StartStationId != station.Id) ||
                    station.BakeProcessId != 0 && (self.BakeProcess(station.BakeProcessId)?.StationId != station.Id)) return "RenovationRelationshipsChanged";
            }
            foreach (GridItem item in self.Items.Values.Select(value => (GridItem)value))
            {
                if (item.Contents.Count != item.Contents.Distinct().Count() || item.Contents.Any(id => self.Item(id) == null ||
                        self.Item(id).Location != GridItemLocation.Container || self.Item(id).HolderId != item.Id) ||
                    item.Location == GridItemLocation.Container && (self.Item(item.HolderId) == null || !self.Item(item.HolderId).Contents.Contains(item.Id)))
                    return "RenovationRelationshipsChanged";
            }
            var required = Tables.Instance.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY))
                .Concat(Tables.Instance.DTCompanionConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY)))
                .Concat(Tables.Instance.DTCustomerFlowConfig.DataList.Select(row => new GridCell(row.QueueX, row.QueueY)))
                .Concat(self.OrderedActors().Select(actor => actor.Cell))
                .Concat(self.OrderedActors().Where(actor => actor.Motion == GridMotion.Transit).Select(actor => actor.NextCell))
                .Concat(roles.Select(role => role.Cell)).Concat(roles.Where(role => role.Motion == GridMotion.Transit).Select(role => role.NextCell))
                .Concat(customers.Select(customer => customer.Cell)).Concat(customers.Where(customer => customer.Motion == GridMotion.Transit).Select(customer => customer.NextCell))
                .Concat(self.Stations.Values.Select(value => (GridStation)value).Where(station => station.GetComponent<FrontTable>() != null).Select(station => station.FrontSeat()));
            try
            {
                terrain = GridStationConfig.ValidateTerrain(definitions,
                    self.Stations.Values.Select(value => (GridStation)value).Select(value => new GridStationPlacement(value.DefinitionId, value.Cell, value.Orientation)).Concat(additions), required);
            }
            catch (InvalidOperationException) { return "RenovationBlocked"; }
            return null;
        }
    }
}
#endif
