using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(RestaurantRenovation))]
    public sealed partial class ProductSessionTests
    {
        private RestaurantRenovation Renovation => Restaurant.GetComponent<RestaurantRenovation>();
        private GridClientComponent RenovationView => ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
        private HostPeer RenovationPeer => ((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer;

        private ChooseRenovationResponse RenovationReply(ChooseRenovationRequest request)
        {
            ClientSessionComponent client = host.Client;
            var task = client.CallProduct(request); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return (ChooseRenovationResponse)task.GetAwaiter().GetResult();
        }

        private void RenovationChoose(int definition, string rejection = null)
        {
            PumpMessages(8);
            using (var receipt = RenovationReply(RenovationView.CreateRenovationChoice(definition, RenovationView.RenovationChoiceVersion)))
                Assert.That(receipt.Error, Is.Zero, receipt.Message);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(RenovationPeer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), RenovationPeer.LastCommandReason);
            Assert.That(RenovationPeer.LastCommandReason, Is.EqualTo(rejection));
        }

        private void RenovationComplete()
        {
            if (CurrentLevel.Phase == LevelPhase.Preparing) LevelDo(LevelControlAction.Start);
            for (int i = 0; i < 1800 && CurrentLevel.Phase != LevelPhase.Completed; ++i) { AdvanceTicks(1); PumpMessages(3); }
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); PumpMessages(8);
            Assert.That(CurrentLevel.Stars, Is.Zero);
        }

        [Test]
        public void SoloRenovationChoiceZeroStarsLatestReplayLockAndOldLevelUseRealMailboxes()
        {
            StartSolo(); RenovationComplete(); long oldLevel = CurrentLevel.Id;
            Assert.That(Renovation.Candidates, Is.EqualTo(new[] { 1, 2 })); Assert.That(Renovation.ResultLevelId, Is.EqualTo(oldLevel));
            var request = RenovationView.CreateRenovationChoice(1, RenovationView.RenovationChoiceVersion);
            var pendingReplay = CopyReplayRequest(request); var finalReplay = CopyReplayRequest(request);
            var oldLevelReplay = CopyReplayRequest(request); var conflict = CopyReplayRequest(request); conflict.DefinitionId = 2;
            using (var reply = RenovationReply(request)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            using (var reply = RenovationReply(pendingReplay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Renovation.Choices.Count, Is.EqualTo(1)); Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            using (var reply = RenovationReply(finalReplay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            using (var reply = RenovationReply(conflict)) Assert.That(reply.Message, Is.EqualTo("CommandConflict"));
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Renovation.AppliedStations, Is.Empty);
            RenovationChoose(2, "RenovationLocked"); Assert.That(Renovation.SelectedDefinitionId, Is.EqualTo(1));
            LevelDo(LevelControlAction.Continue);
            using (var reply = RenovationReply(oldLevelReplay)) Assert.That(reply.Message, Is.EqualTo("WrongLevel"));
            Assert.That(Renovation.AppliedStations.Keys, Is.EqualTo(new[] { 1 })); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
        }

        [Test]
        public void SoloRenovationChoiceInvalidOrStaleResultDoesNotAlterCandidatesOrKitchen()
        {
            StartSolo(); RenovationComplete(); int[] candidates = Renovation.Candidates.ToArray();
            var request = RenovationView.CreateRenovationChoice(1, RenovationView.RenovationChoiceVersion - 1);
            using (var reply = RenovationReply(request)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8); Assert.That(RenovationPeer.LastCommandReason, Is.EqualTo("RenovationChanged"));
            RenovationChoose(999, "InvalidRenovationChoice");
            Assert.That(Renovation.Candidates, Is.EqualTo(candidates)); Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
        }

        [Test]
        public void SoloRenovationChoiceUnavailableAndExhaustedPoolsContinueWithoutFreeItems()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            RenovationComplete(); RenovationChoose(1); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Stations.Count, Is.EqualTo(17)); RenovationComplete();
            Assert.That(Renovation.Candidates, Is.EqualTo(new[] { 2 })); RenovationChoose(2); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Stations.Count, Is.EqualTo(18)); RenovationComplete();
            Assert.That(Renovation.Candidates, Is.Empty); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Stations.Count, Is.EqualTo(18)); Assert.That(Renovation.AppliedStations.Count, Is.EqualTo(2));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            foreach (long id in Renovation.AppliedStations.Values.SelectMany(value => value))
                Assert.That(Grid.Station(id).ItemId, Is.Zero);
        }

        [Test]
        public void SoloRenovationChoiceNoLegalSpaceProducesEmptyPoolAndAllowsContinue()
        {
            StartSolo(); LayoutDo(PrepStation("切配台"), new GridCell(2, 13));
            LayoutDo(PrepStation("交接台"), new GridCell(4, 12));
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray(); RenovationComplete();
            Assert.That(Renovation.Candidates, Is.Empty); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Renovation.AppliedStations, Is.Empty);
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
        }

        [Test]
        public void SoloRenovationInstallKeepsContainerManualIdentityAndOnlyAddsOneStationOnReplay()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long bowl = PrepEggBowl(client);
            long egg = Grid.Item(bowl).Contents.Single(); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 4);
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
            GridManualProcess process = Grid.InputProcess(egg); RenovationComplete();
            long processId = process.Id; int progress = process.ProgressTicks, duration = process.RequiredTicks;
            Assert.That(progress, Is.LessThan(duration)); long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            RenovationChoose(1);
            var request = RenovationView.CreateLevelCommand(LevelControlAction.Continue); var replay = CopyReplayRequest(request);
            using (var receipt = LevelReply(client, request)) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            using (var receipt = LevelReply(client, replay)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.Stations.Count, Is.EqualTo(17)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { egg })); Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground));
            Assert.That(Grid.Process(processId), Is.SameAs(process)); Assert.That(process.ProgressTicks, Is.EqualTo(progress)); Assert.That(process.RequiredTicks, Is.EqualTo(duration));
            Assert.That(Renovation.AppliedStations[1].Count, Is.EqualTo(1)); Assert.That(Grid.Station(Renovation.AppliedStations[1].Single()).Parent, Is.SameAs(Grid));
        }

        [Test]
        public void SoloRenovationLayoutLateGroundConflictRejectsWholeContinueWithoutPartialInstall()
        {
            StartSolo(); RenovationComplete(); RenovationChoose(1);
            long level = CurrentLevel.Id, version = Grid.LayoutVersion;
            GridStation original = PrepStation("备料台"); GridItem item = Grid.Item(original.ItemId); original.ItemId = 0;
            item.Location = GridItemLocation.Ground; item.HolderId = 0; item.Cell = new GridCell(2, 13);
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            LevelDo(LevelControlAction.Continue, "RenovationOccupied");
            Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            Assert.That(Renovation.SelectedDefinitionId, Is.EqualTo(1)); Assert.That(Renovation.AppliedStations, Is.Empty);
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Grid.LayoutVersion, Is.EqualTo(version));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items)); Assert.That(item.Cell, Is.EqualTo(new GridCell(2, 13)));
        }

        [Test]
        public void SoloRenovationLayoutBlocksDisconnectedGeometryAndPreservesRelationships()
        {
            StartSolo(); long[] stations = Grid.Stations.Keys.OrderBy(id => id).ToArray(); long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            var barrier = Enumerable.Range(1, 5).Select(x => new GridStationPlacement(1, new GridCell(x, 7), 0)).ToArray();
            Assert.That(Grid.CheckRenovationInstallation(barrier, out _), Is.EqualTo("RenovationBlocked"));
            var occupied = new[] { new GridStationPlacement(1, PrepStation("碗台").Cell, 0) };
            Assert.That(Grid.CheckRenovationInstallation(occupied, out _), Is.EqualTo("RenovationBlocked"));
            long bowl = PrepStation("碗台").ItemId; Grid.Item(bowl).Contents.Add(long.MaxValue);
            Assert.That(Grid.CheckRenovationInstallation(new[] { new GridStationPlacement(1, new GridCell(2, 13), 0) }, out _), Is.EqualTo("RenovationRelationshipsChanged"));
            Assert.That(Grid.Stations.Keys.OrderBy(id => id), Is.EqualTo(stations)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
        }

        [Test]
        public void SoloRenovationLayoutNewStationUsesOriginalMoveRuleAndIndependentProjection()
        {
            StartSolo(); RenovationComplete(); RenovationChoose(1); LevelDo(LevelControlAction.Continue);
            long stationId = Renovation.AppliedStations[1].Single(); GridStation station = Grid.Station(stationId);
            LayoutDo(station, new GridCell(2, 12)); Assert.That(station.Cell, Is.EqualTo(new GridCell(2, 12)));
            var snapshot = GridBroadcast.CreateSnapshot(Grid, RenovationPeer); snapshot.Version++;
            RenovationView.ApplyGridSnapshot(snapshot); snapshot.Dispose();
            Assert.That(RenovationView.RenovationApplied[1], Is.EqualTo(new[] { stationId }));
            Assert.That(RenovationView.Stations[stationId].Cell, Is.EqualTo(station.Cell));
            LevelDo(LevelControlAction.Start); LayoutDo(station, new GridCell(2, 13), rejection: "WrongPhase");
            Assert.That(station.Cell, Is.EqualTo(new GridCell(2, 12))); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
        }
    }
}
