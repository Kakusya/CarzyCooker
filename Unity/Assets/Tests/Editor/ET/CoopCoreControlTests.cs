using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private ClientSessionComponent CoopRemote()
        {
            var remote = CreateRemoteClient();
            remote.JoinProduct().Forget();
            PumpUntil(() => remote.Stage == ProductClientStage.Joined &&
                remote.GetComponent<GridClientComponent>() is GridClientComponent view && view.Confirmed &&
                view.WorldId == Grid.WorldId && view.LevelId == CurrentLevel.Id,
                "remote Join and matching restaurant projection", 100, advanceTime: true);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(remote.GetComponent<GridClientComponent>().Confirmed, Is.True);
            return remote;
        }

        [Test]
        public void CoopCoreOwnershipRemoteHasOnlyIndependentClientAndSameWorld()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That((HostSessionComponent)runtime.Authority, Is.Null);
            Assert.That(((Scene)runtime.ClientScene).Children.Values.OfType<Match>(), Is.Empty);
            Assert.That(remote.GetComponent<GridClientComponent>().WorldId, Is.EqualTo(Grid.WorldId));
            Assert.That(remote.GetComponent<GridClientComponent>().LevelId, Is.EqualTo(CurrentLevel.Id));
            Assert.That(remote.GetComponent<GridClientComponent>().CanControlRestaurant, Is.False);
            Assert.That(((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CanControlRestaurant, Is.True);
        }

        [TestCase(LevelControlAction.Start)]
        [TestCase(LevelControlAction.Pause)]
        [TestCase(LevelControlAction.Resume)]
        [TestCase(LevelControlAction.Continue)]
        public void CoopCorePermissionsRemoteLevelControlRejectsWithoutStateWriteAndCanStillCook(LevelControlAction action)
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            long level = CurrentLevel.Id, layout = Grid.LayoutVersion; long[] items = Grid.Items.Keys.ToArray();
            var request = view.CreateLevelCommand(action); request.BindingGeneration = ((ClientSessionComponent)host.Client).BindingGeneration;
            using (var response = (LevelCommandResponse)Finish(remote.CallProduct(request), 500))
            { Assert.That(response.Error, Is.Not.Zero); Assert.That(response.Message, Is.EqualTo("HostControlOnly")); }
            Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(CurrentLevel.Paused, Is.False); Assert.That(Grid.LayoutVersion, Is.EqualTo(layout)); Assert.That(Grid.Items.Keys, Is.EqualTo(items));
            var source = PrepStation("备料台"); long tomato = source.ItemId;
            PrepDo(remote, GridAction.TakeCounter, source.Id, tomato);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(tomato));
        }

        [TestCase("layout")]
        [TestCase("buff")]
        public void CoopCorePermissionsRemoteLayoutAndBuffRejectThroughKcpWithoutMutatingSharedState(string kind)
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            var station = PrepStation("备料台"); GridCell cell = station.Cell; long layout = Grid.LayoutVersion;
            var buff = CurrentLevel.GetComponent<LevelBuff>(); int selected = buff.SelectedId;
            IRequest request = kind == "layout" ? view.CreateStationLayout(station.Id, new GridCell(2, 12), 0)
                : view.CreateBuffChoice(buff.Candidates.First(), buff.DrawVersion);
            using (var response = (MessageObject)Finish(remote.CallProduct(request), 500))
            { Assert.That(((IResponse)response).Error, Is.Not.Zero); Assert.That(((IResponse)response).Message, Is.EqualTo("HostControlOnly")); }
            AdvanceTicks(2); Assert.That(station.Cell, Is.EqualTo(cell)); Assert.That(Grid.LayoutVersion, Is.EqualTo(layout));
            Assert.That(buff.SelectedId, Is.EqualTo(selected)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
        }

        [Test]
        public void CoopCorePermissionsHostLocalBuffLayoutStartPauseResumeAndContinueUseOrderedServiceStep()
        {
            StartHost(); ClientSessionComponent local = host.Client; var view = local.GetComponent<GridClientComponent>();
            var buff = CurrentLevel.GetComponent<LevelBuff>(); int choice = buff.Candidates.First();
            using (var response = (ChooseBuffResponse)Finish(local.CallProduct(view.CreateBuffChoice(choice, buff.DrawVersion)))) Assert.That(response.Error, Is.Zero, response.Message);
            AdvanceTicks(1); PumpMessages(8); Assert.That(buff.SelectedId, Is.EqualTo(choice));
            var station = PrepStation("备料台"); LayoutDo(station, new GridCell(2, 12)); Assert.That(station.Cell, Is.EqualTo(new GridCell(2, 12)));
            LevelDo(LevelControlAction.Start); LevelDo(LevelControlAction.Pause); long tick = Grid.Tick;
            AdvanceTicks(15); Assert.That(Grid.Tick, Is.EqualTo(tick));
            LevelDo(LevelControlAction.Resume); Assert.That(Grid.Tick, Is.EqualTo(tick + 1));
            for (int i = 0; i < 3000 && CurrentLevel.Phase != LevelPhase.Completed; ++i) AdvanceTicks(1);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(Renovation.Candidates, Is.Empty, "Coop adds no renovation gate.");
            long oldLevel = CurrentLevel.Id; LevelDo(LevelControlAction.Continue);
            Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(SaveBinding, Is.Null);
        }

        [Test]
        public void CoopCorePermissionsHostBindingRecheckedAtCommitAndLocalLeaveNeverTransfersControl()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            using (var response = LevelReply(local, local.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.Pause)))
                Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            host.Client = remote; AdvanceTicks(1); PumpMessages(8);
            Assert.That(CurrentLevel.Paused, Is.False);
            HostPeer peer = ((LocalMessageComponent)local.Local).Peer; Assert.That(peer.LastCommandReason, Is.EqualTo("HostControlOnly"));
            host.Client = local; Assert.That(Finish(local.LeaveProduct()), Is.True); Pump(12);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            using var rejected = (LevelCommandResponse)Finish(remote.CallProduct(remote.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.Start)), 500);
            Assert.That(rejected.Message, Is.EqualTo("HostControlOnly")); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
        }
    }
}
