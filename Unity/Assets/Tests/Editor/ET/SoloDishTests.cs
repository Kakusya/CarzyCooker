using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(GridBakeProcess))]
    public sealed partial class ProductSessionTests
    {
        private static DRDishStageConfig DishStage(DRDishStageConfig row, int? output=null, params int[] dependencies)
        {
            var buffer=new Luban.ByteBuf();buffer.WriteInt(row.Id);buffer.WriteInt(row.Kind);buffer.WriteInt(row.RecipeId);
            buffer.WriteInt(output??row.OutputDefinitionId);buffer.WriteInt(row.ContainerDefinitionId);
            buffer.WriteSize(dependencies.Length);foreach(int id in dependencies)buffer.WriteInt(id);
            return new DRDishStageConfig(buffer);
        }

        private void ValidateDishMenu(IEnumerable<DRDishStageConfig> stages=null,IEnumerable<DRGridItemConfig> items=null,IEnumerable<DRInitialLayoutConfig> layout=null)
        {
            var tables=Tables.Instance;
            GridDishConfig.ValidateMenu(new[]{1,2},tables.DTDishConfig.DataList,stages??tables.DTDishStageConfig.DataList,
                items??tables.DTGridItemConfig.DataList,tables.DTGridManualRecipeConfig.DataList,tables.DTGridAutoRecipeConfig.DataList,
                tables.DTGridBakeRecipeConfig.DataList,tables.DTStationConfig.DataList,layout??tables.DTInitialLayoutConfig.DataList);
        }

        private GridBakeProcess DishBegin(ClientSessionComponent client,string sourceName="面包台")
        {
            GridStation source=PrepStation(sourceName),oven=PrepStation("烤箱");long bread=source.ItemId;
            PrepDo(client,GridAction.TakeCounter,source.Id,bread);PrepDo(client,GridAction.PlaceCounter,oven.Id,bread);
            using(var receipt=SoloReply(client,client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.StartBake,target:oven.Id,expected:bread,expectedTarget:bread)))
                Assert.That(receipt.Error,Is.Zero,receipt.Message);
            AdvanceTicks(1);PumpMessages(5);
            var process=Grid.BakeProcess(oven.BakeProcessId);Assert.That(process,Is.Not.Null);Assert.That(process.ProgressTicks,Is.Zero);
            return process;
        }

        private long DishBake(ClientSessionComponent client,string sourceName="面包台")
        {
            var process=DishBegin(client,sourceName);AdvanceTicks(process.RequiredTicks);PumpMessages(5);
            Assert.That(process.State,Is.EqualTo(GridBakeState.Completed));return process.OutputItemId;
        }

        [Test]
        public void SoloDishConfigActualMenusAndFiniteRawInventoryHaveNoFreeDish()
        {
            GridDishConfig.ValidateAllMenus();StartSolo();ClientSessionComponent client=host.Client;
            Assert.That(CurrentLevel.AllowedDishIds,Is.EqualTo(new[]{1,2}));
            Assert.That(Tables.Instance.DTLevelConfig.DataList.All(row=>row.AllowedDishIds.SequenceEqual(new[]{1,2})),Is.True);
            Assert.That(Tables.Instance.DTDishConfig.DataList.Select(row=>row.Name),Is.EqualTo(new[]{"番茄蛋花汤","盘装吐司"}));
            Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.Bread),Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.Plate),Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Any(value=>((GridItem)value).DefinitionId is GridKitchenRules.Toast or GridKitchenRules.Soup),Is.False);
            Assert.That(Grid.IdentifyDish(PrepStation("盘台").ItemId),Is.Zero);
            var projection=client.GetComponent<GridClientComponent>();Assert.That(projection.AllowedDishIds,Is.EqualTo(new[]{1,2}));
            Assert.That(projection.ItemDescription(PrepStation("盘台").ItemId),Is.EqualTo("盘（空）"));
        }

        [TestCase("raw")]
        [TestCase("oven")]
        [TestCase("container")]
        [TestCase("dependency")]
        [TestCase("cycle")]
        [TestCase("intermediate")]
        public void SoloDishGraphRejectsMissingClosureAndCycleWithoutCreatingAnything(string invalid)
        {
            var table=Tables.Instance;var stages=table.DTDishStageConfig.DataList.ToArray();var items=table.DTGridItemConfig.DataList.ToArray();
            var layouts=table.DTInitialLayoutConfig.DataList.ToArray();
            if(invalid=="raw")layouts=layouts.Where(row=>row.InitialItemDefinitionId!=GridKitchenRules.Bread).ToArray();
            if(invalid=="oven")layouts=layouts.Where(row=>row.StationDefinitionId!=4).ToArray();
            if(invalid=="container")items=items.Where(row=>row.Id!=GridKitchenRules.Plate).ToArray();
            if(invalid=="dependency")stages=stages.Where(row=>row.Id!=9).ToArray();
            if(invalid=="cycle")stages[8]=DishStage(stages[8],null,10);
            if(invalid=="intermediate") { stages[8]=DishStage(stages[8],GridKitchenRules.Toast);layouts[8]=InitialStation(9,1,1,14,item:GridKitchenRules.Toast); }
            Assert.Throws<InvalidOperationException>(()=>ValidateDishMenu(stages,items,layouts));
            Assert.That(host,Is.Null);Assert.That(fibers,Is.Empty);
        }

        [TestCase("raw")]
        [TestCase("oven")]
        [TestCase("container")]
        [TestCase("cycle")]
        public void SoloDishGraphActualBootstrapRejectsBeforeCreatingWorld(string invalid)
        {
            var layouts=(List<DRInitialLayoutConfig>)Tables.Instance.DTInitialLayoutConfig.DataList;var beforeLayout=layouts.ToArray();
            var items=(List<DRGridItemConfig>)Tables.Instance.DTGridItemConfig.DataList;var beforeItems=items.ToArray();
            var stages=(List<DRDishStageConfig>)Tables.Instance.DTDishStageConfig.DataList;var beforeStages=stages.ToArray();
            var sources=(List<DRSupplyConfig>)Tables.Instance.DTSupplyConfig.DataList;var beforeSources=sources.ToArray();
            try
            {
                if(invalid=="raw") { layouts.RemoveAll(row=>row.InitialItemDefinitionId==GridKitchenRules.Bread); sources.RemoveAll(row=>row.MaterialDefinitionId==GridKitchenRules.Bread); }
                if(invalid=="oven")layouts.RemoveAll(row=>row.StationDefinitionId==4);
                if(invalid=="container")items.RemoveAll(row=>row.Id==GridKitchenRules.Plate);
                if(invalid=="cycle")stages[8]=DishStage(stages[8],null,10);
                Assert.Throws<InvalidOperationException>(()=>StartSolo());
                Assert.That((Scene)host.HostScene,Is.Null);Assert.That((HostSessionComponent)host.Authority,Is.Null);Assert.That(host.Stage,Is.EqualTo(ProductStage.Stopped));
            }
            finally { layouts.Clear();layouts.AddRange(beforeLayout);items.Clear();items.AddRange(beforeItems);stages.Clear();stages.AddRange(beforeStages);sources.Clear();sources.AddRange(beforeSources); }
        }

        [Test]
        public void SoloDishBakeLocalReplayLocksInputAndProducesOneNewIdentity()
        {
            StartSolo();ClientSessionComponent client=host.Client;var process=DishBegin(client);GridStation oven=PrepStation("烤箱");long input=process.InputItemId;
            var replay=GridRequest(client,client.GetComponent<GridClientComponent>().LastCommandId,GridAction.StartBake,target:oven.Id,expected:input);replay.ExpectedTargetItemId=input;
            using(var receipt=SoloReply(client,replay))Assert.That(receipt.Status,Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.BakeProcesses.Count,Is.EqualTo(1));Assert.That(process.Parent,Is.SameAs(Grid));
            AdvanceTicks(2);Assert.That(Grid.Item(input),Is.Not.Null);Assert.That(process.OutputItemId,Is.Zero);
            PrepDo(client,GridAction.TakeCounter,oven.Id,input,rejection:"WorkBusy");
            PrepDo(client,GridAction.StartBake,oven.Id,input,input,rejection:"WorkBusy");
            GridStation food=PrepStation("食材台");long egg=food.ItemId;PrepDo(client,GridAction.TakeCounter,food.Id,egg);
            PrepDo(client,GridAction.AddToContainer,oven.Id,egg,input,rejection:"WorkBusy");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(egg));Assert.That(oven.ItemId,Is.EqualTo(input));
            AdvanceTicks(process.RequiredTicks);PumpMessages(5);
            Assert.That(process.State,Is.EqualTo(GridBakeState.Completed));Assert.That(Grid.Item(input),Is.Null);
            Assert.That(process.OutputItemId,Is.Not.EqualTo(input));Assert.That(oven.ItemId,Is.EqualTo(process.OutputItemId));Assert.That(oven.BakeProcessId,Is.Zero);
            Assert.That(Grid.Item(process.OutputItemId).DefinitionId,Is.EqualTo(GridKitchenRules.Toast));
            AdvanceTicks(10);Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.Toast),Is.EqualTo(1));
        }

        [Test]
        public void SoloDishBakeAndActualSoupSharePauseAndOneBusinessTick()
        {
            StartSolo();ClientSessionComponent client=host.Client;long pot=SoupFillPot(client);var bake=DishBegin(client);var soup=SoupBegin(client,pot);
            Assert.That(bake.State,Is.EqualTo(GridBakeState.Baking));Assert.That(soup.State,Is.EqualTo(GridAutoState.Cooking));
            LevelDo(LevelControlAction.Pause);int baked=bake.ProgressTicks,cooked=soup.ProgressTicks;long tick=Grid.Tick;
            AdvanceTicks(30);Assert.That(Grid.Tick,Is.EqualTo(tick));Assert.That(bake.ProgressTicks,Is.EqualTo(baked));Assert.That(soup.ProgressTicks,Is.EqualTo(cooked));
            LevelDo(LevelControlAction.Resume);Assert.That(bake.ProgressTicks,Is.EqualTo(baked+1));Assert.That(soup.ProgressTicks,Is.EqualTo(cooked+1));Assert.That(Grid.Tick,Is.EqualTo(tick+1));
            AdvanceTicks(120);Assert.That(bake.State,Is.EqualTo(GridBakeState.Completed));Assert.That(soup.State,Is.EqualTo(GridAutoState.Ready));
            Assert.That(Grid.Items.Values.Any(value=>((GridItem)value).DefinitionId==GridKitchenRules.Soup),Is.False,"A cooked pot is still a batch until portioned");
        }

        [Test]
        public void SoloDishBakeLayoutPreservesOriginalInputProcessAndFrozenDuration()
        {
            StartSolo();ClientSessionComponent client=host.Client;var process=DishBegin(client);var oven=PrepStation("烤箱");
            PrepDo(client,GridAction.Move,cell:new GridCell(3,13));int progress=process.ProgressTicks;long input=process.InputItemId,processId=process.Id;
            LayoutDo(oven,new GridCell(5,12),1);
            Assert.That(process.Id,Is.EqualTo(processId));Assert.That(process.ProgressTicks,Is.EqualTo(progress+1));Assert.That(process.RequiredTicks,Is.EqualTo(80));
            Assert.That(process.StationId,Is.EqualTo(oven.Id));Assert.That(oven.ItemId,Is.EqualTo(input));Assert.That(Grid.Item(input).Cell,Is.EqualTo(oven.Cell));
            AdvanceTicks(process.RequiredTicks);Assert.That(process.State,Is.EqualTo(GridBakeState.Completed));Assert.That(Grid.Item(process.OutputItemId).Cell,Is.EqualTo(oven.Cell));
        }

        [Test]
        public void SoloDishBakeCompletedLevelFreezesAndContinuesSameBatchWithoutNewStock()
        {
            StartSolo();ClientSessionComponent client=host.Client;LevelDo(LevelControlAction.Start);AdvanceTicks(CurrentLevel.OperatingTicks-55);
            var process=DishBegin(client);Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Operating));
            AdvanceTicks(CurrentLevel.OperatingTicks-CurrentLevel.ElapsedOperatingTicks+2);PumpMessages(5);
            Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Completed));Assert.That(process.State,Is.EqualTo(GridBakeState.Baking));
            int progress=process.ProgressTicks;var items=Grid.Items.Keys.ToArray();AdvanceTicks(20);Assert.That(process.ProgressTicks,Is.EqualTo(progress));
            LevelDo(LevelControlAction.Continue);Assert.That(CurrentLevel.AllowedDishIds,Is.EqualTo(new[]{1,2}));
            Assert.That(Grid.Items.Keys,Is.EquivalentTo(items));Assert.That(process.ProgressTicks,Is.EqualTo(progress));
            AdvanceTicks(process.RequiredTicks);Assert.That(process.State,Is.EqualTo(GridBakeState.Completed));
            Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.Toast),Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.Bread),Is.Zero);
        }

        [Test]
        public void SoloDishBakeSnapshotsCopyValuesAndWholeExitDisposesProcess()
        {
            StartSolo();ClientSessionComponent client=host.Client;var process=DishBegin(client);var projection=client.GetComponent<GridClientComponent>();
            Assert.That(projection.BakeProcesses[process.Id].InputItemId,Is.EqualTo(process.InputItemId));
            HostPeer peer=((LocalMessageComponent)client.Local).Peer;
            using(var snapshot=GridBroadcast.CreateSnapshot(Grid,peer))
            {
                snapshot.Version=Grid.Version+1;projection.ApplyGridSnapshot(snapshot);
                int progress=projection.BakeProcesses[process.Id].ProgressTicks;snapshot.BakeProcesses[0].ProgressTicks=999;snapshot.AllowedDishIds[0]=999;
                Assert.That(projection.BakeProcesses[process.Id].ProgressTicks,Is.EqualTo(progress));Assert.That(projection.AllowedDishIds,Is.EqualTo(new[]{1,2}));
            }
            host.StopProduct();Assert.That(process.IsDisposed,Is.True);Assert.That(projection.BakeProcesses,Is.Empty);
        }

        [Test]
        public void SoloDishPlateUsesSameToastAndContainerAndDoesNotScoreBeforeOrder()
        {
            StartSolo();ClientSessionComponent client=host.Client;long toast=DishBake(client);GridStation oven=PrepStation("烤箱"),station=PrepStation("盘台");long plate=station.ItemId;
            Assert.That(Grid.IdentifyDish(toast),Is.Zero,"Bare toast is an intermediate");
            PrepDo(client,GridAction.TakeCounter,oven.Id,toast);PrepDo(client,GridAction.AddToContainer,station.Id,toast,plate);
            Assert.That(station.ItemId,Is.EqualTo(plate));Assert.That(Grid.Item(plate).Contents,Is.EqualTo(new[]{toast}));
            Assert.That(Grid.Item(toast).Location,Is.EqualTo(GridItemLocation.Container));Assert.That(Grid.Item(toast).HolderId,Is.EqualTo(plate));Assert.That(Grid.IdentifyDish(plate),Is.EqualTo(2));
            PrepDo(client,GridAction.TakeCounter,station.Id,plate);PrepDo(client,GridAction.PlaceCounter,PrepStation("交接台").Id,plate);
            Assert.That(Grid.Item(plate).Contents,Is.EqualTo(new[]{toast}));Assert.That(CurrentLevel.Score,Is.Zero);
            var projection=client.GetComponent<GridClientComponent>();Assert.That(projection.Items[plate].Contents,Is.EqualTo(new[]{toast}));
            Assert.That(projection.ItemDescription(plate),Is.EqualTo("盘（烤面包）"));
        }

        [Test]
        public void SoloDishPlateRejectsWrongRawInputWithoutTransferringEitherSide()
        {
            StartSolo();ClientSessionComponent client=host.Client;GridStation bread=PrepStation("面包台"),plate=PrepStation("盘台");long raw=bread.ItemId,container=plate.ItemId;
            PrepDo(client,GridAction.TakeCounter,bread.Id,raw);PrepDo(client,GridAction.AddToContainer,plate.Id,raw,container,rejection:"IncompatibleMaterial");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(raw));Assert.That(Grid.Item(container).Contents,Is.Empty);Assert.That(plate.ItemId,Is.EqualTo(container));
            Assert.That(Grid.IdentifyDish(container),Is.Zero);
        }

        [Test]
        public void SoloDishPlateFullPlateRejectsSecondActuallyBakedToast()
        {
            var layouts=(List<DRInitialLayoutConfig>)Tables.Instance.DTInitialLayoutConfig.DataList;
            try
            {
                layouts.Add(InitialStation(101,1,3,12,item:GridKitchenRules.Bread));StartSolo();ClientSessionComponent client=host.Client;
                long toast=DishBake(client);GridStation oven=PrepStation("烤箱"),plate=PrepStation("盘台");long container=plate.ItemId;
                PrepDo(client,GridAction.TakeCounter,oven.Id,toast);PrepDo(client,GridAction.AddToContainer,plate.Id,toast,container);
                long other=DishBake(client,"测试工位101");PrepDo(client,GridAction.TakeCounter,oven.Id,other);
                PrepDo(client,GridAction.AddToContainer,plate.Id,other,container,rejection:"ContainerFull");
                Assert.That(Grid.Item(container).Contents,Is.EqualTo(new[]{toast}));Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(other));
                Assert.That(Grid.Item(other).Location,Is.EqualTo(GridItemLocation.Hand));Assert.That(Grid.IdentifyDish(container),Is.EqualTo(2));
            }
            finally { layouts.RemoveAll(row=>row.Id==101); }
        }
    }
}
