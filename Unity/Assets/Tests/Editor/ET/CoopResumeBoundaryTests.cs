using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed class ResumeRecordingLog : ILog
    {
        public readonly List<string> DebugMessages = new();
        public void Trace(string message) { }
        public void Warning(string message) { }
        public void Info(string message) { }
        public void Debug(string message) { DebugMessages.Add(message); }
        public void Error(string message) { UnityEngine.Debug.LogError(message); }
        public void Error(Exception error) { UnityEngine.Debug.LogException(error); }
    }

    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopMemberResumeBoundaryCredentialMessagesDoNotReachDebugLogButOrdinaryMessagesDo()
        {
            var log = new ResumeRecordingLog(); Logger.Instance.Awake(log); Fiber fiber = CreateFiber();
            const string secret = "credential-must-not-be-logged";
            using (var message = ProductJoinResponse.Create()) { message.ResumeCredential = secret; LogMsg.Instance.Debug(fiber, message); }
            using (var message = ProductControlRequest.Create()) { message.ResumeCredential = secret; LogMsg.Instance.Debug(fiber, message); }
            using (var message = ProductControlResponse.Create()) { message.ResumeCredential = secret; LogMsg.Instance.Debug(fiber, message); }
            using (var message = ProductResumeRequest.Create()) { message.ResumeCredential = secret; LogMsg.Instance.Debug(fiber, message); }
            using (var message = ProductResumeResponse.Create()) { LogMsg.Instance.Debug(fiber, message); }
            Assert.That(log.DebugMessages, Is.Empty);
            using (var message = ProductProbeRequest.Create()) { message.Payload = "ordinary-debug"; LogMsg.Instance.Debug(fiber, message); }
            Assert.That(log.DebugMessages.Count, Is.EqualTo(1)); Assert.That(log.DebugMessages.Single(), Does.Contain("ordinary-debug"));
            Assert.That(log.DebugMessages.Single(), Does.Not.Contain(secret));
        }

        [Test]
        public void CoopMemberResumeBoundaryRealKcpLostReceiptNewAttemptAndOldRequestCannotReplaceBinding()
        {
            StartHost(); var original = CoopRemote(); ProductResumeIntent intent = ((ProductRuntimeComponent)original.Runtime).GetComponent<ProductResumeIntent>();
            int member = original.MemberId; Match match = ((HostSessionComponent)host.Authority).Match;
            Participant participant = match.GetChild<Participant>(member); long generation = participant.BindingGeneration;
            var stale = original.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move, new GridCell(1, 9));
            var first = CreateRemoteClient(); var lost = first.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 41));
            PumpUntil(() => participant.BindingGeneration != generation, "Resume committed before wire receipt", 100, true);
            ((Session)first.Session).Dispose();
            PumpUntil(() => lost.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending, "Closed session retires lost confirmation", 20, true);
            Assert.Catch<Exception>(() => lost.GetAwaiter().GetResult());
            var next = CreateRemoteClient();
            using (var response = (ProductResumeResponse)Finish(next.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 42)), 500))
            {
                Assert.That(response.Error, Is.Zero); Assert.That(response.MemberId, Is.EqualTo(member));
                next.InstallResumeConfirmation(response);
            }
            Pump(30); long current = participant.BindingGeneration;
            using (var rejected = (GridCommandResponse)Finish(next.CallProduct(stale), 500))
                Assert.That(rejected.Message, Is.EqualTo("EntryClosed"));
            Assert.That(participant.BindingGeneration, Is.EqualTo(current));
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(Grid.Actor(member).HandItemId, Is.Zero); Assert.That(Grid.Actors.Count, Is.EqualTo(2));
        }

        [Test]
        public void CoopMemberResumeBoundaryRealHostClosedNoticeStopsClientAndRemovesCredential()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            Match match = ((HostSessionComponent)host.Authority).Match; HostPeer peer = match.GetChild<Participant>(remote.MemberId).BoundPeer;
            ProductDelivery.Send(peer, ProductMessages.Create<ProductMatchClosed>(message =>
            {
                message.MatchId = match.MatchId; message.BindingGeneration = peer.Generation; message.Reason = "HostStopped";
            }));
            PumpUntil(() => runtime.Stage == ProductStage.Stopped, "Explicit Host close received by remote", 100, true);
            Assert.That(runtime.GetComponent<ProductResumeIntent>(), Is.Null);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            host.StopProduct(); Assert.That((HostSessionComponent)host.Authority, Is.Null);
        }

        [Test]
        public void CoopMemberResumeBoundaryRealKcpOtherPlatformCannotUseValidCredential()
        {
            StartHost(); var original = CoopRemote(); ProductResumeIntent intent = ((ProductRuntimeComponent)original.Runtime).GetComponent<ProductResumeIntent>();
            var foreign = CreateRemoteClient(); var request = ResumeRequest(intent.MatchId, intent.Credential, 31);
            request.PlatformId = ProductSessionRules.CurrentPlatformId == 1 ? 2 : 1;
            using (var rejected = (ProductResumeResponse)Finish(foreign.CallProduct(request), 500))
                Assert.That(rejected.Message, Is.EqualTo("PlatformMismatch"));
            Assert.That(Grid.Actor(original.MemberId).Generation, Is.EqualTo(original.BindingGeneration));
            Assert.That(((Match)((HostSessionComponent)host.Authority).Match).Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void CoopMemberResumeBoundaryEarlierUnboundConnectionCannotRollBackGeneration()
        {
            StartHost(); var original = CoopRemote(); ProductResumeIntent intent = ((ProductRuntimeComponent)original.Runtime).GetComponent<ProductResumeIntent>();
            int member = original.MemberId;
            var earlier = CreateRemoteClient();
            using (var rejected = (ProductProbeResponse)Finish(earlier.CallProduct(ProductMessages.Create<ProductProbeRequest>(value =>
                { value.MatchId = intent.MatchId; value.Sequence = 1; value.Payload = "before-binding"; })), 500))
                Assert.That(rejected.Error, Is.EqualTo(ProductError.PeerClosed));
            // 显式冻结较早的未绑定接入，不依赖失败回执的关闭宽限计时。
            Session entry = ((Scene)host.HostScene).GetComponent<NetComponent>().Children.Values.OfType<Session>()
                .Single(session => session.GetComponent<ProductPeerBinding>() == null);
            HostPeer earlyPeer = ProductAuthority.CreatePeer(host.Authority);
            earlyPeer.AddComponent<RemoteDelivery, Session>(entry); entry.AddComponent<ProductPeerBinding, HostPeer>(earlyPeer);
            var later = CreateRemoteClient(); long generation;
            using (var response = (ProductResumeResponse)Finish(later.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 71)), 500))
            {
                Assert.That(response.Error, Is.Zero); generation = response.BindingGeneration;
            }
            using (var response = (ProductResumeResponse)Finish(earlier.CallProduct(ResumeRequest(intent.MatchId, intent.Credential, 72)), 500))
            {
                Assert.That(response.Error, Is.Zero); Assert.That(response.BindingGeneration, Is.GreaterThan(generation));
                Assert.That(response.MemberId, Is.EqualTo(member));
            }
        }

        [Test]
        public void CoopMemberResumeBoundaryTimeoutKeepsUnknownFeedbackAndDoesNotLeaveOrReplay()
        {
            StartHost(); var remote = CoopRemote(); ProductRuntimeComponent runtime = remote.Runtime;
            int member = remote.MemberId; ((Session)remote.Session).Dispose(); Pump(240);
            ProductResumeIntent intent = runtime.GetComponent<ProductResumeIntent>();
            using var unanswered = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
            intent.Endpoint = unanswered.Client.LocalEndPoint.ToString();
            var attempt = runtime.ResumeProduct();
            PumpUntil(() => attempt.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending, "Owned unresponsive endpoint times out", 500, true);
            attempt.GetAwaiter().GetResult();
            Assert.That(runtime.Stage, Is.EqualTo(ProductStage.Disconnected)); Assert.That(intent.AttemptId, Is.EqualTo(1));
            Assert.That(intent.Feedback, Does.Contain("原请求可能已经提交")); Assert.That(intent.Credential, Is.Not.Empty);
            Assert.That(((ClientSessionComponent)runtime.Client).NextCommandId, Is.Zero);
            Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetChild<Participant>(member), Is.Not.Null);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }
    }
}
