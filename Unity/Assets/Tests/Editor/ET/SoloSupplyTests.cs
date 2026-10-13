using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(SupplyShipment))]
    public sealed partial class ProductSessionTests
    {
        private KitchenSupply Supply => Grid.GetComponent<KitchenSupply>();
        private HostPeer SupplyPeer => ((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer;
        private GridClientComponent SupplyView => ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
        private DRSupplyConfig TomatoSupply => Tables.Instance.DTSupplyConfig.DataList.Single(row => row.MaterialDefinitionId == GridKitchenRules.Tomato);

        private SupplyCommandResponse SupplyReply(SupplyCommandRequest request)
        {
            ClientSessionComponent client = host.Client;
            var task = client.CallProduct(request); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "Supply request must use the real two-mailbox path");
            return (SupplyCommandResponse)task.GetAwaiter().GetResult();
        }

        private long SupplyDo(SupplyIntent intent, string rejection = null)
        {
            PumpMessages(8);
            var request = SupplyView.CreateSupplyCommand(intent); long command = request.CommandId;
            using (var receipt = SupplyReply(request))
            {
                if (receipt.Error != 0) { Assert.That(receipt.Message, Is.EqualTo(rejection)); return command; }
            }
            for (int i = 0; i < 200 && SupplyPeer.LastCommandStatus is GridResultStatus.Accepted or GridResultStatus.PendingApproach; ++i)
            { AdvanceTicks(1); PumpMessages(3); }
            Assert.That(SupplyPeer.LastCommandContext.CommandId, Is.EqualTo(command));
            Assert.That(SupplyPeer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), SupplyPeer.LastCommandReason);
            Assert.That(SupplyPeer.LastCommandReason, Is.EqualTo(rejection));
            PumpMessages(8); return command;
        }

        private SupplyShipment SupplyOrderReady(int sourceId)
        {
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: sourceId));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().OrderBy(value => value.Id).Last();
            AdvanceTicks(shipment.RequiredTicks); PumpMessages(8);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); return shipment;
        }

        private SupplyIntent ReceiveIntent(SupplyShipment shipment) => new(SupplyAction.Receive,
            station: shipment.ReceivingStationId, shipment: shipment.Id, layout: SupplyView.LayoutVersion);

        private SupplyIntent PortionIntent(GridStation station, long package, long content) => new(SupplyAction.TakePortion,
            station: station.Id, package: package, content: content, layout: SupplyView.LayoutVersion);

        [Test]
        public void SoloSupplyPurchaseRealMailboxReservesOnceAndRejectsShortageWithoutPartialState()
        {
            StartSolo(); int source = TomatoSupply.Id, initialItems = Grid.Items.Count;
            var request = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source, quantity: 2));
            var pendingReplay = CopyReplayRequest(request); var finalReplay = CopyReplayRequest(request);
            var conflict = CopyReplayRequest(request); conflict.Quantity = 3;
            using (var receipt = SupplyReply(request)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            using (var receipt = SupplyReply(pendingReplay)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Supply.Commands.Count, Is.EqualTo(1)); Assert.That(Supply.RemainingPackages[source], Is.EqualTo(5));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(3)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(2));
            using (var receipt = SupplyReply(finalReplay)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Executed));
            using (var receipt = SupplyReply(conflict)) Assert.That(receipt.Message, Is.EqualTo("CommandConflict"));
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: source, quantity: 4), "SupplyExhausted");
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: source, quantity: 0), "InvalidQuantity");
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(3)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(2));
            Assert.That(Grid.Items.Count, Is.EqualTo(initialItems), "Reservations do not mint materials before receipt");
            Assert.That(SupplyView.SupplySources[source].RemainingPackages, Is.EqualTo(3));
        }

        [Test]
        public void SoloSupplyLifecyclePauseCompletedDrainingAndNextLevelKeepSameOwnerAndShipment()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 10);
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
            AdvanceTicks(2); long supplyId = Supply.Id, shipmentId = shipment.Id, levelId = CurrentLevel.Id;
            LevelDo(LevelControlAction.Pause); int progress = shipment.ProgressTicks; long tick = Grid.Tick;
            AdvanceTicks(30); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress)); Assert.That(Grid.Tick, Is.EqualTo(tick));
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id), "Paused");
            LevelDo(LevelControlAction.Resume); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress + 1));
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            progress = shipment.ProgressTicks;
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id), "WrongPhase");
            AdvanceTicks(1); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress + 1));
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            progress = shipment.ProgressTicks; AdvanceTicks(20); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Continue);
            Assert.That(CurrentLevel.Id, Is.Not.EqualTo(levelId)); Assert.That(Supply.Id, Is.EqualTo(supplyId));
            Assert.That(Supply.GetChild<SupplyShipment>(shipmentId), Is.SameAs(shipment));
            Assert.That(shipment.ProgressTicks, Is.EqualTo(progress)); Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            AdvanceTicks(2); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress + 2));
            Assert.That(Supply.Parent, Is.SameAs(Grid)); Assert.That(shipment.Parent, Is.SameAs(Supply));
        }

        [Test]
        public void SoloSupplyReceiveFullSlotKeepsCargoThenCreatesOnePackageAtMovedStation()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridStation receiving = PrepStation("交接台"), tomatoCounter = PrepStation("备料台");
            long tomato = tomatoCounter.ItemId; PrepDo(client, GridAction.TakeCounter, tomatoCounter.Id, tomato);
            PrepDo(client, GridAction.PlaceCounter, receiving.Id, tomato);
            var shipment = SupplyOrderReady(TomatoSupply.Id); int count = Grid.Items.Count;
            SupplyDo(ReceiveIntent(shipment), "ReceivingFull");
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(shipment.PackageItemId, Is.Zero);
            Assert.That(Grid.Items.Count, Is.EqualTo(count)); Assert.That(receiving.ItemId, Is.EqualTo(tomato));
            PrepDo(client, GridAction.TakeCounter, receiving.Id, tomato); PrepDo(client, GridAction.PlaceCounter, tomatoCounter.Id, tomato);
            LayoutDo(receiving, new GridCell(3, 13));
            SupplyDo(ReceiveIntent(shipment)); long packageId = shipment.PackageItemId;
            Assert.That(Grid.AtStation(Grid.Actor(client.MemberId), receiving), Is.True);
            Assert.That(Grid.Item(packageId).Cell, Is.EqualTo(receiving.Cell));
            Assert.That(Grid.Items.Count, Is.EqualTo(count + 1 + shipment.PackageUnits));
            Assert.That(Grid.Item(packageId).Contents.Count, Is.EqualTo(shipment.PackageUnits));
            Assert.That(Grid.Item(packageId).Contents.All(id => Grid.Item(id).HolderId == packageId &&
                Grid.Item(id).DefinitionId == TomatoSupply.MaterialDefinitionId && Grid.Item(id).Location == GridItemLocation.Container), Is.True);
            SupplyDo(ReceiveIntent(shipment), "AlreadyReceived");
            Assert.That(shipment.PackageItemId, Is.EqualTo(packageId)); Assert.That(Grid.Items.Count, Is.EqualTo(count + 3));
        }

        [Test]
        public void SoloSupplyPackageMovesSameMaterialsRejectsFullHandAndDiscardsOnlyEmptyShellAtTrash()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var shipment = SupplyOrderReady(TomatoSupply.Id); GridStation receiving = Grid.Station(shipment.ReceivingStationId);
            SupplyDo(ReceiveIntent(shipment)); long packageId = shipment.PackageItemId;
            long[] materialIds = Grid.Item(packageId).Contents.ToArray(); int quota = Supply.RemainingPackages[TomatoSupply.Id];
            PrepDo(client, GridAction.TakeCounter, receiving.Id, packageId);
            SupplyDo(PortionIntent(receiving, packageId, materialIds[0]), "HandFull");
            Assert.That(Grid.Item(packageId).Contents, Is.EqualTo(materialIds));
            GridStation empty = PrepStation("切配台"); PrepDo(client, GridAction.PlaceCounter, empty.Id, packageId);
            SupplyDo(PortionIntent(empty, packageId, materialIds[0]));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(materialIds[0]));
            Assert.That(Grid.Item(materialIds[0]).Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(Grid.Item(packageId).Contents, Is.EqualTo(new[] { materialIds[1] }));
            PrepDo(client, GridAction.PlaceCounter, receiving.Id, materialIds[0]);
            SupplyDo(PortionIntent(empty, packageId, materialIds[0]), "ContentsChanged");
            SupplyDo(PortionIntent(empty, packageId, materialIds[1]));
            GridStation spare = PrepStation("烤箱");
            PrepDo(client, GridAction.PlaceCounter, spare.Id, materialIds[1]);
            Assert.That(Grid.Item(packageId).Contents, Is.Empty);
            PrepDo(client, GridAction.TakeCounter, empty.Id, packageId); PrepDo(client, GridAction.Move, cell: new GridCell(1, 16));
            var trash = CleanupFacility(GridStationKind.Trash); var request = SupplyView.CreateGridCommand(SupplyView.CleanupIntent(trash.Id, GridAction.DiscardSolid));
            long command = request.CommandId;
            using (var receipt = (GridCommandResponse)Finish(client.CallProduct(request))) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); Assert.That(Grid.Item(packageId), Is.Not.Null, "Empty shell survives before actual facility arrival");
            for (int i = 0; i < 150 && Grid.Item(packageId) != null; ++i) AdvanceTicks(1);
            Assert.That(Grid.Actor(client.MemberId).LastResultCommandId, Is.EqualTo(command));
            Assert.That(Grid.Item(packageId), Is.Null); Assert.That(Grid.Item(materialIds[0]), Is.Not.Null); Assert.That(Grid.Item(materialIds[1]), Is.Not.Null);
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(quota));
        }

        [Test]
        public void SoloSupplyPackageFilledDisposalRequiresTrashAndKeepsShellWithoutRefund()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var shipment = SupplyOrderReady(TomatoSupply.Id); SupplyDo(ReceiveIntent(shipment));
            long package = shipment.PackageItemId; long[] materials = Grid.Item(package).Contents.ToArray();
            PrepDo(client, GridAction.TakeCounter, shipment.ReceivingStationId, package);
            CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "RequiresLiquid");
            Assert.That(Grid.Item(package).Contents, Is.EqualTo(materials));
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(Grid.Item(package), Is.Not.Null, "Discarding solid contents retains the actual package shell");
            Assert.That(Grid.Item(package).Contents, Is.Empty);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(package));
            Assert.That(materials.All(id => Grid.Item(id) == null), Is.True);
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
        }

        [Test]
        public void SoloSupplyMessagesRejectInternalProtocolOldLevelAndCancelQueuedPurchaseOnPause()
        {
            StartSolo(); ClientSessionComponent client = host.Client; int source = TomatoSupply.Id;
            var badVersion = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source)); badVersion.ProtocolVersion--;
            using (var receipt = SupplyReply(badVersion)) Assert.That(receipt.Message, Is.EqualTo("IncompatibleProtocol"));
            using (var receipt = (SupplySubmitResponse)Finish(client.CallProduct(ProductMessages.Create<SupplySubmitRequest>(value => value.ProtocolVersion = ProductSessionRules.ProtocolVersion))))
                Assert.That(receipt.Message, Is.EqualTo("InternalOnly"));
            var request = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source));
            using (var receipt = SupplyReply(request)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            LevelDo(LevelControlAction.Pause);
            Assert.That(Supply.Commands, Is.Empty); Assert.That(Supply.RemainingPackages[source], Is.EqualTo(5)); Assert.That(Supply.ChildrenCount(), Is.Zero);
            LevelDo(LevelControlAction.Resume);
            var oldLevel = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source));
            LevelDo(LevelControlAction.Start); AdvanceTicks(CurrentLevel.OperatingTicks + 2); PumpMessages(8); LevelDo(LevelControlAction.Continue);
            oldLevel.CommandId = client.NextCommandId + 1;
            using (var receipt = SupplyReply(oldLevel)) Assert.That(receipt.Message, Is.EqualTo("WrongLevel"));
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(5)); Assert.That(Supply.ChildrenCount(), Is.Zero);
        }

        [Test]
        public void SoloSupplyProjectionIsIndependentAfterDtoDisposeAndNextLevelRetainsPackages()
        {
            StartSolo(); var shipment = SupplyOrderReady(TomatoSupply.Id); SupplyDo(ReceiveIntent(shipment));
            long package = shipment.PackageItemId; long[] contents = Grid.Item(package).Contents.ToArray();
            var snapshot = GridBroadcast.CreateSnapshot(Grid, SupplyPeer); snapshot.Version++;
            SupplyView.ApplyGridSnapshot(snapshot); snapshot.Dispose();
            Assert.That(SupplyView.SupplyShipments[shipment.Id].PackageItemId, Is.EqualTo(package));
            Assert.That(SupplyView.Items[package].Contents, Is.EqualTo(contents));
            LevelDo(LevelControlAction.Start); AdvanceTicks(CurrentLevel.OperatingTicks + 2); PumpMessages(8); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Item(package).Contents, Is.EqualTo(contents)); Assert.That(Grid.Items.Count, Is.EqualTo(10));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(1));
        }
    }
}
