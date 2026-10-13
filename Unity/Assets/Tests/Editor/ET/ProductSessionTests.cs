using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace ET
{
    [Event(SceneType.All)]
    [FriendOf(typeof(ClientSessionComponent))]
    public sealed class ProductJoinedAllObserver : AEvent<Scene, ProductClientJoined>
    {
        public static int Count;
        protected override UniTask Run(Scene scene, ProductClientJoined args)
        {
            ClientSessionComponent client = scene.GetComponent<ClientSessionComponent>();
            Assert.That(client.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(client.Roster.ContainsKey(args.MemberId), Is.True);
            ++Count;
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductHost)]
    public sealed class ProductJoinedHostObserver : AEvent<Scene, ProductClientJoined>
    {
        public static int Count;
        protected override UniTask Run(Scene scene, ProductClientJoined args)
        {
            ++Count;
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(LocalMessageComponent))]
    [FriendOf(typeof(ProcessInnerSender))]
    public sealed partial class ProductSessionTests
    {
        private World world;
        private AsyncRuntime fixtureAsyncRuntime;
        private ShutdownTestClock clock;
        private GameObject viewRoot;
        private readonly List<Fiber> fibers = new();
        private ProductRuntimeComponent host;
        private static readonly FieldInfo CurrentFiber = typeof(Fiber).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp]
        public void Setup()
        {
            if (FiberManager.Instance != null || CodeTypes.Instance != null || MessageQueue.Instance != null || EntitySystemSingleton.Instance != null)
            {
                throw new InvalidOperationException("Refusing to replace an active ET World");
            }
            World.Instance.Dispose();
            fixtureAsyncRuntime = AsyncRuntime.Begin();
            world = World.Instance;
            world.AddSingleton(new Options { Process = 1 });
            world.AddSingleton<Logger, ILog>(new ShutdownTestLog());
            clock = new ShutdownTestClock();
            world.AddSingleton<TimeInfo, ITimeNow>(clock);
            world.AddSingleton<IdGenerater>();
            world.AddSingleton<ObjectPool>();
            world.AddSingleton<CodeTypes, Assembly[]>(new[] { typeof(Entity).Assembly, typeof(ProductAuthority).Assembly, typeof(Entry).Assembly, typeof(AsyncOwnerComponent).Assembly, GetType().Assembly });
            world.AddSingleton<EntitySystemSingleton>();
            world.AddSingleton<OpcodeType>();
            world.AddSingleton<EventSystem>();
            world.AddSingleton<MessageDispatcher>();
            world.AddSingleton<MessageQueue>();
            world.AddSingleton<NetServices>();
            world.AddSingleton<LogMsg>();
            Tables tables = world.AddSingleton<Tables>();
#if CLIENT
            const string configMode = "Client";
#else
            const string configMode = "ClientServer";
#endif
            tables.LoadAsync(name => UniTask.FromResult(new Luban.ByteBuf(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath, "Res/ET", configMode, "Luban", name + ".bytes"))))).GetAwaiter().GetResult();
            viewRoot = new GameObject("ProductTestView");
#if UNITY_ET_VIEW && UNITY_EDITOR
            Entity.SetRootView(viewRoot.transform);
#endif
            RuntimeInstanceContext.Reset();
            ProductJoinedAllObserver.Count = 0;
            ProductJoinedHostObserver.Count = 0;
        }

        [TearDown]
        public void Cleanup()
        {
            if (world == null)
            {
                return;
            }
            try
            {
                host?.StopProduct();
                foreach (Fiber fiber in fibers)
                {
                    CurrentFiber.SetValue(null, fiber);
                    fiber.Root.GetComponent<ProcessInnerSender>()?.Stop();
                    fiber.Dispose();
                }
            }
            finally
            {
                fibers.Clear();
                CurrentFiber.SetValue(null, null);
                world.Dispose();
                fixtureAsyncRuntime?.Stop();
                fixtureAsyncRuntime = null;
                world = null;
                host = null;
                UnityEngine.Object.DestroyImmediate(viewRoot);
                RuntimeInstanceContext.Reset();
            }
        }

        private Fiber CreateFiber()
        {
            var fiber = (Fiber)Activator.CreateInstance(typeof(Fiber), BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { 91001 + fibers.Count, 0, SceneType.ProductRoot, "ProductTest" }, null);
            fibers.Add(fiber);
            CurrentFiber.SetValue(null, fiber);
            fiber.Root.AddComponent<TimerComponent>();
            fiber.Root.AddComponent<CoroutineLockComponent>();
            fiber.Root.AddComponent<ProcessInnerSender>();
            return fiber;
        }

        private ProductRuntimeComponent StartHost(bool join = true, int port = 0)
        {
            RuntimeInstanceConfig config = RuntimeInstanceConfig.Create("product-test", "test", "host", "127.0.0.1:" + port);
            RuntimeInstanceContext.Prepare(config);
            Fiber fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(join);
            Pump(8);
            return host;
        }

        private ProductRuntimeComponent StartSolo(bool join = true, bool liveFront = false, bool liveBuff = false)
        {
            RuntimeInstanceConfig config = RuntimeInstanceConfig.Create("product-solo-test", "test", "solo", null);
            RuntimeInstanceContext.Prepare(config);
            Fiber fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            if(!liveBuff)UseOptionalBuffFixture();
            host.InitializeProduct(join);
            if (!liveFront)
            {
                RestaurantRuntime restaurant = ((Match)((HostSessionComponent)host.Authority).Match).Restaurant;
                Level level = restaurant.CurrentLevel;
                level.GetComponent<FrontLevelState>().NextArrivalOperatingTick = 100000;
                foreach (CompanionRole role in level.Children.Values.OfType<CompanionRole>()) role.NextScheduleTick = 100000;
            }
            Pump(8);
            Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Null, "Solo must not create a listener");
            return host;
        }

        private void PumpMessages(int frames = 1)
        {
            for (int i = 0; i < frames; ++i)
            {
                TimeInfo.Instance.Update();
                foreach (Fiber fiber in fibers)
                {
                    CurrentFiber.SetValue(null, fiber);
                    fiber.EntitySystem.Update();
                    fiber.EntitySystem.LateUpdate();
                    fiber.ThreadSynchronizationContext.Update();
                }
            }
        }

        private void AdvanceTicks(int ticks)
        {
            for (int i = 0; i < ticks; ++i)
            {
                clock.Milliseconds += GridKitchenRules.TickMilliseconds;
                PumpMessages();
            }
        }

        private void Pump(int frames = 1)
        {
            for (int i = 0; i < frames; ++i)
            {
                clock.Milliseconds += 15;
                PumpMessages();
            }
        }

        private T Finish<T>(UniTask<T> task, int maxFrames = 100)
        {
            for (int i = 0; task.Status == UniTaskStatus.Pending && i < maxFrames; ++i)
            {
                Pump();
            }
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "Real message pump did not settle request");
            return task.GetAwaiter().GetResult();
        }

        private ClientSessionComponent AddLocalClient()
        {
            Scene scene = EntitySceneFactory.CreateScene(host.Root(), IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "ExtraLocal");
            ClientSessionComponent client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(host);
            client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            HostPeer peer = ProductAuthority.CreatePeer(host.Authority);
            peer.AddComponent<LocalDelivery, ClientSessionComponent>(client);
            client.Local = scene.AddComponent<LocalMessageComponent, ClientSessionComponent, HostPeer>(client, peer);
            return client;
        }

        [Test]
        public void LocalJoinProbeValidationAndSingleLeaveKeepListener()
        {
            StartHost();
            ClientSessionComponent first = host.Client;
            Assert.That(first.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(first.HeartbeatStarted, Is.False);
            ClientSessionComponent second = AddLocalClient();
            second.JoinProduct().Forget();
            Pump(8);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(first.RosterVersion, Is.EqualTo(second.RosterVersion));
            using var duplicate = (ProductJoinResponse)Finish(second.CallProduct(ProductJoinRequest.Create()));
            Assert.That(duplicate.Error, Is.EqualTo(ProductError.IncompatibleProtocol));
            var repeat = ProductJoinRequest.Create();
            repeat.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            using var joined = (ProductJoinResponse)Finish(second.CallProduct(repeat));
            Assert.That(joined.MemberId, Is.EqualTo(second.MemberId));
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            using ProductProbeResponse probe = Finish(second.ProbeProduct("second"));
            Assert.That(probe.MemberId, Is.EqualTo(2));
            var wrong = ProductProbeRequest.Create();
            wrong.MatchId = "wrong";
            wrong.Sequence = 1;
            wrong.Payload = "x";
            using var rejected = (ProductProbeResponse)Finish(second.CallProduct(wrong));
            Assert.That(rejected.Error, Is.EqualTo(ProductError.WrongMatch));
            Assert.That(Finish(first.LeaveProduct()), Is.True);
            Assert.That(Finish(first.LeaveProduct()), Is.False);
            Pump(4);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            using ProductProbeResponse remaining = Finish(second.ProbeProduct("after-local-leave"));
            Assert.That(remaining.Error, Is.Zero);
        }

        [Test]
        public void ClosingBeforeSharedCommitRejectsAndDrainsMailbox()
        {
            StartHost();
            ClientSessionComponent client = AddLocalClient();
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            EntityRef<HostPeer> peerRef = peer;
            var request = ProductJoinRequest.Create();
            request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            UniTask<IResponse> pending = client.CallProduct(request);
            Pump(); // Peer 接收后共同控制请求已入队，尚未提交。
            peer.CloseEntry();
            Assert.That(peer.InFlight, Is.GreaterThan(0));
            Assert.That(peer.IsDisposed, Is.False);
            using var response = (ProductJoinResponse)Finish(pending);
            Assert.That(response.Error, Is.EqualTo(ProductError.PeerClosed));
            Pump(8);
            Assert.That((HostPeer)peerRef, Is.Null);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            Assert.That(match.RosterVersion, Is.EqualTo(1));
        }

        [Test]
        public void CloseAfterJoinCommitRetainsOfflineMemberAndNewPeerGetsNewId()
        {
            StartHost();
            ClientSessionComponent client = AddLocalClient();
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            var request = ProductJoinRequest.Create();
            request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            UniTask<IResponse> pending = client.CallProduct(request);
            Pump(2); // 共同入口已提交，答复尚未返回原调用者。
            peer.CloseEntry();
            peer.CloseEntry();
            using var response = (ProductJoinResponse)Finish(pending);
            Assert.That(response.Error, Is.Zero, "Committed Join is not rolled back by delivery loss");
            Pump(8);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Participant member = match.GetChild<Participant>(response.MemberId);
            Assert.That(member.Online, Is.False);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(match.RosterVersion, Is.EqualTo(3));
            ClientSessionComponent replacement = AddLocalClient();
            replacement.JoinProduct().Forget();
            Pump(8);
            Assert.That(replacement.MemberId, Is.EqualTo(3));
            Assert.That(((ClientSessionComponent)host.Client).Roster[2].Online, Is.False);
        }

        [Test]
        public void CandidateRosterNeedsOwnConfirmationAndCopiesPooledMessages()
        {
            StartHost();
            ClientSessionComponent client = host.Client;
            long version = client.RosterVersion;
            var snapshot = ProductRosterSnapshot.Create(true);
            snapshot.MatchId = client.MatchId;
            snapshot.BindingGeneration = client.BindingGeneration;
            snapshot.RosterVersion = version + 1;
            var member = ProductRosterMember.Create();
            member.MemberId = client.MemberId;
            member.Online = false;
            snapshot.Members.Add(member);
            client.ApplyRoster(snapshot);
            snapshot.Dispose();
            var recycled = ProductRosterSnapshot.Create(true);
            Assert.That(recycled, Is.SameAs(snapshot));
            recycled.MatchId = "foreign";
            recycled.BindingGeneration = client.BindingGeneration + 1;
            recycled.RosterVersion = version + 10;
            member.Online = true;
            client.ApplyRoster(recycled);
            Assert.That(client.Roster[client.MemberId].Online, Is.False);
            Assert.That(client.RosterVersion, Is.EqualTo(version + 1));
            recycled.Dispose();
            ClientSessionComponent waiting = AddLocalClient();
            waiting.Stage = ProductClientStage.Joining;
            var candidate = ProductRosterSnapshot.Create();
            candidate.MatchId = "unconfirmed";
            candidate.BindingGeneration = 123;
            candidate.RosterVersion = 10;
            waiting.ApplyRoster(candidate);
            Assert.That(waiting.MatchId, Is.Null);
            Assert.That(waiting.Stage, Is.EqualTo(ProductClientStage.Joining));
            Assert.That(waiting.Roster.Count, Is.Zero);
        }

        [Test]
        public void HostListeningOnlyConflictStopAndPortRelease()
        {
            StartHost(false);
            Assert.That(host.Stage, Is.EqualTo(ProductStage.HostListening));
            Assert.That((ClientSessionComponent)host.Client, Is.Null);
            IPEndPoint endpoint = NetworkHelper.ToIPEndPoint(host.ListenEndpoint);
            Assert.That(endpoint.Port, Is.GreaterThan(0));
            Exception conflict = Assert.Throws<Exception>(() => new UdpTransport(endpoint));
            Assert.That(conflict.InnerException, Is.TypeOf<SocketException>());
            host.StopProduct();
            host.StopProduct();
            using (var socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(endpoint);
            }
            Assert.That(ProductBootstrapBridge.Root, Is.Null);
            Assert.That(((ProcessInnerSender)host.Sender).requestCallback.Count, Is.Zero);
        }

        [Test]
        public void WholeStopSettlesJoiningAndDoesNotReviveOldClient()
        {
            StartHost(false);
            ClientSessionComponent client = AddLocalClient();
            client.JoinProduct().Forget();
            host.StopProduct();
            Pump(4);
            Assert.That(client.Stage, Is.Not.EqualTo(ProductClientStage.Joined));
            Assert.That(((ProcessInnerSender)host.Sender).requestCallback.Count, Is.Zero);
            Assert.That(ProductBootstrapBridge.Root, Is.Null);
            Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
        }

        private ClientSessionComponent CreateRemoteClient(string target = null, bool pooledSession = false)
        {
            Fiber fiber = CreateFiber();
            target ??= host.ListenEndpoint;
            RuntimeInstanceConfig config = RuntimeInstanceConfig.Create("remote-test", "remote", "client", connect: target);
            ProductRuntimeComponent runtime = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "RemoteTest");
            runtime.ClientScene = scene;
            ClientSessionComponent client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(runtime);
            runtime.Client = client;
            client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            var service = new KService(AddressFamily.InterNetwork, NetworkProtocol.UDP, ServiceType.Outer);
            NetComponent net = scene.AddComponent<NetComponent, AService, bool>(service, false);
            Session session;
            if (pooledSession)
            {
                session = CreatePooledSession(net, target);
            }
            else
            {
                session = net.Create(NetworkHelper.ToIPEndPoint(target));
            }
            session.AddComponent<ProductClientBinding, ClientSessionComponent>(client);
            client.Session = session;
            return client;
        }

        private static Session CreatePooledSession(NetComponent net, string target)
        {
            long channelId = NetServices.Instance.CreateConnectChannelId();
            Session session = net.AddChildWithId<Session, AService>(channelId, net.AService, true);
            session.RemoteAddress = NetworkHelper.ToIPEndPoint(target);
            net.AService.Create(session.Id, session.RemoteAddress);
            return session;
        }

        [Test]
        public void RealKcpSharesAuthorityKeepsAliveAndLeaveReturnsCommitted()
        {
            StartHost();
            ClientSessionComponent remote = CreateRemoteClient();
            remote.JoinProduct().Forget();
            Pump(80);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(remote.MemberId, Is.EqualTo(2));
            Assert.That(remote.HeartbeatStarted, Is.True);
            ClientSessionComponent local = host.Client;
            using ProductProbeResponse localResponse = Finish(local.ProbeProduct("local-concurrent"));
            using ProductProbeResponse remoteResponse = Finish(remote.ProbeProduct("remote-concurrent"));
            Assert.That(localResponse.MemberId, Is.EqualTo(1));
            Assert.That(remoteResponse.MemberId, Is.EqualTo(2));
            Assert.That(remote.RosterVersion, Is.EqualTo(local.RosterVersion));
            Pump(900); // 真 Timer/KCP 空闲跨过既有 10 秒阈值，串行心跳维持连接。
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(Finish(remote.LeaveProduct(), 300), Is.True);
            Pump(30);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            using ProductProbeResponse remaining = Finish(local.ProbeProduct("after-remote-leave"));
            Assert.That(remaining.Error, Is.Zero);
        }

        [Test]
        public void RealKcpRejectsIncompatibleProtocolWithoutRegisteringMember()
        {
            StartHost();
            ClientSessionComponent remote = CreateRemoteClient();
            var request = ProductJoinRequest.Create();
            request.ProtocolVersion = 999;
            using var response = (ProductJoinResponse)Finish(remote.CallProduct(request), 300);
            Assert.That(response.Error, Is.EqualTo(ProductError.IncompatibleProtocol));
            Pump(30);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            Assert.That(match.RosterVersion, Is.EqualTo(1));
            using ProductProbeResponse remaining = Finish(((ClientSessionComponent)host.Client).ProbeProduct("after-reject"));
            Assert.That(remaining.Error, Is.Zero);
        }

        [Test]
        public void NetworkJoinWithoutListenerFailsWithinRequestBudget()
        {
            StartHost(false);
            string target;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                target = socket.LocalEndPoint.ToString();
            }
            ClientSessionComponent client = CreateRemoteClient(target);
            client.JoinProduct().Forget();
            Pump(300);
            Assert.That(client.Stage, Is.Not.EqualTo(ProductClientStage.Joined));
            Assert.That((Session)client.Session, Is.Null);
            Assert.That(host.Stage, Is.EqualTo(ProductStage.HostListening));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.Zero);
        }

        [Test]
        public void WireInternalMessageCannotJoinOrTakeLocalIdentity()
        {
            StartHost();
            ClientSessionComponent client = CreateRemoteClient();
            client.JoinProduct().Forget();
            Pump(80);
            Assert.That(client.Stage, Is.EqualTo(ProductClientStage.Joined));
            Session session = client.Session;
            var request = ProductControlRequest.Create();
            request.Action = (int)ProductControlAction.Join;
            request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            request.PeerId = ((HostSessionComponent)host.Authority).Id;
            request.BindingGeneration = 1;
            session.Send(request);
            request.Dispose();
            Pump(80);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(match.GetChild<Participant>(2).Online, Is.False);
            using ProductProbeResponse response = Finish(((ClientSessionComponent)host.Client).ProbeProduct("after-invalid-wire"));
            Assert.That(response.MemberId, Is.EqualTo(1));
        }

        [Test]
        public void LocalJoinConfirmationAfterPeerClosedCannotPublishJoined()
        {
            StartHost(false);
            ClientSessionComponent client = AddLocalClient();
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            client.JoinProduct().Forget();
            Pump(2);
            peer.CloseEntry();
            Pump(8);
            Assert.That(client.Stage, Is.Not.EqualTo(ProductClientStage.Joined));
            Assert.That(RuntimeInstanceContext.State, Is.EqualTo(RuntimeInstanceState.Failed));
            Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
        }

        [Test]
        public void WireInvalidPayloadReturnsFailureAndKeepsCurrentMember()
        {
            StartHost();
            ClientSessionComponent client = CreateRemoteClient();
            client.JoinProduct().Forget();
            Pump(80);
            long version = client.RosterVersion;
            var probe = ProductProbeRequest.Create();
            probe.MatchId = client.MatchId;
            probe.Sequence = 1;
            probe.Payload = new string('界', 342); // 1026 UTF-8 bytes。
            using var response = (ProductProbeResponse)Finish(client.CallProduct(probe));
            Assert.That(response.Error, Is.EqualTo(ProductError.InvalidRequest));
            var leave = ProductLeaveRequest.Create();
            leave.MatchId = "";
            using var invalidLeave = (ProductLeaveResponse)Finish(client.CallProduct(leave));
            Assert.That(invalidLeave.Error, Is.EqualTo(ProductError.InvalidRequest));
            Assert.That(client.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(client.RosterVersion, Is.EqualTo(version));
            using ProductProbeResponse valid = Finish(client.ProbeProduct("valid-after-invalid"));
            Assert.That(valid.Error, Is.Zero);
        }

        [Test]
        [Category("ProductLifecycleBoundaries")]
        public void ObserversSeeCommittedClientAndAllStillMatchesProductType()
        {
            StartHost();
            Assert.That(ProductJoinedAllObserver.Count, Is.EqualTo(1));
            Assert.That(ProductJoinedHostObserver.Count, Is.Zero);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.RosterVersion, Is.EqualTo(1));
        }

        [Test]
        [Category("ProductLifecycleBoundaries")]
        public void OldSessionTimeoutCannotCompleteReusedSessionRpcId()
        {
            StartHost(false);
            string target;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                target = socket.LocalEndPoint.ToString();
            }
            ClientSessionComponent client = CreateRemoteClient(target, true);
            Session original = client.Session;
            NetComponent net = original.GetParent<NetComponent>();
            var oldRequest = ProductJoinRequest.Create();
            oldRequest.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            UniTask<IResponse> oldTask = original.Call(oldRequest, 100);
            int oldRpcId = oldRequest.RpcId;
            client.Stage = ProductClientStage.Stopping;
            original.Dispose();
            Assert.Throws<RpcException>(() => oldTask.GetAwaiter().GetResult());
            Session replacement = CreatePooledSession(net, target);
            Assert.That(ReferenceEquals(replacement, original), Is.True, "Exercise actual pooled Session reuse");
            replacement.RpcId = oldRpcId - 1;
            var request = ProductJoinRequest.Create();
            request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            UniTask<IResponse> pending = replacement.Call(request, 1000);
            Assert.That(request.RpcId, Is.EqualTo(oldRpcId));
            Pump(14); // 210ms 超过旧100ms计时，未到新1000ms预算。
            Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Pending));
            replacement.Dispose();
            Assert.Throws<RpcException>(() => pending.GetAwaiter().GetResult());
            oldRequest.Dispose();
            request.Dispose();
        }

        [Test]
        [Category("ProductLifecycleBoundaries")]
        public void NewerJoiningSnapshotWinsAndUncommittedLeaveKeepsOfflineMember()
        {
            StartHost();
            ClientSessionComponent second = AddLocalClient();
            ClientSessionComponent third = AddLocalClient();
            second.JoinProduct().Forget();
            third.JoinProduct().Forget();
            Match authorityMatch = ((HostSessionComponent)host.Authority).Match;
            HostPeer joiningPeer = ((LocalMessageComponent)second.Local).Peer;
            // 构造先于本人答复的较新通知；Join/Leave 仍走真实队列，不绑定具体调度帧数。
            var early = ProductRosterSnapshot.Create();
            early.MatchId = authorityMatch.MatchId;
            early.BindingGeneration = joiningPeer.Generation;
            early.RosterVersion = 3;
            for (int id = 1; id <= 3; ++id)
            {
                var member = ProductRosterMember.Create();
                member.MemberId = id;
                member.Online = true;
                early.Members.Add(member);
            }
            second.ApplyRoster(early);
            early.Dispose();
            Assert.That(second.Stage, Is.EqualTo(ProductClientStage.Joining));
            Assert.That(second.MatchId, Is.Null);
            Assert.That(second.CandidateVersion, Is.EqualTo(3));
            Pump(10);
            Assert.That(second.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(second.RosterVersion, Is.EqualTo(3));
            Assert.That(second.Roster.Count, Is.EqualTo(3));
            HostPeer peer = ((LocalMessageComponent)second.Local).Peer;
            var leave = ProductLeaveRequest.Create();
            leave.MatchId = second.MatchId;
            UniTask<IResponse> pending = second.CallProduct(leave);
            peer.CloseEntry();
            using var response = (ProductLeaveResponse)Finish(pending);
            Assert.That(response.Error, Is.EqualTo(ProductError.PeerClosed));
            Assert.That(response.Committed, Is.False);
            Pump(8);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.GetChild<Participant>(second.MemberId).Online, Is.False);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(3));
            Assert.That(match.RosterVersion, Is.EqualTo(4));
            using ProductProbeResponse survivor = Finish(third.ProbeProduct("after-uncommitted-leave"));
            Assert.That(survivor.Error, Is.Zero);
        }

        [Test]
        [Category("ProductNativeOwnership")]
        [Category("ProductAdmissionBounds")]
        public void BootstrapConflictCleansPartialTreeAndDirectRuntimeDisposeReleasesListener()
        {
            int port;
            using (var occupied = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                occupied.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                port = ((IPEndPoint)occupied.LocalEndPoint).Port;
                Exception failure = Assert.Throws<Exception>(() => StartHost(false, port));
                Assert.That(failure.InnerException, Is.TypeOf<SocketException>());
                Assert.That(RuntimeInstanceContext.State, Is.EqualTo(RuntimeInstanceState.Failed));
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.True);
                Assert.That(ProductBootstrapBridge.Root, Is.Null);
            }
            RuntimeInstanceContext.Reset();
            StartHost(false, port);
            IPEndPoint endpoint = NetworkHelper.ToIPEndPoint(host.ListenEndpoint);
            Assert.That(endpoint.Port, Is.EqualTo(port));
            host.Dispose();
            using (var rebound = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                rebound.Bind(endpoint);
            }
            Assert.That(ProductBootstrapBridge.Root, Is.Null);
        }

        [Test]
        [Category("ProductNativeOwnership")]
        public void UnknownWireOpcodeClosesOnlyItsSessionAndRetainsOfflineMember()
        {
            StartHost();
            ClientSessionComponent remote = CreateRemoteClient();
            remote.JoinProduct().Forget();
            Pump(80);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Session session = remote.Session;
            MemoryBuffer buffer = session.AService.Fetch();
            buffer.WriteByte(255);
            buffer.WriteByte(255);
            buffer.Position = 0;
            session.AService.Send(session.Id, buffer); // 真实 KCP，外层仅有未知 Opcode。
            Pump(80);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            Assert.That(match.GetChild<Participant>(2).Online, Is.False);
            using ProductProbeResponse response = Finish(((ClientSessionComponent)host.Client).ProbeProduct("after-decode-reject"));
            Assert.That(response.Error, Is.Zero);
        }

        [Test]
        [Category("ProductAdmissionBounds")]
        public void UnjoinedRequestsRejectedAndAcceptanceTimeoutKeepsOtherMember()
        {
            StartHost();
            ClientSessionComponent unjoined = AddLocalClient();
            var probe = ProductProbeRequest.Create();
            probe.MatchId = ((ClientSessionComponent)host.Client).MatchId;
            probe.Sequence = 1;
            probe.Payload = "before-join";
            using var localResponse = (ProductProbeResponse)Finish(unjoined.CallProduct(probe));
            Assert.That(localResponse.Error, Is.EqualTo(ProductError.NotJoined));
            ClientSessionComponent remote = CreateRemoteClient();
            var heartbeat = ProductHeartbeatRequest.Create();
            heartbeat.ProtocolVersion = ProductSessionRules.ProtocolVersion;
            heartbeat.MatchId = "before-join";
            using var response = (ProductHeartbeatResponse)Finish(remote.CallProduct(heartbeat), 200);
            Assert.That(response.Error, Is.Not.Zero);
            Scene hostScene = host.HostScene;
            Assert.That(hostScene.GetComponent<NetComponent>().ChildrenCount(), Is.EqualTo(1));
            Pump(400); // 真接入计时跨过既有5秒阈值；尚未Join，不解除验证超时。
            Assert.That(hostScene.GetComponent<NetComponent>().ChildrenCount(), Is.Zero);
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            using ProductProbeResponse survivor = Finish(((ClientSessionComponent)host.Client).ProbeProduct("after-acceptance-timeout"));
            Assert.That(survivor.Error, Is.Zero);
        }
    }
}
