using System;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartBeverage(bool cooperative = false)
        {
            var config = RuntimeInstanceConfig.Create("beverage-test", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber(); UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: ProductMenuVariant.Cafe); Pump(15);
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2401 : 1401));
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 9, 10, 11 }));
        }

        private void BeverageRaw(ClientSessionComponent client, int source, string target)
        {
            var shipment = PermissionOrder(client, source); PermissionSupply(client, ReceiveIntent(shipment));
            var receiving = Grid.Station(shipment.ReceivingStationId); var package = Grid.Item(receiving.ItemId);
            long raw = package.Contents[0]; PermissionSupply(client, PortionIntent(receiving, package.Id, raw));
            var station = PrepStation(target);
            PrepDo(client, target == "磨豆机" ? GridAction.PlaceCounter : GridAction.AddToContainer, station.Id, raw, station.ItemId);
            if (package.Contents.Count == 0)
            {
                PrepDo(client, GridAction.TakeCounter, receiving.Id, package.Id); DessertCleanup(client, GridAction.DiscardSolid);
            }
        }

        private GridAutoProcess BeverageAuto(ClientSessionComponent client, string name, GridAction action)
        {
            var station = PrepStation(name);
            PrepDo(client, action, station.Id, station.ItemId, station.ItemId);
            var process = Grid.AutoProcess(Grid.Item(station.ItemId).AutoProcessId);
            Assert.That(process, Is.Not.Null); return process;
        }

        private void BeverageBases(ClientSessionComponent client, bool pause = false, bool finishFoam = true)
        {
            BeverageRaw(client, 14, "磨豆机"); var grinder = PrepStation("磨豆机"); long beans = grinder.ItemId;
            PrepDo(client, GridAction.StartGrind, grinder.Id, beans, beans);
            var grinding = Grid.BakeProcess(grinder.BakeProcessId);
            PrepDo(client, GridAction.TakeCounter, grinder.Id, beans, rejection: "WorkBusy");
            if (pause)
            {
                LevelDo(LevelControlAction.Pause); int progress = grinding.ProgressTicks;
                AdvanceTicks(15); Assert.That(grinding.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            }
            AdvanceTicks(grinding.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.Item(grinder.ItemId).DefinitionId, Is.EqualTo(53)); Assert.That(Grid.Item(beans), Is.Null);
            PrepDo(client, GridAction.TakeCounter, grinder.Id, grinder.ItemId);
            var coffee = PrepStation("咖啡机");
            PrepDo(client, GridAction.AddToContainer, coffee.Id, Grid.Actor(client.MemberId).HandItemId, coffee.ItemId);
            var extraction = BeverageAuto(client, "咖啡机", GridAction.StartExtract);
            PrepDo(client, GridAction.StartExtract, coffee.Id, coffee.ItemId, coffee.ItemId, extraction.Id, rejection: "ProcessLocked");
            if (pause)
            {
                LevelDo(LevelControlAction.Pause); int progress = extraction.ProgressTicks;
                AdvanceTicks(15); Assert.That(extraction.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            }
            BeverageRaw(client, 13, "蒸汽机"); var heat = BeverageAuto(client, "蒸汽机", GridAction.HeatMilk);
            AdvanceTicks(Math.Max(extraction.RequiredTicks, heat.RequiredTicks) + 3); Pump(15);
            Assert.That(extraction.RemainingPortions, Is.EqualTo(4)); Assert.That(heat.OutputDefinitionId, Is.EqualTo(57));
            Assert.That(Grid.AutoProcesses.Values.Any(value => ((GridAutoProcess)value).OutputDefinitionId == 58), Is.False);
            var cupSource = PrepStation("热饮杯台"); long cup = cupSource.ItemId;
            PrepDo(client, GridAction.TakeCounter, cupSource.Id, cup);
            PrepDo(client, GridAction.PlaceCounter, PrepStation("切配台").Id, cup);
            var steam = PrepStation("蒸汽机"); long milk = steam.ItemId;
            PrepDo(client, GridAction.TakeCounter, steam.Id, milk); PrepDo(client, GridAction.PlaceCounter, cupSource.Id, milk);
            var second = PrepStation("第二奶缸台"); long pitcher = second.ItemId;
            PrepDo(client, GridAction.TakeCounter, second.Id, pitcher); PrepDo(client, GridAction.PlaceCounter, steam.Id, pitcher);
            BeverageRaw(client, 13, "蒸汽机"); var foam = BeverageAuto(client, "蒸汽机", GridAction.FoamMilk);
            Assert.That(foam.OutputDefinitionId, Is.EqualTo(58));
            if (finishFoam)
            {
                AdvanceTicks(foam.RequiredTicks + 3); Pump(15);
                Assert.That(foam.RemainingPortions, Is.EqualTo(2));
            }
        }

        private void BeveragePortion(ClientSessionComponent client, string name, string rejection = null)
        {
            var source = PrepStation(name); var process = Grid.AutoProcess(Grid.Item(source.ItemId).AutoProcessId);
            PrepDo(client, GridAction.PortionDrinkBase, source.Id, Grid.Actor(client.MemberId).HandItemId, source.ItemId, process.Id, rejection: rejection);
        }

        private long BeverageSupplyCup(ClientSessionComponent client)
        {
            var receiving = PrepStation("交接台");
            if (receiving.ItemId == 0)
            {
                var shipment = PermissionOrder(client, 15); PermissionSupply(client, ReceiveIntent(shipment));
            }
            var package = Grid.Item(receiving.ItemId); long cup = package.Contents[0];
            PermissionSupply(client, PortionIntent(receiving, package.Id, cup)); return cup;
        }

        [Test, Category("BeverageBinding")]
        public void BeverageActualKcpWrongRebindKeepsOriginalEspressoAndDeliveryConsumesTheCupOnlyOnce()
        {
            StartBeverage(true); var local = (ClientSessionComponent)host.Client; BeverageBases(local); var remote = CoopRemote();
            var source = PrepStation("切配台"); long cup = source.ItemId;
            PrepDo(remote, GridAction.TakeCounter, source.Id, cup); BeveragePortion(remote, "咖啡机");
            var label = PrepStation("贴单台"); PrepDo(remote, GridAction.PlaceCounter, label.Id, cup);
            LevelDo(LevelControlAction.Start); var order = WaitActualOrder(9); var wrong = WaitActualOrder(10);
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, order: order.Id);
            PrepDo(remote, GridAction.BindDrink, label.Id, cup, cup, rejection: "WrongDish", order: wrong.Id);
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(order.Id));
            long liquid = Grid.Item(cup).Contents.Single();
            PrepDo(remote, GridAction.TakeCounter, label.Id, cup); BeveragePortion(remote, "热饮杯台", "DrinkAlreadyBound");
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { liquid }));
            CoopFrontServe(remote, order, cup); CoopFrontServe(remote, order, cup, "OrderEnded");
            Assert.That(Grid.Item(cup), Is.Null); Assert.That(Grid.Item(liquid), Is.Null);
            Assert.That(CurrentLevel.Score, Is.EqualTo(10)); Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
            Assert.That(CurrentLevel.DrinkBindings.ContainsKey(cup), Is.False);
        }

        [Test, Category("BeverageCore")]
        public void BeverageActualThreeRecipesHaveFiniteRawLeavesDistinctModesAndConfiguredLastFoam()
        {
            StartBeverage(); GridDishConfig.ValidateAllMenus(); GridKitchenSystem.ValidateStationConfiguration(); RestaurantChapters.ValidateAll();
            var latte = Tables.Instance.DTGridManualRecipeConfig.Get(10); var cappuccino = Tables.Instance.DTGridManualRecipeConfig.Get(11);
            Assert.That(latte.InputCounts, Is.EqualTo(new[] { 1, 2 })); Assert.That(cappuccino.LastInputDefinitionId, Is.EqualTo(58));
            Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 53 or 56 or 57 or 58 or 60 or 61), Is.False);
            Assert.That(Tables.Instance.DTSupplyConfig.DataList.Any(source => source.MaterialDefinitionId is 53 or 56 or 57 or 58 or 60 or 61), Is.False);
            Assert.That(Tables.Instance.DTGridItemConfig.Get(19).ContainerCapacity, Is.EqualTo(1));
            Assert.That(Tables.Instance.DTGridItemConfig.Get(59).ContainerCapacity, Is.EqualTo(3));
            Assert.That(GridAssemblyConfig.AllowsContainerContents(59, new[] { 58 }), Is.False);
            Assert.That(GridAssemblyConfig.AllowsContainerContents(59, new[] { 56, 58 }), Is.False);
            Assert.That(GridAssemblyConfig.AllowsContainerContents(59, new[] { 56, 57, 58 }), Is.True);
            Assert.That(Tables.Instance.DTRestaurantChapterConfig.Get(2).SoloFirstLevelId, Is.EqualTo(1501));
            Assert.That(Tables.Instance.DTLevelConfig.Get(1201).NextLevelConfigId, Is.EqualTo(1202));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("BeverageCore")]
        public void BeverageActualLocalAndKcpGrindExtractHeatFoamPauseOrderedPortionsAndUniqueBlend(bool cooperative)
        {
            StartBeverage(cooperative); var client = cooperative ? CoopRemote() : (ClientSessionComponent)host.Client;
            BeverageBases(client, true);
            var source = PrepStation("切配台"); long cup = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, cup);
            var foam = Grid.AutoProcess(Grid.Item(PrepStation("蒸汽机").ItemId).AutoProcessId);
            BeveragePortion(client, "蒸汽机", "WrongDrinkSequence");
            Assert.That(foam.RemainingPortions, Is.EqualTo(2)); Assert.That(Grid.Item(cup).Contents, Is.Empty);
            BeveragePortion(client, "咖啡机"); Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(9));
            var coffee = PrepStation("咖啡机"); long coffeeBatch = Grid.Item(coffee.ItemId).AutoProcessId;
            using (var replay = (GridCommandResponse)Finish(client.CallProduct(SoupReplay(client, client.NextCommandId, GridAction.PortionDrinkBase, coffee.Id, cup, coffee.ItemId, coffeeBatch)), 500))
            {
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Assert.That(Grid.AutoProcess(coffeeBatch).RemainingPortions, Is.EqualTo(3), "Same business id cannot take a second portion");
            Assert.That(Grid.Item(cup).Contents.Count, Is.EqualTo(1));
            BeveragePortion(client, "蒸汽机", "WrongDrinkSequence"); Assert.That(foam.RemainingPortions, Is.EqualTo(2));
            BeveragePortion(client, "热饮杯台"); Assert.That(Grid.IdentifyDish(cup), Is.Zero);
            BeveragePortion(client, "蒸汽机"); Assert.That(foam.RemainingPortions, Is.EqualTo(1));
            long[] ingredients = Grid.Item(cup).Contents.ToArray();
            BeveragePortion(client, "蒸汽机", "ContainerFull"); Assert.That(foam.RemainingPortions, Is.EqualTo(1));
            AssemblyStart(client, 11); var work = Grid.InputProcess(cup);
            LevelDo(LevelControlAction.Pause); int progress = work.ProgressTicks;
            AdvanceTicks(15); Assert.That(work.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            AdvanceTicks(work.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(11)); Assert.That(Grid.Item(cup).Contents.Count, Is.EqualTo(1));
            Assert.That(ingredients.All(id => Grid.Item(id) == null), Is.True);
            long output = Grid.Item(cup).Contents.Single(); AdvanceTicks(20); Pump(15);
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { output }));
        }

        [Test, Category("BeverageCore")]
        public void BeverageActualLastEspressoPortionKcpRaceProducesOneUnitAndNoBatchRefill()
        {
            StartBeverage(true); var local = (ClientSessionComponent)host.Client; BeverageBases(local); var remote = CoopRemote();
            var source = PrepStation("切配台"); long one = source.ItemId;
            PrepDo(local, GridAction.TakeCounter, source.Id, one);
            for (int index = 0; index < 3; ++index)
            {
                BeveragePortion(local, "咖啡机"); DessertCleanup(local, GridAction.DrainLiquid);
            }
            var shipment = PermissionOrder(remote, 15); PermissionSupply(remote, ReceiveIntent(shipment));
            var receiving = Grid.Station(shipment.ReceivingStationId); long package = receiving.ItemId;
            long two = Grid.Item(package).Contents[0]; PermissionSupply(remote, PortionIntent(receiving, package, two));
            foreach (var client in new[] { local, remote })
            {
                PrepDo(client, GridAction.Move, cell: new GridCell(4, 9));
            }
            var coffee = PrepStation("咖啡机"); long batch = Grid.Item(coffee.ItemId).AutoProcessId;
            Assert.That(Grid.AutoProcess(batch).RemainingPortions, Is.EqualTo(1));
            var first = local.CallProduct(local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionDrinkBase, target: coffee.Id, expected: one, expectedTarget: coffee.ItemId, process: batch));
            var second = remote.CallProduct(remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionDrinkBase, target: coffee.Id, expected: two, expectedTarget: coffee.ItemId, process: batch));
            using (var reply = (GridCommandResponse)Finish(first, 500)) { Assert.That(reply.Error, Is.Zero); }
            using (var reply = (GridCommandResponse)Finish(second, 500)) { Assert.That(reply.Error, Is.Zero); }
            Pump(150);
            Assert.That(Grid.Item(one).Contents.Count + Grid.Item(two).Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 56), Is.EqualTo(1));
            Assert.That(Grid.AutoProcess(batch), Is.Null); Assert.That(Grid.Item(coffee.ItemId).Contents, Is.Empty);
        }
    }
}
