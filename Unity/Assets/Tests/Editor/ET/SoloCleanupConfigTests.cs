using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRGridItemConfig CleanupItemDefinition(DRGridItemConfig source, int? form = null,
            bool? discardable = null, int[] accepted = null)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(source.Id); buffer.WriteString(source.Name); buffer.WriteInt(source.Kind);
            buffer.WriteInt(source.ContainerCapacity);
            var values = accepted ?? source.AcceptedDefinitionIds.ToArray();
            buffer.WriteSize(values.Length); foreach (int value in values) buffer.WriteInt(value);
            buffer.WriteInt(form ?? source.Form); buffer.WriteBool(discardable ?? source.Discardable);
            buffer.WriteBool(source.AllowsEmptyContainers); buffer.WriteBool(source.Disposable); buffer.WriteInt(source.MenuVariant);
            buffer.WriteSize(source.ExclusiveDefinitionIds.Count);
            foreach (int definition in source.ExclusiveDefinitionIds)
            {
                buffer.WriteInt(definition);
            }
            return new DRGridItemConfig(buffer);
        }

        private static DRStationConfig CleanupStationDefinition(DRStationConfig source, int? capabilities = null,
            int? waterDefinition = null, int? waterUnits = null)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(source.Id); buffer.WriteString(source.Name); buffer.WriteInt(source.Kind);
            buffer.WriteSize(source.FootprintOffsets.Count); foreach (int value in source.FootprintOffsets) buffer.WriteInt(value);
            buffer.WriteSize(source.ApproachOffsets.Count); foreach (int value in source.ApproachOffsets) buffer.WriteInt(value);
            buffer.WriteInt(source.ItemSlotCapacity); buffer.WriteInt(capabilities ?? source.Capabilities);
            buffer.WriteInt(waterDefinition ?? source.WaterDefinitionId); buffer.WriteInt(waterUnits ?? source.WaterUnits);
            return new DRStationConfig(buffer);
        }

        [Test]
        public void SoloCleanupConfigActualFormsFacilitiesAndInitialInventory()
        {
            var items = Tables.Instance.DTGridItemConfig.DataList;
            var stations = Tables.Instance.DTStationConfig.DataList;
            GridKitchenSystem.ValidateCleanupDefinitions(items, stations);
            foreach (int id in new[] { GridKitchenRules.Water, GridKitchenRules.EggLiquid, GridKitchenRules.Soup })
                Assert.That(Tables.Instance.DTGridItemConfig.Get(id).Form, Is.EqualTo((int)GridMaterialForm.Liquid));
            foreach (int id in new[] { GridKitchenRules.Tomato, GridKitchenRules.TomatoChunks, GridKitchenRules.Egg, GridKitchenRules.Bread, GridKitchenRules.Toast })
            {
                var definition = Tables.Instance.DTGridItemConfig.Get(id);
                Assert.That(definition.Form, Is.EqualTo((int)GridMaterialForm.Solid));
                Assert.That(definition.Discardable, Is.True);
            }
            Assert.That(Tables.Instance.DTGridItemConfig.Get(GridKitchenRules.Bowl).AcceptedDefinitionIds, Does.Contain(GridKitchenRules.Water));
            StartSolo();
            var sink = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == GridStationKind.Wash);
            Assert.That(sink.Capabilities, Is.EqualTo(GridStationCapability.Wash | GridStationCapability.FillWater | GridStationCapability.DrainLiquid));
            Assert.That(Tables.Instance.DTStationConfig.Get(sink.DefinitionId).WaterUnits, Is.EqualTo(1));
            var trash = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == GridStationKind.Trash);
            Assert.That(trash.Capabilities, Is.EqualTo(GridStationCapability.Trash));
            Assert.That(trash.Parent, Is.SameAs(Grid));
            Assert.That(Grid.Blocked.Contains(trash.Cell), Is.True);
            Assert.That(trash.StationApproaches().Any(Grid.IsWalkable), Is.True);
            Assert.That(Grid.Items.Count, Is.EqualTo(7), "Facilities do not duplicate the finite initial stock");
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Any(value => value.DefinitionId == GridKitchenRules.Water), Is.False);
        }

        [TestCase("missingWater")]
        [TestCase("bowlCompatibility")]
        [TestCase("invalidForm")]
        [TestCase("discardPot")]
        [TestCase("sinkSource")]
        [TestCase("waterUnits")]
        [TestCase("wrongCapability")]
        [TestCase("missingTrash")]
        public void SoloCleanupConfigRejectsInvalidReferencesWithoutCreatingWorld(string fault)
        {
            var items = (System.Collections.Generic.List<DRGridItemConfig>)Tables.Instance.DTGridItemConfig.DataList;
            var stations = (System.Collections.Generic.List<DRStationConfig>)Tables.Instance.DTStationConfig.DataList;
            var originalItems = items.ToArray(); var originalStations = stations.ToArray();
            try
            {
                if (fault == "missingWater") items.RemoveAll(row => row.Id == GridKitchenRules.Water);
                else if (fault == "bowlCompatibility" || fault == "invalidForm" || fault == "discardPot")
                {
                    int id = fault == "bowlCompatibility" ? GridKitchenRules.Bowl : fault == "discardPot" ? GridKitchenRules.Pot : GridKitchenRules.EggLiquid;
                    int index = items.FindIndex(row => row.Id == id);
                    items[index] = CleanupItemDefinition(items[index], form: fault == "invalidForm" ? 0 : null,
                        discardable: fault == "discardPot" ? true : null,
                        accepted: fault == "bowlCompatibility" ? new[] { GridKitchenRules.Egg, GridKitchenRules.EggLiquid, GridKitchenRules.Soup } : null);
                }
                else if (fault == "missingTrash") stations.RemoveAll(row => row.Kind == (int)GridStationKind.Trash);
                else
                {
                    int index = stations.FindIndex(row => row.Kind == (int)GridStationKind.Wash);
                    stations[index] = CleanupStationDefinition(stations[index],
                        capabilities: fault == "wrongCapability" ? (int)(GridStationCapability.Wash | GridStationCapability.FillWater) : null,
                        waterDefinition: fault == "sinkSource" ? GridKitchenRules.Tomato : null,
                        waterUnits: fault == "waterUnits" ? 2 : null);
                }
                Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateCleanupDefinitions(items, stations));
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { items.Clear(); items.AddRange(originalItems); stations.Clear(); stations.AddRange(originalStations); }
        }
    }
}
