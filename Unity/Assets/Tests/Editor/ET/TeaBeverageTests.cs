using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartTea(bool cooperative = false)
        {
            if (cooperative)
            {
                StartRoomFixture(); UseOptionalBuffFixture(); OpenRoomForFixture(ProductMenuVariant.Tea);
            }
            else
            {
                var config = RuntimeInstanceConfig.Create("tea-test", "solo", "solo");
                RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber(); UseOptionalBuffFixture();
                host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
                host.InitializeProduct(menuVariant: ProductMenuVariant.Tea); Pump(18);
            }
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2701 : 1701));
            Assert.That(CurrentLevel.ProductionPermit.DishIds.OrderBy(id => id), Is.EqualTo(new[] { 13, 14, 15 }));
        }

        private GridStation TeaStation(int layout) => Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == layout);

        private void TeaCleanup(ClientSessionComponent client, GridStation source, GridAction action, string rejection = null)
        {
            var view = client.GetComponent<GridClientComponent>(); long held = Grid.Actor(client.MemberId).HandItemId;
            long[] contents = Grid.Item(held).Contents.ToArray();
            PumpUntil(() => view.Actors.TryGetValue(client.MemberId, out var actor) && actor.HandItemId == held &&
                view.Items.TryGetValue(held, out var item) && item.Contents.SequenceEqual(contents), "Actual tea hand/contents before facility action", 180, true);
            ExtractionFinish(client, view.CreateGridCommand(view.CleanupIntent(source.Id, action)), rejection);
        }

        private GridAutoProcess TeaBrew(ClientSessionComponent client, bool finish = true)
        {
            var station = TeaStation(43); long barrel = station.ItemId;
            PrepDo(client, GridAction.StartBrewTea, station.Id, barrel, barrel, rejection: "NoRecipe");
            PrepDo(client, GridAction.TakeCounter, station.Id, barrel);
            TeaCleanup(client, TeaStation(42), GridAction.FillWater);
            long water = Grid.Item(barrel).Contents.Single();
            Assert.That(Grid.Item(water).DefinitionId, Is.EqualTo(GridKitchenRules.HotWater));
            PrepDo(client, GridAction.PlaceCounter, station.Id, barrel);
            PrepDo(client, GridAction.StartBrewTea, station.Id, barrel, barrel, rejection: "NoRecipe");
            Assert.That(Grid.Item(barrel).Contents, Is.EqualTo(new[] { water }));
            var leaf = TeaStation(50); long raw = leaf.ItemId;
            PrepDo(client, GridAction.TakeCounter, leaf.Id, raw); PrepDo(client, GridAction.AddToContainer, station.Id, raw, barrel);
            PrepDo(client, GridAction.StartBrewTea, station.Id, barrel, barrel);
            var batch = Grid.AutoProcess(Grid.Item(barrel).AutoProcessId);
            Assert.That(batch.RequiredCapability, Is.EqualTo(GridStationCapability.BrewTea));
            Assert.That(batch.TotalPortions, Is.EqualTo(3)); Assert.That(batch.InputItemIds, Does.Contain(water)); Assert.That(batch.InputItemIds, Does.Contain(raw));
            PrepDo(client, GridAction.TakeCounter, station.Id, barrel, rejection: "ProcessLocked");
            LevelDo(LevelControlAction.Pause); int progress = batch.ProgressTicks;
            AdvanceTicks(20); Assert.That(batch.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            if (finish)
            {
                AdvanceTicks(batch.RequiredTicks - batch.ProgressTicks + 3); Pump(15);
                Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready));
            }
            return batch;
        }

        private GridAutoProcess TeaMilk(ClientSessionComponent client, GridAutoProcess tea)
        {
            var mix = TeaStation(45); long jug = mix.ItemId;
            PrepDo(client, GridAction.TakeCounter, mix.Id, jug);
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(43).Id, jug, tea.PotItemId, tea.Id);
            Assert.That(tea.RemainingPortions, Is.EqualTo(2));
            PrepDo(client, GridAction.PlaceCounter, mix.Id, jug);
            foreach (int layout in new[] { 52, 53 })
            {
                var source = TeaStation(layout); long raw = source.ItemId;
                PrepDo(client, GridAction.TakeCounter, source.Id, raw); PrepDo(client, GridAction.AddToContainer, mix.Id, raw, jug);
            }
            PrepDo(client, GridAction.StartMixTea, mix.Id, jug, jug);
            var batch = Grid.AutoProcess(Grid.Item(jug).AutoProcessId);
            Assert.That(batch.TotalPortions, Is.EqualTo(2));
            PrepDo(client, GridAction.TakeCounter, mix.Id, jug, rejection: "ProcessLocked");
            LevelDo(LevelControlAction.Pause); int progress = batch.ProgressTicks;
            AdvanceTicks(20); Assert.That(batch.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            AdvanceTicks(batch.RequiredTicks - batch.ProgressTicks + 3); Pump(15);
            Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready)); return batch;
        }

        private long TeaPearls(ClientSessionComponent client)
        {
            var source = TeaStation(51); var boiler = TeaStation(44); var jar = TeaStation(48);
            long raw = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, raw);
            PrepDo(client, GridAction.AddToContainer, TeaStation(46).Id, raw, TeaStation(46).ItemId, rejection: "IncompatibleMaterial");
            PrepDo(client, GridAction.PlaceCounter, boiler.Id, raw);
            PrepDo(client, GridAction.StartBoilPearls, boiler.Id, raw, raw);
            var work = Grid.BakeProcess(boiler.BakeProcessId);
            PrepDo(client, GridAction.TakeCounter, boiler.Id, raw, rejection: "WorkBusy");
            LevelDo(LevelControlAction.Pause); int progress = work.ProgressTicks;
            AdvanceTicks(20); Assert.That(work.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
            AdvanceTicks(work.RequiredTicks - work.ProgressTicks + 3); Pump(15);
            Assert.That(Grid.Item(raw), Is.Null);
            long cooked = work.OutputItemId;
            PrepDo(client, GridAction.TakeCounter, boiler.Id, cooked);
            PrepDo(client, GridAction.AddToContainer, jar.Id, cooked, jar.ItemId); Pump(15);
            ExtractionFinish(client, ExtractionRequest(client, jar, cooked));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(cooked)); return cooked;
        }

        [TestCase(false), TestCase(true), Category("TeaBeverageCore")]
        public void TeaActualSoloAndKcpExplicitWaterFiniteBasesPearlCookingColdOrderAndSelectiveCleanup(bool cooperative)
        {
            StartTea(cooperative); GridDishConfig.ValidateAllMenus(); GridKitchenSystem.ValidateStationConfiguration();
            var client = cooperative ? CoopRemote() : (ClientSessionComponent)host.Client;
            Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 78 or 80 or 82 or 88 or 89), Is.False);
            var cold = TeaStation(46); long cup = cold.ItemId;
            var iceSource = TeaStation(54); long ice = iceSource.ItemId;
            PrepDo(client, GridAction.TakeCounter, iceSource.Id, ice);
            PrepDo(client, GridAction.AddToContainer, cold.Id, ice, cup, rejection: "WrongDrinkSequence");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(ice)); Assert.That(Grid.Item(cup).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, iceSource.Id, ice);
            var tea = TeaBrew(client); var milk = TeaMilk(client, tea); long milkProcess = milk.Id;
            long pearl = TeaPearls(client);
            PrepDo(client, GridAction.AddToContainer, cold.Id, pearl, cup);
            PrepDo(client, GridAction.TakeCounter, cold.Id, cup);
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id);
            Assert.That(milk.RemainingPortions, Is.EqualTo(1));
            long liquid = Grid.Item(cup).Contents.Last();
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { pearl, liquid }));
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id, rejection: "WrongDrinkSequence");
            Assert.That(milk.RemainingPortions, Is.EqualTo(1));
            TeaCleanup(client, PrepStation("水池／洗碗台"), GridAction.DrainLiquid);
            Assert.That(Grid.Item(liquid), Is.Null); Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { pearl }));
            TeaCleanup(client, PrepStation("垃圾桶"), GridAction.DiscardSolid);
            Assert.That(Grid.Item(pearl), Is.Null); Assert.That(Grid.Item(cup).Contents, Is.Empty);
            Assert.That(milk.RemainingPortions, Is.EqualTo(1));
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id);
            Assert.That(Grid.AutoProcess(milkProcess), Is.Null); Assert.That(Grid.Item(TeaStation(45).ItemId).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, cold.Id, cup);
            PrepDo(client, GridAction.TakeCounter, iceSource.Id, ice); PrepDo(client, GridAction.AddToContainer, cold.Id, ice, cup);
            PrepDo(client, GridAction.TakeCounter, cold.Id, cup); AssemblyStart(client, 15);
            var mixed = Grid.InputProcess(cup); AdvanceTicks(mixed.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(14)); Assert.That(Grid.Item(ice), Is.Null);
            PrepDo(client, GridAction.PlaceCounter, cold.Id, cup);
            var hot = TeaStation(47); long hotCup = hot.ItemId;
            PrepDo(client, GridAction.TakeCounter, hot.Id, hotCup);
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(43).Id, hotCup, tea.PotItemId, tea.Id);
            Assert.That(Grid.IdentifyDish(hotCup), Is.EqualTo(13)); Assert.That(tea.RemainingPortions, Is.EqualTo(1));
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
        }

        [Test, Category("TeaBeverageCore")]
        public void TeaActualKcpLastMilkPortionRaceAndReplayKeepOneUnitAndOriginalFiniteCupPackage()
        {
            StartTea(true); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var tea = TeaBrew(local); var milk = TeaMilk(local, tea); long process = milk.Id;
            var source = TeaStation(45); long jug = source.ItemId;
            var cups = TeaStation(46); long firstCup = cups.ItemId;
            PrepDo(local, GridAction.TakeCounter, cups.Id, firstCup);
            PrepDo(local, GridAction.PortionTeaBase, source.Id, firstCup, jug, process);
            TeaCleanup(local, PrepStation("水池／洗碗台"), GridAction.DrainLiquid);
            Assert.That(milk.RemainingPortions, Is.EqualTo(1));
            var shipment = PermissionOrder(remote, 24); PermissionSupply(remote, ReceiveIntent(shipment));
            var receiving = Grid.Station(shipment.ReceivingStationId); var package = Grid.Item(receiving.ItemId);
            long secondCup = package.Contents.First(); PermissionSupply(remote, PortionIntent(receiving, package.Id, secondCup));
            Assert.That(Grid.Item(secondCup).DefinitionId, Is.EqualTo(GridKitchenRules.ColdDrinkCup));
            foreach (var client in new[] { local, remote }) { PrepDo(client, GridAction.Move, cell: new GridCell(4, 12)); }
            var request = local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionTeaBase, target: source.Id, expected: firstCup, expectedTarget: jug, process: process);
            var replay = ExtractionCopy(request); replay.ExpectedProcessId = request.ExpectedProcessId; replay.X = request.X; replay.Y = request.Y;
            replay.ExpectedRemainingPortions = request.ExpectedRemainingPortions; replay.ExpectedOrderId = request.ExpectedOrderId; replay.RecipeId = request.RecipeId;
            var other = remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionTeaBase, target: source.Id, expected: secondCup, expectedTarget: jug, process: process);
            var one = local.CallProduct(request); var two = remote.CallProduct(other);
            using (var first = (GridCommandResponse)Finish(one, 500))
            using (var second = (GridCommandResponse)Finish(two, 500))
            {
                Assert.That(first.Error, Is.Zero, first.Message); Assert.That(second.Error, Is.Zero, second.Message);
            }
            Pump(15);
            Assert.That(new[] { firstCup, secondCup }.Count(id => Grid.Item(id).Contents.Count == 1), Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.MilkTeaBase), Is.EqualTo(1));
            Assert.That(Grid.AutoProcess(process), Is.Null); Assert.That(Grid.Item(jug).Contents, Is.Empty);
            int count = Grid.Items.Count;
            using (var again = (GridCommandResponse)Finish(local.CallProduct(replay), 500)) { Assert.That(again.Error, Is.Zero, again.Message); }
            Assert.That(Grid.Items.Count, Is.EqualTo(count)); Assert.That(package.Contents.Count, Is.EqualTo(1));
            Assert.That(Supply.RemainingPackages[24], Is.EqualTo(4));
        }

        [Test, Category("TeaBeverageCore")]
        public void TeaActualKcpSolidCleanupKeepsLiquidStrictPearlPrefixAndBoundFinalCupServesOnlyOnce()
        {
            StartTea(true); var client = CoopRemote(); var tea = TeaBrew(client); var milk = TeaMilk(client, tea);
            var cold = TeaStation(46); long cup = cold.ItemId; var jar = TeaStation(48);
            long firstPearl = TeaPearls(client);
            PrepDo(client, GridAction.AddToContainer, cold.Id, firstPearl, cup); PrepDo(client, GridAction.TakeCounter, cold.Id, cup);
            PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id);
            long liquid = Grid.Item(cup).Contents.Last();
            TeaCleanup(client, PrepStation("垃圾桶"), GridAction.DiscardSolid);
            Assert.That(Grid.Item(firstPearl), Is.Null); Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { liquid }));
            PrepDo(client, GridAction.PlaceCounter, cold.Id, cup);
            var shipment = PermissionOrder(client, 20); PermissionSupply(client, ReceiveIntent(shipment));
            var receiver = Grid.Station(shipment.ReceivingStationId); var package = Grid.Item(receiver.ItemId); long raw = package.Contents.First();
            PermissionSupply(client, PortionIntent(receiver, package.Id, raw));
            var boiler = TeaStation(44); PrepDo(client, GridAction.PlaceCounter, boiler.Id, raw);
            PrepDo(client, GridAction.StartBoilPearls, boiler.Id, raw, raw);
            var cooking = Grid.BakeProcess(boiler.BakeProcessId); AdvanceTicks(cooking.RequiredTicks + 3); Pump(15);
            long pearl = cooking.OutputItemId; PrepDo(client, GridAction.TakeCounter, boiler.Id, pearl);
            PrepDo(client, GridAction.AddToContainer, jar.Id, pearl, jar.ItemId); Pump(15);
            ExtractionFinish(client, ExtractionRequest(client, jar, pearl));
            PrepDo(client, GridAction.AddToContainer, cold.Id, pearl, cup, rejection: "WrongDrinkSequence");
            Assert.That(Grid.Item(cup).Contents, Is.EqualTo(new[] { liquid })); Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(pearl));
            PrepDo(client, GridAction.AddToContainer, jar.Id, pearl, jar.ItemId);
            PrepDo(client, GridAction.TakeCounter, cold.Id, cup); TeaCleanup(client, PrepStation("水池／洗碗台"), GridAction.DrainLiquid);
            Assert.That(Grid.Item(liquid), Is.Null); PrepDo(client, GridAction.PlaceCounter, cold.Id, cup); Pump(15);
            ExtractionFinish(client, ExtractionRequest(client, jar, pearl)); PrepDo(client, GridAction.AddToContainer, cold.Id, pearl, cup);
            PrepDo(client, GridAction.TakeCounter, cold.Id, cup); PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id);
            PrepDo(client, GridAction.PlaceCounter, cold.Id, cup);
            var ice = TeaStation(54); long iceItem = ice.ItemId;
            PrepDo(client, GridAction.TakeCounter, ice.Id, iceItem); PrepDo(client, GridAction.AddToContainer, cold.Id, iceItem, cup);
            PrepDo(client, GridAction.TakeCounter, cold.Id, cup); AssemblyStart(client, 16);
            var blending = Grid.InputProcess(cup); AdvanceTicks(blending.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(15)); Assert.That(Grid.Item(pearl), Is.Null); Assert.That(Grid.Item(iceItem), Is.Null);
            long finished = Grid.Item(cup).Contents.Single(); var label = TeaStation(49); PrepDo(client, GridAction.PlaceCounter, label.Id, cup);
            LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Order>().Any(value => value.State == OrderState.Waiting && value.DishId == 15) &&
                CurrentLevel.Children.Values.OfType<Order>().Any(value => value.State == OrderState.Waiting && value.DishId == 13), 1100);
            var order = CurrentLevel.Children.Values.OfType<Order>().First(value => value.State == OrderState.Waiting && value.DishId == 15);
            var wrong = CurrentLevel.Children.Values.OfType<Order>().First(value => value.State == OrderState.Waiting && value.DishId == 13);
            PrepDo(client, GridAction.BindDrink, label.Id, cup, cup, order: order.Id);
            PrepDo(client, GridAction.BindDrink, label.Id, cup, cup, order: wrong.Id, rejection: "WrongDish");
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(order.Id));
            PrepDo(client, GridAction.TakeCounter, label.Id, cup); CoopFrontServe(client, order, cup); CoopFrontServe(client, order, cup, "OrderEnded");
            Assert.That(CurrentLevel.Score, Is.EqualTo(10)); Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
            Assert.That(Grid.Item(cup), Is.Null); Assert.That(Grid.Item(finished), Is.Null); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
            Assert.That(Supply.RemainingPackages[20], Is.EqualTo(4)); Assert.That(package.Contents.Count, Is.EqualTo(1));
        }
    }
}
