using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void FirstDrinkConfigAllowsPerStageMenuBeforeWorldCreation()
        {
            Tables.Instance.DTLevelConfig.Get(202).AllowedDrinkIds.Clear();
            Assert.DoesNotThrow(() => RestaurantConfig.ValidateLevelDefinitions(Tables.Instance.DTLevelConfig.DataList, GridKitchenSystem.IsInitialGridWalkable));
            Assert.That(host, Is.Null); Assert.That(fibers, Is.Empty);
        }

        [TestCase(4)]
        [TestCase(5)]
        public void FirstDrinkHostRejectsSoloSupplyWithoutMintingMaterialOrDecrementingStock(int source)
        {
            StartHost(); int items = Grid.Items.Count;
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: source), "InvalidSupply");
            Assert.That(Grid.Items.Count, Is.EqualTo(items)); Assert.That(Supply.RemainingPackages.Keys, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(Supply.RemainingPackages.Values.All(value => value == 5), Is.True);
            Assert.That(Supply.Children.Values.OfType<SupplyShipment>(), Is.Empty);
        }

        [Test]
        public void FirstDrinkWrongDishBindingOrServingKeepsCupCurrentOrderAndScore()
        {
            StartSolo(liveFront: true, liveDrinks: true); var batch = DrinkBegin(); long cup = DrinkFirstCup(batch);
            LevelDo(LevelControlAction.Start); Order foodOrder = WaitActualOrder(1); long food = Grid.Item(cup).Contents.Single();
            ServeDo(foodOrder, cup, "WrongDish");
            var label = PrepStation("贴单台"); PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, rejection: "WrongDish", order: foodOrder.Id);
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { food })); Assert.That(Grid.Item(food), Is.Not.Null);
            Assert.That(foodOrder.State, Is.EqualTo(OrderState.Waiting)); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.ServedOrders, Is.Zero);
        }

        private GridAutoProcess DrinkBegin()
        {
            ClientSessionComponent client = host.Client;
            GridStation orange = PrepStation("橙子台"), chop = PrepStation("切配台"), jugSource = PrepStation("果汁壶台"), juicer = PrepStation("榨汁机");
            long raw = orange.ItemId, jug = jugSource.ItemId;
            PrepDo(client, GridAction.TakeCounter, orange.Id, raw);
            PrepDo(client, GridAction.PlaceCounter, chop.Id, raw);
            PrepDo(client, GridAction.StartStationWork, chop.Id, raw, raw);
            GridManualProcess cutting = Grid.InputProcess(raw);
            AdvanceTicks(cutting.RequiredTicks + 1); PumpMessages(8);
            Assert.That(cutting.State, Is.EqualTo(GridWorkState.Completed)); Assert.That(Grid.Item(raw), Is.Null);
            long chunks = cutting.OutputItemId;
            PrepDo(client, GridAction.TakeCounter, chop.Id, chunks);
            PrepDo(client, GridAction.AddToContainer, jugSource.Id, chunks, jug);
            PrepDo(client, GridAction.TakeCounter, jugSource.Id, jug);
            PrepDo(client, GridAction.PlaceCounter, juicer.Id, jug);
            PrepDo(client, GridAction.StartJuice, juicer.Id, jug, jug);
            GridAutoProcess batch = Grid.AutoProcess(Grid.Item(jug).AutoProcessId);
            Assert.That(batch.LockContainer, Is.True); Assert.That(batch.RequiredCapability, Is.EqualTo(GridStationCapability.Juice));
            Assert.That(batch.TotalPortions, Is.EqualTo(2)); Assert.That(batch.RequiredTicks, Is.EqualTo(120));
            return batch;
        }

        private long DrinkFirstCup(GridAutoProcess batch)
        {
            AdvanceTicks(batch.RequiredTicks + 1); PumpMessages(8);
            Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready));
            GridStation cupSource = PrepStation("杯台"), juicer = PrepStation("榨汁机"); long cup = cupSource.ItemId;
            PrepDo(host.Client, GridAction.TakeCounter, cupSource.Id, cup);
            PrepDo(host.Client, GridAction.PortionJuice, juicer.Id, cup, batch.PotItemId, batch.Id);
            Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(3)); Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            return cup;
        }

        private void DrinkOnlyCustomers(int levelConfig = 201, int interval = 100)
        {
            var rows = (System.Collections.Generic.List<DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataList;
            var map = (System.Collections.Generic.Dictionary<int, DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataMap;
            var original = rows.Single(row => row.LevelConfigId == levelConfig); var buffer = new Luban.ByteBuf();
            foreach (int value in new[] { original.Id, original.LevelConfigId, 20, interval, original.QueueCapacity, 1200,
                original.EatTicks, original.EntranceX, original.EntranceY, original.QueueX, original.QueueY }) buffer.WriteInt(value);
            buffer.WriteSize(1); buffer.WriteInt(3);
            var replacement = new DRCustomerFlowConfig(buffer); rows[rows.IndexOf(original)] = replacement; map[replacement.Id] = replacement;
        }

        [Test]
        public void FirstDrinkConfigValidatesClosedMenuFiniteCupsAndKeepsLegacyWorld()
        {
            GridDishConfig.ValidateAllMenus();
            GridKitchenSystem.ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridManualRecipeConfig.DataList);
            GridKitchenSystem.ValidateGridSoupDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridAutoRecipeConfig.DataList);
            StartSolo(liveDrinks: true);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(201)); Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(Grid.Stations.Count, Is.EqualTo(21)); Assert.That(Grid.Items.Count, Is.EqualTo(10)); Assert.That(Supply.RemainingPackages.Count, Is.EqualTo(5));
            Assert.That(PrepStation("榨汁机").Capabilities, Is.EqualTo(GridStationCapability.Juice));
            Assert.That(PrepStation("贴单台").Capabilities, Is.EqualTo(GridStationCapability.LabelDrink));
            Assert.That(Tables.Instance.DTLevelConfig.Get(101).AllowedDrinkIds, Is.Empty);
            Assert.That(Tables.Instance.DTLevelConfig.Get(203).NextLevelConfigId, Is.Zero);
        }

        [Test]
        public void FirstDrinkRealBatchLocksJugAndMachinePausesThenMovesAndConsumesLastPortionOnce()
        {
            StartSolo(liveDrinks: true); ClientSessionComponent client = host.Client; var batch = DrinkBegin();
            GridStation juicer = PrepStation("榨汁机"), jugSource = PrepStation("果汁壶台"); long jug = batch.PotItemId;
            long[] input = batch.InputItemIds.ToArray();
            PrepDo(client, GridAction.TakeCounter, juicer.Id, jug, rejection: "ProcessLocked");
            Assert.That(juicer.ItemId, Is.EqualTo(jug)); Assert.That(Grid.Item(jug).Contents, Is.EqualTo(input));
            using (var reply = (StationLayoutResponse)FinishMessages(client.CallProduct(SupplyView.CreateStationLayout(juicer.Id, juicer.Cell, juicer.Orientation)), "locked juice station receipt"))
                Assert.That(reply.Error, Is.Zero, reply.Message);
            AdvanceTicks(1); PumpMessages(8); Assert.That(SupplyPeer.LastCommandReason, Is.EqualTo("ProcessLocked"));
            LevelDo(LevelControlAction.Pause); int progress = batch.ProgressTicks; AdvanceTicks(30); Assert.That(batch.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(121); PumpMessages(8);
            PrepDo(client, GridAction.TakeCounter, juicer.Id, jug); PrepDo(client, GridAction.PlaceCounter, jugSource.Id, jug);
            Assert.That(batch.RemainingPortions, Is.EqualTo(2)); Assert.That(juicer.AutoProcessId, Is.Zero);
            GridStation cupSource = PrepStation("杯台"); long firstCup = cupSource.ItemId;
            PrepDo(client, GridAction.TakeCounter, cupSource.Id, firstCup);
            PrepDo(client, GridAction.PortionJuice, jugSource.Id, firstCup, jug, batch.Id);
            PrepDo(client, GridAction.PortionJuice, jugSource.Id, firstCup, jug, batch.Id, rejection: "ContainerFull");
            Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            PrepDo(client, GridAction.PlaceCounter, cupSource.Id, firstCup);
            var shipment = SupplyOrderReady(5); SupplyDo(ReceiveIntent(shipment)); var receiver = Grid.Station(shipment.ReceivingStationId);
            GridItem package = Grid.Item(receiver.ItemId); long lastCup = package.Contents.First();
            SupplyDo(PortionIntent(receiver, package.Id, lastCup));
            Assert.That(Grid.Item(lastCup).Contents, Is.Empty); Assert.That(Supply.RemainingPackages[5], Is.EqualTo(4));
            long batchId = batch.Id;
            PrepDo(client, GridAction.PortionJuice, jugSource.Id, lastCup, jug, batchId);
            Assert.That(Grid.AutoProcess(batchId), Is.Null); Assert.That(Grid.Item(jug).Contents, Is.Empty);
            Assert.That(input.All(id => Grid.Item(id) == null), Is.True); Assert.That(Grid.Item(jug).AutoProcessId, Is.Zero);
            PrepDo(client, GridAction.PortionJuice, jugSource.Id, lastCup, jug, batchId, rejection: "ProcessChanged");
            Assert.That(Grid.Item(lastCup).Contents.Count, Is.EqualTo(1)); Assert.That(Grid.IdentifyDish(firstCup), Is.EqualTo(3));
        }

        [Test]
        public void FirstDrinkRealBindingRejectsInvalidRebindAndUnboundServeConsumesCupAndClearsWithoutWash()
        {
            DrinkOnlyCustomers(); StartSolo(liveFront: true, liveDrinks: true); var batch = DrinkBegin(); long cup = DrinkFirstCup(batch);
            LevelDo(LevelControlAction.Start); Order order = WaitActualOrder(3); long food = Grid.Item(cup).Contents.Single();
            ServeDo(order, cup, "DrinkNotBound"); Assert.That(Grid.Item(food), Is.Not.Null); Assert.That(CurrentLevel.Score, Is.Zero);
            var label = PrepStation("贴单台");
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, order: order.Id);
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(order.Id)); Assert.That(SupplyView.Items[cup].BoundOrderId, Is.EqualTo(order.Id));
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, rejection: "OrderEnded", order: long.MaxValue);
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(order.Id));
            PrepDo(host.Client, GridAction.UnbindDrink, label.Id, cup, cup, order: order.Id);
            Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, order: order.Id);
            PrepDo(host.Client, GridAction.TakeCounter, label.Id, cup); ServeDo(order, cup);
            Assert.That(Grid.Item(cup), Is.Null); Assert.That(Grid.Item(food), Is.Null); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1)); Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score));
            ServeDo(order, cup, "OrderEnded", move: false); Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score));
            Customer customer = CurrentLevel.GetChild<Customer>(order.CustomerId); GridStation table = Grid.Station(order.TableStationId);
            FrontWait(() => customer.Phase == CustomerPhase.Gone, 1200);
            FrontWait(() => CurrentLevel.Children.Values.OfType<FrontWork>().Any(w => w.CustomerId == customer.Id && w.Kind == FrontWorkKind.Clear), 30);
            var clear = CurrentLevel.Children.Values.OfType<FrontWork>().Single(w => w.CustomerId == customer.Id && w.Kind == FrontWorkKind.Clear);
            Assert.That(clear.ItemId, Is.Zero);
            FrontPlayerDo(FrontWorkAction.Claim, clear);
            FrontWait(() => clear.State == FrontWorkState.Completed, 1200);
            Assert.That(table.GetComponent<FrontTable>().State, Is.EqualTo(FrontTableState.Free));
            Assert.That(clear.SuccessorPending, Is.False);
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Any(w => w.Kind == FrontWorkKind.Wash && w.ItemId == cup), Is.False);
            Assert.That(Grid.Stations.Values.Select(s => (GridStation)s).All(s => !s.PoolItemIds.Contains(cup)), Is.True);
        }

        [Test]
        public void FirstDrinkActualFileRoundtripCarriesHalfBatchWithFrozenLockAndNoOldBindings()
        {
            StartSolo(liveDrinks: true); var batch = DrinkBegin(); long cup = DrinkFirstCup(batch); long[] originalInputs = batch.InputItemIds.ToArray();
            RenovationComplete(); UpgradeChoose(3, PrepStation("切配台"));
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); var repository = TestSaveRepository(out string directory);
            try
            {
                string path = Path.Combine(directory, "drink-roundtrip.ccsave"); File.WriteAllBytes(path, SoloSaveFile.Encode(header, payload));
                using var stream = File.OpenRead(path); var copy = SoloSaveFile.ReadPayload(stream, SaveProfileId, out var copiedHeader);
                Assert.That(copiedHeader.Version, Is.EqualTo(4)); Assert.That(copy.Batches.Single().RemainingPortions, Is.EqualTo(1));
                Assert.That(copy.Batches.Single().RequiredTicks, Is.EqualTo(120)); Assert.That(copy.Batches.Single().LockContainer, Is.True);
                host.StopProduct(); StartSavedFixture(copiedHeader, copy); Pump(8);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(202)); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
                var restored = Grid.AutoProcesses.Values.Select(p => (GridAutoProcess)p).Single();
                Assert.That(restored.RemainingPortions, Is.EqualTo(1)); Assert.That(restored.RequiredTicks, Is.EqualTo(120));
                Assert.That(restored.InputItemIds.Count, Is.EqualTo(originalInputs.Length)); Assert.That(restored.LockContainer, Is.True);
                Assert.That(Grid.Items.Values.Select(i => (GridItem)i).Single(i => i.DefinitionId == GridKitchenRules.DisposableCup).Contents.Count, Is.EqualTo(1));
            }
            finally { RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void FirstDrinkTwoCupsRejectOccupiedOrderSwapAtomicallyAndReleaseOnDrainAndActualExpiry()
        {
            DrinkOnlyCustomers(); StartSolo(liveFront: true, liveDrinks: true); var batch = DrinkBegin(); long first = DrinkFirstCup(batch);
            GridStation cupSource = PrepStation("杯台"), label = PrepStation("贴单台"), jugSource = PrepStation("果汁壶台"), juicer = PrepStation("榨汁机");
            PrepDo(host.Client, GridAction.PlaceCounter, cupSource.Id, first);
            var shipment = SupplyOrderReady(5); SupplyDo(ReceiveIntent(shipment)); var receiver = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiver.ItemId); long second = package.Contents.First(); SupplyDo(PortionIntent(receiver, package.Id, second));
            PrepDo(host.Client, GridAction.PortionJuice, juicer.Id, second, batch.PotItemId, batch.Id);
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, second);
            LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Order>().Count(o => o.State == OrderState.Waiting) == 2, 400);
            var orders = CurrentLevel.Children.Values.OfType<Order>().Where(o => o.State == OrderState.Waiting).OrderBy(o => o.Id).ToArray();
            PrepDo(host.Client, GridAction.BindDrink, label.Id, second, second, order: orders[0].Id);
            PrepDo(host.Client, GridAction.TakeCounter, label.Id, second); PrepDo(host.Client, GridAction.PlaceCounter, jugSource.Id, second);
            PrepDo(host.Client, GridAction.TakeCounter, cupSource.Id, first); PrepDo(host.Client, GridAction.PlaceCounter, label.Id, first);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, first, first, rejection: "OrderAlreadyBound", order: orders[0].Id);
            Assert.That(CurrentLevel.DrinkBindings.Count, Is.EqualTo(1)); Assert.That(CurrentLevel.DrinkBindings[second], Is.EqualTo(orders[0].Id));
            PrepDo(host.Client, GridAction.BindDrink, label.Id, first, first, order: orders[1].Id);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, first, first, rejection: "OrderAlreadyBound", order: orders[0].Id);
            Assert.That(CurrentLevel.DrinkBindings[first], Is.EqualTo(orders[1].Id));
            PrepDo(host.Client, GridAction.TakeCounter, jugSource.Id, second);
            CleanupDo(host.Client, GridAction.DrainLiquid, PrepStation("水池／洗碗台"));
            Assert.That(CurrentLevel.DrinkBindings.ContainsKey(second), Is.False); Assert.That(Grid.Item(second).Contents, Is.Empty);
            PrepDo(host.Client, GridAction.PlaceCounter, jugSource.Id, second);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, first, first, order: orders[0].Id);
            Assert.That(CurrentLevel.DrinkBindings.Values, Is.EqualTo(new[] { orders[0].Id }));
            Assert.That(SupplyView.Items[first].BoundOrderId, Is.EqualTo(orders[0].Id));
            FrontWait(() => orders[0].State == OrderState.Expired, 1600);
            Assert.That(CurrentLevel.DrinkBindings, Is.Empty); Assert.That(SupplyView.Items[first].BoundOrderId, Is.Zero);
            Assert.That(Grid.IdentifyDish(first), Is.EqualTo(3)); Assert.That(CurrentLevel.Score, Is.Zero);
        }

        [Test]
        public void FirstDrinkUnfinishedBatchActualFileRestoresFrozenProgressAndPhysicalMachineLock()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(201), duration: 10));
            StartSolo(liveDrinks: true); var batch = DrinkBegin(); long oldId = batch.Id;
            RenovationComplete(); UpgradeChoose(3, PrepStation("切配台"));
            Assert.That(batch.State, Is.EqualTo(GridAutoState.Cooking)); int frozen = batch.ProgressTicks;
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); var repository = TestSaveRepository(out string directory);
            try
            {
                string path = Path.Combine(directory, "drink-working.ccsave"); File.WriteAllBytes(path, SoloSaveFile.Encode(header, payload));
                using var stream = File.OpenRead(path); var copy = SoloSaveFile.ReadPayload(stream, SaveProfileId, out var copiedHeader);
                host.StopProduct(); StartSavedFixture(copiedHeader, copy); PumpMessages(8);
                var restored = Grid.AutoProcesses.Values.Select(p => (GridAutoProcess)p).Single();
                Assert.That(restored.Id, Is.Not.EqualTo(oldId)); Assert.That(restored.ProgressTicks, Is.EqualTo(frozen));
                Assert.That(restored.RequiredTicks, Is.EqualTo(120)); Assert.That(restored.LockContainer, Is.True);
                var juicer = PrepStation("榨汁机");
                Assert.That(juicer.ItemId, Is.EqualTo(restored.PotItemId)); Assert.That(juicer.AutoProcessId, Is.EqualTo(restored.Id));
                Pump(8);
                PrepDo(host.Client, GridAction.TakeCounter, juicer.Id, juicer.ItemId, rejection: "ProcessLocked");
                AdvanceTicks(121); PumpMessages(8); Assert.That(restored.State, Is.EqualTo(GridAutoState.Ready));
                PrepDo(host.Client, GridAction.TakeCounter, juicer.Id, restored.PotItemId);
            }
            finally { RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
