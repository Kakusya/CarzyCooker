using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(Companion))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(CompanionGrowth))]
    [FriendOf(typeof(FrontWork))]
    [FriendOf(typeof(MailBoxComponent))]
    public sealed partial class ProductSessionTests
    {
        private DRCompanionConfig CompanionDefinition(int id = 1, int post = 1, int x = 2, int wash = 61)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(id); buffer.WriteString("伙伴"); buffer.WriteInt(post); buffer.WriteInt(x); buffer.WriteInt(5);
            buffer.WriteInt(30); buffer.WriteInt(20); buffer.WriteInt(wash);
            return new DRCompanionConfig(buffer);
        }

        [Test]
        public void SoloCompanionOwnershipUsesActualGeneratedConfigAndOrderedRoleMailbox()
        {
            StartSolo();
            Companion companion = Restaurant.Children.Values.OfType<Companion>().Single();
            CompanionRole role = CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
            DRCompanionConfig definition = Tables.Instance.DTCompanionConfig.Get(companion.DefinitionId);
            Assert.That(companion.Parent, Is.SameAs(Restaurant));
            Assert.That(companion.Name, Is.EqualTo(definition.Name));
            Assert.That(companion.Post, Is.EqualTo(CompanionPost.FrontService));
            Assert.That(role.Parent, Is.SameAs(CurrentLevel));
            Assert.That((Companion)role.Companion, Is.SameAs(companion));
            Assert.That(role.LevelGeneration, Is.EqualTo(CurrentLevel.Generation));
            Assert.That(role.Cell, Is.EqualTo(new GridCell(definition.SpawnX, definition.SpawnY)));
            Assert.That(Grid.IsWalkable(role.Cell), Is.True);
            Assert.That(role.WorkId, Is.Zero);
            Assert.That(role.GetComponent<MailBoxComponent>().MailBoxType, Is.EqualTo(MailBoxType.OrderedMessage));
            Assert.That(role.GetComponent<AsyncOwnerComponent>().Parent, Is.SameAs(role));
            Assert.That(role.GetComponent<AsyncOwnerComponent>().AsyncScope, Is.Not.Null);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            Assert.That(definition.WashTicks, Is.EqualTo(61), "Odd sample supports the later ceil acceptance");
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>(), Is.Empty, "No fabricated work during preparation");
            Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Null);
        }

        [TestCase(0, 1, 2, 61)]
        [TestCase(1, 0, 2, 61)]
        [TestCase(1, 1, -1, 61)]
        [TestCase(1, 1, 2, 0)]
        public void SoloCompanionOwnershipRejectsInvalidDefinition(int id, int post, int x, int wash)
        {
            Assert.Throws<InvalidOperationException>(() => CompanionConfig.Validate(
                new[] { CompanionDefinition(id, post, x, wash) }, GridKitchenSystem.IsInitialGridWalkable));
            Assert.That(host, Is.Null);
        }

        [Test]
        public void SoloCompanionOwnershipRejectsMissingIdentityBeforeHostCreation()
        {
            var rows = (System.Collections.Generic.List<DRCompanionConfig>)Tables.Instance.DTCompanionConfig.DataList;
            DRCompanionConfig[] original = rows.ToArray();
            try
            {
                rows.Clear();
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { rows.AddRange(original); }
        }

        [Test]
        public void SoloCompanionOwnershipNextLevelKeepsIdentityAndDisposesRoleWorkGrowthAndWait()
        {
            StartSolo();
            Companion companion = Restaurant.Children.Values.OfType<Companion>().Single();
            Level oldLevel = CurrentLevel;
            CompanionRole oldRole = oldLevel.Children.Values.OfType<CompanionRole>().Single();
            CompanionGrowth oldGrowth = oldLevel.GetComponent<CompanionGrowth>();
            // 合法初始历史只验证生命周期；不声称完成了顾客工作或成长行为。
            oldGrowth.CompletedByCompanion.Add(companion.Id, 3);
            GridStation source = PrepStation("碗台");
            var target = new FrontWorkDefinition(FrontWorkKind.Wash, 0, source.Id, source.ItemId, 61);
            FrontWork oldWork = oldLevel.AddChild<FrontWork, FrontWorkDefinition, long>(target, oldLevel.Generation);
            Assert.That(oldWork.Parent, Is.SameAs(oldLevel));
            Assert.That(oldWork.ItemId, Is.EqualTo(source.ItemId));
            Assert.That(oldWork.StationId, Is.EqualTo(source.Id));
            Assert.That(oldWork.BaseTicks, Is.EqualTo(target.BaseTicks));
            Assert.That(oldWork.Progress, Is.Zero);
            Assert.That(oldWork.WorkerKind, Is.EqualTo(FrontWorkerKind.None));
            Assert.That(oldWork.BlocksLevelCompletion, Is.True);
            oldWork.State = FrontWorkState.Cancelled;
            Assert.That(oldWork.BlocksLevelCompletion, Is.False);
            var owned = oldRole.GetAsyncOwner();
            var scope = owned.AsyncScope;
            var pending = scope.RunAsync(context => context.DelayAsync(fibers[0].Root.GetComponent<TimerComponent>(), 60000));
            LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1);
            LevelDo(LevelControlAction.Continue);
            Assert.That(oldLevel.IsDisposed && oldRole.IsDisposed && oldWork.IsDisposed && oldGrowth.IsDisposed, Is.True);
            Assert.That(pending.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
            Assert.That(companion.IsDisposed, Is.False);
            Assert.That(Restaurant.Children.Values.OfType<Companion>().Single(), Is.SameAs(companion));
            CompanionRole nextRole = CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
            Assert.That(nextRole, Is.Not.SameAs(oldRole));
            Assert.That((Companion)nextRole.Companion, Is.SameAs(companion));
            Assert.That(nextRole.LevelGeneration, Is.EqualTo(oldLevel.Generation + 1));
            Assert.That(nextRole.WorkId, Is.Zero);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>(), Is.Empty);
            Assert.That(fibers[0].Root.GetComponent<ProcessInnerSender>().IsStopped, Is.False);
        }

        [Test]
        public void SoloCompanionOwnershipBlockedNextSpawnPreservesCurrentTree()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1);
            Level level = CurrentLevel;
            Companion companion = Restaurant.Children.Values.OfType<Companion>().Single();
            CompanionRole role = level.Children.Values.OfType<CompanionRole>().Single();
            Grid.Blocked.Add(role.Cell);
            try
            {
                LevelDo(LevelControlAction.Continue, "InvalidCompanionSpawn");
                Assert.That(CurrentLevel, Is.SameAs(level));
                Assert.That(role.IsDisposed || companion.IsDisposed, Is.False);
            }
            finally { Grid.Blocked.Remove(role.Cell); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloCompanionOwnershipLayoutCannotCoverCurrentOrTransitCell(bool transit)
        {
            StartSolo();
            CompanionRole role = CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
            GridCell target = role.Cell;
            if (transit)
            {
                role.NextCell = new GridCell(2, 4); role.Motion = GridMotion.Transit;
                target = role.NextCell;
            }
            GridStation oven = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "烤箱");
            GridCell original = oven.Cell; long version = Grid.LayoutVersion;
            var projection = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            var request = projection.CreateStationLayout(oven.Id, target, transit ? 1 : 0);
            using (var receipt = LayoutReply(host.Client, request)) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            HostPeer peer = ((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer;
            Assert.That(peer.LastCommandReason, Is.EqualTo("LayoutOccupied"));
            Assert.That(peer.LastCommandStatus, Is.EqualTo(GridResultStatus.Rejected));
            Assert.That(oven.Cell, Is.EqualTo(original)); Assert.That(Grid.LayoutVersion, Is.EqualTo(version));
            Assert.That(Grid.Blocked.Contains(role.Cell), Is.False);
            Assert.That(Grid.Blocked.Contains(role.NextCell), Is.False);
        }

        [Test]
        public void SoloCompanionOwnershipWholeExitDisposesIdentityAndOwnedRole()
        {
            StartSolo();
            Companion companion = Restaurant.Children.Values.OfType<Companion>().Single();
            CompanionRole role = CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
            CompanionGrowth growth = CurrentLevel.GetComponent<CompanionGrowth>();
            AsyncOwnerComponent owner = role.GetAsyncOwner();
            host.StopProduct();
            Assert.That(companion.IsDisposed && role.IsDisposed && growth.IsDisposed && owner.IsDisposed, Is.True);
        }
    }
}
