#if !DOTNET
namespace ET
{
    [EntitySystemOf(typeof(FrontLevelState))]
    [FriendOf(typeof(FrontLevelState))]
    public static partial class FrontLevelStateSystem
    {
        [EntitySystem]
        private static void Awake(this FrontLevelState self, DRCustomerFlowConfig definition)
        {
            self.FlowDefinitionId = definition.Id; self.NextArrivalOperatingTick = definition.FirstArrivalTicks;
            self.NextCustomerSequence = 0; self.SkippedArrivals = 0; self.Waiting.Clear();
            self.Serves.Clear();
            self.Transitions.Clear(); self.FinishedTransitions.Clear();
            self.ArrivalSerial = 1; self.ArrivalQueued = false;
            self.CustomerStepBuffer.Clear();self.TableStepBuffer.Clear();
        }
        [EntitySystem]
        private static void Destroy(this FrontLevelState self)
        {
            self.CustomerStepBuffer.Clear();self.TableStepBuffer.Clear();
        }
    }

    [EntitySystemOf(typeof(FrontTable))]
    [FriendOf(typeof(FrontTable))]
    public static partial class FrontTableSystem
    {
        [EntitySystem]
        private static void Awake(this FrontTable self, DRTableConfig definition)
        { self.DefinitionId = definition.Id; self.SeatOffset = new GridCell(definition.SeatOffsetX, definition.SeatOffsetY); self.CustomerId = 0; self.State = FrontTableState.Free; }
    }

    [EntitySystemOf(typeof(Customer))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Level))]
    public static partial class CustomerSystem
    {
        [EntitySystem]
        private static void Awake(this Customer self, DRCustomerFlowConfig definition, long generation, int sequence)
        {
            self.FlowDefinitionId = definition.Id; self.LevelGeneration = generation; self.ArrivalSequence = sequence;
            Level level = self.GetParent<Level>();
            int available = 0;
            foreach (int id in definition.OrderConfigIds)
            {
                if (System.Array.IndexOf(level.AllowedDishIds, Tables.Instance.DTOrderConfig.Get(id).DishId) >= 0)
                {
                    ++available;
                }
            }
            if (available == 0)
            {
                throw new System.InvalidOperationException("NoPermittedCustomerOrder");
            }
            int selected = (sequence - 1) % available;
            foreach (int id in definition.OrderConfigIds)
            {
                if (System.Array.IndexOf(level.AllowedDishIds, Tables.Instance.DTOrderConfig.Get(id).DishId) < 0)
                {
                    continue;
                }
                if (selected-- == 0)
                {
                    self.OrderDefinitionId = id;
                    break;
                }
            }
            self.Phase = CustomerPhase.Entering; self.TableStationId = 0; self.AskWorkId = 0; self.OrderId = 0;
            self.Cell = self.NextCell = new GridCell(definition.EntranceX, definition.EntranceY); self.Motion = GridMotion.Standing;
            self.StepStartTick = 0; self.Path.Clear(); self.PathIndex = 0; self.RemainingEatTicks = 0;
            self.PhaseEnteredTick = 0;
            self.TransitionSerial = 1; self.TransitionQueued = self.SuccessorPending = self.SuccessorQueued = false;
        }
    }

    [EntitySystemOf(typeof(Order))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(Customer))]
    public static partial class OrderSystem
    {
        [EntitySystem]
        private static void Awake(this Order self, DROrderConfig definition, Customer customer, long deadline)
        {
            self.DefinitionId = definition.Id; self.DishId = definition.DishId; self.Score = definition.Score;
            self.CustomerId = customer.Id; self.TableStationId = customer.TableStationId; self.LevelGeneration = customer.LevelGeneration;
            self.DeadlineTick = deadline; self.ServedTick = 0; self.ServedItemId = 0; self.State = OrderState.Waiting;
            self.TransitionSerial = 1; self.TransitionQueued = false;
        }
    }

    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(GridStation))]
    public static class FrontTableAccess
    {
        public static GridCell FrontSeat(this GridStation self)
        {
            FrontTable table = self.GetComponent<FrontTable>();
            return GridStationGeometry.Cells(new[] { table.SeatOffset.X, table.SeatOffset.Y }, self.Cell, self.Orientation)[0];
        }
    }
}
#endif
