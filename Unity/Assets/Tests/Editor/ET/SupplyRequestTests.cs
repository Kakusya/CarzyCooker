using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(CooperativeSupplyRequests))]
    public sealed partial class ProductSessionTests
    {
        private CooperativeSupplyRequests SupplyRequests => CurrentLevel.GetComponent<CooperativeSupplyRequests>();

        private void RequestSupply(ClientSessionComponent client, SupplyRequestAction action, int source = 0, string rejected = null,
            long? expectedId = null, long? expectedRevision = null)
        {
            var request = client.GetComponent<GridClientComponent>().CreateSupplyRequest(action, source, expectedId, expectedRevision);
            var task = client.CallProduct(request);
            using var reply = (SupplyRequestResponse)((LocalMessageComponent)client.Local != null
                ? FinishMessages(task, "Actual cooperative supply request local mailbox") : Finish(task, 500));
            Assert.That(reply.Error == 0, Is.EqualTo(rejected == null), reply.Message);
            Assert.That(reply.Message, Is.EqualTo(rejected));
            if (rejected == null)
            {
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Pump(12);
        }

        private static SupplyIntent ProcessSupply(SupplyRequestValue request) => new(SupplyAction.Purchase, source: request.SourceId,
            cooperativeRequestId: request.Id, cooperativeRevision: request.Revision);

        private SupplyCommandRequest CopyLinkedPurchase(SupplyCommandRequest original) => ProductMessages.Create<SupplyCommandRequest>(copy =>
        {
            copy.ProtocolVersion = original.ProtocolVersion; copy.MatchId = original.MatchId; copy.WorldId = original.WorldId;
            copy.WorldGeneration = original.WorldGeneration; copy.BindingGeneration = original.BindingGeneration;
            copy.LevelId = original.LevelId; copy.LevelGeneration = original.LevelGeneration; copy.CommandId = original.CommandId;
            copy.Action = original.Action; copy.SourceId = original.SourceId; copy.Quantity = original.Quantity;
            copy.CooperativeRequestId = original.CooperativeRequestId; copy.CooperativeRevision = original.CooperativeRevision;
        });

        [Test, Category("SupplyRequests")]
        public void EntrySupplyRequestActualKcpPublishDoesNotBuyAndTwoMembersRaceOneOriginalReservation()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            int source = TomatoSupply.Id, quota = Supply.RemainingPackages[source], items = Grid.Items.Count;
            RequestSupply(remote, SupplyRequestAction.Publish, source);
            var request = SupplyRequests.Members[remote.MemberId];
            Assert.That(request.State, Is.EqualTo(SupplyRequestState.Pending));
            Assert.That(Supply.ChildrenCount(), Is.Zero);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(quota));
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            RequestSupply(local, SupplyRequestAction.Cancel, rejected: "SupplyRequestChanged", expectedId: request.Id, expectedRevision: request.Revision);
            Assert.That(SupplyRequests.Members[remote.MemberId].Id, Is.EqualTo(request.Id));
            var localRequest = local.GetComponent<GridClientComponent>().CreateSupplyCommand(ProcessSupply(request));
            var remoteRequest = remote.GetComponent<GridClientComponent>().CreateSupplyCommand(ProcessSupply(request));
            var replay = CopyLinkedPurchase(localRequest);
            var first = local.CallProduct(localRequest); var second = remote.CallProduct(remoteRequest);
            using (var localReply = (SupplyCommandResponse)Finish(first, 500))
            using (var remoteReply = (SupplyCommandResponse)Finish(second, 500))
            {
                Assert.That(localReply.Error, Is.Zero, localReply.Message);
                Assert.That(remoteReply.Error == 0 || remoteReply.Message == "SupplyRequestHandled", Is.True, remoteReply.Message);
            }
            AdvanceTicks(2); Pump(15);
            Assert.That(Supply.RemainingPackages[source], Is.EqualTo(quota - 1));
            Assert.That(Supply.ChildrenCount(), Is.EqualTo(1));
            var ordered = SupplyRequests.Members[remote.MemberId];
            Assert.That(ordered.State, Is.EqualTo(SupplyRequestState.Ordered));
            Assert.That(Supply.GetChild<SupplyShipment>(ordered.ShipmentId), Is.Not.Null);
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            using (var repeated = (SupplyCommandResponse)Finish(local.CallProduct(replay), 500))
            {
                Assert.That(repeated.Error, Is.Zero, repeated.Message);
                Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Assert.That(Supply.ChildrenCount(), Is.EqualTo(1));
            CoopSupplyDo(remote, ProcessSupply(request), "SupplyRequestHandled");
            int egg = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Egg).Id;
            RequestSupply(local, SupplyRequestAction.Publish, egg);
            var pending = SupplyRequests.Members[local.MemberId];
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.Purchase, source: egg));
            Assert.That(SupplyRequests.Members[local.MemberId].Id, Is.EqualTo(pending.Id));
            Assert.That(SupplyRequests.Members[local.MemberId].State, Is.EqualTo(SupplyRequestState.Pending), "Independent purchases cannot silently process a request");
            var projection = remote.GetComponent<GridClientComponent>();
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            using var dto = GridBroadcast.CreateSnapshot(Grid, peer);
            dto.Version = projection.Version + 1; projection.ApplyGridSnapshot(dto);
            dto.SupplyRequests[0].ShipmentId = -1; dto.SupplyRequests.Clear();
            Assert.That(projection.SupplyRequests.Count, Is.EqualTo(2));
            Assert.That(projection.SupplyRequests.Single(value => value.MemberId == remote.MemberId).ShipmentId, Is.EqualTo(ordered.ShipmentId));
        }

        [Test, Category("SupplyRequests")]
        public void EntrySupplyRequestActualOriginalQuotaMenuPauseAndDrainingFailuresKeepPendingAndOldReplacementCannotWrite()
        {
            StartGrowth(true); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            RequestSupply(remote, SupplyRequestAction.Publish, TomatoSupply.Id);
            var original = SupplyRequests.Members[remote.MemberId];
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id, quantity: 5));
            CoopSupplyDo(local, ProcessSupply(original), "SupplyExhausted");
            CoopSupplyDo(remote, ProcessSupply(original), "SupplyExhausted");
            Assert.That(SupplyRequests.Members[remote.MemberId].State, Is.EqualTo(SupplyRequestState.Pending));
            Assert.That(Supply.ChildrenCount(), Is.EqualTo(5));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.Zero);
            int orange = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Orange).Id;
            RequestSupply(remote, SupplyRequestAction.Publish, orange);
            var restricted = SupplyRequests.Members[remote.MemberId];
            Assert.That(restricted.Id, Is.Not.EqualTo(original.Id));
            RequestSupply(remote, SupplyRequestAction.Cancel, rejected: "SupplyRequestChanged", expectedId: original.Id, expectedRevision: original.Revision);
            CoopSupplyDo(local, ProcessSupply(original), "SupplyRequestChanged");
            CoopSupplyDo(local, ProcessSupply(restricted), nameof(ProductionPermissionDecision.MenuRestricted));
            Assert.That(SupplyRequests.Members[remote.MemberId].Id, Is.EqualTo(restricted.Id));
            Assert.That(Supply.RemainingPackages[orange], Is.EqualTo(5));
            int egg = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Egg).Id;
            RequestSupply(remote, SupplyRequestAction.Publish, egg);
            var paused = SupplyRequests.Members[remote.MemberId];
            LevelDo(LevelControlAction.Pause);
            CoopSupplyDo(local, ProcessSupply(paused), "Paused");
            RequestSupply(remote, SupplyRequestAction.Cancel, rejected: "Paused");
            Assert.That(SupplyRequests.Members[remote.MemberId].State, Is.EqualTo(SupplyRequestState.Pending));
            LevelDo(LevelControlAction.Resume);
            CoopSupplyDo(remote, ProcessSupply(paused));
            Assert.That(SupplyRequests.Members[remote.MemberId].State, Is.EqualTo(SupplyRequestState.Ordered), "Same request can retry after the real paused restriction ends");
            RequestSupply(remote, SupplyRequestAction.Publish, egg);
            var draining = SupplyRequests.Members[remote.MemberId];
            CurrentLevel.GetComponent<FrontLevelState>().NextArrivalOperatingTick = 100000;
            CoopFrontStart(); FrontRole.NextScheduleTick = 100000; InitialSeatedCustomer();
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            CoopSupplyDo(local, ProcessSupply(draining), "WrongPhase");
            Assert.That(SupplyRequests.Members[remote.MemberId].State, Is.EqualTo(SupplyRequestState.Pending));
            Assert.That(Supply.ChildrenCount(), Is.EqualTo(6));
        }

        [Test, Category("SupplyRequests")]
        public void EntrySupplyRequestActualPublisherLeaveAndNextLevelKeepSameShipmentThenOriginalReceiveAndSplit()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            RequestSupply(remote, SupplyRequestAction.Publish, TomatoSupply.Id);
            var request = SupplyRequests.Members[remote.MemberId];
            CoopSupplyDo(local, ProcessSupply(request));
            var shipment = Supply.GetChild<SupplyShipment>(SupplyRequests.Members[remote.MemberId].ShipmentId);
            shipment.RequiredTicks = 1600;
            var oldProcess = local.GetComponent<GridClientComponent>().CreateSupplyCommand(ProcessSupply(request));
            using (var leave = (ProductLeaveResponse)Finish(remote.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value => value.MatchId = remote.MatchId)), 500))
            {
                Assert.That(leave.Error, Is.Zero);
            }
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(SupplyRequests.Members, Is.Empty);
            Assert.That(Supply.GetChild<SupplyShipment>(shipment.Id), Is.SameAs(shipment));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            CoopFrontStart(); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 4000);
            int progress = shipment.ProgressTicks;
            AdvanceTicks(50); Assert.That(shipment.ProgressTicks, Is.EqualTo(progress));
            FrontWait(() => Renovation.Prepared, 20);
            if (!Renovation.SelectionFinalized)
            {
                RenovationChoose(Renovation.Candidates.First());
            }
            long completedTick = Grid.Tick;
            LevelDo(LevelControlAction.Continue);
            Assert.That(SupplyRequests.Members, Is.Empty);
            Assert.That(Supply.GetChild<SupplyShipment>(shipment.Id), Is.SameAs(shipment));
            Assert.That(shipment.ProgressTicks - progress, Is.EqualTo(Grid.Tick - completedTick), "Only the real resumed business ticks advance the same shipment");
            using (var late = (SupplyCommandResponse)Finish(local.CallProduct(oldProcess), 500))
            {
                Assert.That(late.Message, Is.EqualTo("WrongLevel"));
            }
            AdvanceTicks(shipment.RequiredTicks + 1); Pump(12);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: Grid.LayoutVersion));
            var package = Grid.Item(shipment.PackageItemId); long material = package.Contents[0];
            CoopSupplyDo(local, new SupplyIntent(SupplyAction.TakePortion, station: shipment.ReceivingStationId, package: package.Id, content: material, layout: Grid.LayoutVersion));
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.EqualTo(material));
            Assert.That(package.Contents.Count, Is.EqualTo(shipment.PackageUnits - 1));
            Assert.That(Supply.ChildrenCount(), Is.EqualTo(1));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
        }
    }
}
