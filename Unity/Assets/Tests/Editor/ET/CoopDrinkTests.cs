using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private GridAutoProcess CoopDrinkBegin(ClientSessionComponent remote)
        {
            ClientSessionComponent local = host.Client;
            GridStation orange = PrepStation("橙子台"), chop = PrepStation("切配台"), jugSource = PrepStation("果汁壶台"), juicer = PrepStation("榨汁机");
            long raw = orange.ItemId, jug = jugSource.ItemId;
            PrepDo(local, GridAction.TakeCounter, orange.Id, raw); PrepDo(local, GridAction.PlaceCounter, chop.Id, raw);
            PrepDo(local, GridAction.StartStationWork, chop.Id, raw, raw);
            GridManualProcess cutting = Grid.InputProcess(raw); AdvanceTicks(cutting.RequiredTicks + 1); Pump(30);
            long chunks = cutting.OutputItemId;
            PrepDo(remote, GridAction.TakeCounter, chop.Id, chunks); PrepDo(remote, GridAction.AddToContainer, jugSource.Id, chunks, jug);
            PrepDo(remote, GridAction.TakeCounter, jugSource.Id, jug); PrepDo(remote, GridAction.PlaceCounter, juicer.Id, jug);
            PrepDo(remote, GridAction.StartJuice, juicer.Id, jug, jug);
            return Grid.AutoProcess(Grid.Item(jug).AutoProcessId);
        }

        [Test]
        public void CoopDrinkWrongDishRealKcpFoodOrderKeepsFinishedCupAndScore()
        {
            StartHost(liveDrinks: true); var remote = CoopRemote(); var batch = CoopDrinkBegin(remote);
            long cup = DrinkFirstCup(batch), food = Grid.Item(cup).Contents.Single(); var label = PrepStation("贴单台");
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup); CoopFrontStart(); Order order = WaitActualOrder(1);
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, rejection: "WrongDish", order: order.Id);
            Assert.That(CurrentLevel.DrinkBindings, Is.Empty); Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { food }));
            PrepDo(remote, GridAction.TakeCounter, label.Id, cup); CoopFrontServe(remote, order, cup, "WrongDish");
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { food })); Assert.That(Grid.Item(food), Is.Not.Null);
            Assert.That(order.State, Is.EqualTo(OrderState.Waiting)); Assert.That(CurrentLevel.Score, Is.Zero);
            Assert.That(CurrentLevel.ServedOrders, Is.Zero);
        }

        [Test]
        public void CoopDrinkAtomicRealKcpRebindRejectsOccupiedOrderAndPreservesBothCups()
        {
            DrinkOnlyCustomers(3); StartHost(liveDrinks: true); var remote = CoopRemote(); var batch = CoopDrinkBegin(remote);
            long first = DrinkFirstCup(batch); var label = PrepStation("贴单台");
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, first);
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Purchase, source: 5));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single(); AdvanceTicks(shipment.RequiredTicks + 1); Pump(30);
            CoopSupplyDo(remote, ReceiveIntent(shipment)); var receiver = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiver.ItemId); long second = package.Contents.First(); CoopSupplyDo(remote, PortionIntent(receiver, package.Id, second));
            PrepDo(remote, GridAction.PortionJuice, PrepStation("榨汁机").Id, second, batch.PotItemId, batch.Id);
            PrepDo(remote, GridAction.PlaceCounter, PrepStation("果汁壶台").Id, second);
            CoopFrontStart(); FrontWait(() => CurrentLevel.Children.Values.OfType<Order>().Count(order => order.State == OrderState.Waiting) == 2, 400);
            Order[] orders = CurrentLevel.Children.Values.OfType<Order>().Where(order => order.State == OrderState.Waiting).OrderBy(order => order.Id).ToArray();
            PrepDo(remote, GridAction.BindDrink, label.Id, first, first, order: orders[0].Id);
            PrepDo(remote, GridAction.BindDrink, label.Id, first, first, order: orders[1].Id);
            Assert.That(CurrentLevel.DrinkBindings[first], Is.EqualTo(orders[1].Id));
            PrepDo(remote, GridAction.TakeCounter, label.Id, first); PrepDo(remote, GridAction.PlaceCounter, PrepStation("杯台").Id, first);
            PrepDo(host.Client, GridAction.TakeCounter, PrepStation("果汁壶台").Id, second); PrepDo(host.Client, GridAction.PlaceCounter, label.Id, second);
            PrepDo(host.Client, GridAction.BindDrink, label.Id, second, second, order: orders[0].Id);
            PrepDo(remote, GridAction.BindDrink, label.Id, second, second, rejection: "OrderAlreadyBound", order: orders[1].Id);
            Assert.That(CurrentLevel.DrinkBindings[first], Is.EqualTo(orders[1].Id)); Assert.That(CurrentLevel.DrinkBindings[second], Is.EqualTo(orders[0].Id));
            Assert.That(Grid.Item(first).Contents.Count, Is.EqualTo(1)); Assert.That(Grid.Item(second).Contents.Count, Is.EqualTo(1));
        }

        [Test]
        public void CoopDrinkRealKcpLocksJugCopiesPortionsTransfersCupAndResumesEmptyHanded()
        {
            StartHost(liveDrinks: true); var remote = CoopRemote(); var batch = CoopDrinkBegin(remote);
            long jug = batch.PotItemId; var juicer = PrepStation("榨汁机"); Pump(30);
            var view = remote.GetComponent<GridClientComponent>();
            Assert.That(view.AutoProcesses[batch.Id].LockContainer, Is.True);
            Assert.That(view.AutoProcesses[batch.Id].RequiredCapability, Is.EqualTo(GridStationCapability.Juice));
            PrepDo(remote, GridAction.TakeCounter, juicer.Id, jug, rejection: "ProcessLocked");
            AdvanceTicks(batch.RequiredTicks + 1); Pump(30);
            PrepDo(remote, GridAction.TakeCounter, juicer.Id, jug); PrepDo(remote, GridAction.PlaceCounter, PrepStation("果汁壶台").Id, jug);
            long cup = PrepStation("杯台").ItemId; PrepDo(host.Client, GridAction.TakeCounter, PrepStation("杯台").Id, cup);
            PrepDo(host.Client, GridAction.PortionJuice, PrepStation("果汁壶台").Id, cup, jug, batch.Id);
            PrepDo(host.Client, GridAction.PlaceCounter, PrepStation("交接台").Id, cup);
            PrepDo(remote, GridAction.TakeCounter, PrepStation("交接台").Id, cup); Pump(30);
            Assert.That(view.AutoProcesses[batch.Id].RemainingPortions, Is.EqualTo(1));
            long[] contents = Grid.Item(cup).Contents.ToArray(); Assert.That(view.Items[cup].Contents, Is.EqualTo(contents));
            int member = remote.MemberId; ProductRuntimeComponent runtime = remote.Runtime;
            ((Session)remote.Session).Dispose(); Pump(240);
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(contents)); Assert.That(Grid.Item(cup).Location, Is.EqualTo(GridItemLocation.Ground));
            PrepDo(host.Client, GridAction.Move, cell: Grid.Item(cup).Cell); PrepDo(host.Client, GridAction.PickupDrop, cup, cup);
            var recovering = runtime.ResumeProduct(); PumpUntil(() => recovering.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending, "drink member Resume", 500, true);
            recovering.GetAwaiter().GetResult(); Pump(30);
            Assert.That(Grid.Actor(member).HandItemId, Is.Zero); Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).HandItemId, Is.EqualTo(cup));
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(contents));
        }

        [Test]
        public void CoopDrinkRealKcpLastPortionRaceConsumesOneAndKeepsLosingCup()
        {
            StartHost(liveDrinks: true); var remote = CoopRemote(); var batch = CoopDrinkBegin(remote);
            long full = DrinkFirstCup(batch); PrepDo(host.Client, GridAction.PlaceCounter, PrepStation("杯台").Id, full);
            long localCup = SoupInitialItem(GridKitchenRules.DisposableCup, PrepStation("橙子台")).Id;
            PrepDo(host.Client, GridAction.TakeCounter, PrepStation("橙子台").Id, localCup);
            long command = CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Purchase, source: 5));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
            FrontWait(() => shipment.Stage == ShipmentStage.Ready, 300);
            CoopSupplyDo(remote, ReceiveIntent(shipment)); var receiver = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiver.ItemId); long remoteCup = package.Contents.First(); CoopSupplyDo(remote, PortionIntent(receiver, package.Id, remoteCup));
            PrepDo(host.Client, GridAction.Move, cell: new GridCell(1, 9)); PrepDo(remote, GridAction.Move, cell: new GridCell(1, 9));
            var station = PrepStation("榨汁机"); long jug = batch.PotItemId, process = batch.Id;
            var first = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionJuice, target: station.Id, expected: localCup, expectedTarget: jug, process: process);
            var second = remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionJuice, target: station.Id, expected: remoteCup, expectedTarget: jug, process: process);
            var firstCall = ((ClientSessionComponent)host.Client).CallProduct(first); var secondCall = remote.CallProduct(second);
            using (var reply = (GridCommandResponse)Finish(firstCall, 500)) Assert.That(reply.Error, Is.Zero);
            using (var reply = (GridCommandResponse)Finish(secondCall, 500)) Assert.That(reply.Error, Is.Zero);
            Pump(350);
            Assert.That(Grid.Item(localCup).Contents.Count + Grid.Item(remoteCup).Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.AutoProcess(process), Is.Null); Assert.That(Grid.Item(jug).AutoProcessId, Is.Zero);
            Assert.That(Grid.Item(full).Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.Actor(remote.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.Executed).Or.EqualTo(GridResultStatus.Rejected));
        }

        [Test]
        public void CoopDrinkRealKcpAtomicBindingWrongOrderKeepsCupAndRemoteServesOriginalContents()
        {
            DrinkOnlyCustomers(3, interval: 100000); StartHost(liveDrinks: true); var remote = CoopRemote(); var batch = CoopDrinkBegin(remote);
            long cup = DrinkFirstCup(batch), food = Grid.Item(cup).Contents.Single();
            var label = PrepStation("贴单台"); PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup);
            CoopFrontStart(); Order order = WaitActualOrder(3);
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, order: order.Id); Pump(30);
            Assert.That(remote.GetComponent<GridClientComponent>().Items[cup].BoundOrderId, Is.EqualTo(order.Id));
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, rejection: "OrderEnded", order: long.MaxValue);
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(order.Id)); Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { food }));
            PrepDo(remote, GridAction.UnbindDrink, label.Id, cup, cup, order: order.Id);
            Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, order: order.Id);
            Match match = ((HostSessionComponent)host.Authority).Match;
            using var oldSnapshot = GridBroadcast.CreateSnapshot(Grid, match.GetChild<Participant>(remote.MemberId).BoundPeer);
            PrepDo(remote, GridAction.TakeCounter, label.Id, cup); CoopFrontServe(remote, order, cup);
            Assert.That(Grid.Item(cup), Is.Null); Assert.That(Grid.Item(food), Is.Null); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
            Customer served = CurrentLevel.GetChild<Customer>(order.CustomerId);
            FrontWait(() => served.Phase == CustomerPhase.Gone, 500);
            FrontWait(() => CurrentLevel.Children.Values.OfType<FrontWork>().Any(work => work.CustomerId == served.Id && work.Kind == FrontWorkKind.Clear), 20);
            FrontWork clear = CurrentLevel.Children.Values.OfType<FrontWork>().Single(work => work.CustomerId == served.Id && work.Kind == FrontWorkKind.Clear);
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed, 500);
            PrepDo(remote, GridAction.Move, cell: new GridCell(1, 10));
            long oldLevel = CurrentLevel.Id;
            FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 500);
            FrontWait(() => Renovation.Prepared, 8);
            Assert.That(CurrentLevel.Stars, Is.EqualTo(1));
            LevelDo(LevelControlAction.Continue); Pump(30);
            var view = remote.GetComponent<GridClientComponent>(); long version = view.Version;
            oldSnapshot.Version = long.MaxValue; view.ApplyGridSnapshot(oldSnapshot);
            Assert.That(view.LevelId, Is.Not.EqualTo(oldLevel)); Assert.That(view.Version, Is.EqualTo(version));
            Assert.That(view.Items.ContainsKey(cup), Is.False); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
        }
    }
}
