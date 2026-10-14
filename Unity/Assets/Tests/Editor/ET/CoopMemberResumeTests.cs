using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(ProductResumeIntent))]
    public sealed partial class ProductSessionTests
    {
        private ProductResumeRequest ResumeRequest(string match, string credential, long attempt) => ProductMessages.Create<ProductResumeRequest>(value =>
        {
            value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = match;
            value.ResumeCredential = credential; value.AttemptId = attempt;
            value.PlatformId = ProductSessionRules.CurrentPlatformId;
        });

        [Test]
        public void CoopMemberResumeRealKcpCurrentLevelEmptyHandsOneMemberAndOldProjectionRejected()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            Match match = ((HostSessionComponent)host.Authority).Match;
            int member = remote.MemberId, count = match.Children.Values.OfType<Participant>().Count(); long generation = remote.BindingGeneration;
            var peer = (HostPeer)match.GetChild<Participant>(member).BoundPeer;
            var stale = GridBroadcast.CreateSnapshot(Grid, peer);
            long bowl = PrepStation("碗台").ItemId; PrepDo(remote, GridAction.TakeCounter, PrepStation("碗台").Id, bowl);
            ((Session)remote.Session).Dispose(); Pump(240);
            Assert.That(runtime.Stage, Is.EqualTo(ProductStage.Disconnected)); Assert.That(((ProcessInnerSender)runtime.Sender).IsStopped, Is.False);
            Assert.That(Grid.Actor(member), Is.Null); Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground));
            Assert.Throws<InvalidOperationException>(() => remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move));
            CoopRenovationComplete(); LevelDo(LevelControlAction.Continue);
            var recovering = runtime.ResumeProduct();
            Assert.That(((ClientSessionComponent)runtime.Client).GetComponent<GridClientComponent>().Confirmed, Is.False);
            PumpUntil(() => recovering.Status != UniTaskStatus.Pending, "manual Resume confirmation", 500, true); recovering.GetAwaiter().GetResult();
            remote = runtime.Client;
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "Resume full projection", 100, true);
            Assert.That(remote.MemberId, Is.EqualTo(member)); Assert.That(remote.BindingGeneration, Is.GreaterThan(generation));
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(count));
            Assert.That(Grid.Actor(member).HandItemId, Is.Zero); Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground));
            var view = remote.GetComponent<GridClientComponent>(); long level = view.LevelId, version = view.Version;
            stale.Version = long.MaxValue; view.ApplyGridSnapshot(stale); stale.Dispose();
            Assert.That(view.LevelId, Is.EqualTo(level)); Assert.That(view.Version, Is.EqualTo(version));
            PrepDo(remote, GridAction.Move, cell: new GridCell(1, 9));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        public void CoopMemberResumeRealKcpRejectsWrongMatchRandomCredentialAndRevokedLeave()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            ProductResumeIntent intent = runtime.GetComponent<ProductResumeIntent>(); string match = intent.MatchId, credential = intent.Credential;
            var wrong = CreateRemoteClient();
            using (var reply = (ProductResumeResponse)Finish(wrong.CallProduct(ResumeRequest("wrong", credential, 1)), 500))
                Assert.That(reply.Error, Is.EqualTo(ProductError.WrongMatch));
            var random = CreateRemoteClient();
            using (var reply = (ProductResumeResponse)Finish(random.CallProduct(ResumeRequest(match, "random", 2)), 500))
                Assert.That(reply.Error, Is.EqualTo(ProductError.InvalidRequest));
            Assert.That(Finish(remote.LeaveProduct(), 500), Is.True);
            Assert.That(runtime.GetComponent<ProductResumeIntent>(), Is.Null);
            var revoked = CreateRemoteClient();
            using (var reply = (ProductResumeResponse)Finish(revoked.CallProduct(ResumeRequest(match, credential, 3)), 500))
                Assert.That(reply.Error, Is.EqualTo(ProductError.InvalidRequest));
            Assert.That(((Match)((HostSessionComponent)host.Authority).Match).Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void CoopMemberResumeRealKcpDuplicateAttemptAndTwoConnectionsKeepOneBinding()
        {
            StartHost(); var original = CoopRemote(); ProductResumeIntent intent = ((ProductRuntimeComponent)original.Runtime).GetComponent<ProductResumeIntent>();
            int member = original.MemberId; Match match = ((HostSessionComponent)host.Authority).Match;
            var first = CreateRemoteClient(); long firstGeneration;
            using (var reply = (ProductResumeResponse)Finish(first.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 1)), 500))
            {
                Assert.That(reply.Error, Is.Zero); Assert.That(reply.MemberId, Is.EqualTo(member)); firstGeneration = reply.BindingGeneration;
            }
            long actor = Grid.Actor(member).InstanceId;
            using (var reply = (ProductResumeResponse)Finish(first.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 1)), 500))
                Assert.That(reply.BindingGeneration, Is.EqualTo(firstGeneration));
            Assert.That(Grid.Actor(member).InstanceId, Is.EqualTo(actor));
            var second = CreateRemoteClient();
            using (var reply = (ProductResumeResponse)Finish(second.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 2)), 500))
            {
                Assert.That(reply.Error, Is.Zero); Assert.That(reply.BindingGeneration, Is.GreaterThan(firstGeneration));
            }
            Pump(30);
            Participant participant = match.GetChild<Participant>(member); HostPeer current = participant.BoundPeer;
            Assert.That(current.Generation, Is.EqualTo(participant.BindingGeneration)); Assert.That(participant.Online, Is.True);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(Grid.Actors.Count, Is.EqualTo(2)); Assert.That(Grid.Actor(member).HandItemId, Is.Zero);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        public void CoopMemberResumeLocalCancelDoesNotClaimLeaveAndHostStopRevokesIntent()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            int member = remote.MemberId; ((Session)remote.Session).Dispose(); Pump(240);
            runtime.CancelProductResume();
            Assert.That(runtime.GetComponent<ProductResumeIntent>().Credential, Is.Null);
            Assert.That(runtime.GetComponent<ProductResumeIntent>().Feedback, Does.Contain("未向 Host 确认 Leave"));
            Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetChild<Participant>(member), Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() => runtime.ResumeProduct().GetAwaiter().GetResult());
            runtime.StopProduct(); Assert.That(runtime.GetComponent<ProductResumeIntent>(), Is.Null);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }
    }
}
