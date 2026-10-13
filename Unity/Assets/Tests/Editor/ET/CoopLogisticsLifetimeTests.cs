using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopLogisticsLifetimePauseCompletedNextLevelAndLateJoinKeepFrozenPublicShipment()
        {
            var remote = CoopFrontFixture(); int source = TomatoSupply.Id;
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 10);
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Purchase, source: source));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single(); shipment.RequiredTicks = 1600;
            var view = remote.GetComponent<GridClientComponent>();
            var oldPurchase = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: source));
            var oldReceive = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: Grid.LayoutVersion));
            long supply = Supply.Id, id = shipment.Id; LevelDo(LevelControlAction.Pause); int paused = shipment.ProgressTicks;
            AdvanceTicks(30); Assert.That(shipment.ProgressTicks, Is.EqualTo(paused));
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Purchase, source: source), "Paused");
            LevelDo(LevelControlAction.Resume); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 1000);
            int completed = shipment.ProgressTicks; AdvanceTicks(20); Assert.That(shipment.ProgressTicks, Is.EqualTo(completed));
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit)); LevelDo(LevelControlAction.Continue);
            Assert.That(Supply.Id, Is.EqualTo(supply)); Assert.That(Supply.GetChild<SupplyShipment>(id), Is.SameAs(shipment));
            Assert.That(shipment.ProgressTicks, Is.EqualTo(completed)); AdvanceTicks(2); Assert.That(shipment.ProgressTicks, Is.EqualTo(completed + 2));
            Assert.That(CoopLogisticsCall(remote, oldPurchase).Message, Is.EqualTo("WrongLevel"));
            Assert.That(CoopLogisticsCall(remote, oldReceive).Message, Is.EqualTo("WrongLevel"));
            var joining = CoopRemote(); var observed = joining.GetComponent<GridClientComponent>();
            Assert.That(observed.SupplySources[source].RemainingPackages, Is.EqualTo(4));
            Assert.That(observed.SupplyShipments[id].ProgressTicks, Is.EqualTo(shipment.ProgressTicks));
            Assert.That(observed.SupplyShipments[id].RequiredTicks, Is.EqualTo(1600)); Assert.That(observed.SupplyShipments[id].Stage, Is.EqualTo(ShipmentStage.InTransit));
            Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(observed.Items.Count, Is.EqualTo(7));
        }

        [Test]
        public void CoopLogisticsLifetimeCommittedPurchaseSurvivesDisconnectBeforeClientReadsConfirmation()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            var request = view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id)); request.RpcId = 88001;
            Session session = remote.Session; session.Send(request); request.Dispose();
            // 只刷新请求发送端一次，之后只泵本fixture的Host；Client不消费回执／快照。
            Fiber hostFiber = host.Fiber(); object originalFiber = CurrentFiber.GetValue(null);
            try
            {
                clock.Milliseconds += 15; TimeInfo.Instance.Update(); CurrentFiber.SetValue(null, remote.Fiber());
                ((Scene)remote.IScene).GetComponent<NetComponent>().AService.Update();
                for (int i = 0; i < 100 && Supply.ChildrenCount() == 0; ++i)
                {
                    clock.Milliseconds += 15; TimeInfo.Instance.Update(); CurrentFiber.SetValue(null, hostFiber);
                    hostFiber.EntitySystem.Update(); hostFiber.EntitySystem.LateUpdate(); hostFiber.ThreadSynchronizationContext.Update();
                }
                Assert.That(Supply.ChildrenCount(), Is.EqualTo(1)); Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
                Assert.That(view.SupplySources[TomatoSupply.Id].RemainingPackages, Is.EqualTo(5), "Client has not consumed the committed fact or confirmation");
                CurrentFiber.SetValue(null, remote.Fiber()); session.Dispose();
            }
            finally { CurrentFiber.SetValue(null, originalFiber); }
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single(); Pump(900);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(Supply.ChildrenCount(), Is.EqualTo(1)); Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            ClientSessionComponent local = host.Client;
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: Grid.LayoutVersion));
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Received)); Assert.That(Grid.Item(shipment.PackageItemId).Contents.Count, Is.EqualTo(shipment.PackageUnits));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        public void CoopLogisticsLifetimePooledDtoAndLateJoinSeeSamePackageMaterialsAndQuota()
        {
            StartHost(); var remote = CoopRemote(); var shipment = CoopOrderReady(remote, TomatoSupply.Id);
            CoopSupplyDo(remote, new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: Grid.LayoutVersion));
            long package = shipment.PackageItemId; long[] material = Grid.Item(package).Contents.ToArray();
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            var view = remote.GetComponent<GridClientComponent>(); var snapshot = GridBroadcast.CreateSnapshot(Grid, peer); snapshot.Version = view.Version + 1;
            view.ApplyGridSnapshot(snapshot);
            snapshot.SupplySources.Single(value => value.SourceId == TomatoSupply.Id).RemainingPackages = 999;
            snapshot.SupplyShipments.Single(value => value.ShipmentId == shipment.Id).PackageItemId = 0;
            snapshot.Items.Single(value => value.ItemId == package).Contents.Clear(); snapshot.Dispose();
            Assert.That(view.SupplySources[TomatoSupply.Id].RemainingPackages, Is.EqualTo(4));
            Assert.That(view.SupplyShipments[shipment.Id].PackageItemId, Is.EqualTo(package)); Assert.That(view.Items[package].Contents, Is.EqualTo(material));
            var joining = CoopRemote().GetComponent<GridClientComponent>();
            Assert.That(joining.SupplySources[TomatoSupply.Id].RemainingPackages, Is.EqualTo(4)); Assert.That(joining.Items[package].Contents, Is.EqualTo(material));
            Assert.That(joining.SupplyShipments[shipment.Id].PackageItemId, Is.EqualTo(package)); Assert.That(Grid.Items.Count, Is.EqualTo(10));
        }
    }
}
