using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void CoopRenovationComplete()
        {
            if (CurrentLevel.Phase == LevelPhase.Preparing && Buff.SelectedId == 0)
            {
                FrontWait(() => Buff.Prepared, 8);
                BuffDo(Buff.Candidates.First());
            }
            RenovationComplete();
        }
        [Test]
        public void CoopRenovationDrinkLayoutInstallsSupportedAdditionWithoutMintingItems()
        {
            StartHost(liveDrinks: true); var remote = CoopRemote(); CoopRenovationComplete();
            Assert.That(Renovation.Candidates, Is.EqualTo(new[] { 5, 6 }));
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray(); RenovationChoose(5); LevelDo(LevelControlAction.Continue); Pump(30);
            long station = Renovation.AppliedStations[5].Single();
            Assert.That(Grid.Stations.Count, Is.EqualTo(22)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Station(station).ItemId, Is.Zero);
            Assert.That(remote.GetComponent<GridClientComponent>().RenovationApplied[5], Is.EqualTo(new[] { station }));
            LayoutDo(Grid.Station(station), new GridCell(2, 15)); Pump(30);
            Assert.That(remote.GetComponent<GridClientComponent>().Stations[station].Cell, Is.EqualTo(new GridCell(2, 15)));
        }

        [Test]
        public void CoopRenovationHostChoosesRemoteReadsLateJoinAndInstallsExactlyOnce()
        {
            StartHost(); var remote = CoopRemote(); CoopRenovationComplete(); Pump(30);
            var view = remote.GetComponent<GridClientComponent>();
            Assert.That(Renovation.Candidates, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(view.RenovationCandidates, Is.EqualTo(Renovation.Candidates));
            Assert.That(view.UpgradeTargets, Is.Empty); Assert.That(view.SaveEnabled, Is.False);
            using (var reply = (ChooseRenovationResponse)Finish(remote.CallProduct(view.CreateRenovationChoice(1, view.RenovationChoiceVersion)), 500))
                Assert.That(reply.Message, Is.EqualTo("HostControlOnly"));
            Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
            RenovationChoose(1); Pump(30);
            var late = CoopRemote();
            Assert.That(late.GetComponent<GridClientComponent>().SelectedRenovationId, Is.EqualTo(1));
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            var next = RenovationView.CreateLevelCommand(LevelControlAction.Continue); var replay = CopyReplayRequest(next);
            using (var reply = LevelReply(host.Client, next)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); FrontWait(() => Buff.Prepared, 8); Pump(30);
            using (var reply = LevelReply(host.Client, replay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Renovation.AppliedStations[1].Count, Is.EqualTo(1));
            long station = Renovation.AppliedStations[1].Single();
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(view.Stations.ContainsKey(station), Is.True);
            Assert.That(late.GetComponent<GridClientComponent>().RenovationApplied[1], Is.EqualTo(new[] { station }));
            LayoutDo(Grid.Station(station), new GridCell(2, 12)); Pump(30);
            Assert.That(view.Stations[station].Cell, Is.EqualTo(Grid.Station(station).Cell));
        }

        [Test]
        public void CoopRenovationLateGroundConflictRejectsContinueWithoutPartialInstallation()
        {
            StartHost(); var remote = CoopRemote(); CoopRenovationComplete(); RenovationChoose(1);
            long level = CurrentLevel.Id, layout = Grid.LayoutVersion;
            GridStation source = PrepStation("备料台"); GridItem item = Grid.Item(source.ItemId); source.ItemId = 0;
            item.Location = GridItemLocation.Ground; item.HolderId = 0; item.Cell = new GridCell(2, 13);
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            LevelDo(LevelControlAction.Continue, "RenovationOccupied");
            LevelDo(LevelControlAction.Continue, "RenovationOccupied"); Pump(30);
            Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(Grid.LayoutVersion, Is.EqualTo(layout));
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Renovation.AppliedStations, Is.Empty);
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(remote.GetComponent<GridClientComponent>().SelectedRenovationId, Is.EqualTo(1));
            Assert.That(remote.GetComponent<GridClientComponent>().RenovationFailure, Is.EqualTo("RenovationOccupied"));
            Assert.That(RenovationView.RenovationFailure, Is.EqualTo("RenovationOccupied"));
        }

        [Test]
        public void CoopRenovationOldLevelAndOldBindingCannotChangeNewChoice()
        {
            StartHost(); var remote = CoopRemote(); CoopRenovationComplete();
            var old = RenovationView.CreateRenovationChoice(1, RenovationView.RenovationChoiceVersion);
            var stale = CopyReplayRequest(old);
            using (var reply = RenovationReply(old)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8); LevelDo(LevelControlAction.Continue);
            using (var reply = RenovationReply(stale)) Assert.That(reply.Message, Is.EqualTo("WrongLevel"));
            CoopRenovationComplete();
            var binding = RenovationView.CreateRenovationChoice(2, RenovationView.RenovationChoiceVersion); --binding.BindingGeneration;
            using (var reply = RenovationReply(binding)) Assert.That(reply.Message, Is.EqualTo("EntryClosed"));
            Assert.That(Renovation.SelectedDefinitionId, Is.Zero); Assert.That(Renovation.AppliedStations[1].Count, Is.EqualTo(1));
            Assert.That(remote.GetComponent<GridClientComponent>().CanControlRestaurant, Is.False);
        }
    }
}
