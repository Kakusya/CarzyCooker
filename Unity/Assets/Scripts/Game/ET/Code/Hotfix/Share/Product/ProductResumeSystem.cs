#if !DOTNET
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(ProductResumeIntent))]
    [FriendOf(typeof(ProductResumeIntent))]
    public static partial class ProductResumeIntentSystem
    {
        [EntitySystem]
        private static void Awake(this ProductResumeIntent self)
        {
            self.MatchId = self.Credential = self.Endpoint = self.Feedback = null; self.AttemptId = 0;
        }

        [EntitySystem]
        private static void Destroy(this ProductResumeIntent self)
        {
            self.MatchId = self.Credential = self.Endpoint = self.Feedback = null; self.AttemptId = 0;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProductPeerResumeHandler : ProductActorRequestHandler<HostPeer, ProductResumeRequest, ProductResumeResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductResumeRequest request, ProductResumeResponse response)
        {
            response.AttemptId = request.AttemptId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; return; }
            using ProductControlResponse result = await peer.Forward(ProductControlAction.Resume, request.ProtocolVersion,
                request.MatchId, credential: request.ResumeCredential, attempt: request.AttemptId, platform: request.PlatformId);
            response.Error = result.Error; response.Message = result.Message; response.MatchId = result.MatchId;
            response.MemberId = result.MemberId; response.BindingGeneration = result.BindingGeneration; response.RosterVersion = result.RosterVersion;
            foreach (ProductRosterMember member in result.Members)
            {
                response.Members.Add(ProductMessages.Create<ProductRosterMember>(value => { value.MemberId = member.MemberId; value.Online = member.Online; }));
            }
        }
    }

    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ProductResumeIntent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static class ProductResumeCommands
    {
        public static async UniTask ResumeProduct(this ProductRuntimeComponent self)
        {
            ProductResumeIntent intent = self.GetComponent<ProductResumeIntent>();
            if (self.Config.Role != RuntimeInstanceRole.Client || self.Stage != ProductStage.Disconnected || intent?.Credential == null)
            {
                throw new InvalidOperationException("ResumeUnavailable");
            }
            self.Stage = ProductStage.Resuming; self.Failure = null;
            intent.Feedback = "正在恢复连接，等待本人确认与完整投影";
            long attempt = checked(++intent.AttemptId);
            Scene scene = self.ClientScene;
            scene.RemoveComponent<ClientSessionComponent>();
            ClientSessionComponent client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(self);
            client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            self.Client = client;
            Session session = scene.GetComponent<NetComponent>().Create(NetworkHelper.ToIPEndPoint(intent.Endpoint));
            session.AddComponent<ProductClientBinding, ClientSessionComponent>(client); client.Session = session;
            client.Stage = ProductClientStage.Joining;
            EntityRef<ClientSessionComponent> original = client; EntityRef<ProductRuntimeComponent> runtimeRef = self;
            EntityRef<ProductResumeIntent> intentRef = intent;
            try
            {
                using var response = (ProductResumeResponse)await client.CallProduct(ProductMessages.Create<ProductResumeRequest>(value =>
                {
                    value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = intent.MatchId;
                    value.ResumeCredential = intent.Credential; value.AttemptId = attempt;
                    value.PlatformId = ProductSessionRules.CurrentPlatformId;
                }));
                client = original; self = runtimeRef; intent = intentRef;
                if (client == null || self == null || intent == null || intent.AttemptId != attempt || self.Stage != ProductStage.Resuming)
                {
                    return;
                }
                if (response.Error != 0) { throw new RpcException(response.Error, response.Message ?? "ResumeRejected"); }
                if (response.AttemptId != attempt || response.MatchId != intent.MatchId || response.MemberId <= 0 || response.BindingGeneration <= 0 ||
                    !response.Members.Any(member => member.MemberId == response.MemberId && member.Online))
                {
                    throw new InvalidOperationException("InvalidResumeConfirmation");
                }
                client.InstallResumeConfirmation(response);
                intent.Feedback = "成员已恢复，等待当前关完整投影";
                self.Report(ProductStage.ClientReady);
                EventSystem.Instance.Publish(scene, new ProductClientJoined { MemberId = client.MemberId });
            }
            catch (Exception)
            {
                client = original; self = runtimeRef; intent = intentRef;
                if (client != null && self != null && intent != null && intent.AttemptId == attempt && self.Stage == ProductStage.Resuming)
                {
                    client.DisconnectProduct("恢复未确认，可手动重试；原请求可能已经提交");
                }
            }
        }

        public static void CancelProductResume(this ProductRuntimeComponent self)
        {
            ProductResumeIntent intent = self.GetComponent<ProductResumeIntent>();
            if (intent == null) { return; }
            intent.Credential = null; intent.MatchId = null; ++intent.AttemptId;
            intent.Feedback = "本地恢复已结束，未向 Host 确认 Leave";
            ClientSessionComponent client = self.Client;
            client?.DisconnectProduct(intent.Feedback);
        }
    }

    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ProductResumeIntent))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class ClientSessionComponentSystem
    {
        public static void InstallResumeConfirmation(this ClientSessionComponent self, ProductResumeResponse response)
        {
            self.MatchId = response.MatchId; self.MemberId = response.MemberId; self.BindingGeneration = response.BindingGeneration;
            Install(self, response.RosterVersion, CopyMembers(response.Members));
            if (self.CandidateMatchId == self.MatchId && self.CandidateGeneration == self.BindingGeneration && self.CandidateVersion > self.RosterVersion)
            {
                Install(self, self.CandidateVersion, self.CandidateRoster);
            }
            self.CandidateRoster.Clear(); self.CandidateMatchId = null; self.Stage = ProductClientStage.Joined;
            self.ApplyCandidateRoom();
            self.GetComponent<GridClientComponent>().ConfirmGridProjection();
            self.HeartbeatStarted = true; HeartbeatLoop(self).Forget();
        }

        public static void DisconnectProduct(this ClientSessionComponent self, string feedback)
        {
            ProductRuntimeComponent runtime = self.Runtime;
            if (runtime == null || runtime.Stage is ProductStage.Stopping or ProductStage.Stopped) { return; }
            self.Stage = ProductClientStage.Disconnected; self.GetComponent<GridClientComponent>().Confirmed = false;
            runtime.Stage = ProductStage.Disconnected; runtime.Failure = feedback;
            ProductResumeIntent intent = runtime.GetComponent<ProductResumeIntent>();
            if (intent != null) { intent.Feedback = feedback; }
            Session session = self.Session; self.Session = default;
            EntityRef<Session> sessionRef = session;
            self.Fiber().ThreadSynchronizationContext.Post(() => { Session previous = sessionRef; previous?.Dispose(); });
            RuntimeInstanceContext.ReportProductStage(runtime.Config, "Disconnected");
            EventSystem.Instance.Publish((Scene)self.IScene, new ProductConnectionLost());
        }
    }
}
#endif
