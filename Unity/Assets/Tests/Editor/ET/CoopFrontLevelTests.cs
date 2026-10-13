using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopFrontLevelLastRemoteWashBlocksNaturalCompletionAndCleanBowlSurvivesContinue()
        {
            var remote = CoopFrontFixture(); ClientSessionComponent local = host.Client;
            FrontWork clear = InitialDirtyTable(); long bowl = clear.ItemId;
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            CoopFrontDo(remote, FrontWorkAction.Claim, wash); FrontWait(() => wash.State == FrontWorkState.Working); AdvanceTicks(7);
            double progress = wash.Progress; long tick = Grid.Tick;
            Assert.That(remote.GetComponent<GridClientComponent>().LevelPhase, Is.EqualTo(LevelPhase.Draining));
            LevelDo(LevelControlAction.Pause); AdvanceTicks(100); PumpMessages(8);
            Assert.That(wash.Progress, Is.EqualTo(progress)); Assert.That(Grid.Tick, Is.EqualTo(tick));
            using (var rejected = (LevelCommandResponse)Finish(remote.CallProduct(remote.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.Resume)), 500))
                Assert.That(rejected.Message, Is.EqualTo("HostControlOnly"));
            Assert.That(CurrentLevel.Paused, Is.True); Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            LevelDo(LevelControlAction.Resume); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed);
            Assert.That(wash.State, Is.EqualTo(FrontWorkState.Completed)); Assert.That(Grid.Item(bowl).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(remote.GetComponent<GridClientComponent>().LevelPhase, Is.EqualTo(LevelPhase.Completed));
            long pool = Grid.Item(bowl).HolderId; LevelDo(LevelControlAction.Continue); PumpMessages(8);
            Assert.That(Grid.Item(bowl).HolderId, Is.EqualTo(pool)); Assert.That(Grid.Station(pool).PoolItemIds, Is.EqualTo(new[] { bowl }));
            Assert.That(remote.GetComponent<GridClientComponent>().Confirmed, Is.True); Assert.That(remote.GetComponent<GridClientComponent>().LevelPhase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            PrepDo(remote, GridAction.TakeCounter, pool, bowl);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(bowl));
        }

        [Test]
        public void CoopFrontLevelNaturalZeroRepeatedContinueAndOldFrontMessagesKeepNewRolesAndInventory()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            long bowl = PrepStation("碗台").ItemId; PrepDo(remote, GridAction.TakeCounter, PrepStation("碗台").Id, bowl);
            long actorInstance = Grid.Actor(remote.MemberId).InstanceId; GridCell oldCell = Grid.Actor(remote.MemberId).Cell;
            var view = remote.GetComponent<GridClientComponent>();
            var oldFront = view.CreateFrontWorkCommand(FrontWorkAction.Claim, long.MaxValue, 0);
            var oldServe = view.CreateServeOrder(long.MaxValue, long.MaxValue, long.MaxValue, bowl);
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            var oldSnapshot = GridBroadcast.CreateSnapshot(Grid, peer);
            long[] items = Grid.Items.Keys.ToArray(); string world = Grid.WorldId;
            CoopFrontStart(); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 3000);
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.Stars, Is.Zero);
            Assert.That(view.LevelPhase, Is.EqualTo(LevelPhase.Completed)); Assert.That(view.LevelScore, Is.Zero);
            var localView = local.GetComponent<GridClientComponent>();
            var next = localView.CreateLevelCommand(LevelControlAction.Continue);
            var context = new ProductCommandContext(next.MatchId, next.WorldId, next.WorldGeneration, next.LevelId, next.LevelGeneration, next.BindingGeneration, next.CommandId);
            int nextConfig = next.NextLevelConfigId;
            using (var accepted = LevelReply(local, next)) Assert.That(accepted.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8); long installed = CurrentLevel.Id;
            FrontWait(() => Buff.Prepared, 8);
            var replay = ProductMessages.Create<LevelCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId; value.WorldGeneration = context.WorldGeneration;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId;
                value.Action = (int)LevelControlAction.Continue; value.NextLevelConfigId = nextConfig;
            });
            using (var repeated = LevelReply(local, replay)) { Assert.That(repeated.Error, Is.Zero); Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed)); }
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(CurrentLevel.Id, Is.EqualTo(installed)); Assert.That(Grid.WorldId, Is.EqualTo(world)); Assert.That(Grid.Items.Keys, Is.EquivalentTo(items));
            Assert.That(Grid.Actor(remote.MemberId).Parent, Is.SameAs(CurrentLevel)); Assert.That(Grid.Actor(remote.MemberId).InstanceId, Is.Not.EqualTo(actorInstance));
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground)); Assert.That(Grid.Item(bowl).Cell, Is.EqualTo(oldCell));
            Assert.That(view.LevelId, Is.EqualTo(installed)); Assert.That(view.Confirmed, Is.True); Assert.That(view.LevelPhase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(view.SelectedBuffId, Is.Zero); Assert.That(view.BuffCandidates, Is.Not.Empty); Assert.That(view.Customers, Is.Empty); Assert.That(view.Orders, Is.Empty); Assert.That(view.FrontWorks, Is.Empty);
            Assert.That(CoopFrontCall(remote, oldFront).Message, Is.EqualTo("WrongLevel"));
            Assert.That(CoopFrontCall(remote, oldServe).Message, Is.EqualTo("WrongLevel"));
            long version = view.Version; oldSnapshot.Version = version + 1000; oldSnapshot.Actors.Clear(); oldSnapshot.Items.Clear();
            view.ApplyGridSnapshot(oldSnapshot); oldSnapshot.Dispose();
            Assert.That(view.Version, Is.EqualTo(version)); Assert.That(view.LevelId, Is.EqualTo(installed)); Assert.That(view.Items.Keys, Is.EquivalentTo(items)); Assert.That(view.Confirmed, Is.True);
        }

        [Test]
        public void CoopFrontLevelPauseFreezesStartedWashAndCancelsRemoteApproachWithoutRestoringOldTarget()
        {
            var remote = CoopFrontFixture();
            FrontWork clear = InitialDirtyTable(); CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash); wash.BaseTicks = 600;
            Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id, wash.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); FrontWait(() => wash.State == FrontWorkState.Working); AdvanceTicks(8);
            Customer customer = InitialSeatedCustomer(2, 12); FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            CoopFrontDo(remote, FrontWorkAction.Claim, ask); FrontWait(() => Grid.Actor(remote.MemberId).Motion == GridMotion.Transit);
            var actor = Grid.Actor(remote.MemberId); GridCell cell = actor.Cell, next = actor.NextCell;
            double progress = wash.Progress; LevelDo(LevelControlAction.Pause); long tick = Grid.Tick;
            AdvanceTicks(30); PumpMessages(8);
            Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(wash.Progress, Is.EqualTo(progress));
            Assert.That(actor.Cell, Is.EqualTo(cell)); Assert.That(actor.NextCell, Is.EqualTo(next)); Assert.That(actor.FrontWorkId, Is.Zero);
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(ask.Progress, Is.Zero); Assert.That(actor.HasIntent, Is.False);
            Assert.That(remote.GetComponent<GridClientComponent>().LevelPaused, Is.True);
            LevelDo(LevelControlAction.Resume); AdvanceTicks(30); PumpMessages(8);
            Assert.That(wash.Progress, Is.GreaterThan(progress)); Assert.That(ask.Progress, Is.Zero); Assert.That(customer.OrderId, Is.Zero); Assert.That(actor.FrontWorkId, Is.Zero);
            Assert.That(actor.Cell, Is.EqualTo(next)); Assert.That(actor.Motion, Is.EqualTo(GridMotion.Standing)); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }
    }
}
