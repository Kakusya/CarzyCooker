#if !DOTNET
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(Customer))]
    public static partial class GridKitchenSystem
    {
        static partial void CheckFrontLayout(this GridKitchenComponent self, HashSet<GridCell> terrain, GridCell[] footprint, GridStationLayoutIntent intent, ref string error)
        {
            Level level = self.ProductLevel();
            if (level == null) return;
            CompanionRole[] roles = level.Children.Values.OfType<CompanionRole>().ToArray();
            if (roles.Any(role => footprint.Contains(role.Cell) || role.Motion == GridMotion.Transit && footprint.Contains(role.NextCell)))
            { error = "LayoutOccupied"; return; }
            Customer[] customers = level.Children.Values.OfType<Customer>().Where(value => value.Phase != CustomerPhase.Gone).ToArray();
            if (customers.Any(customer => footprint.Contains(customer.Cell) || customer.Motion == GridMotion.Transit && footprint.Contains(customer.NextCell)))
            { error = "LayoutOccupied"; return; }
            HashSet<GridCell> reachable = GridStationConfig.Reachable(terrain, GridStationConfig.Entrance(), self.Width, self.Height);
            if (roles.Any(role => !reachable.Contains(role.Cell) || role.Motion == GridMotion.Transit && !reachable.Contains(role.NextCell)) ||
                Tables.Instance.DTCompanionConfig.DataList.Any(row => !reachable.Contains(new GridCell(row.SpawnX, row.SpawnY))))
                error = "LayoutBlocked";
            if (error != null) return;
            if (customers.Any(customer => !reachable.Contains(customer.Cell) || customer.Motion == GridMotion.Transit && !reachable.Contains(customer.NextCell)))
            { error = "LayoutBlocked"; return; }
            foreach (GridStation station in self.Stations.Values.Select(value => (GridStation)value))
            {
                FrontTable table = station.GetComponent<FrontTable>();
                if (table == null) continue;
                bool moving = station.Id == intent.StationId;
                GridCell seat = GridStationGeometry.Cells(new[] { table.SeatOffset.X, table.SeatOffset.Y },
                    moving ? intent.Cell : station.Cell, moving ? intent.Orientation : station.Orientation)[0];
                if (!reachable.Contains(seat)) { error = "LayoutBlocked"; return; }
            }
            if (Tables.Instance.DTCustomerFlowConfig.DataList.Any(row => !reachable.Contains(new GridCell(row.QueueX, row.QueueY))))
                error = "LayoutBlocked";
        }
    }
}
#endif
