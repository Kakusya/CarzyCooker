using System;
using System.IO;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartSalad(bool cooperative = false)
        {
            var config = RuntimeInstanceConfig.Create("assembly-test", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber(); UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: (int)ProductMenuVariant.Salad); Pump(12);
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 4, 5 }));
        }

        private long AssemblyChopIntoPlate(ClientSessionComponent client, string sourceName)
        {
            var source = PrepStation(sourceName); var chop = PrepStation("切配台"); var plate = PrepStation("盘台");
            long raw = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, raw);
            PrepDo(client, GridAction.PlaceCounter, chop.Id, raw);
            PrepDo(client, GridAction.StartStationWork, chop.Id, raw, raw);
            var work = Grid.InputProcess(raw); Assert.That(work, Is.Not.Null);
            PrepTick(work.RequiredTicks + 2); Pump(15);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            long output = work.OutputItemId;
            PrepDo(client, GridAction.TakeCounter, chop.Id, output);
            PrepDo(client, GridAction.AddToContainer, plate.Id, output, plate.ItemId);
            return output;
        }

        private void AssemblyStart(ClientSessionComponent client, int recipe, string rejection = null)
        {
            GridItem carrier = Grid.Item(Grid.Actor(client.MemberId).HandItemId);
            var work = Grid.InputProcess(carrier.Id);
            var projection = client.GetComponent<GridClientComponent>();
            var request = projection.CreateGridCommand(new GridIntent(0, GridAction.StartAssembly, Grid.Actor(client.MemberId).Cell,
                carrier.Id, carrier.Id, carrier.Id, work?.Id ?? 0, carrier.Contents, recipeId: recipe));
            ExtractionFinish(client, request, rejection);
        }

        private static DRGridManualRecipeConfig AssemblyRecipe(DRGridManualRecipeConfig source, params int[] counts)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(source.Id); buffer.WriteInt((int)source.Mode); buffer.WriteInt(source.InputDefinitionId);
            buffer.WriteInt(source.OutputDefinitionId); buffer.WriteInt(source.RequiredTicks);
            buffer.WriteSize(source.InputDefinitionIds.Count);
            foreach (int id in source.InputDefinitionIds)
            {
                buffer.WriteInt(id);
            }
            buffer.WriteSize(counts.Length);
            foreach (int count in counts)
            {
                buffer.WriteInt(count);
            }
            buffer.WriteInt(source.ContainerDefinitionId); buffer.WriteInt(source.LastInputDefinitionId);
            return new DRGridManualRecipeConfig(buffer);
        }

        [Test]
        [Category("GridAssembly")]
        public void GridAssemblyConfigurationRequiresExactCountsFiniteLeavesAndAcyclicClosureWhileOldDishesStayValid()
        {
            var tables = Tables.Instance; GridDishConfig.ValidateAllMenus(); ValidateDishMenu();
            var recipe = tables.DTGridManualRecipeConfig.Get(6);
            Assert.That(GridAssemblyConfig.Matches(recipe, new[] { 2, 23, 25 }), Is.True);
            Assert.That(GridAssemblyConfig.Matches(recipe, new[] { 2, 23 }), Is.False);
            Assert.That(GridAssemblyConfig.Matches(recipe, new[] { 2, 23, 25, 26 }), Is.False);
            var twice = AssemblyRecipe(recipe, 2, 1, 1);
            GridAssemblyConfig.Validate(twice, tables.DTGridItemConfig.DataList.ToDictionary(row => row.Id));
            Assert.That(GridAssemblyConfig.Matches(twice, new[] { 2, 23, 25 }), Is.False);
            Assert.That(GridAssemblyConfig.Matches(twice, new[] { 2, 2, 23, 25 }), Is.True);
            Assert.Throws<InvalidOperationException>(() => GridAssemblyConfig.Validate(AssemblyRecipe(recipe, 0, 1, 1), tables.DTGridItemConfig.DataList.ToDictionary(row => row.Id)));
            var stages = tables.DTDishStageConfig.DataList.Select(row => row.Id == 24 ? DishStage(row, null, 24) : row).ToArray();
            Assert.Throws<InvalidOperationException>(() => GridDishConfig.ValidateMenu(new[] { 4, 5 }, tables.DTDishConfig.DataList, stages,
                tables.DTGridItemConfig.DataList, tables.DTGridManualRecipeConfig.DataList, tables.DTGridAutoRecipeConfig.DataList,
                tables.DTGridBakeRecipeConfig.DataList, tables.DTStationConfig.DataList, tables.DTInitialLayoutConfig.DataList, tables.DTSupplyConfig.DataList));
            Assert.Throws<InvalidOperationException>(() => GridDishConfig.ValidateMenu(new[] { 4 }, tables.DTDishConfig.DataList, tables.DTDishStageConfig.DataList,
                tables.DTGridItemConfig.DataList, tables.DTGridManualRecipeConfig.DataList, tables.DTGridAutoRecipeConfig.DataList,
                tables.DTGridBakeRecipeConfig.DataList, tables.DTStationConfig.DataList,
                tables.DTInitialLayoutConfig.DataList.Where(row => row.InitialItemDefinitionId != 22), tables.DTSupplyConfig.DataList.Where(row => row.MaterialDefinitionId != 22)));
        }

        [Test]
        [Category("GridAssembly")]
        public void GridAssemblyRealKcpSharedCuttingRejectsWrongInputsThenTransfersOnePausedPlateAndFinishesOnce()
        {
            StartSalad(true); var local = (ClientSessionComponent)host.Client; var remote = CoopRemote();
            long tomato = AssemblyChopIntoPlate(local, "备料台");
            var plate = PrepStation("盘台"); long carrier = plate.ItemId;
            PrepDo(local, GridAction.TakeCounter, plate.Id, carrier);
            AssemblyStart(local, 6, "AssemblyIngredientsMismatch");
            Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { tomato }));
            PrepDo(local, GridAction.PlaceCounter, plate.Id, carrier);
            PrepDo(local, GridAction.Move, cell: new GridCell(2, 16));
            long lettuce = AssemblyChopIntoPlate(remote, "生菜台");
            long cucumber = AssemblyChopIntoPlate(remote, "黄瓜台");
            PrepDo(remote, GridAction.TakeCounter, plate.Id, carrier);
            var quantityFixture = Tables.Instance.DTGridManualRecipeConfig.Get(6);
            int originalCount = quantityFixture.InputCounts[0];
            try
            {
                quantityFixture.InputCounts[0] = 2;
                AssemblyStart(remote, 6, "AssemblyIngredientsMismatch");
                Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { tomato, lettuce, cucumber }), "One tomato cannot satisfy an actual two-unit recipe");
            }
            finally { quantityFixture.InputCounts[0] = originalCount; }
            AssemblyStart(remote, 7, "AssemblyIngredientsMismatch");
            Assert.That(Grid.IdentifyDish(carrier), Is.Zero, "Vegetables do not prematurely become F01 or F02");
            AssemblyStart(remote, 6);
            GridManualProcess work = Grid.InputProcess(carrier);
            Assert.That(work, Is.Not.Null);
            PrepTick(8);
            PrepDo(remote, GridAction.StopWork, input: carrier, expectedTarget: carrier, process: work.Id);
            int paused = work.ProgressTicks; PrepTick(9);
            Assert.That(work.ProgressTicks, Is.EqualTo(paused));
            PrepDo(remote, GridAction.PlaceCounter, plate.Id, carrier);
            var localRequest = local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.TakeCounter, target: plate.Id, expected: carrier);
            var remoteRequest = remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.TakeCounter, target: plate.Id, expected: carrier);
            var first = local.CallProduct(localRequest); var second = remote.CallProduct(remoteRequest);
            using (var response = (GridCommandResponse)Finish(first, 500))
            {
                Assert.That(response.Error, Is.Zero);
            }
            using (var response = (GridCommandResponse)Finish(second, 500))
            {
                Assert.That(response.Error, Is.Zero);
            }
            Pump(350);
            var winner = Grid.Actor(local.MemberId).HandItemId == carrier ? local : remote;
            Assert.That(new[] { local, remote }.Count(client => Grid.Actor(client.MemberId).HandItemId == carrier), Is.EqualTo(1));
            AssemblyStart(winner, 6); PrepTick(work.RequiredTicks + 2); Pump(20);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { work.OutputItemId }));
            Assert.That(Grid.IdentifyDish(carrier), Is.EqualTo(4));
            Assert.That(new[] { tomato, lettuce, cucumber }.All(id => Grid.Item(id) == null), Is.True);
            Assert.That(local.GetComponent<GridClientComponent>().Items[carrier].Contents, Is.EqualTo(new[] { work.OutputItemId }));
            Assert.That(remote.GetComponent<GridClientComponent>().Items[carrier].Contents, Is.EqualTo(new[] { work.OutputItemId }));
            Assert.That(Grid.Items.Values.Count(reference => ((GridItem)reference).DefinitionId == 27), Is.EqualTo(1));
            AssemblyStart(winner, 6, "AssemblyIngredientsMismatch");
            Assert.That(Grid.Item(carrier).Contents.Count, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("GridAssembly")]
        public void GridAssemblyF02RequiresCheeseAndActualFileRestoresPausedCarrierWithoutReplenishing(bool cooperative)
        {
            StartSalad(cooperative); var client = (ClientSessionComponent)host.Client;
            AssemblyChopIntoPlate(client, "备料台"); AssemblyChopIntoPlate(client, "生菜台"); AssemblyChopIntoPlate(client, "黄瓜台");
            var plate = PrepStation("盘台"); long carrier = plate.ItemId;
            var cheese = PrepStation("芝士台"); long cheeseId = cheese.ItemId;
            PrepDo(client, GridAction.TakeCounter, cheese.Id, cheeseId); PrepDo(client, GridAction.AddToContainer, plate.Id, cheeseId, carrier);
            PrepDo(client, GridAction.TakeCounter, plate.Id, carrier);
            AssemblyStart(client, 6, "AssemblyIngredientsMismatch");
            AssemblyStart(client, 7); var work = Grid.InputProcess(carrier);
            PrepTick(7); PrepDo(client, GridAction.StopWork, input: carrier, expectedTarget: carrier, process: work.Id);
            int ticks = work.ProgressTicks;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(client, repository);
                }
                LevelDo(LevelControlAction.Start);
                for (int step = 0; step < 4000 && CurrentLevel.Phase != LevelPhase.Completed; ++step)
                {
                    AdvanceTicks(1); PumpMessages(3);
                }
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed), "Actual longer menu drains naturally before capture");
                FrontWait(() => Renovation.Prepared, 8);
                RenovationChoose(Renovation.Candidates.First());
                SoloSaveHeader header;
                if (cooperative)
                {
                    PumpUntil(() => ProgressClaimFiles.Browse(repository).Length == 1, "Actual signed cooperative result reaches Root repository", 250, true);
                    var receipt = ProgressClaimFiles.Claim(repository, ProgressClaimFiles.Browse(repository).Single().TicketId);
                    repository.Select(receipt.SlotId, out header);
                }
                else
                {
                    header = SaveHeader(); repository.Commit(header, SoloSaveCapture.Kitchen(Restaurant));
                }
                byte[] before = File.ReadAllBytes(Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"));
                var saved = repository.Select(header.SlotId, out var read);
                Assert.That(saved.ManualWork.Single().ProgressTicks, Is.EqualTo(ticks));
                Assert.That(saved.ManualWork.Single().InputKey, Is.EqualTo(saved.ManualWork.Single().TargetKey));
                Assert.That(read.Version, Is.EqualTo(SoloSaveFormat.Current));
                int count = saved.Items.Length;
                host.StopProduct(); StartSavedFixture(read, saved); Pump(15);
                Assert.That(Grid.Items.Count, Is.EqualTo(count));
                work = Grid.Processes.Values.Select(reference => (GridManualProcess)reference).Single();
                Assert.That(work.State, Is.EqualTo(GridWorkState.Paused));
                Assert.That(work.ProgressTicks, Is.EqualTo(ticks));
                GridItem restored = Grid.Item(work.TargetItemId);
                Assert.That(restored.Contents.Count, Is.EqualTo(4));
                Assert.That(PrepStation("芝士台").ItemId, Is.Zero);
                client = host.Client;
                PrepDo(client, GridAction.Move, cell: restored.Cell);
                PrepDo(client, GridAction.PickupDrop, restored.Id);
                AssemblyStart(client, 7); PrepTick(work.RequiredTicks + 2); Pump(12);
                Assert.That(Grid.IdentifyDish(restored.Id), Is.EqualTo(5));
                Assert.That(File.ReadAllBytes(Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave")), Is.EqualTo(before));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("GridAssemblySupplyAndLegacy")]
        public void GridAssemblyNewSupplyUsesFiniteRawPackagesAndSamePhysicalPortionWithoutFreeCuts()
        {
            StartSalad();
            int initial = Supply.RemainingPackages[6];
            var shipment = SupplyOrderReady(6);
            Assert.That(Supply.RemainingPackages[6], Is.EqualTo(initial - 1));
            SupplyDo(ReceiveIntent(shipment));
            var station = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(station.ItemId);
            Assert.That(package.DefinitionId, Is.EqualTo(29));
            Assert.That(package.Contents.Count, Is.EqualTo(2));
            Assert.That(package.Contents.All(id => Grid.Item(id).DefinitionId == 22), Is.True);
            long raw = package.Contents[0];
            SupplyDo(PortionIntent(station, package.Id, raw));
            Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).HandItemId, Is.EqualTo(raw));
            Assert.That(package.Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 23 or 25 or 27 or 28), Is.False);
        }

        [Test]
        [Category("GridAssemblySupplyAndLegacy")]
        public void GridAssemblyActualV6BaseFileRestoresReadOnlyWithoutAddingNewMenuInventory()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader(); header.Version = 6;
            var repository = TestSaveRepository(out string directory);
            try
            {
                // v6与v7都是原28成员Header；本案仅新增内容语法，没有附加Header成员。
                repository.Commit(header, payload);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                var restored = repository.Select(header.SlotId, out var read);
                host.StopProduct(); StartSavedFixture(read, restored); Pump(12);
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.Items.Values.All(reference => ((GridItem)reference).DefinitionId < 22), Is.True);
                Assert.That(Grid.Stations.Values.All(reference => ((GridStation)reference).InitialLayoutId < 22), Is.True);
                Assert.That(Supply.RemainingPackages.ContainsKey(6), Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
