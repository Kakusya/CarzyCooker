using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void ConfigureFrontFlow(int interval = 60, int wait = 200, int? firstArrival = null)
        {
            var rows = (System.Collections.Generic.List<DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataList;
            var map = (System.Collections.Generic.Dictionary<int, DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataMap;
            DRCustomerFlowConfig original = rows.Single(value => value.LevelConfigId == 1);
            var buffer = new Luban.ByteBuf();
            foreach (int value in new[] { original.Id, original.LevelConfigId, firstArrival ?? original.FirstArrivalTicks, interval, original.QueueCapacity, wait,
                original.EatTicks, original.EntranceX, original.EntranceY, original.QueueX, original.QueueY }) buffer.WriteInt(value);
            buffer.WriteSize(original.OrderConfigIds.Count); foreach (int id in original.OrderConfigIds) buffer.WriteInt(id);
            var replacement = new DRCustomerFlowConfig(buffer); rows[rows.IndexOf(original)] = replacement; map[replacement.Id] = replacement;
        }

        [Test]
        public void SoloCustomerFlowDefaultArrivalWalksRealTerrainAndAskOpensOrder()
        {
            StartSolo(liveFront: true); AdvanceTicks(30);
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty);
            LevelDo(LevelControlAction.Start); AdvanceTicks(18);
            Assert.That(CurrentLevel.ElapsedOperatingTicks, Is.EqualTo(19)); Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty);
            AdvanceTicks(1); PumpMessages(4);
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty, "Arrival at deadline is not yet delivered into the next business step");
            AdvanceTicks(1); PumpMessages(4);
            Customer first = CurrentLevel.Children.Values.OfType<Customer>().Single();
            Assert.That(first.Phase, Is.EqualTo(CustomerPhase.Entering)); Assert.That(first.Cell, Is.EqualTo(GridStationConfig.Entrance()));
            Assert.That(first.Motion, Is.EqualTo(GridMotion.Transit)); Assert.That(first.OrderId, Is.Zero);
            FrontWait(() => first.Phase == CustomerPhase.WaitingAsk);
            GridStation table = Grid.Station(first.TableStationId);
            Assert.That(first.Cell, Is.EqualTo(table.FrontSeat())); Assert.That(first.Motion, Is.EqualTo(GridMotion.Standing));
            Assert.That(first.OrderId, Is.Zero); Assert.That(CurrentLevel.Score, Is.Zero);
            FrontWait(() => first.AskWorkId != 0);
            FrontWork ask = CurrentLevel.GetChild<FrontWork>(first.AskWorkId);
            Assert.That(ask.Progress, Is.Zero);
            FrontWait(() => ask.State == FrontWorkState.Working);
            Assert.That(FrontRole.Cell, Is.EqualTo(ask.OperationCell));
            Assert.That(Grid.IsWalkable(FrontRole.Cell), Is.True); Assert.That(table.StationApproaches().Contains(FrontRole.Cell), Is.True);
            FrontWait(() => first.Phase == CustomerPhase.Ordered);
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Completed)); Assert.That(first.OrderId, Is.Not.Zero);
            Assert.That(CurrentLevel.GetChild<Order>(first.OrderId).DishId, Is.EqualTo(1));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[((Companion)FrontRole.Companion).Id], Is.EqualTo(1));
        }

        [Test]
        public void SoloCustomerFlowWaitingAskHasNoPenaltyAndPlayerCanOpenRealOrder()
        {
            StartSolo(liveFront: true); FrontRole.NextScheduleTick = 100000; LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.Phase == CustomerPhase.WaitingAsk));
            Customer first = CurrentLevel.Children.Values.OfType<Customer>().OrderBy(value => value.Id).First();
            AdvanceTicks(100);
            Assert.That(first.Phase, Is.EqualTo(CustomerPhase.WaitingAsk), $"customer={first.Id};" + string.Join(";", CurrentLevel.Children.Values.OfType<Customer>().Select(value => $"{value.ArrivalSequence}:{value.Phase}/{value.TableStationId}"))); Assert.That(first.OrderId, Is.Zero);
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty);
            FrontWork ask = CurrentLevel.GetChild<FrontWork>(first.AskWorkId);
            FrontPlayerDo(FrontWorkAction.Claim, ask); FrontWait(() => ask.State == FrontWorkState.Completed);
            Assert.That(first.OrderId, Is.Not.Zero); Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
        }

        [Test]
        public void SoloCustomerFlowFullQueueSkipsWithoutHiddenCustomerAndSeatsFifo()
        {
            ConfigureFrontFlow(interval: 10, wait: 20);
            StartSolo(liveFront: true); FrontRole.NextScheduleTick = 100000; LevelDo(LevelControlAction.Start); AdvanceTicks(90);
            FrontLevelState state = CurrentLevel.GetComponent<FrontLevelState>();
            Customer[] customers = CurrentLevel.Children.Values.OfType<Customer>().OrderBy(value => value.ArrivalSequence).ToArray();
            Assert.That(customers.Length, Is.EqualTo(4)); Assert.That(state.Waiting.Count, Is.EqualTo(2));
            Assert.That(state.SkippedArrivals, Is.EqualTo(4)); Assert.That(state.Waiting.ToArray(), Is.EqualTo(customers.Skip(2).Select(value => value.Id).ToArray()));
            Assert.That(CurrentLevel.Score, Is.Zero);
            FrontWork ask = CurrentLevel.GetChild<FrontWork>(customers[0].AskWorkId);
            FrontPlayerDo(FrontWorkAction.Claim, ask); FrontWait(() => customers[0].Phase == CustomerPhase.Gone);
            FrontWait(() => customers[2].TableStationId != 0);
            Assert.That(customers[2].TableStationId, Is.EqualTo(customers[0].TableStationId));
            Assert.That(customers[2].Phase, Is.EqualTo(CustomerPhase.WalkingToSeat));
            Assert.That(state.Waiting.Peek(), Is.EqualTo(customers[3].Id));
        }

        [Test]
        public void SoloCustomerFlowUnservedOrderExpiresAndCustomerActuallyWalksOutForZero()
        {
            StartSolo(liveFront: true); LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.Phase == CustomerPhase.Ordered));
            Customer first = CurrentLevel.Children.Values.OfType<Customer>().OrderBy(value => value.Id).First();
            Order order = CurrentLevel.GetChild<Order>(first.OrderId);
            AdvanceTicks((int)(order.DeadlineTick - Grid.Tick - 1));
            Assert.That(first.Phase, Is.EqualTo(CustomerPhase.Ordered)); Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
            AdvanceTicks(1);
            Assert.That(order.State, Is.EqualTo(OrderState.Waiting)); Assert.That(order.TransitionQueued, Is.True);
            AdvanceTicks(1);
            Assert.That(order.State, Is.EqualTo(OrderState.Expired)); Assert.That(first.Phase, Is.EqualTo(CustomerPhase.Leaving));
            Assert.That(first.Cell, Is.Not.EqualTo(GridStationConfig.Entrance()));
            FrontWait(() => first.Phase == CustomerPhase.Gone);
            Assert.That(first.Cell, Is.EqualTo(GridStationConfig.Entrance())); Assert.That(CurrentLevel.Score, Is.Zero);
            Assert.That(Grid.Station(first.TableStationId).ItemId, Is.Zero);
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Any(value => value.Kind == FrontWorkKind.Clear && value.CustomerId == first.Id), Is.False);
        }

        [Test]
        public void SoloCustomerFlowPauseFreezesArrivalMovementAndOrderDeadlineClock()
        {
            StartSolo(liveFront: true); LevelDo(LevelControlAction.Start); AdvanceTicks(20);
            Customer first = CurrentLevel.Children.Values.OfType<Customer>().Single(); GridCell cell = first.Cell, next = first.NextCell;
            LevelDo(LevelControlAction.Pause); long tick = Grid.Tick; int count = CurrentLevel.Children.Values.OfType<Customer>().Count();
            AdvanceTicks(100);
            Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(first.Cell, Is.EqualTo(cell)); Assert.That(first.NextCell, Is.EqualTo(next));
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>().Count(), Is.EqualTo(count));
            LevelDo(LevelControlAction.Resume); FrontWait(() => first.Phase == CustomerPhase.Ordered);
            Order order = CurrentLevel.GetChild<Order>(first.OrderId); long deadline = order.DeadlineTick;
            LevelDo(LevelControlAction.Pause); tick = Grid.Tick; AdvanceTicks(250);
            Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(order.DeadlineTick, Is.EqualTo(deadline)); Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
            LevelDo(LevelControlAction.Resume); Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
        }

        [Test]
        public void SoloCustomerFlowDrainingStopsNewArrivalsAndNaturallyFinishesRealOrders()
        {
            StartSolo(liveFront: true); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            int created = CurrentLevel.GetComponent<FrontLevelState>().NextCustomerSequence;
            Assert.That(created, Is.GreaterThan(0)); Assert.That(CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.BlocksLevelCompletion), Is.True);
            FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 1500);
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().NextCustomerSequence, Is.EqualTo(created));
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>().All(value => value.Phase == CustomerPhase.Gone), Is.True);
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().All(value => !value.BlocksLevelCompletion), Is.True);
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.Stars, Is.Zero);
        }

        [Test]
        public void SoloCompanionScheduleAskPriorityAndStartedWashIsNotPreempted()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            FrontWork wash = InitialWashTarget(); Customer customer = InitialSeatedCustomer(); FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            FrontRole.NextScheduleTick = Grid.Tick; AdvanceTicks(2); PumpMessages(8);
            Assert.That(FrontRole.WorkId, Is.EqualTo(ask.Id)); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Available));
            FrontWait(() => ask.State == FrontWorkState.Completed); FrontWait(() => wash.State == FrontWorkState.Working);
            customer.Phase = CustomerPhase.Gone; Customer nextCustomer = InitialSeatedCustomer(2); FrontWork nextAsk = CurrentLevel.GetChild<FrontWork>(nextCustomer.AskWorkId);
            double progress = wash.Progress; AdvanceTicks(7);
            Assert.That(FrontRole.WorkId, Is.EqualTo(wash.Id)); Assert.That(wash.Progress, Is.GreaterThan(progress)); Assert.That(nextAsk.Progress, Is.Zero);
            FrontWait(() => wash.State == FrontWorkState.Completed);
            Assert.That(Grid.Item(wash.ItemId).Location, Is.EqualTo(GridItemLocation.CleanPool));
            FrontWait(() => nextAsk.State == FrontWorkState.Completed); Assert.That(nextCustomer.OrderId, Is.Not.Zero);
        }

        [Test]
        public void SoloCompanionScheduleFrontSnapshotCopiesEveryRoleAndTargetValue()
        {
            StartSolo(liveFront: true); LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.Phase == CustomerPhase.Ordered));
            var projection = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            using var first = GridBroadcast.CreateSnapshot(Grid, FrontPeer); using var second = GridBroadcast.CreateSnapshot(Grid, FrontPeer);
            first.Version = projection.Version + 1; projection.ApplyGridSnapshot(first);
            long role = first.Companions[0].RoleId, customer = first.Customers[0].CustomerId, order = first.Orders[0].OrderId;
            string name = projection.Companions[role].Name; GridCell position = projection.Customers[customer].Cell;
            int score = projection.Orders[order].Score; int count = projection.FrontWorks.Count;
            first.Companions[0].Name = "changed"; first.Companions[0].Path.Clear(); first.Customers[0].X = 99;
            first.Orders[0].Score = 999; first.FrontWorks.Clear(); first.Tables.Clear(); first.WaitingCustomers.Add(999);
            Assert.That(projection.Companions[role].Name, Is.EqualTo(name)); Assert.That(projection.Customers[customer].Cell, Is.EqualTo(position));
            Assert.That(projection.Orders[order].Score, Is.EqualTo(score)); Assert.That(projection.FrontWorks.Count, Is.EqualTo(count));
            Assert.That(projection.Tables.Count, Is.EqualTo(2)); Assert.That(projection.WaitingCustomers.Contains(999), Is.False);
            Assert.That(second.Companions[0].Name, Is.EqualTo(name)); Assert.That(second.Customers[0].X, Is.Not.EqualTo(99));
            Assert.That(second.FrontWorks, Is.Not.Empty); Assert.That(CurrentLevel.Score, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloCompanionScheduleSameKindUsesCreationFifo(bool washing)
        {
            StartSolo(); LevelDo(LevelControlAction.Start); FrontWork first, second;
            if (!washing)
            {
                Customer one = InitialSeatedCustomer(), two = InitialSeatedCustomer(2, 12);
                first = CurrentLevel.GetChild<FrontWork>(one.AskWorkId); second = CurrentLevel.GetChild<FrontWork>(two.AskWorkId);
            }
            else
            {
                first = InitialWashTarget(); GridStation source = PrepStation("第二碗台"); GridItem bowl = Grid.Item(source.ItemId); source.ItemId = 0;
                GridStation pool = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == GridStationKind.DirtyPool);
                bowl.Location = GridItemLocation.DirtyPool; bowl.HolderId = pool.Id; bowl.Cell = pool.Cell; bowl.Hygiene = ItemHygiene.Dirty; pool.PoolItemIds.Add(bowl.Id);
                var target = new FrontWorkDefinition(FrontWorkKind.Wash, 0, first.StationId, bowl.Id, 61);
                second = CurrentLevel.AddChild<FrontWork, FrontWorkDefinition, long>(target, CurrentLevel.Generation);
            }
            Assert.That(first.Id, Is.LessThan(second.Id)); FrontRole.NextScheduleTick = Grid.Tick;
            AdvanceTicks(2); PumpMessages(8);
            Assert.That(FrontRole.WorkId, Is.EqualTo(first.Id)); Assert.That(second.State, Is.EqualTo(FrontWorkState.Available));
            FrontWait(() => first.State == FrontWorkState.Completed); FrontWait(() => second.State == FrontWorkState.Completed);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[((Companion)FrontRole.Companion).Id], Is.EqualTo(2));
            if (washing)
            { Assert.That(Grid.Item(first.ItemId).Hygiene, Is.EqualTo(ItemHygiene.Clean)); Assert.That(Grid.Item(second.ItemId).Hygiene, Is.EqualTo(ItemHygiene.Clean)); }
        }

        [Test]
        public void SoloStationTerrainPlayerCompanionAndActualCustomerUseMovedTableTerrain()
        {
            StartSolo(liveFront: true); GridStation table = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == 11);
            LayoutDo(table, new GridCell(3, 4));
            Assert.That(Grid.Blocked.Contains(new GridCell(3, 4)), Is.True); Assert.That(Grid.Blocked.Contains(new GridCell(1, 3)), Is.False);
            LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.Phase == CustomerPhase.WalkingToSeat));
            Customer first = CurrentLevel.Children.Values.OfType<Customer>().OrderBy(value => value.Id).First();
            Assert.That(first.TableStationId, Is.EqualTo(table.Id));
            Assert.That(first.Path.All(Grid.IsWalkable), Is.True);
            Assert.That(first.Path.Contains(new GridCell(3, 4)), Is.False);
            ClientSessionComponent client = host.Client;
            using (var response = SoloReply(client, client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move, table.FrontSeat()))) Assert.That(response.Error, Is.Zero);
            AdvanceTicks(1);
            Assert.That(Grid.Actor(client.MemberId).Path.All(Grid.IsWalkable), Is.True);
            FrontWait(() => CurrentLevel.GetChild<FrontWork>(first.AskWorkId)?.State == FrontWorkState.Approaching);
            Assert.That(FrontRole.Path.All(Grid.IsWalkable), Is.True); Assert.That(FrontRole.Path.Contains(new GridCell(3, 4)), Is.False);
            FrontWait(() => first.Phase == CustomerPhase.Ordered);
            Assert.That(first.Cell, Is.EqualTo(table.FrontSeat()));
            Assert.That(table.StationApproaches().Contains(FrontRole.Cell), Is.True);
            Assert.That(Grid.Actor(client.MemberId).Cell, Is.EqualTo(table.FrontSeat()));
        }
    }
}
