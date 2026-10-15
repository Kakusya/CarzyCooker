#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(FrontWork))]
    public static partial class GridKitchenSystem
    {
        public static void InstallFrontTables(this GridKitchenComponent self)
        {
            foreach (DRTableConfig definition in Tables.Instance.DTTableConfig.DataList)
            {
                if (definition.InitialLayoutId == 0)
                {
                    continue;
                }
                GridStation station = self.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == definition.InitialLayoutId);
                if (station.GetComponent<FrontTable>() == null) station.AddComponent<FrontTable, DRTableConfig>(definition);
            }
        }

        public static string CheckFrontTarget(this GridKitchenComponent self, Level level, FrontWork work)
        {
            if (level == null || self.ProductLevel() != level || work == null || work.Parent != level || work.LevelGeneration != level.Generation)
                return "WrongLevel";
            if (!work.BlocksLevelCompletion) return "WorkEnded";
            GridStation station = self.Station(work.StationId);
            if (station == null) return "TargetChanged";
            switch (work.Kind)
            {
                case FrontWorkKind.Ask:
                    Customer customer = level.GetChild<Customer>(work.CustomerId); FrontTable table = station.GetComponent<FrontTable>();
                    if (customer == null || customer.LevelGeneration != level.Generation || customer.Phase != CustomerPhase.WaitingAsk ||
                        customer.TableStationId != station.Id || customer.AskWorkId != work.Id || customer.OrderId != 0 ||
                        table == null || table.CustomerId != customer.Id || table.State != FrontTableState.Occupied || station.ItemId != 0 ||
                        customer.Cell != station.FrontSeat() || customer.Motion != GridMotion.Standing)
                        return "CustomerChanged";
                    DROrderConfig order = Tables.Instance.DTOrderConfig.GetOrDefault(customer.OrderDefinitionId);
                    return order == null || !level.AllowedDishIds.Contains(order.DishId) ? "OrderUnavailable" : null;
                case FrontWorkKind.Clear:
                    Customer departed = level.GetChild<Customer>(work.CustomerId); FrontTable dirtyTable = station.GetComponent<FrontTable>();
                    GridItem used = self.Item(work.ItemId);
                    if (departed == null || departed.Phase != CustomerPhase.Gone || departed.LevelGeneration != level.Generation ||
                        departed.TableStationId != station.Id || dirtyTable == null || dirtyTable.CustomerId != departed.Id ||
                        dirtyTable.State != FrontTableState.Dirty || station.ItemId != work.ItemId ||
                        (work.ItemId == 0 ? level.GetChild<Order>(departed.OrderId)?.State != OrderState.Served ||
                            !Tables.Instance.DTDishConfig.Get(level.GetChild<Order>(departed.OrderId).DishId).RequiresOrderBinding :
                            used == null || used.Hygiene != ItemHygiene.Dirty || used.Location != GridItemLocation.Table || used.HolderId != station.Id || used.Contents.Count != 0))
                        return "TableChanged";
                    return null;
                case FrontWorkKind.Wash:
                    GridItem dirty = self.Item(work.ItemId);
                    GridStation dirtyPool = dirty == null ? null : self.Station(dirty.HolderId);
                    return station.Kind != GridStationKind.Wash || dirty == null || dirty.Hygiene != ItemHygiene.Dirty ||
                        dirty.Location != GridItemLocation.DirtyPool || dirty.Contents.Count != 0 || dirtyPool == null ||
                        dirtyPool.Kind != GridStationKind.DirtyPool || dirtyPool.PoolItemIds.Count(id => id == dirty.Id) != 1
                        ? "ItemChanged" : null;
                default: return "InvalidWork";
            }
        }

        // 只由 Host 业务步在达到进度后提交；调用者成功后才终结工作并计成长。
        public static string CompleteFrontTarget(this GridKitchenComponent self, Level level, FrontWork work)
        {
            string error = self.CheckFrontTarget(level, work);
            if (error != null) return error;
            if (level.Paused) return "Paused";
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return "WrongPhase";
            if (work.State != FrontWorkState.Working || work.Progress < 1 - 1e-9) return "WorkIncomplete";
            GridStation station = self.Station(work.StationId);
            switch (work.Kind)
            {
                case FrontWorkKind.Ask:
                    Customer customer = level.GetChild<Customer>(work.CustomerId);
                    DROrderConfig definition = Tables.Instance.DTOrderConfig.Get(customer.OrderDefinitionId);
                    var flow = Tables.Instance.DTCustomerFlowConfig.Get(customer.FlowDefinitionId);
                    Order order = level.AddChild<Order, DROrderConfig, Customer, long>(definition, customer, self.Tick + flow.OrderWaitTicks);
                    customer.OrderId = order.Id; customer.Phase = CustomerPhase.Ordered;
                    break;
                case FrontWorkKind.Clear:
                    if (work.ItemId == 0)
                    {
                        FrontTable emptyTable = station.GetComponent<FrontTable>();
                        emptyTable.CustomerId = 0; emptyTable.State = FrontTableState.Free;
                        break;
                    }
                    GridStation destination = self.AvailableUtensilPool(GridStationKind.DirtyPool);
                    if (destination == null) return "PoolFull";
                    GridStation washing = self.Stations.Values.Select(value => (GridStation)value).OrderBy(value => value.Id).FirstOrDefault(value => value.Kind == GridStationKind.Wash);
                    if (washing == null) return "MissingWashStation";
                    GridItem used = self.Item(work.ItemId);
                    used.Location = GridItemLocation.DirtyPool; used.HolderId = destination.Id; used.Cell = destination.Cell;
                    destination.PoolItemIds.Add(used.Id); station.ItemId = 0;
                    FrontTable table = station.GetComponent<FrontTable>(); table.CustomerId = 0; table.State = FrontTableState.Free;
                    break;
                case FrontWorkKind.Wash:
                    GridStation clean = self.AvailableUtensilPool(GridStationKind.CleanPool);
                    if (clean == null) return "PoolFull";
                    GridItem dirty = self.Item(work.ItemId); GridStation source = self.Station(dirty.HolderId);
                    source.PoolItemIds.Remove(dirty.Id); clean.PoolItemIds.Add(dirty.Id);
                    dirty.Location = GridItemLocation.CleanPool; dirty.HolderId = clean.Id; dirty.Cell = clean.Cell; dirty.Hygiene = ItemHygiene.Clean;
                    break;
            }
            self.Dirty = true;
            return null;
        }

        private static GridStation AvailableUtensilPool(this GridKitchenComponent self, GridStationKind kind) => self.Stations.Values
            .Select(value => (GridStation)value).OrderBy(value => value.Id).FirstOrDefault(value => value.Kind == kind && value.PoolItemIds.Count < value.ItemSlotCapacity);
    }
}
#endif
