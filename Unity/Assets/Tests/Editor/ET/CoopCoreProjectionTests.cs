using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopCoreProjectionSnapshotBeforeOwnConfirmationCopiesValuesButCannotOpenInput()
        {
            StartHost(); var remote = CoopRemote(); var waiting = AddLocalClient(); waiting.Stage = ProductClientStage.Joining;
            var member = ((Match)Grid.Match).GetChild<Participant>(remote.MemberId); HostPeer peer = member.BoundPeer;
            var view = waiting.GetComponent<GridClientComponent>() ?? waiting.AddComponent<GridClientComponent>();
            var snapshot = GridBroadcast.CreateSnapshot(Grid, peer); snapshot.Version++;
            view.ApplyGridSnapshot(snapshot); snapshot.Dispose();
            Assert.That(view.Confirmed, Is.False); Assert.That(waiting.MatchId, Is.Null); Assert.That(waiting.MemberId, Is.Zero);
            Assert.That(view.Items.Count, Is.EqualTo(Grid.Items.Count)); Assert.That(view.CanControlRestaurant, Is.False);
            waiting.MatchId = remote.MatchId; waiting.MemberId = remote.MemberId; waiting.BindingGeneration = remote.BindingGeneration; waiting.Stage = ProductClientStage.Joined;
            view.ConfirmGridProjection(); Assert.That(view.Confirmed, Is.True);
            Assert.That(view.Actors[remote.MemberId].Generation, Is.EqualTo(remote.BindingGeneration));
        }

        [Test]
        public void CoopCoreProjectionPooledCompleteDtoRemainsIndependentAndOldLevelHigherVersionCannotReplaceNew()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            var view = remote.GetComponent<GridClientComponent>(); HostPeer peer = ((Participant)((Match)Grid.Match).GetChild<Participant>(remote.MemberId)).BoundPeer;
            var copied = GridBroadcast.CreateSnapshot(Grid, peer); copied.Version = view.Version + 1;
            long stationId = copied.Stations[0].StationId, itemId = copied.Items[0].ItemId;
            view.ApplyGridSnapshot(copied);
            copied.Stations[0].Name = "mutated"; copied.Items[0].Name = "mutated"; copied.BuffCandidates.Clear(); copied.CanControlRestaurant = true;
            copied.Dispose();
            Assert.That(view.Stations[stationId].Name, Is.Not.EqualTo("mutated")); Assert.That(view.Items[itemId].Name, Is.Not.EqualTo("mutated"));
            Assert.That(view.BuffCandidates, Is.Not.Empty); Assert.That(view.CanControlRestaurant, Is.False);
            var old = GridBroadcast.CreateSnapshot(Grid, peer);
            var buff = CurrentLevel.GetComponent<LevelBuff>();
            using (var receipt = (ChooseBuffResponse)Finish(local.CallProduct(local.GetComponent<GridClientComponent>().CreateBuffChoice(buff.Candidates.First(), buff.DrawVersion)))) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8); LevelDo(LevelControlAction.Start);
            for (int i = 0; i < 3000 && CurrentLevel.Phase != LevelPhase.Completed; ++i) AdvanceTicks(1);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); LevelDo(LevelControlAction.Continue); Pump(40);
            long newLevel = view.LevelId, generation = view.LevelGeneration, version = view.Version;
            long[] items = view.Items.Keys.ToArray(); old.Version = version + 1000; old.Actors.Clear(); old.Items.Clear();
            view.ApplyGridSnapshot(old); old.Dispose();
            Assert.That(view.LevelId, Is.EqualTo(newLevel)); Assert.That(view.LevelGeneration, Is.EqualTo(generation)); Assert.That(view.Version, Is.EqualTo(version));
            Assert.That(view.Items.Keys, Is.EqualTo(items)); Assert.That(view.Confirmed, Is.True);
        }
    }
}
