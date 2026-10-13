using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopLogisticsItemsTwoPeersReceiveOneCargoOnlyOnce()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var shipment = CoopOrderReady(remote, TomatoSupply.Id); var receiving = Grid.Station(shipment.ReceivingStationId);
            GridCell approach = receiving.StationApproaches().First(Grid.IsWalkable);
            PrepDo(local, GridAction.Move, cell: approach); PrepDo(remote, GridAction.Move, cell: approach);
            int count = Grid.Items.Count;
            var intent = new SupplyIntent(SupplyAction.Receive, station: receiving.Id, shipment: shipment.Id, layout: Grid.LayoutVersion);
            var first = local.CallProduct(local.GetComponent<GridClientComponent>().CreateSupplyCommand(intent));
            var second = remote.CallProduct(remote.GetComponent<GridClientComponent>().CreateSupplyCommand(intent));
            using (var reply = (SupplyCommandResponse)Finish(first, 500)) Assert.That(reply.Error, Is.Zero);
            using (var reply = (SupplyCommandResponse)Finish(second, 500)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(2); PumpMessages(8);
            var localView = local.GetComponent<GridClientComponent>(); var remoteView = remote.GetComponent<GridClientComponent>();
            Assert.That(new[] { localView.LastStatus, remoteView.LastStatus }.Count(value => value == GridResultStatus.Executed), Is.EqualTo(1));
            Assert.That(new[] { localView.LastReason, remoteView.LastReason }, Does.Contain("AlreadyReceived"));
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Received)); Assert.That(receiving.ItemId, Is.EqualTo(shipment.PackageItemId));
            Assert.That(Grid.Items.Count, Is.EqualTo(count + 1 + shipment.PackageUnits)); Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == shipment.PackageDefinitionId), Is.EqualTo(1));
        }

        [Test]
        public void CoopLogisticsItemsFullReceivingSlotAndOldLayoutKeepCargoForMovedRealStation()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            GridStation receiving = PrepStation("交接台"), source = PrepStation("备料台"); long tomato = source.ItemId;
            PrepDo(local, GridAction.TakeCounter, source.Id, tomato); PrepDo(local, GridAction.PlaceCounter, receiving.Id, tomato);
            var shipment = CoopOrderReady(remote, TomatoSupply.Id); int items = Grid.Items.Count;
            var intent = new SupplyIntent(SupplyAction.Receive, station: receiving.Id, shipment: shipment.Id, layout: Grid.LayoutVersion);
            CoopSupplyDo(remote, intent, "ReceivingFull"); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(shipment.PackageItemId, Is.Zero);
            Assert.That(Grid.Items.Count, Is.EqualTo(items)); Assert.That(receiving.ItemId, Is.EqualTo(tomato));
            var old = remote.GetComponent<GridClientComponent>().CreateSupplyCommand(intent);
            PrepDo(local, GridAction.TakeCounter, receiving.Id, tomato); PrepDo(local, GridAction.PlaceCounter, source.Id, tomato);
            LayoutDo(receiving, new GridCell(2, 12));
            long command = old.CommandId; var receipt = CoopLogisticsCall(remote, old);
            Assert.That(receipt.Error, Is.Zero, receipt.Message); CleanupWait(remote, command, "LayoutChanged");
            Assert.That(remote.GetComponent<GridClientComponent>().LastReason, Is.EqualTo("LayoutChanged")); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Receive, station: receiving.Id, shipment: shipment.Id, layout: Grid.LayoutVersion));
            Assert.That(Grid.Item(shipment.PackageItemId).Cell, Is.EqualTo(receiving.Cell)); Assert.That(receiving.Cell, Is.EqualTo(new GridCell(2, 12)));
            Assert.That(Grid.AtStation(Grid.Actor(remote.MemberId), receiving), Is.True); Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
        }

        [Test]
        public void CoopLogisticsItemsTwoPeersMoveOriginalPortionsAndDiscardEmptyShellWithoutRefund()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var shipment = CoopOrderReady(remote, TomatoSupply.Id); var station = Grid.Station(shipment.ReceivingStationId);
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Receive, station: station.Id, shipment: shipment.Id, layout: Grid.LayoutVersion));
            long package = shipment.PackageItemId; long[] material = Grid.Item(package).Contents.ToArray(); Assert.That(material.Length, Is.EqualTo(2));
            var first = new SupplyIntent(SupplyAction.TakePortion, station: station.Id, package: package, content: material[0], layout: Grid.LayoutVersion);
            CoopSupplyDo(remote, first); Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(material[0]));
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.TakePortion, station: station.Id, package: package, content: material[1], layout: Grid.LayoutVersion), "HandFull");
            CoopSupplyDo(local, first, "ContentsChanged"); Assert.That(Grid.Item(package).Contents, Is.EqualTo(new[] { material[1] }));
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.TakePortion, station: station.Id, package: package, content: material[1], layout: Grid.LayoutVersion));
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.EqualTo(material[1])); Assert.That(Grid.Item(package).Contents, Is.Empty);
            CoopCleanupDo(remote, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            CoopCleanupDo(local, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(material.All(value => Grid.Item(value) == null), Is.True);
            PrepDo(remote, GridAction.TakeCounter, station.Id, package);
            CoopCleanupDo(remote, GridAction.DiscardSolid, station, "RequiresTrash"); Assert.That(Grid.Item(package), Is.Not.Null);
            CoopCleanupDo(remote, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(Grid.Item(package), Is.Null); Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.Zero);
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4)); Assert.That(CurrentLevel.Score, Is.Zero);
        }
    }
}
