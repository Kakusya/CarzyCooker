#if !DOTNET
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(LocalMessageComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(ProductClientBinding))]
    public static partial class ClientSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientSessionComponent self, ProductRuntimeComponent runtime)
        {
            self.Runtime = runtime;
            self.Stage = ProductClientStage.Connecting;
            self.MatchId = null;
            self.MemberId = 0;
            self.BindingGeneration = 0;
            self.RosterVersion = 0;
            self.Roster.Clear();
            self.Local = default;
            self.Session = default;
            self.HeartbeatStarted = false;
            self.HeartbeatInFlight = false;
            self.NextProbeSequence = 0;
            self.LastProbePayload = null;
            self.LastProbeMemberId = 0;
            self.CandidateMatchId = null;
            self.CandidateGeneration = 0;
            self.CandidateVersion = 0;
            self.CandidateRoster.Clear();
            self.AddComponent<GridClientComponent>();
        }

        [EntitySystem]
        private static void Destroy(this ClientSessionComponent self)
        {
            self.Stage = ProductClientStage.Stopped;
            LocalMessageComponent local = self.Local;
            HostPeer peer = local?.Peer ?? default;
            self.Local = default;
            peer?.CloseEntry();
            Session session = self.Session;
            self.Session = default;
            session?.Dispose();
            self.Roster.Clear();
            self.CandidateRoster.Clear();
            self.Runtime = default;
        }

        public static async UniTask<IResponse> CallProduct(this ClientSessionComponent self, IRequest request, int timeout = ProductSessionRules.RequestTimeout)
        {
            LocalMessageComponent local = self.Local;
            if (local == null)
            {
                Session session = self.Session;
                if (session == null)
                {
                    ((MessageObject)request).Dispose();
                    throw new InvalidOperationException("Product session unavailable");
                }
                try
                {
                    return await session.Call(request, timeout);
                }
                finally
                {
                    ((MessageObject)request).Dispose();
                }
            }
            HostPeer peer = local.Peer;
            if (peer == null || !peer.BeginEntry())
            {
                ((MessageObject)request).Dispose();
                throw new RpcException(ProductError.PeerClosed, "Product local entry closed");
            }
            EntityRef<HostPeer> peerRef = peer;
            try
            {
                return await local.Root().GetComponent<ProcessInnerSender>().Call(peer.GetActorId(), request, false);
            }
            finally
            {
                peer = peerRef;
                if (peer != null)
                {
                    peer.EndEntry();
                }
            }
        }

        public static async UniTask<T> CallProductValueAsync<T>(this ClientSessionComponent self, AsyncContext context, IRequest request, Func<IResponse, T> read)
        {
            try { context.ThrowIfStopped(); }
            catch { ((MessageObject)request).Dispose(); throw; }
            LocalMessageComponent local = self.Local;
            if (local == null)
            {
                Session session = self.Session;
                if (session == null) { ((MessageObject)request).Dispose(); throw new InvalidOperationException("Product session unavailable"); }
                return await context.CallSessionValueAsync(session, request, read, ProductSessionRules.RequestTimeout);
            }
            HostPeer peer = local.Peer;
            if (peer == null || !peer.BeginEntry()) { ((MessageObject)request).Dispose(); throw new RpcException(ProductError.PeerClosed, "Product local entry closed"); }
            EntityRef<HostPeer> peerRef = peer;
            try { return await context.CallLocalValueAsync(local.Root().GetComponent<ProcessInnerSender>(), peer.GetActorId(), request, read); }
            finally { peer = peerRef; peer?.EndEntry(); }
        }

        public static async UniTask JoinProduct(this ClientSessionComponent self, int version = ProductSessionRules.ProtocolVersion)
        {
            if (self.Stage != ProductClientStage.Connecting)
            {
                throw new InvalidOperationException("Product Join already started");
            }
            self.Stage = ProductClientStage.Joining;
            ProductRuntimeComponent runtime = self.Runtime;
            runtime.Report(ProductStage.Joining);
            EntityRef<ClientSessionComponent> selfRef = self;
            try
            {
                using var response = (ProductJoinResponse)await self.CallProduct(ProductMessages.Create<ProductJoinRequest>(message => { message.ProtocolVersion = version; }));
                self = selfRef;
                if (self == null || self.Stage != ProductClientStage.Joining)
                {
                    return;
                }
                if (response.Error != 0)
                {
                    throw new RpcException(response.Error, response.Message ?? "Product Join rejected");
                }
                if (!self.HasCurrentEntry(response.BindingGeneration))
                {
                    throw new RpcException(ProductError.PeerClosed, "Product Join entry closed before confirmation");
                }
                if (string.IsNullOrEmpty(response.MatchId) || response.MemberId <= 0 || response.BindingGeneration <= 0 || response.RosterVersion <= 0)
                {
                    throw new InvalidOperationException("Invalid Product Join confirmation");
                }
                var members = CopyMembers(response.Members);
                if (!members.TryGetValue(response.MemberId, out ProductMemberSnapshot member) || !member.Online)
                {
                    throw new InvalidOperationException("Product Join confirmation has no own online member");
                }
                self.MatchId = response.MatchId;
                self.MemberId = response.MemberId;
                self.BindingGeneration = response.BindingGeneration;
                Install(self, response.RosterVersion, members);
                if (self.CandidateMatchId == self.MatchId && self.CandidateGeneration == self.BindingGeneration && self.CandidateVersion > self.RosterVersion)
                {
                    Install(self, self.CandidateVersion, self.CandidateRoster);
                }
                self.CandidateRoster.Clear();
                self.CandidateMatchId = null;
                self.Stage = ProductClientStage.Joined;
                self.GetComponent<GridClientComponent>().ConfirmGridProjection();
                runtime = self.Runtime;
                runtime.Report(runtime.Config.Role == RuntimeInstanceRole.Solo ? ProductStage.SoloReady :
                    runtime.Config.Role == RuntimeInstanceRole.Host ? ProductStage.HostReady : ProductStage.ClientReady);
                EventSystem.Instance.Publish((Scene)self.IScene, new ProductClientJoined { MemberId = self.MemberId });
                Log.Info($"Product Joined: match={self.MatchId} member={self.MemberId} version={self.RosterVersion}");
                if ((Session)self.Session != null)
                {
                    self.HeartbeatStarted = true;
                    HeartbeatLoop(self).Forget();
                }
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null && self.Stage == ProductClientStage.Joining)
                {
                    self.FailClient($"ProductJoinFailed: {e.Message}");
                }
            }
        }

        public static void ApplyRoster(this ClientSessionComponent self, ProductRosterSnapshot snapshot)
        {
            if (self.Stage == ProductClientStage.Joining)
            {
                if (string.IsNullOrEmpty(snapshot.MatchId) || snapshot.BindingGeneration <= 0 || snapshot.RosterVersion <= 0)
                {
                    return;
                }
                if (self.CandidateMatchId == snapshot.MatchId && self.CandidateGeneration == snapshot.BindingGeneration && snapshot.RosterVersion <= self.CandidateVersion)
                {
                    return;
                }
                Dictionary<int, ProductMemberSnapshot> members = CopyMembers(snapshot.Members);
                self.CandidateRoster.Clear();
                foreach (var entry in members)
                {
                    self.CandidateRoster.Add(entry.Key, entry.Value);
                }
                self.CandidateMatchId = snapshot.MatchId;
                self.CandidateGeneration = snapshot.BindingGeneration;
                self.CandidateVersion = snapshot.RosterVersion;
                return;
            }
            if (self.Stage != ProductClientStage.Joined || self.MatchId != snapshot.MatchId || self.BindingGeneration != snapshot.BindingGeneration || snapshot.RosterVersion <= self.RosterVersion)
            {
                return;
            }
            Install(self, snapshot.RosterVersion, CopyMembers(snapshot.Members));
        }

        private static Dictionary<int, ProductMemberSnapshot> CopyMembers(List<ProductRosterMember> source)
        {
            var result = new Dictionary<int, ProductMemberSnapshot>();
            foreach (ProductRosterMember member in source)
            {
                if (member == null || member.MemberId <= 0 || !result.TryAdd(member.MemberId, new ProductMemberSnapshot(member.MemberId, member.Online)))
                {
                    throw new InvalidOperationException("Invalid product roster");
                }
            }
            return result;
        }

        private static void Install(ClientSessionComponent self, long version, Dictionary<int, ProductMemberSnapshot> members)
        {
            if (version <= self.RosterVersion)
            {
                return;
            }
            self.Roster.Clear();
            foreach (var entry in members)
            {
                self.Roster.Add(entry.Key, entry.Value);
            }
            self.RosterVersion = version;
            EventSystem.Instance.Publish((Scene)self.IScene, new ProductRosterApplied { RosterVersion = version });
        }

        public static async UniTask<ProductProbeResponse> ProbeProduct(this ClientSessionComponent self, string payload)
        {
            if (self.Stage != ProductClientStage.Joined || self.NextProbeSequence == int.MaxValue)
            {
                throw new InvalidOperationException("Product client is not joined or sequence exhausted");
            }
            EntityRef<ClientSessionComponent> selfRef = self;
            int sequence = ++self.NextProbeSequence;
            var response = (ProductProbeResponse)await self.CallProduct(ProductMessages.Create<ProductProbeRequest>(message => { message.MatchId = self.MatchId; message.Sequence = sequence; message.Payload = payload; }));
            self = selfRef;
            if (self == null || self.Stage != ProductClientStage.Joined || !self.HasCurrentEntry(self.BindingGeneration))
            {
                response.Dispose();
                throw new RpcException(ProductError.PeerClosed, "Product probe caller stopped");
            }
            if (response.Error == 0)
            {
                if (response.Sequence != sequence || response.MemberId != self.MemberId || response.Payload != payload)
                {
                    response.Dispose();
                    throw new InvalidOperationException("Product probe response mismatch");
                }
                self.LastProbePayload = response.Payload;
                self.LastProbeMemberId = response.MemberId;
                Log.Info($"Product Probe: member={response.MemberId} sequence={response.Sequence} payload={response.Payload}");
            }
            return response; // 调用者消费并释放。
        }

        private static bool HasCurrentEntry(this ClientSessionComponent self, long generation)
        {
            LocalMessageComponent local = self.Local;
            if (local != null)
            {
                HostPeer peer = local.Peer;
                return peer != null && peer.EntryOpen && !peer.Closing && peer.Generation == generation;
            }
            Session session = self.Session;
            ProductClientBinding binding = session?.GetComponent<ProductClientBinding>();
            return binding != null && (ClientSessionComponent)binding.Client == self;
        }

        public static async UniTask<bool> LeaveProduct(this ClientSessionComponent self)
        {
            if (self.Stage != ProductClientStage.Joined)
            {
                return false;
            }
            EntityRef<ClientSessionComponent> selfRef = self;
            try
            {
                using var response = (ProductLeaveResponse)await self.CallProduct(ProductMessages.Create<ProductLeaveRequest>(message => { message.MatchId = self.MatchId; }));
                bool committed = response.Error == 0 && response.Committed;
                self = selfRef;
                if (self != null)
                {
                    self.StopClient();
                }
                return committed;
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.StopClient();
                }
            }
        }

        public static void StopClient(this ClientSessionComponent self)
        {
            if (self.Stage is ProductClientStage.Stopping or ProductClientStage.Stopped)
            {
                return;
            }
            self.Stage = ProductClientStage.Stopping;
            LocalMessageComponent local = self.Local;
            HostPeer peer = local?.Peer ?? default;
            peer?.CloseEntry();
            Session session = self.Session;
            self.Session = default;
            session?.Dispose();
            self.Roster.Clear();
            self.CandidateRoster.Clear();
            self.Stage = ProductClientStage.Stopped;
            EventSystem.Instance.Publish((Scene)self.IScene, new ProductClientLeft());
        }

        public static void FailClient(this ClientSessionComponent self, string reason)
        {
            if (self.Stage is ProductClientStage.Stopping or ProductClientStage.Stopped or ProductClientStage.Failed)
            {
                return;
            }
            self.Stage = ProductClientStage.Failed;
            ProductRuntimeComponent runtime = self.Runtime;
            runtime?.FailProduct(reason); // 同步失效，销毁在原 Fiber 下帧进行以避开 Session.Destroy 重入。
        }

        private static async UniTaskVoid HeartbeatLoop(ClientSessionComponent client)
        {
            EntityRef<ClientSessionComponent> clientRef = client;
            EntityRef<TimerComponent> timerRef = client.Root().GetComponent<TimerComponent>();
            try
            {
                while (true)
                {
                    TimerComponent timer = timerRef;
                    if (timer == null)
                    {
                        return;
                    }
                    await timer.WaitAsync(ProductSessionRules.HeartbeatInterval);
                    client = clientRef;
                    if (client == null || client.Stage != ProductClientStage.Joined)
                    {
                        return;
                    }
                    client.HeartbeatInFlight = true;
                    using var response = (ProductHeartbeatResponse)await client.CallProduct(ProductMessages.Create<ProductHeartbeatRequest>(message => { message.ProtocolVersion = ProductSessionRules.ProtocolVersion; message.MatchId = client.MatchId; }), ProductSessionRules.HeartbeatTimeout);
                    client = clientRef;
                    if (client == null || client.Stage != ProductClientStage.Joined)
                    {
                        return;
                    }
                    client.HeartbeatInFlight = false;
                    if (response.Error != 0)
                    {
                        throw new RpcException(response.Error, "Product heartbeat rejected");
                    }
                }
            }
            catch (Exception e)
            {
                client = clientRef;
                if (client != null && client.Stage == ProductClientStage.Joined)
                {
                    client.HeartbeatInFlight = false;
                    client.FailClient($"ProductHeartbeatFailed: {e.Message}");
                }
            }
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    public sealed class ProductRosterHandler : MessageHandler<ClientSessionComponent, ProductRosterSnapshot>
    {
        protected override UniTask Run(ClientSessionComponent client, ProductRosterSnapshot message)
        {
            try
            {
                client.ApplyRoster(message);
            }
            finally
            {
                message.Dispose();
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(ClientSessionComponent))]
    public sealed class ProductMatchClosedHandler : MessageHandler<ClientSessionComponent, ProductMatchClosed>
    {
        protected override UniTask Run(ClientSessionComponent client, ProductMatchClosed message)
        {
            try
            {
                if (client.MatchId == message.MatchId && client.BindingGeneration == message.BindingGeneration)
                {
                    client.FailClient($"ProductMatchClosed: {message.Reason}");
                }
            }
            finally
            {
                message.Dispose();
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
