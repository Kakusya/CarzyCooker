#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(FrontTable))]
    public static partial class GridKitchenSystem
    {
        public static void AdvanceCustomers(this GridKitchenComponent self, Level level)
        {
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return;
            FrontLevelState state = level.GetComponent<FrontLevelState>();
            DRCustomerFlowConfig flow = Tables.Instance.DTCustomerFlowConfig.Get(state.FlowDefinitionId);
            if (level.Phase == LevelPhase.Operating && level.ElapsedOperatingTicks + 1 >= state.NextArrivalOperatingTick)
            {
                state.NextArrivalOperatingTick += flow.ArrivalIntervalTicks;
                if (state.Waiting.Count >= flow.QueueCapacity) ++state.SkippedArrivals;
                else
                {
                    Customer customer = level.AddChild<Customer, DRCustomerFlowConfig, long, int>(flow, level.Generation, ++state.NextCustomerSequence);
                    self.PlanCustomerPath(customer, new GridCell(flow.QueueX, flow.QueueY)); state.Waiting.Enqueue(customer.Id);
                }
                self.Dirty = true;
            }
            foreach (Customer customer in level.Children.Values.OfType<Customer>().OrderBy(value => value.Id).ToArray())
            {
                if (customer.Phase is CustomerPhase.Entering or CustomerPhase.WalkingToSeat or CustomerPhase.Leaving)
                {
                    self.AdvanceCustomerMovement(customer);
                    if (customer.Motion != GridMotion.Standing || customer.PathIndex < customer.Path.Count) continue;
                    switch (customer.Phase)
                    {
                        case CustomerPhase.Entering:
                            customer.Phase = CustomerPhase.Queued;
                            break;
                        case CustomerPhase.WalkingToSeat:
                            GridStation tableStation = self.Station(customer.TableStationId);
                            FrontTable table = tableStation.GetComponent<FrontTable>();
                            table.State = FrontTableState.Occupied; customer.Phase = CustomerPhase.WaitingAsk;
                            var target = new FrontWorkDefinition(FrontWorkKind.Ask, customer.Id, tableStation.Id, 0, Tables.Instance.DTCompanionConfig.DataList.Single().AskTicks);
                            customer.AskWorkId = level.AddChild<FrontWork, FrontWorkDefinition, long>(target, level.Generation).Id;
                            break;
                        case CustomerPhase.Leaving:
                            self.FinishCustomerExit(level, customer);
                            break;
                    }
                    self.Dirty = true;
                }
                else if (customer.Phase == CustomerPhase.Ordered)
                {
                    Order order = level.GetChild<Order>(customer.OrderId);
                    if (order != null && order.State == OrderState.Waiting && self.Tick >= order.DeadlineTick)
                    { order.State = OrderState.Expired; self.BeginCustomerExit(customer, flow); }
                }
                else if (customer.Phase == CustomerPhase.Eating && customer.PhaseEnteredTick != self.Tick)
                {
                    --customer.RemainingEatTicks; self.Dirty = true;
                    if (customer.RemainingEatTicks <= 0) self.BeginCustomerExit(customer, flow);
                }
            }
            foreach (GridStation station in self.Stations.Values.Select(value => (GridStation)value).Where(value => value.Kind == GridStationKind.Table).OrderBy(value => value.InitialLayoutId))
            {
                FrontTable table = station.GetComponent<FrontTable>();
                if (table.State != FrontTableState.Free || station.ItemId != 0 || state.Waiting.Count == 0) continue;
                Customer customer = level.GetChild<Customer>(state.Waiting.Peek());
                if (customer == null || customer.Phase == CustomerPhase.Gone) { state.Waiting.Dequeue(); continue; }
                if (customer.Phase != CustomerPhase.Queued) continue;
                if (!self.PlanCustomerPath(customer, station.FrontSeat())) continue;
                state.Waiting.Dequeue(); table.CustomerId = customer.Id; table.State = FrontTableState.Reserved;
                customer.TableStationId = station.Id; customer.Phase = CustomerPhase.WalkingToSeat; self.Dirty = true;
            }
        }

        private static bool PlanCustomerPath(this GridKitchenComponent self, Customer customer, GridCell goal)
        {
            var path = self.FindGridPath(customer.Cell, goal);
            if (path == null) return false;
            customer.Path.Clear(); customer.Path.AddRange(path.Skip(1)); customer.PathIndex = 0; customer.Motion = GridMotion.Standing;
            return true;
        }

        private static void AdvanceCustomerMovement(this GridKitchenComponent self, Customer customer)
        {
            if (customer.Motion == GridMotion.Transit)
            {
                if (self.Tick - customer.StepStartTick < GridKitchenRules.StepTicks) return;
                customer.Cell = customer.NextCell; customer.Motion = GridMotion.Standing; ++customer.PathIndex; self.Dirty = true;
            }
            if (customer.PathIndex >= customer.Path.Count) return;
            GridCell next = customer.Path[customer.PathIndex];
            if (!self.IsWalkable(next)) return;
            customer.NextCell = next; customer.StepStartTick = self.Tick; customer.Motion = GridMotion.Transit; self.Dirty = true;
        }

        private static void BeginCustomerExit(this GridKitchenComponent self, Customer customer, DRCustomerFlowConfig flow)
        {
            if (!self.PlanCustomerPath(customer, new GridCell(flow.EntranceX, flow.EntranceY))) return;
            customer.Phase = CustomerPhase.Leaving; customer.PhaseEnteredTick = self.Tick; self.Dirty = true;
        }

        private static void FinishCustomerExit(this GridKitchenComponent self, Level level, Customer customer)
        {
            GridStation station = self.Station(customer.TableStationId); FrontTable table = station.GetComponent<FrontTable>();
            if (station.ItemId == 0) { table.CustomerId = 0; table.State = FrontTableState.Free; }
            else
            {
                GridItem utensil = self.Item(station.ItemId); utensil.Hygiene = ItemHygiene.Dirty; table.State = FrontTableState.Dirty;
                var target = new FrontWorkDefinition(FrontWorkKind.Clear, customer.Id, station.Id, utensil.Id, Tables.Instance.DTCompanionConfig.DataList.Single().ClearTicks);
                level.AddChild<FrontWork, FrontWorkDefinition, long>(target, level.Generation);
            }
            customer.Phase = CustomerPhase.Gone; customer.Path.Clear(); customer.PathIndex = 0;
            self.Dirty = true;
        }
    }
}
#endif
