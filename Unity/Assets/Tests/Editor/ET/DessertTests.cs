using System;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartDessert(bool cooperative = false)
        {
            var config = RuntimeInstanceConfig.Create("dessert-test", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber(); UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: ProductMenuVariant.Dessert); Pump(15);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2301 : 1301));
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 8 }));
        }

        private void DessertCleanup(ClientSessionComponent client, GridAction action)
        {
            var station = PrepStation(action == GridAction.DrainLiquid ? "水池／洗碗台" : "垃圾桶");
            var view = client.GetComponent<GridClientComponent>();
            long held = Grid.Actor(client.MemberId).HandItemId;
            long[] contents = Grid.Item(held).Contents.ToArray();
            PumpUntil(() => view.Actors.TryGetValue(client.MemberId, out var actor) && actor.HandItemId == held &&
                view.Items.TryGetValue(held, out var item) && item.Contents.SequenceEqual(contents), "Current KCP hand and contents before cleanup", 180, true);
            ExtractionFinish(client, view.CreateGridCommand(view.CleanupIntent(station.Id, action)));
        }

        private void DessertAddSupply(ClientSessionComponent client, int source)
        {
            var shipment = PermissionOrder(client, source);
            PermissionSupply(client, ReceiveIntent(shipment));
            var receiving = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiving.ItemId);
            long raw = package.Contents.Single();
            PermissionSupply(client, PortionIntent(receiving, package.Id, raw));
            var mixer = PrepStation("搅拌机");
            PrepDo(client, GridAction.AddToContainer, mixer.Id, raw, mixer.ItemId);
            PrepDo(client, GridAction.TakeCounter, receiving.Id, package.Id);
            DessertCleanup(client, GridAction.DiscardSolid);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(4));
        }

        private GridAutoProcess DessertMix(ClientSessionComponent client, bool finish = true)
        {
            var mixer = PrepStation("搅拌机");
            DessertAddSupply(client, 12);
            var egg = PrepStation("食材台"); long raw = egg.ItemId;
            PrepDo(client, GridAction.TakeCounter, egg.Id, raw);
            PrepDo(client, GridAction.AddToContainer, mixer.Id, raw, mixer.ItemId);
            PrepDo(client, GridAction.StartMix, mixer.Id, mixer.ItemId, mixer.ItemId, rejection: "NoRecipe");
            Assert.That(Grid.AutoProcesses, Is.Empty);
            DessertAddSupply(client, 13);
            PrepDo(client, GridAction.StartMix, mixer.Id, mixer.ItemId, mixer.ItemId);
            var batch = Grid.AutoProcess(Grid.Item(mixer.ItemId).AutoProcessId);
            Assert.That(batch.RequiredCapability, Is.EqualTo(GridStationCapability.Mixing));
            Assert.That(batch.TotalPortions, Is.EqualTo(2));
            if (finish)
            {
                AdvanceTicks(batch.RequiredTicks + 3); Pump(15);
                Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready));
            }
            return batch;
        }

        private void DessertPortion(ClientSessionComponent client)
        {
            var mixer = PrepStation("搅拌机");
            var batch = Grid.AutoProcess(Grid.Item(mixer.ItemId).AutoProcessId);
            PrepDo(client, GridAction.PortionBatter, mixer.Id, Grid.Actor(client.MemberId).HandItemId, mixer.ItemId, batch.Id);
        }

        private GridBakeProcess DessertBeginBake(ClientSessionComponent client)
        {
            long mold = Grid.Actor(client.MemberId).HandItemId;
            var oven = PrepStation("烤箱");
            PrepDo(client, GridAction.PlaceCounter, oven.Id, mold);
            var view = client.GetComponent<GridClientComponent>();
            PumpUntil(() => view.Stations[oven.Id].ItemId == mold && view.Items.TryGetValue(mold, out var item) && item.Contents.Count == 1,
                "Current KCP oven carrier before constructing bake intent", 180, true);
            ExtractionFinish(client, view.CreateGridCommand(view.CounterIntent(oven.Id)));
            var process = Grid.BakeProcess(oven.BakeProcessId);
            Assert.That(process.CarrierItemId, Is.EqualTo(mold));
            Assert.That(process.InputItemId, Is.EqualTo(Grid.Item(mold).Contents.Single()));
            return process;
        }

        private void DessertUnmold(ClientSessionComponent client, string rejection = null)
        {
            var plate = PrepStation("甜品盘台");
            var view = client.GetComponent<GridClientComponent>();
            long held = Grid.Actor(client.MemberId).HandItemId;
            long[] contents = Grid.Item(held).Contents.ToArray();
            PumpUntil(() => view.Actors.TryGetValue(client.MemberId, out var actor) && actor.HandItemId == held &&
                view.Items.TryGetValue(held, out var item) && item.Contents.SequenceEqual(contents) && view.Stations[plate.Id].ItemId == plate.ItemId,
                "Current KCP mold and plate before constructing unmold intent", 180, true);
            ExtractionFinish(client, view.CreateGridCommand(view.UnmoldIntent(plate.Id)), rejection);
        }

        [Test, Category("DessertCore")]
        public void DessertActualConfigurationHasThreeRawInputsTwoPortionsAndAReusableNonNestedCarrierChain()
        {
            StartDessert(); GridDishConfig.ValidateAllMenus();
            GridKitchenSystem.ValidateStationConfiguration();
            var recipe = Tables.Instance.DTGridAutoRecipeConfig.Get(3);
            Assert.That(recipe.InputDefinitionIds, Is.EqualTo(new[] { 43, 3, 44 }));
            Assert.That(recipe.InputCounts, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 46 or 48), Is.False);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 47), Is.EqualTo(2));
            Assert.That(Tables.Instance.DTSupplyConfig.DataList.Any(row => row.MaterialDefinitionId is 46 or 48), Is.False);
            Assert.That(Tables.Instance.DTGridItemConfig.Get(47).AllowsEmptyContainers, Is.False);
            Assert.That(Tables.Instance.DTDishConfig.Get(8).ContainerDefinitionId, Is.EqualTo(49));
            Assert.Throws<InvalidOperationException>(() => GridDishConfig.ValidateMenu(new[] { 8 }, Tables.Instance.DTDishConfig.DataList,
                Tables.Instance.DTDishStageConfig.DataList, Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridManualRecipeConfig.DataList,
                Tables.Instance.DTGridAutoRecipeConfig.DataList, Tables.Instance.DTGridBakeRecipeConfig.DataList, Tables.Instance.DTStationConfig.DataList,
                Tables.Instance.DTInitialLayoutConfig.DataList.Where(row => row.StationDefinitionId != 13), Tables.Instance.DTSupplyConfig.DataList));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("DessertCore")]
        public void DessertActualLocalAndKcpMixPauseLockBakeUnmoldFullTargetAndSameMoldReuse(bool cooperative)
        {
            StartDessert(cooperative);
            var client = cooperative ? CoopRemote() : (ClientSessionComponent)host.Client;
            var batch = DessertMix(client, false);
            var mixing = PrepStation("搅拌机");
            PrepDo(client, GridAction.TakeCounter, mixing.Id, mixing.ItemId, rejection: "ProcessLocked");
            LevelDo(LevelControlAction.Pause); int progress = batch.ProgressTicks;
            AdvanceTicks(20); Assert.That(batch.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(batch.RequiredTicks + 3); Pump(15);
            Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready));
            var source = PrepStation("模具台"); long mold = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, mold); DessertPortion(client);
            Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            Assert.That(Grid.IdentifyDish(mold), Is.Zero);
            var work = DessertBeginBake(client);
            var oven = PrepStation("烤箱");
            PrepDo(client, GridAction.TakeCounter, oven.Id, mold, rejection: "WorkBusy");
            var view = client.GetComponent<GridClientComponent>(); Pump(15);
            Assert.That(view.BakeProcesses[work.Id].CarrierItemId, Is.EqualTo(mold), "Actual snapshot transports the outer carrier separately");
            LevelDo(LevelControlAction.Pause); progress = work.ProgressTicks;
            AdvanceTicks(20); Assert.That(work.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(work.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.Item(work.InputItemId), Is.Null);
            Assert.That(Grid.Item(mold).Contents, Is.EqualTo(new[] { work.OutputItemId }));
            Assert.That(Grid.Item(work.OutputItemId).HolderId, Is.EqualTo(mold));
            Assert.That(Grid.IdentifyDish(mold), Is.Zero);
            PrepDo(client, GridAction.TakeCounter, oven.Id, mold); DessertUnmold(client);
            var plate = PrepStation("甜品盘台"); long carrier = plate.ItemId, firstCake = Grid.Item(carrier).Contents.Single();
            Assert.That(Grid.IdentifyDish(carrier), Is.EqualTo(8));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(mold));
            Assert.That(Grid.Item(mold).Contents, Is.Empty);
            DessertPortion(client);
            Assert.That(Grid.AutoProcesses, Is.Empty);
            Assert.That(Grid.Item(PrepStation("搅拌机").ItemId).Contents, Is.Empty);
            var second = DessertBeginBake(client);
            Assert.That(second.Id, Is.Not.EqualTo(work.Id));
            Assert.That(second.CarrierItemId, Is.EqualTo(mold));
            AdvanceTicks(second.RequiredTicks + 3); Pump(15);
            PrepDo(client, GridAction.TakeCounter, oven.Id, mold);
            DessertUnmold(client, "ContainerFull");
            Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { firstCake }));
            Assert.That(Grid.Item(mold).Contents, Is.EqualTo(new[] { second.OutputItemId }));
            PrepDo(client, GridAction.PlaceCounter, source.Id, mold);
            PrepDo(client, GridAction.TakeCounter, plate.Id, carrier);
            DessertCleanup(client, GridAction.DiscardSolid);
            Assert.That(Grid.Item(firstCake), Is.Null);
            PrepDo(client, GridAction.PlaceCounter, plate.Id, carrier);
            PrepDo(client, GridAction.TakeCounter, source.Id, mold); DessertUnmold(client);
            Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { second.OutputItemId }));
            Assert.That(Grid.Item(mold).Contents, Is.Empty);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 47), Is.EqualTo(2));
        }

        [Test, Category("DessertCore")]
        public void DessertActualKcpLastPortionRaceCreatesOnePhysicalBatterWithoutRefillingTheBatch()
        {
            StartDessert(true); var local = (ClientSessionComponent)host.Client; var remote = CoopRemote();
            var batch = DessertMix(local);
            long first = PrepStation("模具台").ItemId, second = PrepStation("第二模具台").ItemId;
            PrepDo(local, GridAction.TakeCounter, PrepStation("模具台").Id, first); DessertPortion(local);
            DessertCleanup(local, GridAction.DrainLiquid);
            Assert.That(Grid.Item(first).Contents, Is.Empty); Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            PrepDo(remote, GridAction.TakeCounter, PrepStation("第二模具台").Id, second);
            foreach (var client in new[] { local, remote })
            {
                PrepDo(client, GridAction.Move, cell: new GridCell(2, 7));
            }
            var mixer = PrepStation("搅拌机");
            var one = local.CallProduct(local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionBatter, target: mixer.Id,
                expected: first, expectedTarget: mixer.ItemId, process: batch.Id));
            var two = remote.CallProduct(remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionBatter, target: mixer.Id,
                expected: second, expectedTarget: mixer.ItemId, process: batch.Id));
            using (var reply = (GridCommandResponse)Finish(one, 500)) { Assert.That(reply.Error, Is.Zero); }
            using (var reply = (GridCommandResponse)Finish(two, 500)) { Assert.That(reply.Error, Is.Zero); }
            Pump(160);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 46), Is.EqualTo(1));
            Assert.That(new[] { first, second }.Count(id => Grid.Item(id).Contents.Count == 1), Is.EqualTo(1));
            Assert.That(Grid.AutoProcesses, Is.Empty);
            Assert.That(Grid.Item(mixer.ItemId).Contents, Is.Empty);
        }

        [Test, Category("DessertCore")]
        public void DessertActualKcpCannotServeUnbakedOrStillMoldedCakeToARealOrder()
        {
            StartDessert(true); var local = (ClientSessionComponent)host.Client; var remote = CoopRemote();
            DessertMix(local);
            long mold = PrepStation("模具台").ItemId;
            PrepDo(remote, GridAction.TakeCounter, PrepStation("模具台").Id, mold); DessertPortion(remote);
            LevelDo(LevelControlAction.Start);
            var order = WaitActualOrder(8);
            CoopFrontServe(remote, order, mold, "WrongDish");
            var work = DessertBeginBake(remote);
            AdvanceTicks(work.RequiredTicks + 3); Pump(15);
            PrepDo(remote, GridAction.TakeCounter, PrepStation("烤箱").Id, mold);
            CoopFrontServe(remote, order, mold, "WrongDish");
            Assert.That(Grid.Item(mold).Contents, Is.EqualTo(new[] { work.OutputItemId }));
            Assert.That(CurrentLevel.Score, Is.Zero);
        }
    }
}
