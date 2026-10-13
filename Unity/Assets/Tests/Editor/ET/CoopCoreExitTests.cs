using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopCoreExitRealKcpDisconnectDropsSameCookingPotOnceAndKeepsClockSenderAndOfflineMember()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote(); var batch = CoopSoup(local, remote);
            long pot = batch.PotItemId, batchId = batch.Id;
            PrepDo(remote, GridAction.TakeCounter, PrepStation("灶台").Id, pot);
            var actor = Grid.Actor(remote.MemberId); GridCell cell = actor.Cell; long binding = remote.BindingGeneration;
            Assert.That(Grid.Item(pot).Location, Is.EqualTo(GridItemLocation.Hand)); int progress = batch.ProgressTicks;
            ((Session)remote.Session).Dispose(); Pump(900);
            Assert.That(Grid.Actor(remote.MemberId), Is.Null); Assert.That(Grid.Item(pot).Location, Is.EqualTo(GridItemLocation.Ground));
            Assert.That(Grid.Item(pot).Cell, Is.EqualTo(cell)); Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).Id == pot), Is.EqualTo(1));
            var member = ((Match)Grid.Match).GetChild<Participant>(remote.MemberId); Assert.That(member.Online, Is.False);
            Assert.That(Grid.AutoProcess(batchId), Is.SameAs(batch)); Assert.That(batch.ProgressTicks, Is.GreaterThan(progress));
            Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready)); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            Assert.That(Grid.SubmitGridIntent(remote.MemberId, binding, new GridIntent(999, GridAction.PickupDrop, cell, pot)), Is.EqualTo("EntryClosed"));
            long tick = Grid.Tick; Pump(20); Assert.That(Grid.Tick, Is.GreaterThan(tick)); Assert.That(Grid.Item(pot).Cell, Is.EqualTo(cell));
            PrepDo(local, GridAction.Move, cell: cell); PrepDo(local, GridAction.PickupDrop, pot);
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.EqualTo(pot)); Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batchId));
        }

        [Test]
        public void CoopCoreExitRealRemoteLeavePausesHeldWorkAndLocalCanResumeSameProcess()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            long bowl = PrepEggBowl(remote), egg = Grid.Item(bowl).Contents.Single();
            PrepDo(remote, GridAction.StartHeldWork, bowl, egg, bowl); var process = Grid.InputProcess(egg); process.RequiredTicks = 600;
            AdvanceTicks(8); GridCell cell = Grid.Actor(remote.MemberId).Cell; long binding = remote.BindingGeneration;
            Assert.That(Finish(remote.LeaveProduct(), 500), Is.True); Pump(15);
            Assert.That(((Match)Grid.Match).GetChild<Participant>(remote.MemberId), Is.Null); Assert.That(Grid.Actor(remote.MemberId), Is.Null);
            Assert.That(process.State, Is.EqualTo(GridWorkState.Paused)); int progress = process.ProgressTicks;
            AdvanceTicks(10); Assert.That(process.ProgressTicks, Is.EqualTo(progress));
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground)); Assert.That(Grid.Item(egg).HolderId, Is.EqualTo(bowl));
            Assert.That(Grid.SubmitGridIntent(remote.MemberId, binding, new GridIntent(999, GridAction.StartHeldWork, cell, bowl, egg, bowl, process.Id)), Is.EqualTo("EntryClosed"));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            PrepDo(local, GridAction.Move, cell: cell); PrepDo(local, GridAction.PickupDrop, bowl);
            PrepDo(local, GridAction.StartHeldWork, bowl, egg, bowl, process.Id); AdvanceTicks(process.RequiredTicks); PumpMessages(8);
            Assert.That(process.State, Is.EqualTo(GridWorkState.Completed)); Assert.That(Grid.Item(egg), Is.Null);
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { process.OutputItemId }));
        }

        [Test]
        public void CoopCoreExitWholeHostStopThenNewRunRejectsOldCommandAndKeepsIndependentSevenItems()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            var old = local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move, new GridCell(2, 9));
            string worldId = Grid.WorldId; long[] oldItems = Grid.Items.Keys.ToArray(); long oldLevel = CurrentLevel.Id;
            host.StopProduct(); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.True); Pump(20);
            Game.RuntimeInstanceContext.Reset(); StartHost();
            Assert.That(Grid.WorldId, Is.Not.EqualTo(worldId)); Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel));
            Assert.That(Grid.Items.Keys.Intersect(oldItems), Is.Empty); Assert.That(Grid.Items.Count, Is.EqualTo(7));
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(SaveBinding, Is.Null);
            using var response = (GridCommandResponse)Finish(((ClientSessionComponent)host.Client).CallProduct(old));
            Assert.That(response.Error, Is.Not.Zero); Assert.That(Grid.Items.Count, Is.EqualTo(7));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }
    }
}
