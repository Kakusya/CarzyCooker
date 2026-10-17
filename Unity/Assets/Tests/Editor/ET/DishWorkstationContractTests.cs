using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public enum DishRoutingConflict
    {
        Manual,
        Bake,
        Batch,
        Dish
    }

    public enum BakingLayoutRelationship
    {
        InputReference,
        CarrierReference,
        DuplicateContents
    }

    public sealed partial class ProductSessionTests
    {
        private const int ConflictingRecipeId = 9001;

        private static DRGridManualRecipeConfig ConflictingManual(DRGridManualRecipeConfig row)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(ConflictingRecipeId); buffer.WriteInt((int)row.Mode); buffer.WriteInt(row.InputDefinitionId);
            buffer.WriteInt(GridKitchenRules.TomatoChunks); buffer.WriteInt(row.RequiredTicks);
            buffer.WriteSize(0); buffer.WriteSize(0); buffer.WriteInt(0); buffer.WriteInt(0);
            return new DRGridManualRecipeConfig(buffer);
        }

        private static DRGridBakeRecipeConfig ConflictingBake(DRGridBakeRecipeConfig row)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(ConflictingRecipeId); buffer.WriteInt(row.InputDefinitionId); buffer.WriteInt(GridKitchenRules.TomatoChunks);
            buffer.WriteInt((int)row.StationCapability); buffer.WriteInt(row.RequiredTicks); buffer.WriteInt(row.CarrierDefinitionId);
            return new DRGridBakeRecipeConfig(buffer);
        }

        private static DRGridAutoRecipeConfig ConflictingBatch(DRGridAutoRecipeConfig row)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(ConflictingRecipeId); buffer.WriteInt(row.ContainerDefinitionId);
            buffer.WriteSize(row.InputDefinitionIds.Count);
            foreach (int value in row.InputDefinitionIds.AsEnumerable().Reverse())
            {
                buffer.WriteInt(value);
            }
            buffer.WriteSize(row.InputCounts.Count);
            foreach (int value in row.InputCounts.AsEnumerable().Reverse())
            {
                buffer.WriteInt(value);
            }
            buffer.WriteInt(row.RequiredTicks); buffer.WriteInt(row.OutputDefinitionId); buffer.WriteInt(row.OutputPortions);
            buffer.WriteInt(row.ServingContainerDefinitionId); buffer.WriteInt((int)row.StationCapability);
            buffer.WriteBool(row.LockContainer); buffer.WriteInt((int)row.MenuVariant);
            return new DRGridAutoRecipeConfig(buffer);
        }

        private static DRDishConfig ConflictingDish(DRDishConfig row)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(ConflictingRecipeId); buffer.WriteString("重复实物菜品");
            buffer.WriteInt(row.ContentDefinitionId); buffer.WriteInt(row.ContainerDefinitionId);
            buffer.WriteInt(row.FinalStageId); buffer.WriteBool(row.RequiresOrderBinding);
            return new DRDishConfig(buffer);
        }

        [TestCase(DishRoutingConflict.Manual)]
        [TestCase(DishRoutingConflict.Bake)]
        [TestCase(DishRoutingConflict.Batch)]
        [TestCase(DishRoutingConflict.Dish)]
        [Category("DishWorkstationContracts")]
        public void DishRoutingAmbiguityRejectsBeforeWorldAndNamesBothDefinitions(DishRoutingConflict conflict)
        {
            var tables = Tables.Instance;
            var manuals = (List<DRGridManualRecipeConfig>)tables.DTGridManualRecipeConfig.DataList;
            var bakes = (List<DRGridBakeRecipeConfig>)tables.DTGridBakeRecipeConfig.DataList;
            var batches = (List<DRGridAutoRecipeConfig>)tables.DTGridAutoRecipeConfig.DataList;
            var dishes = (List<DRDishConfig>)tables.DTDishConfig.DataList;
            int originalId;
            switch (conflict)
            {
                case DishRoutingConflict.Manual:
                    originalId = 3; manuals.Add(ConflictingManual(tables.DTGridManualRecipeConfig.Get(originalId))); break;
                case DishRoutingConflict.Bake:
                    originalId = 1; bakes.Add(ConflictingBake(tables.DTGridBakeRecipeConfig.Get(originalId))); break;
                case DishRoutingConflict.Batch:
                    originalId = 3; batches.Add(ConflictingBatch(tables.DTGridAutoRecipeConfig.Get(originalId))); break;
                default:
                    originalId = 2; dishes.Add(ConflictingDish(tables.DTDishConfig.Get(originalId))); break;
            }
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That(error.Message, Does.Contain("Ambiguous").And.Contain($"{originalId},{ConflictingRecipeId}"));
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally
            {
                manuals.RemoveAll(row => row.Id == ConflictingRecipeId);
                bakes.RemoveAll(row => row.Id == ConflictingRecipeId);
                batches.RemoveAll(row => row.Id == ConflictingRecipeId);
                dishes.RemoveAll(row => row.Id == ConflictingRecipeId);
            }
        }

        [Test, Category("DishWorkstationContracts")]
        public void DishRoutingActualElevenMenusKeepSteamActionsAndExplicitAssemblyDistinct()
        {
            GridDishConfig.ValidateAllMenus();
            Assert.That(Tables.Instance.DTDishConfig.DataList.Count, Is.EqualTo(11));
            var milk = Tables.Instance.DTGridAutoRecipeConfig.DataList.Where(row =>
                row.StationCapability is GridStationCapability.HeatMilk or GridStationCapability.FoamMilk).ToArray();
            Assert.That(milk.Select(row => row.ContainerDefinitionId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(milk.Select(row => row.StationCapability).Distinct().Count(), Is.EqualTo(2));
            Assert.That(Tables.Instance.DTGridManualRecipeConfig.DataList.Count(row => row.Mode == GridManualMode.Assembly), Is.GreaterThan(1));
            StartDessert();
            Assert.That(Grid, Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("DishWorkstationContracts")]
        public void DessertBakingLayoutMovesSameCarrierAndCompletesOnceAtNewApproach(bool cooperative)
        {
            StartDessert(cooperative);
            var client = cooperative ? CoopRemote() : (ClientSessionComponent)host.Client;
            DessertMix(client);
            var source = PrepStation("模具台");
            PrepDo(client, GridAction.TakeCounter, source.Id, source.ItemId); DessertPortion(client);
            var process = DessertLongBake(client);
            var oven = PrepStation("烤箱");
            var carrier = Grid.Item(process.CarrierItemId);
            long input = process.InputItemId, id = process.Id;
            int duration = process.RequiredTicks, progress = process.ProgressTicks;
            long version = Grid.LayoutVersion;
            long[] allItems = Grid.Items.Keys.ToArray();
            LayoutDo(oven, new GridCell(5, 7), GridOrientation.CounterClockwise90);
            Assert.That(Grid.LayoutVersion, Is.EqualTo(version + 1));
            Assert.That(oven.ItemId, Is.EqualTo(carrier.Id));
            Assert.That(oven.BakeProcessId, Is.EqualTo(id));
            Assert.That(carrier.BakeProcessId, Is.EqualTo(id));
            Assert.That(carrier.Contents, Is.EqualTo(new[] { input }));
            Assert.That(Grid.Item(input).BakeProcessId, Is.EqualTo(id));
            Assert.That(Grid.Item(input).HolderId, Is.EqualTo(carrier.Id));
            Assert.That(carrier.Cell, Is.EqualTo(oven.Cell));
            Assert.That(Grid.Item(input).Cell, Is.EqualTo(oven.Cell));
            Assert.That(Grid.Items.Keys, Is.EquivalentTo(allItems));
            Assert.That(process.RequiredTicks, Is.EqualTo(duration));
            Assert.That(process.ProgressTicks, Is.EqualTo(progress + 1));
            int afterMove = process.ProgressTicks;
            LayoutDo(oven, oven.Cell, GridOrientation.HalfTurn);
            Assert.That(process.ProgressTicks, Is.EqualTo(afterMove + 1));
            var view = client.GetComponent<GridClientComponent>();
            PumpUntil(() => view.LayoutVersion == Grid.LayoutVersion && view.Items[carrier.Id].Cell == oven.Cell,
                "Local/KCP receives carrier and new layout in the same committed snapshot", 200);
            Assert.That(view.BakeProcesses[id].CarrierItemId, Is.EqualTo(carrier.Id));
            Assert.That(view.BakeProcesses[id].InputItemId, Is.EqualTo(input));
            Assert.That(view.Stations[oven.Id].Orientation, Is.EqualTo(GridOrientation.HalfTurn));
            LayoutDo(oven, oven.Cell, rejection: "LayoutChanged", version: version);
            LayoutDo(oven, new GridCell(3, 1), rejection: "LayoutBlocked");
            AdvanceTicks(duration - process.ProgressTicks + 3); Pump(15);
            Assert.That(process.State, Is.EqualTo(GridBakeState.Completed));
            Assert.That(Grid.Item(input), Is.Null);
            Assert.That(carrier.Contents, Is.EqualTo(new[] { process.OutputItemId }));
            long cake = process.OutputItemId;
            AdvanceTicks(3); Assert.That(carrier.Contents, Is.EqualTo(new[] { cake }));
            PrepDo(client, GridAction.TakeCounter, oven.Id, carrier.Id);
            Assert.That(oven.StationApproaches(), Does.Contain(Grid.Actor(client.MemberId).Cell));
            DessertUnmold(client);
            Assert.That(Grid.Item(PrepStation("甜品盘台").ItemId).Contents, Is.EqualTo(new[] { cake }));
            Assert.That(Grid.IdentifyDish(PrepStation("甜品盘台").ItemId), Is.EqualTo(8));
            Assert.That(carrier.Contents, Is.Empty);
        }

        [TestCase(BakingLayoutRelationship.InputReference)]
        [TestCase(BakingLayoutRelationship.CarrierReference)]
        [TestCase(BakingLayoutRelationship.DuplicateContents)]
        [Category("DishWorkstationContracts")]
        public void DessertBakingLayoutRejectsBrokenRelationshipsWithoutPartialMove(BakingLayoutRelationship relationship)
        {
            StartDessert(); var client = (ClientSessionComponent)host.Client;
            DessertMix(client);
            var source = PrepStation("模具台");
            PrepDo(client, GridAction.TakeCounter, source.Id, source.ItemId); DessertPortion(client);
            var process = DessertLongBake(client);
            var oven = PrepStation("烤箱"); var carrier = Grid.Item(process.CarrierItemId); var input = Grid.Item(process.InputItemId);
            GridCell cell = oven.Cell; long version = Grid.LayoutVersion; int progress = process.ProgressTicks;
            GridCell inputCell = input.Cell;
            if (relationship == BakingLayoutRelationship.InputReference)
            {
                input.BakeProcessId = 0;
            }
            else if (relationship == BakingLayoutRelationship.CarrierReference)
            {
                carrier.BakeProcessId = 0;
            }
            else
            {
                carrier.Contents.Add(input.Id);
            }
            try
            {
                LayoutDo(oven, new GridCell(5, 7), GridOrientation.CounterClockwise90, "LayoutRelationshipsChanged");
                Assert.That(oven.Cell, Is.EqualTo(cell));
                Assert.That(oven.Orientation, Is.EqualTo(GridOrientation.Unrotated));
                Assert.That(Grid.LayoutVersion, Is.EqualTo(version));
                Assert.That(carrier.Cell, Is.EqualTo(cell));
                Assert.That(input.Cell, Is.EqualTo(inputCell));
                Assert.That(process.ProgressTicks, Is.EqualTo(progress));
                Assert.That(process.State, Is.EqualTo(GridBakeState.Baking));
            }
            finally
            {
                input.BakeProcessId = carrier.BakeProcessId = process.Id;
                carrier.Contents.Clear(); carrier.Contents.Add(input.Id);
            }
        }
    }
}
