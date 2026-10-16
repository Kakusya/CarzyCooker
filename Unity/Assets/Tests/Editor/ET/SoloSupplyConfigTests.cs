using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRSupplyConfig SupplyDefinition(DRSupplyConfig source, int? material = null, int? package = null,
            int? units = null, int? quota = null, int? transit = null, int? receiving = null)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(source.Id); buffer.WriteString(source.Name);
            buffer.WriteInt(material ?? source.MaterialDefinitionId); buffer.WriteInt(package ?? source.PackageDefinitionId);
            buffer.WriteInt(units ?? source.PackageUnits); buffer.WriteInt(quota ?? source.AvailablePackages);
            buffer.WriteInt(transit ?? source.TransitTicks); buffer.WriteInt(receiving ?? source.ReceivingLayoutId);
            buffer.WriteInt((int)(source.MenuVariant));
            return new DRSupplyConfig(buffer);
        }

        [Test]
        public void SoloSupplyConfigActualSourcesPackagesAndMenuClosureCreateNoFreeFood()
        {
            var tables = Tables.Instance;
            var definitions = SupplyConfig.Validate(tables.DTSupplyConfig.DataList, tables.DTGridItemConfig.DataList,
                tables.DTStationConfig.DataList, tables.DTInitialLayoutConfig.DataList, SupplyConfig.ProducedDefinitions());
            Assert.That(definitions.Values.Select(row => row.MaterialDefinitionId).OrderBy(id => id), Is.EqualTo(new[] { 1, 3, 8, 15, 19 }));
            foreach (var definition in definitions.Values)
            {
                var package = tables.DTGridItemConfig.Get(definition.PackageDefinitionId);
                Assert.That(package.Kind, Is.EqualTo(2)); Assert.That(package.Discardable, Is.True);
                Assert.That(package.AcceptedDefinitionIds, Is.EqualTo(new[] { definition.MaterialDefinitionId }));
                Assert.That(definition.ReceivingLayoutId, Is.EqualTo(4)); Assert.That(definition.PackageUnits, Is.EqualTo(2));
                Assert.That(definition.AvailablePackages, Is.EqualTo(5)); Assert.That(definition.TransitTicks, Is.EqualTo(40));
            }
            var layouts = (System.Collections.Generic.List<DRInitialLayoutConfig>)tables.DTInitialLayoutConfig.DataList;
            var original = layouts.ToArray();
            try
            {
                foreach (var row in original.Where(row => row.InitialItemDefinitionId is 1 or 3 or 8))
                {
                    int index = layouts.FindIndex(value => value.Id == row.Id);
                    var buffer = new Luban.ByteBuf(); buffer.WriteInt(row.Id); buffer.WriteInt(row.StationDefinitionId);
                    buffer.WriteInt(row.X); buffer.WriteInt(row.Y); buffer.WriteInt((int)(row.Orientation)); buffer.WriteString(row.Name); buffer.WriteInt(0);
                    buffer.WriteInt((int)(row.MenuVariant));
                    layouts[index] = new DRInitialLayoutConfig(buffer);
                }
                GridDishConfig.ValidateAllMenus(); StartSolo();
                Assert.That(Grid.Items.Count, Is.EqualTo(4), "Menu closure validates definitions without minting foods or packages");
                Assert.That(Grid.Items.Values.Select(value => (GridItem)value).All(value => value.DefinitionId is 5 or 6 or 10), Is.True);
                Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(PrepStation("交接台").InitialLayoutId, Is.EqualTo(4));
                Assert.That(PrepStation("交接台").ItemSlotCapacity, Is.EqualTo(1));
            }
            finally { layouts.Clear(); layouts.AddRange(original); }
        }

        [TestCase("missingRaw")]
        [TestCase("intermediate")]
        [TestCase("package")]
        [TestCase("capacity")]
        [TestCase("quota")]
        [TestCase("transit")]
        [TestCase("receiving")]
        [TestCase("equipment")]
        public void SoloSupplyConfigInvalidSourceOrMenuRejectsBeforeCreatingHost(string fault)
        {
            var tables = Tables.Instance;
            var sources = (System.Collections.Generic.List<DRSupplyConfig>)tables.DTSupplyConfig.DataList;
            var layouts = (System.Collections.Generic.List<DRInitialLayoutConfig>)tables.DTInitialLayoutConfig.DataList;
            var originalSources = sources.ToArray(); var originalLayouts = layouts.ToArray();
            try
            {
                if (fault == "missingRaw") sources.RemoveAll(row => row.MaterialDefinitionId == GridKitchenRules.Bread);
                else if (fault == "equipment") layouts.RemoveAll(row => row.StationDefinitionId == 4);
                else sources[0] = SupplyDefinition(sources[0],
                    material: fault == "intermediate" ? GridKitchenRules.TomatoChunks : null,
                    package: fault == "package" ? GridKitchenRules.Bowl : null,
                    units: fault == "capacity" ? 3 : null, quota: fault == "quota" ? 0 : null,
                    transit: fault == "transit" ? 0 : null, receiving: fault == "receiving" ? 6 : null);
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null); Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { sources.Clear(); sources.AddRange(originalSources); layouts.Clear(); layouts.AddRange(originalLayouts); }
        }
    }
}
