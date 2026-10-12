using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRStationConfig StationDefinition(DRStationConfig source, int? capacity = null,
            int[] footprint = null, int[] approaches = null)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(source.Id); buffer.WriteString(source.Name); buffer.WriteInt(source.Kind);
            var parts = footprint ?? source.FootprintOffsets.ToArray(); var points = approaches ?? source.ApproachOffsets.ToArray();
            buffer.WriteSize(parts.Length); foreach (var value in parts) buffer.WriteInt(value);
            buffer.WriteSize(points.Length); foreach (var value in points) buffer.WriteInt(value);
            buffer.WriteInt(capacity ?? source.ItemSlotCapacity); buffer.WriteInt(source.Capabilities);
            return new DRStationConfig(buffer);
        }

        private static DRInitialLayoutConfig InitialStation(int id, int definition, int x, int y, int orientation = 0, int item = 0)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(id); buffer.WriteInt(definition); buffer.WriteInt(x); buffer.WriteInt(y);
            buffer.WriteInt(orientation); buffer.WriteString("测试工位" + id); buffer.WriteInt(item);
            return new DRInitialLayoutConfig(buffer);
        }

        [Test]
        public void SoloStationConfigActualDefinitionsHaveSingleSlotsAndFinitePools()
        {
            var definitions = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            Assert.That(definitions.Values.Where(row => row.Kind < (int)GridStationKind.DirtyPool).All(row => row.ItemSlotCapacity == 1), Is.True);
            Assert.That(definitions.Values.Where(row => row.Kind >= (int)GridStationKind.DirtyPool).Select(row => row.ItemSlotCapacity), Is.EqualTo(new[] { 4, 4 }));
            var oven = definitions.Values.Single(row => row.Kind == (int)GridStationKind.Oven);
            Assert.That(oven.Capabilities, Is.EqualTo((int)GridStationCapability.Bake));
            Assert.That(oven.FootprintOffsets.Count, Is.EqualTo(4));
            var terrain = GridKitchenSystem.ValidateStationConfiguration();
            Assert.That(terrain.Count, Is.EqualTo(16));
            Assert.That(terrain.Contains(new GridCell(5, 14)) && terrain.Contains(new GridCell(5, 15)), Is.True);
            Assert.That(terrain.Contains(new GridCell(1, 10)) || terrain.Contains(GridStationConfig.Entrance()), Is.False);
        }

        [TestCase("ordinaryCapacity")]
        [TestCase("poolCapacity")]
        [TestCase("insideApproach")]
        [TestCase("oddOffsets")]
        public void SoloStationConfigRejectsInvalidCapacityAndGeometry(string invalid)
        {
            var definitions = Tables.Instance.DTStationConfig.DataList.ToArray();
            int index = invalid == "poolCapacity" ? 6 : 0;
            definitions[index] = StationDefinition(definitions[index],
                capacity: invalid == "ordinaryCapacity" ? 2 : invalid == "poolCapacity" ? 0 : null,
                footprint: invalid == "oddOffsets" ? new[] { 0 } : null,
                approaches: invalid == "insideApproach" ? new[] { 0, 0 } : null);
            Assert.Throws<InvalidOperationException>(() => GridStationConfig.ValidateDefinitions(definitions));
        }

        [Test]
        public void SoloStationConfigTwoOvensKeepIndependentRuntimeIdentityAndSlots()
        {
            var layouts = (List<DRInitialLayoutConfig>)Tables.Instance.DTInitialLayoutConfig.DataList;
            try
            {
                layouts.Add(InitialStation(101, 4, 1, 15));
                StartSolo();
                var ovens = Grid.Stations.Values.Select(value => (GridStation)value).Where(value => value.Kind == GridStationKind.Oven).OrderBy(value => value.Cell.X).ToArray();
                Assert.That(ovens.Length, Is.EqualTo(2));
                Assert.That(ovens[0].Id, Is.Not.EqualTo(ovens[1].Id));
                Assert.That(ovens[0].DefinitionId, Is.EqualTo(ovens[1].DefinitionId));
                Assert.That(ovens.All(value => value.Parent == Grid && value.AutoProcessId == 0), Is.True);
                ClientSessionComponent client = host.Client;
                GridStation bowl = PrepStation("第二碗台"); long item = bowl.ItemId;
                PrepDo(client, GridAction.TakeCounter, bowl.Id, item);
                PrepDo(client, GridAction.PlaceCounter, ovens[0].Id, item);
                Assert.That(ovens[0].ItemId, Is.EqualTo(item));
                Assert.That(ovens[1].ItemId, Is.Zero);
                Assert.That(Grid.Item(item).HolderId, Is.EqualTo(ovens[0].Id));
            }
            finally { layouts.RemoveAll(value => value.Id == 101); }
        }

        [TestCase("overlap")]
        [TestCase("outside")]
        [TestCase("entrance")]
        [TestCase("disconnected")]
        public void SoloStationTerrainInvalidInitialLayoutLeavesNoHalfWorld(string invalid)
        {
            var layouts = (List<DRInitialLayoutConfig>)Tables.Instance.DTInitialLayoutConfig.DataList;
            var original = layouts.ToArray();
            try
            {
                if (invalid == "disconnected")
                    for (int x = 1; x <= 5; ++x) layouts.Add(InitialStation(100 + x, 1, x, 3));
                else layouts[7] = InitialStation(8, 4, invalid == "overlap" ? 1 : invalid == "outside" ? 0 : 3, invalid == "overlap" ? 11 : invalid == "outside" ? 14 : 1);
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { layouts.Clear(); layouts.AddRange(original); }
        }

        [Test]
        public void SoloStationTerrainRotatesFootprintAndKeepsConfigurationUnmodified()
        {
            var definitions = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            var placements = Tables.Instance.DTInitialLayoutConfig.DataList.Select(row => new GridStationPlacement(row.StationDefinitionId,
                new GridCell(row.X, row.Y), row.Id == 8 ? 1 : row.Orientation));
            var terrain = GridStationConfig.ValidateTerrain(definitions, placements, new[] { new GridCell(1, 10) });
            Assert.That(terrain.Contains(new GridCell(4, 14)), Is.True);
            Assert.That(terrain.Contains(new GridCell(5, 15)), Is.False);
            Assert.That(Tables.Instance.DTInitialLayoutConfig.Get(8).Orientation, Is.Zero);
            StartSolo();
            Assert.That(Grid.Blocked.Contains(new GridCell(5, 15)), Is.True, "A candidate map must not install itself");
            var path = Grid.FindGridPath(GridStationConfig.Entrance(), new GridCell(4, 15));
            Assert.That(path, Is.Not.Null);
            Assert.That(path.All(cell => !Grid.Blocked.Contains(cell)), Is.True);
            for (int i = 1; i < path.Count; ++i) Assert.That(GridKitchenSystem.GridDistance(path[i - 1], path[i]), Is.EqualTo(1));
        }

        [Test]
        public void SoloStationTerrainOvenUsesDeclaredApproachAndCopiesGeometryToProjection()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridStation oven = PrepStation("烤箱"), bowl = PrepStation("第二碗台"); long item = bowl.ItemId;
            PrepDo(client, GridAction.TakeCounter, bowl.Id, item);
            GridCell approach = new(4, 15);
            PrepDo(client, GridAction.Move, cell: approach);
            Assert.That(GridKitchenSystem.GridDistance(approach, oven.Cell), Is.EqualTo(2));
            PrepDo(client, GridAction.PlaceCounter, oven.Id, item);
            Assert.That(Grid.Actor(client.MemberId).Cell, Is.EqualTo(approach), "Use the declared operation point beside the second occupied cell");
            Assert.That(Grid.Item(item).HolderId, Is.EqualTo(oven.Id));
            var projection = client.GetComponent<GridClientComponent>();
            Assert.That(projection.LayoutVersion, Is.EqualTo(Grid.LayoutVersion));
            Assert.That(projection.Stations[oven.Id].Footprint, Is.EquivalentTo(oven.StationFootprint()));
            Assert.That(projection.Stations[oven.Id].Approaches, Is.EquivalentTo(oven.StationApproaches()));
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            using var snapshot = GridBroadcast.CreateSnapshot(Grid, peer); snapshot.Version = Grid.Version + 1;
            projection.ApplyGridSnapshot(snapshot);
            var state = snapshot.Stations.Single(value => value.StationId == oven.Id); state.Footprint[0].X = 999;
            Assert.That(projection.Stations[oven.Id].Footprint.Any(cell => cell.X == 999), Is.False);
        }
    }
}
