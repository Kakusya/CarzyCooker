using System;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartHotFood(bool cooperative = false)
        {
            var config = RuntimeInstanceConfig.Create("hot-food-test", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber();
            UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: ProductMenuVariant.Hot);
            Pump(15);
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 1001 : 901));
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 6, 7 }));
        }

        private GridBakeProcess HotFoodStartFrying(ClientSessionComponent client)
        {
            var source = PrepStation("牛肉饼台");
            var frying = PrepStation("煎台");
            long input = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, input);
            PrepDo(client, GridAction.PlaceCounter, frying.Id, input);
            PrepDo(client, GridAction.StartFry, frying.Id, input, input);
            return Grid.BakeProcess(frying.BakeProcessId);
        }

        [Test, Category("HotFoodCore")]
        public void HotFoodConfigurationRequiresDifferentRawBranchesExactAssemblyAndReachableEquipment()
        {
            StartHotFood();
            var tables = Tables.Instance;
            GridDishConfig.ValidateAllMenus();
            GridStationConfig.ValidateInitialLayout(tables.DTStationConfig.DataList, tables.DTInitialLayoutConfig.DataList,
                tables.DTGridItemConfig.DataList, tables.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY)));
            Assert.That(GridAssemblyConfig.Matches(tables.DTGridManualRecipeConfig.Get(8), new[] { 32, 34 }), Is.True);
            Assert.That(GridAssemblyConfig.Matches(tables.DTGridManualRecipeConfig.Get(8), new[] { 32, 33 }), Is.False);
            Assert.That(GridAssemblyConfig.Matches(tables.DTGridManualRecipeConfig.Get(9), new[] { 36, 37, 9 }), Is.True);
            Assert.That(GridAssemblyConfig.AllowsContainerContents(10, new[] { 9, 37 }), Is.True);
            Assert.That(GridAssemblyConfig.AllowsContainerContents(10, new[] { 9, 9 }), Is.False);
            Assert.That(GridAssemblyConfig.AllowsContainerContents(10, new[] { 9, 2 }), Is.False);
            Assert.That(tables.DTSupplyConfig.DataList.Any(row => new[] { 34, 36, 37, 38, 39 }.Contains(row.MaterialDefinitionId)), Is.False);
            Assert.Throws<InvalidOperationException>(() => GridDishConfig.ValidateMenu(new[] { 6, 7 }, tables.DTDishConfig.DataList,
                tables.DTDishStageConfig.DataList, tables.DTGridItemConfig.DataList, tables.DTGridManualRecipeConfig.DataList,
                tables.DTGridAutoRecipeConfig.DataList, tables.DTGridBakeRecipeConfig.DataList, tables.DTStationConfig.DataList,
                tables.DTInitialLayoutConfig.DataList.Where(row => row.StationDefinitionId != 12), tables.DTSupplyConfig.DataList));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("HotFoodCore")]
        public void HotFoodRealLocalAndKcpFryingLocksOneInputPausesAndCompletesOnceAlongsideOriginalOven(bool remoteFrying)
        {
            StartHotFood(true);
            var local = (ClientSessionComponent)host.Client;
            var remote = CoopRemote();
            var cook = remoteFrying ? remote : local;
            var baker = remoteFrying ? local : remote;
            var bread = PrepStation("面包台");
            var oven = PrepStation("烤箱");
            long breadId = bread.ItemId;
            PrepDo(baker, GridAction.TakeCounter, bread.Id, breadId);
            PrepDo(baker, GridAction.PlaceCounter, oven.Id, breadId);
            var process = HotFoodStartFrying(cook);
            var frying = PrepStation("煎台");
            long input = process.InputItemId;
            Assert.That(process.RecipeId, Is.EqualTo(2));
            Assert.That(process.OutputDefinitionId, Is.EqualTo(34));
            Assert.That(process.RequiredTicks, Is.EqualTo(80));
            PrepDo(cook, GridAction.StartBake, frying.Id, input, input, rejection: "RequiresOven");
            PrepDo(cook, GridAction.StartFry, frying.Id, input, input, rejection: "WorkBusy");
            PrepDo(cook, GridAction.TakeCounter, frying.Id, input, rejection: "WorkBusy");
            Assert.That(Grid.BakeProcesses.Count, Is.EqualTo(1));
            LevelDo(LevelControlAction.Pause);
            int progress = process.ProgressTicks;
            long tick = Grid.Tick;
            AdvanceTicks(25);
            Assert.That(Grid.Tick, Is.EqualTo(tick));
            Assert.That(process.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume);
            PrepDo(baker, GridAction.StartBake, oven.Id, breadId, breadId);
            Assert.That(process.State, Is.EqualTo(GridBakeState.Baking));
            Assert.That(Grid.BakeProcess(oven.BakeProcessId).State, Is.EqualTo(GridBakeState.Baking));
            AdvanceTicks(100);
            Pump(18);
            Assert.That(process.State, Is.EqualTo(GridBakeState.Completed));
            Assert.That(Grid.Item(input), Is.Null);
            Assert.That(Grid.Item(breadId), Is.Null);
            Assert.That(Grid.Item(frying.ItemId).DefinitionId, Is.EqualTo(34));
            Assert.That(Grid.Item(oven.ItemId).DefinitionId, Is.EqualTo(9));
            long output = frying.ItemId;
            AdvanceTicks(90);
            Pump(15);
            Assert.That(frying.ItemId, Is.EqualTo(output));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 34), Is.EqualTo(1));
            Assert.That(remote.GetComponent<GridClientComponent>().Items[output].DefinitionId, Is.EqualTo(34));
            PrepDo(cook, GridAction.TakeCounter, frying.Id, output);
            PrepDo(cook, GridAction.PlaceCounter, PrepStation("牛肉饼台").Id, output);
            Assert.That(Grid.Item(output).Location, Is.EqualTo(GridItemLocation.Counter));
        }
    }
}
