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
            self.AdvanceCustomerTransitions(level, state, flow);
            if (level.Phase == LevelPhase.Operating && level.ElapsedOperatingTicks < level.OperatingTicks && level.ElapsedOperatingTicks + 1 >= state.NextArrivalOperatingTick && !state.ArrivalQueued)
            {
                state.ArrivalQueued = true;
                self.RequestCustomerTransition(level, state, state.ArrivalSerial, CustomerFlowAction.Arrive);
            }
            foreach (Customer customer in level.Children.Values.OfType<Customer>().OrderBy(value => value.Id).ToArray())
            {
                if (customer.TransitionQueued) continue;
                if (customer.Phase is CustomerPhase.Entering or CustomerPhase.WalkingToSeat or CustomerPhase.Leaving)
                {
                    self.AdvanceCustomerMovement(customer);
                    if (customer.Motion != GridMotion.Standing || customer.PathIndex < customer.Path.Count) continue;
                    switch (customer.Phase)
                    {
                        case CustomerPhase.Entering:
                            self.QueueCustomerTransition(level, customer, CustomerFlowAction.ReachedQueue);
                            break;
                        case CustomerPhase.WalkingToSeat:
                            self.QueueCustomerTransition(level, customer, CustomerFlowAction.ReachedSeat);
                            break;
                        case CustomerPhase.Leaving:
                            self.QueueCustomerTransition(level, customer, CustomerFlowAction.ReachedExit);
                            break;
                    }
                    self.Dirty = true;
                }
                else if (customer.Phase == CustomerPhase.Ordered)
                {
                    Order order = level.GetChild<Order>(customer.OrderId);
                    if (order != null && order.State == OrderState.Waiting && self.Tick >= order.DeadlineTick && !order.TransitionQueued)
                    { order.TransitionQueued = true; self.RequestCustomerTransition(level, order, order.TransitionSerial, CustomerFlowAction.OrderExpired); }
                }
                else if (customer.Phase == CustomerPhase.Eating && customer.PhaseEnteredTick != self.Tick)
                {
                    customer.RemainingEatTicks = System.Math.Max(0, customer.RemainingEatTicks - 1); self.Dirty = true;
                    if (customer.RemainingEatTicks <= 0) self.QueueCustomerTransition(level, customer, CustomerFlowAction.FinishedEating);
                }
            }
            foreach (GridStation station in self.Stations.Values.Select(value => (GridStation)value).Where(value => value.Kind == GridStationKind.Table).OrderBy(value => value.InitialLayoutId))
            {
                FrontTable table = station.GetComponent<FrontTable>();
                if (table.State != FrontTableState.Free || station.ItemId != 0 || state.Waiting.Count == 0) continue;
                Customer customer = level.GetChild<Customer>(state.Waiting.Peek());
                if (customer == null || customer.Phase == CustomerPhase.Gone) { state.Waiting.Dequeue(); continue; }
                if (customer.Phase != CustomerPhase.Queued || customer.TransitionQueued) continue;
                self.QueueCustomerTransition(level, customer, CustomerFlowAction.AdmitSeat);
                break;
            }
        }

        private static void AdvanceCustomerTransitions(this GridKitchenComponent self, Level level, FrontLevelState state, DRCustomerFlowConfig flow)
        {
            while (state.FinishedTransitions.Count > 0)
            {
                CustomerFlowFinishedValue finished = state.FinishedTransitions.Dequeue();
                Customer source = level.GetChild<Customer>(finished.Identity.SourceId);
                if (source == null || source.InstanceId != finished.Identity.SourceInstanceId || source.TransitionSerial != finished.Identity.Serial ||
                    source.Phase != finished.Phase || source.TableStationId != finished.TableStationId || !source.SuccessorPending || source.SuccessorQueued) continue;
                source.SuccessorQueued = true;
                self.RequestFrontCreation(level, source, source.TransitionSerial, source.Phase == CustomerPhase.WaitingAsk ? FrontWorkKind.Ask : FrontWorkKind.Clear);
            }
            while (state.Transitions.Count > 0)
            {
                CustomerFlowTransitionValue value = state.Transitions.Dequeue(); ProductTransitionIdentity identity = value.Identity;
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation) continue;
                if (value.Action == CustomerFlowAction.Arrive)
                {
                    if (identity.SourceId != state.Id || identity.SourceInstanceId != state.InstanceId || identity.Serial != state.ArrivalSerial || !state.ArrivalQueued) continue;
                    state.ArrivalQueued = false; ++state.ArrivalSerial;
                    state.NextArrivalOperatingTick += flow.ArrivalIntervalTicks;
                    if (level.Phase == LevelPhase.Operating && level.ElapsedOperatingTicks < level.OperatingTicks)
                    {
                        if (state.Waiting.Count >= flow.QueueCapacity) ++state.SkippedArrivals;
                        else
                        {
                            Customer arrived = level.AddChild<Customer, DRCustomerFlowConfig, long, int>(flow, level.Generation, ++state.NextCustomerSequence);
                            self.PlanCustomerPath(arrived, new GridCell(flow.QueueX, flow.QueueY)); state.Waiting.Enqueue(arrived.Id);
                        }
                    }
                    self.Dirty = true; continue;
                }
                if (value.Action == CustomerFlowAction.OrderExpired)
                {
                    Order order = level.GetChild<Order>(identity.SourceId);
                    Customer ordered = order == null ? null : level.GetChild<Customer>(order.CustomerId);
                    if (order == null || order.InstanceId != identity.SourceInstanceId || order.TransitionSerial != identity.Serial || !order.TransitionQueued) continue;
                    order.TransitionQueued = false; ++order.TransitionSerial;
                    if (order.State == OrderState.Waiting && self.Tick >= order.DeadlineTick && ordered?.Phase == CustomerPhase.Ordered)
                    { order.State = OrderState.Expired; self.ReleaseDrinkOrder(level, order.Id); ++level.UnfulfilledOrders; self.BeginCustomerExit(ordered, flow); ++ordered.TransitionSerial; ordered.TransitionQueued = false; }
                    continue;
                }
                Customer customer = level.GetChild<Customer>(identity.SourceId);
                if (customer == null || customer.InstanceId != identity.SourceInstanceId || customer.TransitionSerial != identity.Serial || !customer.TransitionQueued) continue;
                switch (value.Action)
                {
                    case CustomerFlowAction.ReachedQueue:
                        if (customer.Phase != CustomerPhase.Entering || customer.Motion != GridMotion.Standing || customer.PathIndex < customer.Path.Count) continue;
                        customer.Phase = CustomerPhase.Queued; break;
                    case CustomerFlowAction.AdmitSeat:
                        if (customer.Phase != CustomerPhase.Queued || state.Waiting.Count == 0 || state.Waiting.Peek() != customer.Id) continue;
                        GridStation station = null;
                        foreach (GridStation candidate in self.Stations.Values.Select(item => (GridStation)item).Where(item => item.Kind == GridStationKind.Table).OrderBy(item => item.InitialLayoutId))
                        {
                            if (candidate.GetComponent<FrontTable>().State != FrontTableState.Free || candidate.ItemId != 0 || !self.PlanCustomerPath(customer, candidate.FrontSeat())) continue;
                            station = candidate; break;
                        }
                        if (station != null)
                        {
                            state.Waiting.Dequeue(); FrontTable table = station.GetComponent<FrontTable>(); table.CustomerId = customer.Id; table.State = FrontTableState.Reserved;
                            customer.TableStationId = station.Id; customer.Phase = CustomerPhase.WalkingToSeat;
                        }
                        break;
                    case CustomerFlowAction.ReachedSeat:
                        if (customer.Phase != CustomerPhase.WalkingToSeat || customer.Motion != GridMotion.Standing || customer.PathIndex < customer.Path.Count) continue;
                        GridStation seat = self.Station(customer.TableStationId);
                        if (seat == null || customer.Cell != seat.FrontSeat()) continue;
                        seat.GetComponent<FrontTable>().State = FrontTableState.Occupied; customer.Phase = CustomerPhase.WaitingAsk;
                        customer.SuccessorPending = true; customer.SuccessorQueued = false;
                        break;
                    case CustomerFlowAction.FinishedEating:
                        if (customer.Phase != CustomerPhase.Eating || customer.RemainingEatTicks > 0) continue;
                        self.BeginCustomerExit(customer, flow); break;
                    case CustomerFlowAction.ReachedExit:
                        if (customer.Phase != CustomerPhase.Leaving || customer.Motion != GridMotion.Standing || customer.PathIndex < customer.Path.Count) continue;
                        self.FinishCustomerExit(level, customer); break;
                    default: continue;
                }
                customer.TransitionQueued = false; ++customer.TransitionSerial; self.Dirty = true;
                if (customer.SuccessorPending) self.NotifyCustomerFinished(level, customer);
            }
        }

        private static void QueueCustomerTransition(this GridKitchenComponent self, Level level, Customer customer, CustomerFlowAction action)
        { customer.TransitionQueued = true; self.RequestCustomerTransition(level, customer, customer.TransitionSerial, action); }

        private static void RequestCustomerTransition(this GridKitchenComponent self, Level level, Entity source, long serial, CustomerFlowAction action)
        {
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<CustomerFlowTransition>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = source.Id; value.SourceInstanceId = source.InstanceId; value.TransitionSerial = serial; value.Action = (int)action;
            }));
        }

        private static void NotifyCustomerFinished(this GridKitchenComponent self, Level level, Customer source)
        {
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<CustomerFlowFinished>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = source.Id; value.SourceInstanceId = source.InstanceId; value.TransitionSerial = source.TransitionSerial;
                value.Phase = (int)source.Phase; value.TableStationId = source.TableStationId;
            }));
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
            self.ReleaseDrinkOrder(level, customer.OrderId);
            Order servedOrder = level.GetChild<Order>(customer.OrderId);
            bool disposableMeal = servedOrder?.State == OrderState.Served && Tables.Instance.DTDishConfig.Get(servedOrder.DishId).RequiresOrderBinding;
            if (station.ItemId == 0 && !disposableMeal) { table.CustomerId = 0; table.State = FrontTableState.Free; }
            else
            {
                GridItem utensil = self.Item(station.ItemId);
                if (utensil != null) utensil.Hygiene = ItemHygiene.Dirty;
                table.State = FrontTableState.Dirty;
                customer.SuccessorPending = true; customer.SuccessorQueued = false;
            }
            customer.Phase = CustomerPhase.Gone; customer.Path.Clear(); customer.PathIndex = 0;
            self.Dirty = true;
        }
    }
}
#endif
