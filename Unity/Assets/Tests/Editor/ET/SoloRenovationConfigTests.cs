using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRRenovationConfig RenovationDefinition(DRRenovationConfig source, int? station = null,
            int? quantity = null, int? x = null, int? y = null, int? orientation = null)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(source.Id); buffer.WriteString(source.Name);
            buffer.WriteInt(station ?? source.StationDefinitionId); buffer.WriteInt(quantity ?? source.Quantity);
            buffer.WriteInt(x ?? source.InstallX); buffer.WriteInt(y ?? source.InstallY); buffer.WriteInt(orientation ?? source.Orientation);
            return new DRRenovationConfig(buffer);
        }

        [Test]
        public void SoloRenovationConfigActualFiniteCounterAndChoppingRewardsCreateNoExtraInitialItems()
        {
            RenovationConfig.ValidateAll(); var definitions = Tables.Instance.DTRenovationConfig.DataList;
            Assert.That(definitions.Count, Is.EqualTo(2)); Assert.That(definitions.Select(row => row.Quantity), Is.All.EqualTo(1));
            Assert.That(definitions.Select(row => Tables.Instance.DTStationConfig.Get(row.StationDefinitionId).Kind), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(Tables.Instance.DTLevelConfig.DataList.All(row => row.AllowedRenovationIds.SequenceEqual(new[] { 1, 2 })), Is.True);
            StartSolo(); Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(Grid.Stations.Count, Is.EqualTo(16));
            Assert.That(CurrentLevel.AllowedRenovationIds, Is.EqualTo(new[] { 1, 2 }));
        }

        [TestCase("station")]
        [TestCase("quantity")]
        [TestCase("orientation")]
        [TestCase("outside")]
        [TestCase("levelReference")]
        public void SoloRenovationConfigInvalidSourceRejectsBeforeCreatingHost(string fault)
        {
            var rows = (System.Collections.Generic.List<DRRenovationConfig>)Tables.Instance.DTRenovationConfig.DataList;
            var originals = rows.ToArray();
            try
            {
                if (fault == "levelReference") rows.RemoveAt(0);
                else rows[0] = RenovationDefinition(rows[0], station: fault == "station" ? 3 : null,
                    quantity: fault == "quantity" ? 0 : null, x: fault == "outside" ? 0 : null, orientation: fault == "orientation" ? 4 : null);
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null); Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { rows.Clear(); rows.AddRange(originals); }
        }
    }
}
