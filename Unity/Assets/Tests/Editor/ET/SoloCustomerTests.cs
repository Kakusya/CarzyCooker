using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Order))]
    public sealed partial class ProductSessionTests
    {
        private Customer InitialSeatedCustomer(int sequence = 1, int layout = 11)
        {
            var definition = Tables.Instance.DTCustomerFlowConfig.Get(CurrentLevel.GetComponent<FrontLevelState>().FlowDefinitionId);
            Customer customer = CurrentLevel.AddChild<Customer, DRCustomerFlowConfig, long, int>(definition, CurrentLevel.Generation, sequence);
            GridStation station = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == layout);
            FrontTable table = station.GetComponent<FrontTable>();
            customer.TableStationId = station.Id; customer.Cell = customer.NextCell = station.FrontSeat();
            customer.Phase = CustomerPhase.WaitingAsk; table.CustomerId = customer.Id; table.State = FrontTableState.Occupied;
            var target = new FrontWorkDefinition(FrontWorkKind.Ask, customer.Id, station.Id, 0, Tables.Instance.DTCompanionConfig.DataList.Single().AskTicks);
            FrontWork work = CurrentLevel.AddChild<FrontWork, FrontWorkDefinition, long>(target, CurrentLevel.Generation);
            customer.AskWorkId = work.Id;
            return customer;
        }

        private FrontWork InitialDirtyTable()
        {
            Customer customer = InitialSeatedCustomer(); customer.Phase = CustomerPhase.Gone;
            CurrentLevel.GetChild<FrontWork>(customer.AskWorkId).State = FrontWorkState.Cancelled;
            GridStation station = Grid.Station(customer.TableStationId), source = PrepStation("碗台");
            GridItem bowl = Grid.Item(source.ItemId); source.ItemId = 0;
            bowl.Location = GridItemLocation.Table; bowl.HolderId = station.Id; bowl.Cell = station.Cell; bowl.Hygiene = ItemHygiene.Dirty;
            station.ItemId = bowl.Id; station.GetComponent<FrontTable>().State = FrontTableState.Dirty;
            var definition = new FrontWorkDefinition(FrontWorkKind.Clear, customer.Id, station.Id, bowl.Id, Tables.Instance.DTCompanionConfig.DataList.Single().ClearTicks);
            return CurrentLevel.AddChild<FrontWork, FrontWorkDefinition, long>(definition, CurrentLevel.Generation);
        }

        private void InitialTargetReady(FrontWork work)
        {
            // 单目标完成接口的合法初始条件；真实认领、到位和业务 Tick 由伙伴工作用例另验。
            CompanionRole role = CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
            GridStation station = Grid.Station(work.StationId);
            role.Cell = role.NextCell = station.StationApproaches().First(Grid.IsWalkable);
            role.Motion = GridMotion.Standing; role.WorkId = work.Id;
            work.WorkerKind = FrontWorkerKind.Companion; work.WorkerId = role.Id; work.WorkerInstanceId = role.InstanceId;
            work.WorkerGeneration = CurrentLevel.Generation; work.ClaimSerial = 1; role.ClaimSerial = 1;
            work.OperationCell = role.Cell; work.FrozenTicks = work.BaseTicks; work.State = FrontWorkState.Working; work.Progress = 1;
        }

        private DROrderConfig OrderDefinition(int dish = 1, int score = 10)
        { var buffer = new Luban.ByteBuf(); buffer.WriteInt(1); buffer.WriteInt(dish); buffer.WriteInt(score); return new DROrderConfig(buffer); }

        private DRTableConfig TableDefinition(int layout = 11, int seatX = 1)
        { var buffer = new Luban.ByteBuf(); buffer.WriteInt(1); buffer.WriteInt(layout); buffer.WriteInt(seatX); buffer.WriteInt(0); return new DRTableConfig(buffer); }

        private DRCustomerFlowConfig CustomerFlowDefinition(int order = 1)
        {
            var buffer = new Luban.ByteBuf();
            foreach (int value in new[] { 1, 1, 20, 60, 2, 200, 60, 3, 1, 3, 2 }) buffer.WriteInt(value);
            buffer.WriteSize(2); buffer.WriteInt(order); buffer.WriteInt(2);
            return new DRCustomerFlowConfig(buffer);
        }

        [Test]
        public void SoloCustomerTargetsActualConfigAndPersistentTablesHaveUniqueOwners()
        {
            FrontConfig.Validate(Tables.Instance.DTCustomerFlowConfig.DataList, Tables.Instance.DTOrderConfig.DataList, Tables.Instance.DTTableConfig.DataList);
            StartSolo();
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().Parent, Is.SameAs(CurrentLevel));
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().Waiting, Is.Empty);
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty);
            var stations = Grid.Stations.Values.Select(value => (GridStation)value).ToArray();
            Assert.That(stations.Length, Is.EqualTo(16)); Assert.That(Grid.Blocked.Count, Is.EqualTo(17));
            Assert.That(stations.Count(value => value.Kind == GridStationKind.Table), Is.EqualTo(2));
            foreach (var station in stations.Where(value => value.Kind == GridStationKind.Table))
            {
                Assert.That(station.GetComponent<FrontTable>().Parent, Is.SameAs(station));
                Assert.That(station.GetComponent<FrontTable>().State, Is.EqualTo(FrontTableState.Free));
                Assert.That(Grid.FindGridPath(GridStationConfig.Entrance(), station.FrontSeat()), Is.Not.Null);
            }
            var pool = stations.Single(value => value.Kind == GridStationKind.CleanPool);
            Assert.That(pool.PoolItemIds, Is.Empty); Assert.That(pool.ItemSlotCapacity, Is.EqualTo(4));
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(item => item.DefinitionId == 5 || item.DefinitionId == 10), Is.EqualTo(3));
        }

        [TestCase("dish")]
        [TestCase("score")]
        [TestCase("layout")]
        [TestCase("seat")]
        [TestCase("flow")]
        public void SoloCustomerTargetsRejectInvalidReferencesBeforeCreatingWorld(string problem)
        {
            var orders = Tables.Instance.DTOrderConfig.DataList.ToArray(); var tables = Tables.Instance.DTTableConfig.DataList.ToArray();
            var flows = Tables.Instance.DTCustomerFlowConfig.DataList.ToArray();
            if (problem == "dish") orders[0] = OrderDefinition(999);
            if (problem == "score") orders[0] = OrderDefinition(score: 0);
            if (problem == "layout") tables[0] = TableDefinition(13);
            if (problem == "seat") tables[0] = TableDefinition(seatX: 0);
            if (problem == "flow") flows[0] = CustomerFlowDefinition(999);
            Assert.Throws<InvalidOperationException>(() => FrontConfig.Validate(flows, orders, tables));
            Assert.That(host, Is.Null);
        }

        [Test]
        public void SoloCustomerTargetsActualBootstrapRejectsMissingTableBeforeHost()
        {
            var rows = (System.Collections.Generic.List<DRTableConfig>)Tables.Instance.DTTableConfig.DataList;
            DRTableConfig original = rows[0];
            try
            {
                rows[0] = TableDefinition(999);
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null); Assert.That((HostSessionComponent)host.Authority, Is.Null);
            }
            finally { rows[0] = original; }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void SoloCustomerTargetsAskOpensIndependentOrderOnce(int sequence)
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            Customer customer = InitialSeatedCustomer(sequence); FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            long[] items = Grid.Items.Keys.ToArray();
            Assert.That(customer.OrderId, Is.Zero); Assert.That(Grid.CheckFrontTarget(CurrentLevel, work), Is.Null);
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, work), Is.EqualTo("WorkIncomplete"));
            InitialTargetReady(work);
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, work), Is.Null);
            Order order = CurrentLevel.GetChild<Order>(customer.OrderId);
            Assert.That(order.Id, Is.Not.EqualTo(customer.Id)); Assert.That(order.Id, Is.Not.EqualTo(customer.TableStationId));
            Assert.That(order.Parent, Is.SameAs(CurrentLevel)); Assert.That(order.CustomerId, Is.EqualTo(customer.Id));
            Assert.That(order.DishId, Is.EqualTo(sequence)); Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Ordered));
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, work), Is.EqualTo("CustomerChanged"));
            Assert.That(CurrentLevel.Children.Values.OfType<Order>().Count(), Is.EqualTo(1));
            Assert.That(Grid.Items.Keys, Is.EquivalentTo(items)); Assert.That(CurrentLevel.Score, Is.Zero);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty, "Target effect alone is not worker growth");
        }

        [Test]
        public void SoloCustomerTargetsLateAskCannotOpenForNextCustomerAtSameTable()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            Customer first = InitialSeatedCustomer(); FrontWork stale = CurrentLevel.GetChild<FrontWork>(first.AskWorkId);
            first.Phase = CustomerPhase.Gone;
            Customer second = InitialSeatedCustomer(2); InitialTargetReady(stale);
            Assert.That(first.TableStationId, Is.EqualTo(second.TableStationId));
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, stale), Is.EqualTo("CustomerChanged"));
            Assert.That(first.OrderId, Is.Zero); Assert.That(second.OrderId, Is.Zero);
            Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty); Assert.That(CurrentLevel.Score, Is.Zero);
        }

        [Test]
        public void SoloCustomerTargetsClearAndWashTransferSameBowlOnceWithoutGrowthOrStock()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            FrontWork clear = InitialDirtyTable(); GridItem bowl = Grid.Item(clear.ItemId); GridStation table = Grid.Station(clear.StationId);
            long[] items = Grid.Items.Keys.ToArray(); InitialTargetReady(clear);
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, clear), Is.Null);
            Assert.That(table.ItemId, Is.Zero); Assert.That(table.GetComponent<FrontTable>().State, Is.EqualTo(FrontTableState.Free));
            Assert.That(table.GetComponent<FrontTable>().CustomerId, Is.Zero);
            Assert.That(bowl.Location, Is.EqualTo(GridItemLocation.DirtyPool)); Assert.That(bowl.Hygiene, Is.EqualTo(ItemHygiene.Dirty));
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            Assert.That(wash.ItemId, Is.EqualTo(bowl.Id)); Assert.That(wash.Parent, Is.SameAs(CurrentLevel));
            Assert.That(wash.Progress, Is.Zero); Assert.That(wash.BaseTicks, Is.EqualTo(61));
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, clear), Is.EqualTo("TableChanged"));
            InitialTargetReady(wash); Assert.That(Grid.CompleteFrontTarget(CurrentLevel, wash), Is.Null);
            Assert.That(bowl.Location, Is.EqualTo(GridItemLocation.CleanPool)); Assert.That(bowl.Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(Grid.Station(bowl.HolderId).PoolItemIds, Is.EqualTo(new[] { bowl.Id }));
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, wash), Is.EqualTo("ItemChanged"));
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Count(value => value.Kind == FrontWorkKind.Wash), Is.EqualTo(1));
            Assert.That(Grid.Items.Keys, Is.EquivalentTo(items)); Assert.That(CurrentLevel.Score, Is.Zero);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloCustomerTargetsFullDestinationPreservesWorkAndOriginalItem(bool cleanDestination)
        {
            StartSolo(); LevelDo(LevelControlAction.Start); FrontWork clear = InitialDirtyTable();
            GridItem bowl = Grid.Item(clear.ItemId); FrontWork work = clear;
            if (cleanDestination)
            {
                InitialTargetReady(clear); Assert.That(Grid.CompleteFrontTarget(CurrentLevel, clear), Is.Null);
                work = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            }
            GridStation pool = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind ==
                (cleanDestination ? GridStationKind.CleanPool : GridStationKind.DirtyPool));
            GridStation plateSource = PrepStation("盘台"); GridItem plate = Grid.Item(plateSource.ItemId); plateSource.ItemId = 0;
            plate.Location = cleanDestination ? GridItemLocation.CleanPool : GridItemLocation.DirtyPool;
            plate.Hygiene = cleanDestination ? ItemHygiene.Clean : ItemHygiene.Dirty; plate.HolderId = pool.Id; plate.Cell = pool.Cell;
            pool.PoolItemIds.Add(plate.Id); pool.ItemSlotCapacity = 1;
            GridItemLocation location = bowl.Location; long holder = bowl.HolderId; long[] items = Grid.Items.Keys.ToArray();
            InitialTargetReady(work);
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, work), Is.EqualTo("PoolFull"));
            Assert.That(bowl.Location, Is.EqualTo(location)); Assert.That(bowl.HolderId, Is.EqualTo(holder));
            Assert.That(bowl.Hygiene, Is.EqualTo(ItemHygiene.Dirty)); Assert.That(work.BlocksLevelCompletion, Is.True);
            Assert.That(work.Progress, Is.EqualTo(1)); Assert.That(Grid.Items.Keys, Is.EquivalentTo(items));
            Assert.That(pool.PoolItemIds, Is.EqualTo(new[] { plate.Id }));
        }

        [Test]
        public void SoloCustomerTargetsPausedCompletionAndWrongLevelHaveNoEffects()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            Customer customer = InitialSeatedCustomer(); FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            InitialTargetReady(work); LevelDo(LevelControlAction.Pause);
            Assert.That(Grid.CompleteFrontTarget(CurrentLevel, work), Is.EqualTo("Paused"));
            Assert.That(customer.OrderId, Is.Zero); Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty);
            work.LevelGeneration--;
            Assert.That(Grid.CheckFrontTarget(CurrentLevel, work), Is.EqualTo("WrongLevel"));
            Assert.That(customer.OrderId, Is.Zero);
        }

        [Test]
        public void SoloCustomerTargetsLayoutRechecksMovedTableSeatBeforeCommit()
        {
            StartSolo(); GridStation table = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == 11);
            var projection = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            GridCell original = table.Cell; long version = Grid.LayoutVersion;
            using (var receipt = LayoutReply(host.Client, projection.CreateStationLayout(table.Id, new GridCell(4, 3), 0))) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            HostPeer peer = ((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer;
            Assert.That(peer.LastCommandReason, Is.EqualTo("LayoutBlocked"), "Candidate seat would occupy the other table footprint");
            Assert.That(table.Cell, Is.EqualTo(original)); Assert.That(Grid.LayoutVersion, Is.EqualTo(version));
            using (var receipt = LayoutReply(host.Client, projection.CreateStationLayout(table.Id, new GridCell(1, 4), 0))) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(peer.LastCommandStatus, Is.EqualTo(GridResultStatus.Executed), peer.LastCommandReason);
            Assert.That(table.Cell, Is.EqualTo(new GridCell(1, 4))); Assert.That(table.FrontSeat(), Is.EqualTo(new GridCell(2, 4)));
            Assert.That(Grid.LayoutVersion, Is.EqualTo(version + 1));
            Assert.That(Grid.FindGridPath(GridStationConfig.Entrance(), table.FrontSeat()), Is.Not.Null);
        }
    }
}
