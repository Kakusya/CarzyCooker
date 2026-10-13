using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private ClientSessionComponent CoopFrontFixture()
        {
            ConfigureFrontFlow(interval: 100000, wait: 2000, firstArrival: 100000);
            StartHost(); var remote = CoopRemote(); CoopFrontStart(); FrontRole.NextScheduleTick = 100000;
            return remote;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CoopFrontWorkTwoPlayersAndCompanionKeepOneExecutorAndOneProgressClock(bool npcFirst)
        {
            var remote = CoopFrontFixture(); ClientSessionComponent local = host.Client;
            Customer customer = InitialSeatedCustomer(); FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            ask.BaseTicks = 400;
            if (npcFirst)
            {
                Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, ask.Id, ask.ClaimSerial)).Error, Is.Zero);
                AdvanceTicks(1); PumpMessages(8);
                CoopFrontDo(remote, FrontWorkAction.Claim, ask, "WorkBusy");
            }
            else
            {
                CoopFrontDo(remote, FrontWorkAction.Claim, ask);
                Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, ask.Id, ask.ClaimSerial)).Error, Is.Zero);
                AdvanceTicks(1); PumpMessages(8); Assert.That(FrontRole.LastWorkReason, Is.EqualTo("WorkBusy"));
            }
            CoopFrontDo(local, FrontWorkAction.Claim, ask, "WorkBusy");
            int owners = Grid.Actors.Values.Count(value => ((GridActor)value).FrontWorkId == ask.Id) + (FrontRole.WorkId == ask.Id ? 1 : 0);
            Assert.That(owners, Is.EqualTo(1)); Assert.That(ask.ClaimSerial, Is.EqualTo(1));
            Assert.That(ask.WorkerKind, Is.EqualTo(npcFirst ? FrontWorkerKind.Companion : FrontWorkerKind.Player));
            Assert.That(ask.WorkerId, Is.EqualTo(npcFirst ? FrontRole.Id : remote.MemberId));
            FrontWait(() => ask.State == FrontWorkState.Working);
            double progress = ask.Progress; AdvanceTicks(5);
            Assert.That(ask.Progress, Is.EqualTo(progress + 5d / ask.FrozenTicks).Within(1e-9));
            Assert.That(customer.OrderId, Is.Zero, "Receipt and claimant count cannot prematurely open an order");
            FrontWait(() => ask.State == FrontWorkState.Completed, 1000);
            Assert.That(CurrentLevel.Children.Values.OfType<Order>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CoopFrontWorkRemoteClearStopAndCompanionResumeKeepSameBowlAndRejectOldClaim()
        {
            var remote = CoopFrontFixture();
            FrontWork clear = InitialDirtyTable(); long bowl = clear.ItemId;
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.DirtyPool));
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            Assert.That(wash.ItemId, Is.EqualTo(bowl));
            CoopFrontDo(remote, FrontWorkAction.Claim, wash); FrontWait(() => wash.State == FrontWorkState.Working); AdvanceTicks(10);
            double beforeStop = wash.Progress; long serial = wash.ClaimSerial;
            CoopFrontDo(remote, FrontWorkAction.Stop, wash);
            Assert.That(wash.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(wash.Progress, Is.GreaterThanOrEqualTo(beforeStop));
            double stopped = wash.Progress; AdvanceTicks(10); Assert.That(wash.Progress, Is.EqualTo(stopped));
            Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id, wash.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            CoopFrontDo(remote, FrontWorkAction.Stop, wash, "ClaimChanged", serial);
            Assert.That(wash.WorkerKind, Is.EqualTo(FrontWorkerKind.Companion)); Assert.That(wash.ClaimSerial, Is.EqualTo(serial + 1));
            Assert.That(wash.Progress, Is.GreaterThanOrEqualTo(stopped)); FrontWait(() => wash.State == FrontWorkState.Completed);
            GridItem original = Grid.Item(bowl); Assert.That(original.Location, Is.EqualTo(GridItemLocation.CleanPool)); Assert.That(original.Hygiene, Is.EqualTo(ItemHygiene.Clean));
            GridStation pool = Grid.Station(original.HolderId);
            Assert.That(pool.PoolItemIds.Count(value => value == bowl), Is.EqualTo(1));
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Count(value => value.Kind == FrontWorkKind.Wash), Is.EqualTo(1));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[((Companion)FrontRole.Companion).Id], Is.EqualTo(1));
            PrepDo(remote, GridAction.TakeCounter, pool.Id, bowl);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(bowl)); Assert.That(pool.PoolItemIds.Contains(bowl), Is.False);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CoopFrontWorkRemoteLeaveOrDisconnectReleasesOriginalWashForHost(bool disconnect)
        {
            var remote = CoopFrontFixture(); ClientSessionComponent local = host.Client;
            FrontWork clear = InitialDirtyTable(); long bowl = clear.ItemId;
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            wash.BaseTicks = 600; CoopFrontDo(remote, FrontWorkAction.Claim, wash);
            FrontWait(() => wash.State == FrontWorkState.Working); AdvanceTicks(8);
            double beforeExit = wash.Progress; long serial = wash.ClaimSerial; int member = remote.MemberId;
            if (disconnect) { ((Session)remote.Session).Dispose(); Pump(900); }
            else { Assert.That(Finish(remote.LeaveProduct(), 500), Is.True); Pump(15); }
            Assert.That(Grid.Actor(member), Is.Null); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Available));
            Assert.That(wash.Progress, Is.GreaterThanOrEqualTo(beforeExit)); Assert.That(wash.Progress, Is.LessThan(1));
            double released = wash.Progress; AdvanceTicks(10); Assert.That(wash.Progress, Is.EqualTo(released));
            var participant = ((Match)Grid.Match).GetChild<Participant>(member);
            if (disconnect) Assert.That(participant.Online, Is.False); else Assert.That(participant, Is.Null);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.DirtyPool));
            CoopFrontDo(local, FrontWorkAction.Claim, wash);
            Assert.That(wash.WorkerId, Is.EqualTo(local.MemberId)); Assert.That(wash.ClaimSerial, Is.EqualTo(serial + 1));
            Assert.That(wash.Progress, Is.GreaterThanOrEqualTo(released)); FrontWait(() => wash.State == FrontWorkState.Completed, 1000);
            GridStation pool = Grid.Station(Grid.Item(bowl).HolderId);
            Assert.That(pool.PoolItemIds.Count(value => value == bowl), Is.EqualTo(1));
            Assert.That(Grid.Item(bowl).Hygiene, Is.EqualTo(ItemHygiene.Clean)); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            if (CurrentLevel.Phase == LevelPhase.Completed) LevelDo(LevelControlAction.Continue);
            PrepDo(local, GridAction.TakeCounter, pool.Id, bowl);
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.EqualTo(bowl));
        }
    }
}
