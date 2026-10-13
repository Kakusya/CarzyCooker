using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private (int Error, string Message, int Status, long Command, int Rpc) CoopLogisticsCall(ClientSessionComponent client, IRequest request)
        {
            return Finish(client.GetAsyncOwner().AsyncScope.RunAsync(context => client.CallProductValueAsync(context, request, value =>
            {
                if (value is SupplyCommandResponse supply) return (supply.Error, supply.Message, supply.Status, supply.CommandId, supply.RpcId);
                var grid = (GridCommandResponse)value;
                return (grid.Error, grid.Message, grid.Status, grid.CommandId, grid.RpcId);
            })), 500);
        }

        private long CoopSupplyDo(ClientSessionComponent client, SupplyIntent intent, string rejection = null)
        {
            PumpMessages(8); var view = client.GetComponent<GridClientComponent>();
            var request = view.CreateSupplyCommand(intent); long command = request.CommandId;
            var receipt = CoopLogisticsCall(client, request);
            Assert.That(receipt.Command, Is.EqualTo(command)); Assert.That(receipt.Rpc, Is.GreaterThan(0));
            if (receipt.Error != 0) { Assert.That(receipt.Message, Is.EqualTo(rejection)); return command; }
            for (int i = 0; i < 300 && (view.LastCommandId != command || view.LastStatus is GridResultStatus.Accepted or GridResultStatus.PendingApproach); ++i)
            { AdvanceTicks(1); PumpMessages(8); }
            Assert.That(view.LastCommandId, Is.EqualTo(command));
            Assert.That(view.LastStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), view.LastReason);
            Assert.That(view.LastReason, Is.EqualTo(rejection)); return command;
        }

        private long CoopCleanupDo(ClientSessionComponent client, GridAction action, GridStation facility, string rejection = null)
        {
            PumpMessages(8); var view = client.GetComponent<GridClientComponent>();
            var request = view.CreateGridCommand(view.CleanupIntent(facility.Id, action)); long command = request.CommandId;
            var receipt = CoopLogisticsCall(client, request);
            Assert.That(receipt.Error, Is.Zero, receipt.Message); Assert.That(receipt.Command, Is.EqualTo(command)); Assert.That(receipt.Rpc, Is.GreaterThan(0));
            CleanupWait(client, command, rejection); return command;
        }

        private SupplyShipment CoopOrderReady(ClientSessionComponent client, int source)
        {
            CoopSupplyDo(client, new SupplyIntent(SupplyAction.Purchase, source: source));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().OrderBy(value => value.Id).Last();
            AdvanceTicks(shipment.RequiredTicks); PumpMessages(8);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); return shipment;
        }

        [Test]
        public void CoopLogisticsWireOwnedKcpPurchaseReplayQuotaAndInternalSubmitStayDistinct()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            int source = TomatoSupply.Id; var request = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source, quantity: 2));
            var replay = CopyReplayRequest(request); var conflict = CopyReplayRequest(request); conflict.Quantity = 3;
            var receipt = CoopLogisticsCall(remote, request); Assert.That(receipt.Error, Is.Zero); Assert.That(receipt.Rpc, Is.GreaterThan(0));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(3)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(2));
            var repeated = CoopLogisticsCall(remote, replay); Assert.That(repeated.Error, Is.Zero); Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(CoopLogisticsCall(remote, conflict).Message, Is.EqualTo("CommandConflict"));
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Purchase, source: source, quantity: 4), "SupplyExhausted");
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(3)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(2)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
            ProductRuntimeComponent runtime = remote.Runtime; Session session = remote.Session;
            var forged = ProductMessages.Create<SupplySubmitRequest>(value => value.ProtocolVersion = ProductSessionRules.ProtocolVersion);
            session.Send(forged); forged.Dispose(); Pump(150);
            Assert.That(session.IsDisposed, Is.True); Assert.That(runtime.Failure, Is.Not.Null.And.Not.Empty);
            Assert.That(((ClientSessionComponent)host.Client).Stage, Is.EqualTo(ProductClientStage.Joined)); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(3));
        }

        [Test]
        public void CoopLogisticsWireOldProtocolAndOldLevelCannotReserveOrReceiveCargo()
        {
            var remote = CoopFrontFixture(); var view = remote.GetComponent<GridClientComponent>();
            var oldProtocol = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id)); oldProtocol.ProtocolVersion--;
            Assert.That(CoopLogisticsCall(remote, oldProtocol).Message, Is.EqualTo("IncompatibleProtocol"));
            var shipment = CoopOrderReady(remote, TomatoSupply.Id);
            var oldReceive = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: view.LayoutVersion));
            var oldPurchase = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 1000); LevelDo(LevelControlAction.Continue);
            Assert.That(CoopLogisticsCall(remote, oldReceive).Message, Is.EqualTo("WrongLevel"));
            Assert.That(CoopLogisticsCall(remote, oldPurchase).Message, Is.EqualTo("WrongLevel"));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4)); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
            Assert.That(shipment.PackageItemId, Is.Zero); Assert.That(Grid.Items.Count, Is.EqualTo(7));
        }

        [Test]
        public void CoopLogisticsWireGridReceiveAndTakePortionUseSamePublicShipmentAndMaterial()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            var shipment = CoopOrderReady(remote, TomatoSupply.Id); var station = Grid.Station(shipment.ReceivingStationId);
            var receive = view.CreateGridCommand(GridAction.ReceiveSupply, target: station.Id, expectedTarget: station.ItemId,
                process: shipment.Id, expectedLayoutVersion: view.LayoutVersion);
            long command = receive.CommandId;
            var receiveReply = CoopLogisticsCall(remote, receive); Assert.That(receiveReply.Error, Is.Zero, receiveReply.Message); CleanupWait(remote, command);
            long package = shipment.PackageItemId, material = Grid.Item(package).Contents.First();
            PumpMessages(8);
            var take = view.CreateGridCommand(GridAction.TakeSupplyPortion, target: station.Id, expected: material, expectedTarget: package,
                expectedLayoutVersion: view.LayoutVersion); command = take.CommandId;
            var takeReply = CoopLogisticsCall(remote, take); Assert.That(takeReply.Error, Is.Zero, takeReply.Message); CleanupWait(remote, command);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(material)); Assert.That(Grid.Item(material).Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(Grid.Item(package).Contents.Count, Is.EqualTo(shipment.PackageUnits - 1));
        }
    }
}
