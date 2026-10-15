using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void PermissionSupply(ClientSessionComponent client, SupplyIntent intent, string rejection = null)
        {
            var view = client.GetComponent<GridClientComponent>();
            var request = view.CreateSupplyCommand(intent); long command = request.CommandId;
            using (var reply = (SupplyCommandResponse)Finish(client.CallProduct(request), 500))
            {
                Assert.That(reply.Error, Is.Zero, reply.Message);
            }
            for (int step = 0; step < 350 && (view.LastCommandId != command || view.LastStatus is GridResultStatus.Accepted or GridResultStatus.PendingApproach); ++step)
            {
                AdvanceTicks(1); Pump(3);
            }
            Assert.That(view.LastCommandId, Is.EqualTo(command));
            Assert.That(view.LastStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), view.LastReason);
            Assert.That(view.LastReason, Is.EqualTo(rejection));
        }

        private SupplyShipment PermissionOrder(ClientSessionComponent client, int source)
        {
            PermissionSupply(client, new SupplyIntent(SupplyAction.Purchase, source: source));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().OrderBy(value => value.Id).Last();
            AdvanceTicks(shipment.RequiredTicks + 1); Pump(12);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
            return shipment;
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantPermissionActions")]
        public void RestaurantPermissionsLocalAndRealKcpPreserveOldWorkCargoAndItemsWhileRejectingNewProduction(bool useRemote)
        {
            StartGrowth(true);
            var client = useRemote ? CoopRemote() : (ClientSessionComponent)host.Client;
            var view = client.GetComponent<GridClientComponent>();
            string restricted = nameof(ProductionPermissionDecision.MenuRestricted);
            PermissionSupply(client, new SupplyIntent(SupplyAction.Purchase, source: 4), restricted);
            Assert.That(Supply.RemainingPackages[4], Is.EqualTo(5));
            var orange = PrepStation("橙子台"); var chop = PrepStation("切配台"); long orangeId = orange.ItemId;
            PrepDo(client, GridAction.TakeCounter, orange.Id, orangeId);
            PrepDo(client, GridAction.PlaceCounter, chop.Id, orangeId);
            PrepDo(client, GridAction.StartStationWork, chop.Id, orangeId, orangeId, rejection: restricted);
            Assert.That(Grid.InputProcess(orangeId), Is.Null);
            Assert.That(chop.ItemId, Is.EqualTo(orangeId));
            PrepDo(client, GridAction.TakeCounter, chop.Id, orangeId); PrepDo(client, GridAction.PlaceCounter, orange.Id, orangeId);
            RenovationComplete(); RenovationChoose(7); LevelDo(LevelControlAction.Continue); Pump(20);
            Assert.That(view.AllowedDishIds, Does.Contain(3));
            Assert.That(view.UnlockedDishIds, Does.Contain(3));
            Assert.That(view.UnlockedStationIds, Does.Contain(10));

            long pot = SoupFillPot(client);
            PrepDo(client, GridAction.StartAutoWork, PrepStation("灶台").Id, pot, pot);
            var soup = Grid.AutoProcess(Grid.Item(pot).AutoProcessId);
            Assert.That(soup, Is.Not.Null);
            AdvanceTicks(soup.RequiredTicks + 2); Pump(12);
            Assert.That(soup.State, Is.EqualTo(GridAutoState.Ready));
            var eggShipment = PermissionOrder(client, 2);
            PermissionSupply(client, ReceiveIntent(eggShipment));
            var receiving = Grid.Station(eggShipment.ReceivingStationId);
            var eggPackage = Grid.Item(receiving.ItemId); long egg = eggPackage.Contents[0];
            PermissionSupply(client, PortionIntent(receiving, eggPackage.Id, egg));
            var bowlStation = PrepStation("碗台"); long bowl = bowlStation.ItemId;
            PrepDo(client, GridAction.AddToContainer, bowlStation.Id, egg, bowl);
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
            var paused = Grid.InputProcess(egg); PrepTick(5);
            PrepDo(client, GridAction.StopWork, input: egg, expectedTarget: bowl, process: paused.Id);
            PrepDo(client, GridAction.PlaceCounter, bowlStation.Id, bowl);
            var food = PrepStation("食材台");
            PrepDo(client, GridAction.TakeCounter, receiving.Id, eggPackage.Id);
            PrepDo(client, GridAction.PlaceCounter, food.Id, eggPackage.Id);
            var oldCargo = PermissionOrder(client, 2);
            int cargoStock = Supply.RemainingPackages[2];

            // 已解锁阶段准备真实未启动的榨汁与烘焙输入，缩小时不能以旧原料绕过新启动限制。
            var jugStation = PrepStation("果汁壶台"); long jug = jugStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, orange.Id, orangeId); PrepDo(client, GridAction.PlaceCounter, chop.Id, orangeId);
            PrepDo(client, GridAction.StartStationWork, chop.Id, orangeId, orangeId);
            var orangeWork = Grid.InputProcess(orangeId); PrepTick(orangeWork.RequiredTicks + 2); Pump(12);
            long chunks = orangeWork.OutputItemId;
            PrepDo(client, GridAction.TakeCounter, chop.Id, chunks); PrepDo(client, GridAction.AddToContainer, jugStation.Id, chunks, jug);
            PrepDo(client, GridAction.TakeCounter, jugStation.Id, jug);
            var juicer = PrepStation("榨汁机"); PrepDo(client, GridAction.PlaceCounter, juicer.Id, jug);
            var breadStation = PrepStation("面包台"); long bread = breadStation.ItemId; var oven = PrepStation("烤箱");
            PrepDo(client, GridAction.TakeCounter, breadStation.Id, bread); PrepDo(client, GridAction.PlaceCounter, oven.Id, bread);

            var tomatoShipment = PermissionOrder(client, 1); PermissionSupply(client, ReceiveIntent(tomatoShipment));
            var tomatoPackage = Grid.Item(receiving.ItemId); long raw = tomatoPackage.Contents[0];
            PermissionSupply(client, PortionIntent(receiving, tomatoPackage.Id, raw));
            var tomatoStation = PrepStation("备料台"); PrepDo(client, GridAction.PlaceCounter, tomatoStation.Id, raw);
            PrepDo(client, GridAction.TakeCounter, receiving.Id, tomatoPackage.Id); PrepDo(client, GridAction.PlaceCounter, breadStation.Id, tomatoPackage.Id);
            AssemblyChopIntoPlate(client, "备料台"); AssemblyChopIntoPlate(client, "生菜台"); AssemblyChopIntoPlate(client, "黄瓜台");
            var plateStation = PrepStation("盘台"); long plate = plateStation.ItemId; var cheeseStation = PrepStation("芝士台"); long cheese = cheeseStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, cheeseStation.Id, cheese); PrepDo(client, GridAction.AddToContainer, plateStation.Id, cheese, plate);
            int progress = paused.ProgressTicks;
            var oldItems = Grid.Items.Keys.OrderBy(id => id).ToArray();
            RenovationComplete(); LevelDo(LevelControlAction.Continue); Pump(20);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(803));
            Assert.That(view.AllowedDishIds, Is.EqualTo(new[] { 4 }));
            Assert.That(view.UnlockedDishIds, Does.Contain(3));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(oldItems));
            Assert.That(paused.ProgressTicks, Is.EqualTo(progress));
            Assert.That(oldCargo.Stage, Is.EqualTo(ShipmentStage.Ready));
            PermissionSupply(client, new SupplyIntent(SupplyAction.Purchase, source: 2), restricted);
            PermissionSupply(client, new SupplyIntent(SupplyAction.Purchase, source: 4), restricted);
            Assert.That(Supply.RemainingPackages[2], Is.EqualTo(cargoStock));
            PermissionSupply(client, ReceiveIntent(oldCargo));
            Assert.That(oldCargo.Stage, Is.EqualTo(ShipmentStage.Received));
            Assert.That(Supply.RemainingPackages[2], Is.EqualTo(cargoStock));
            var received = Grid.Item(receiving.ItemId);
            Assert.That(received.Contents.Count, Is.EqualTo(2));
            Assert.That(received.Contents.All(id => Grid.Item(id).DefinitionId == GridKitchenRules.Egg), Is.True);

            var secondBowl = PrepStation("第二碗台"); long empty = secondBowl.ItemId; var stove = PrepStation("灶台");
            int portions = soup.RemainingPortions; var potInputs = Grid.Item(pot).Contents.ToArray();
            PrepDo(client, GridAction.TakeCounter, secondBowl.Id, empty);
            PrepDo(client, GridAction.PortionSoup, stove.Id, empty, pot, soup.Id, rejection: restricted);
            Assert.That(soup.RemainingPortions, Is.EqualTo(portions)); Assert.That(Grid.Item(pot).Contents, Is.EqualTo(potInputs));
            Assert.That(Grid.Item(empty).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, secondBowl.Id, empty);
            PrepDo(client, GridAction.StartJuice, juicer.Id, jug, jug, rejection: restricted);
            Assert.That(Grid.Item(jug).Contents, Is.EqualTo(new[] { chunks })); Assert.That(Grid.Item(jug).AutoProcessId, Is.Zero);
            PrepDo(client, GridAction.StartBake, oven.Id, bread, bread, rejection: restricted);
            Assert.That(oven.ItemId, Is.EqualTo(bread)); Assert.That(oven.BakeProcessId, Is.Zero);
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl, paused.Id);
            PrepTick(paused.RequiredTicks + 2); Pump(12);
            Assert.That(paused.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { paused.OutputItemId }));
            ExtractionFinish(client, view.CreateGridCommand(view.CleanupIntent(CleanupFacility(GridStationKind.Wash).Id, GridAction.DrainLiquid)));
            Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, bowlStation.Id, bowl);
            long restrictedEgg = received.Contents[0];
            PermissionSupply(client, PortionIntent(receiving, received.Id, restrictedEgg));
            PrepDo(client, GridAction.AddToContainer, bowlStation.Id, restrictedEgg, bowl, rejection: restricted);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(restrictedEgg));
            Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, orange.Id, restrictedEgg);
            PrepDo(client, GridAction.TakeCounter, plateStation.Id, plate);
            var full = Grid.Item(plate).Contents.ToArray(); AssemblyStart(client, 7, restricted);
            Assert.That(Grid.Item(plate).Contents, Is.EqualTo(full));
            PrepDo(client, GridAction.PlaceCounter, plateStation.Id, plate);
            ExtractionFinish(client, ExtractionRequest(client, plateStation, cheese));
            PrepDo(client, GridAction.PlaceCounter, cheeseStation.Id, cheese);
            PrepDo(client, GridAction.TakeCounter, plateStation.Id, plate); AssemblyStart(client, 6);
            var assembly = Grid.InputProcess(plate); PrepTick(assembly.RequiredTicks + 2); Pump(12);
            Assert.That(Grid.IdentifyDish(plate), Is.EqualTo(4));
            int before = Supply.RemainingPackages[1]; PermissionSupply(client, new SupplyIntent(SupplyAction.Purchase, source: 1));
            Assert.That(Supply.RemainingPackages[1], Is.EqualTo(before - 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantPermissionArrival")]
        public void RestaurantPermissionRechecksCurrentClosureAfterAcceptedApproachWithoutConsumingInput(bool useRemote)
        {
            StartGrowth(true); RenovationComplete(); RenovationChoose(7); LevelDo(LevelControlAction.Continue);
            var client = useRemote ? CoopRemote() : (ClientSessionComponent)host.Client;
            var source = PrepStation("橙子台"); var chop = PrepStation("切配台"); long orange = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, orange); PrepDo(client, GridAction.PlaceCounter, chop.Id, orange);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 16));
            var request = client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.StartStationWork, target: chop.Id, expected: orange, expectedTarget: orange);
            long command = request.CommandId;
            using (var reply = (GridCommandResponse)Finish(client.CallProduct(request), 500))
            {
                Assert.That(reply.Error, Is.Zero, reply.Message);
            }
            AdvanceTicks(1); Pump(2);
            var actor = Grid.Actor(client.MemberId);
            Assert.That(actor.HasIntent, Is.True, "Actual accepted action is still approaching its station");
            Assert.That(Grid.InputProcess(orange), Is.Null);
            var definition = Tables.Instance.DTLevelConfig.Get(CurrentLevel.ConfigId);
            var dishes = definition.AllowedDishIds.ToArray(); var drinks = definition.AllowedDrinkIds.ToArray();
            try
            {
                // 同一Level的源配置夹具收窄闭包，隔离执行点复验；普通换关另由Launcher用例覆盖。
                definition.AllowedDishIds.Clear(); definition.AllowedDishIds.Add(4); definition.AllowedDrinkIds.Clear();
                Restaurant.RefreshProductionPermissions();
                for (int step = 0; step < 300 && (actor.LastResultCommandId != command || actor.LastResultStatus is GridResultStatus.Accepted or GridResultStatus.PendingApproach); ++step)
                {
                    AdvanceTicks(1); Pump(3);
                }
                Assert.That(actor.LastResultCommandId, Is.EqualTo(command));
                Assert.That(actor.LastResultStatus, Is.EqualTo(GridResultStatus.Rejected));
                Assert.That(actor.LastResultReason, Is.EqualTo(nameof(ProductionPermissionDecision.MenuRestricted)));
                Assert.That(Grid.InputProcess(orange), Is.Null); Assert.That(chop.ItemId, Is.EqualTo(orange));
                Assert.That(Grid.Item(orange).DefinitionId, Is.EqualTo(GridKitchenRules.Orange));
            }
            finally
            {
                definition.AllowedDishIds.Clear(); definition.AllowedDishIds.AddRange(dishes);
                definition.AllowedDrinkIds.Clear(); definition.AllowedDrinkIds.AddRange(drinks);
                Restaurant.RefreshProductionPermissions();
            }
        }
    }
}
