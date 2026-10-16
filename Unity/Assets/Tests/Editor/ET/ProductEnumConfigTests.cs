using System;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test, Category("ProductSemanticEnums")]
        public void ProductEnumsActualGeneratedConfigKeepsOriginalValuesAndLegalZero()
        {
            Assert.That((int)GridItemKind.Material, Is.EqualTo(1));
            Assert.That((int)GridItemKind.Container, Is.EqualTo(2));
            Assert.That((int)GridMaterialForm.Liquid, Is.EqualTo(2));
            Assert.That((int)GridManualMode.Assembly, Is.EqualTo(3));
            Assert.That((int)DishStageKind.Grind, Is.EqualTo(11));
            Assert.That((int)CompanionPost.FrontService, Is.EqualTo(1));
            Assert.That((int)StationWorkKind.Batch, Is.EqualTo(1));
            Assert.That((int)StationWorkKind.Chop, Is.EqualTo(2));
            Assert.That((int)StationWorkKind.Bake, Is.EqualTo(3));
            Assert.That((int)RenovationRewardKind.ExpandSpace, Is.EqualTo(4));
            Assert.That((int)ProductMenuVariant.Cafe, Is.EqualTo(6));
            Assert.That((int)GridStationKind.Counter, Is.Zero);
            Assert.That((int)DishStageKind.Raw, Is.Zero);
            Assert.That((int)GridOrientation.Unrotated, Is.Zero);
            Assert.That(ProductMenuCatalog.IsValid(ProductMenuVariant.Basic), Is.True);
            Assert.That(ProductMenuCatalog.IsValid((ProductMenuVariant)99), Is.False);
            Assert.That(Tables.Instance.DTGridItemConfig.Get(GridKitchenRules.Tomato).Kind, Is.EqualTo(GridItemKind.Material));
            Assert.That(Tables.Instance.DTGridItemConfig.Get(GridKitchenRules.Bowl).Form, Is.EqualTo(GridMaterialForm.Container));
            var stations = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            Assert.That((int)stations.Values.Single(row => row.Kind == GridStationKind.Wash).Capabilities, Is.EqualTo(400));
            Assert.That(stations.Values.Single(row => row.Kind == GridStationKind.SteamMachine).Capabilities,
                Is.EqualTo(GridStationCapability.HeatMilk | GridStationCapability.FoamMilk));
            Assert.That(Tables.Instance.DTCompanionConfig.Get(1).Post, Is.EqualTo(CompanionPost.FrontService));
            GridDishConfig.ValidateAllMenus();
            BuffConfig.ValidateAll();
            StationUpgradeConfig.ValidateAll();
            RenovationConfig.ValidateAll();
            RestaurantChapters.ValidateAll();
        }

        [Test, Category("ProductSemanticEnums")]
        public void ProductEnumsRejectUnknownAndMismatchedConfigCategories()
        {
            var stations = Tables.Instance.DTStationConfig.DataList.ToArray();
            int index = Array.FindIndex(stations, row => row.Kind == GridStationKind.Wash);
            var original = stations[index];
            foreach (GridStationCapability invalid in new[] { (GridStationCapability)(1 << 25), GridStationCapability.Wash | GridStationCapability.Chop })
            {
                stations[index] = CleanupStationDefinition(original, capabilities: invalid);
                Assert.Throws<InvalidOperationException>(() => GridStationConfig.ValidateDefinitions(stations));
            }
            var items = Tables.Instance.DTGridItemConfig.DataList.ToArray();
            items[0] = CleanupItemDefinition(items[0], form: (GridMaterialForm)99);
            Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateCleanupDefinitions(items, Tables.Instance.DTStationConfig.DataList));
            Assert.Throws<InvalidOperationException>(() => CompanionConfig.Validate(new[] { CompanionDefinition(post: 99) }, GridKitchenSystem.IsInitialGridWalkable));
        }

        [Test, Category("ProductSemanticEnums")]
        [Category("ProductEnumDirectionAndLegacy")]
        public void ProductEnumsFourFacingMovesAndCounterClockwiseGeometryKeepDistinctMeanings()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            GridCell origin = Enumerable.Range(1, GridKitchenRules.Width - 2)
                .SelectMany(x => Enumerable.Range(GridKitchenRules.KitchenBottom, GridKitchenRules.Height - GridKitchenRules.KitchenBottom - 1)
                    .Select(y => new GridCell(x, y)))
                .First(cell => Grid.IsWalkable(cell) && Grid.IsWalkable(new GridCell(cell.X + 1, cell.Y)) &&
                    Grid.IsWalkable(new GridCell(cell.X, cell.Y + 1)) && Grid.IsWalkable(new GridCell(cell.X + 1, cell.Y + 1)));
            PrepDo(client, GridAction.Move, cell: origin);
            var cells = new[] { new GridCell(origin.X, origin.Y + 1), new GridCell(origin.X + 1, origin.Y + 1), new GridCell(origin.X + 1, origin.Y), origin };
            var directions = new[] { GridFacing.Up, GridFacing.Right, GridFacing.Down, GridFacing.Left };
            for (int i = 0; i < cells.Length; ++i)
            {
                PrepDo(client, GridAction.Move, cell: cells[i]);
                Assert.That(Grid.Actor(client.MemberId).Facing, Is.EqualTo(directions[i]));
                Assert.That(client.GetComponent<GridClientComponent>().Actors[client.MemberId].Facing, Is.EqualTo(directions[i]));
            }
            var offsets = new[] { new GridCell(1, 0), new GridCell(0, 1), new GridCell(-1, 0), new GridCell(0, -1) };
            GridOrientation orientation = GridOrientation.Unrotated;
            foreach (GridCell expected in offsets)
            {
                Assert.That(GridStationGeometry.Cells(new[] { 1, 0 }, default, orientation).Single(), Is.EqualTo(expected));
                orientation = GridStationGeometry.NextOrientation(orientation);
            }
            Assert.That(orientation, Is.EqualTo(GridOrientation.Unrotated));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridStationGeometry.Cells(new[] { 1, 0 }, default, (GridOrientation)4));
        }

        [TestCase("facing"), TestCase("orientation"), TestCase("capability"), TestCase("form")]
        [Category("ProductSemanticEnums")]
        public void ProductEnumsInvalidIntegerSnapshotDoesNotReplaceValidProjection(string field)
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            GridClientComponent projection = client.GetComponent<GridClientComponent>();
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            long version = projection.Version;
            var actors = projection.Actors;
            using var snapshot = GridBroadcast.CreateSnapshot(Grid, peer);
            snapshot.Version = version + 1;
            switch (field)
            {
                case "facing": snapshot.Actors[0].Facing = 4; break;
                case "orientation": snapshot.Stations[0].Orientation = -1; break;
                case "capability": snapshot.Stations[0].Capabilities |= 1 << 25; break;
                case "form": snapshot.Items[0].Form = 99; break;
            }
            projection.ApplyGridSnapshot(snapshot);
            Assert.That(projection.Version, Is.EqualTo(version));
            Assert.That(projection.Actors, Is.SameAs(actors));
        }

        [Test, Category("ProductEnumDirectionAndLegacy")]
        public void ProductEnumsUnknownMenuAndOrientationRequestsLeaveCurrentWorldUnchanged()
        {
            StartRoomFixture();
            ClientSessionComponent client = host.Client;
            using (var reply = RoomReply(client, client.CreateRoomCommand(ProductRoomAction.StartRestaurant, (ProductMenuVariant)99)))
            {
                Assert.That(reply.Error, Is.EqualTo(ProductError.InvalidRequest));
                Assert.That(reply.Message, Is.EqualTo("InvalidRoomCommand"));
            }
            Assert.That(Grid, Is.Null);
            RoomDo(ProductRoomAction.StartRestaurant);
            GridStation station = PrepStation("烤箱");
            long version = Grid.LayoutVersion;
            using (var reply = LayoutReply(client, client.GetComponent<GridClientComponent>().CreateStationLayout(station.Id, station.Cell, (GridOrientation)4)))
            {
                Assert.That(reply.Error, Is.EqualTo(ProductError.InvalidRequest));
                Assert.That(reply.Message, Is.EqualTo("InvalidLayout"));
            }
            Assert.That(Grid.LayoutVersion, Is.EqualTo(version));
            Assert.That(station.Orientation, Is.EqualTo(GridOrientation.Unrotated));
            Assert.That(Grid.LayoutCommands, Is.Empty);
        }
    }
}
