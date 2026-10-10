#if !DOTNET
using System;
using System.Net;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(Match))]
    public static class ProductRuntimeBootstrap
    {
        public static void InitializeProduct(this ProductRuntimeComponent self, bool joinLocal = true)
        {
            EntityRef<ProductRuntimeComponent> selfRef = self;
            ProductBootstrapBridge.Register(self.Config, self, () =>
            {
                ProductRuntimeComponent current = selfRef;
                current?.StopProduct();
            });
            Scene root = self.Root();
            try
            {
                if (self.Config.Role == RuntimeInstanceRole.Host)
                {
                    Scene host = EntitySceneFactory.CreateScene(root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductHost, "ProductHost");
                    self.HostScene = host;
                    Match match = host.AddChild<Match>();
                    HostSessionComponent authority = host.AddComponent<HostSessionComponent, Match, ProductRuntimeComponent>(match, self);
                    authority.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
                    self.Authority = authority;
                    var service = new KService(new IPEndPoint(IPAddress.Parse(self.Config.Address), self.Config.Port), NetworkProtocol.UDP, ServiceType.Outer);
                    try
                    {
                        host.AddComponent<NetComponent, AService, bool>(service, true);
                    }
                    catch
                    {
                        service.Dispose();
                        throw;
                    }
                    self.ListenEndpoint = ((UdpTransport)service.Transport).LocalEndPoint.ToString();
                    self.Report(ProductStage.HostListening);
                    Log.Info($"Product listening: {self.ListenEndpoint} match={match.MatchId}");
                    if (!joinLocal)
                    {
                        return;
                    }
                    ClientSessionComponent client = self.CreateClientScene();
                    HostPeer peer = ProductAuthority.CreatePeer(authority);
                    peer.AddComponent<LocalDelivery, ClientSessionComponent>(client);
                    LocalMessageComponent local = ((Scene)client.IScene).AddComponent<LocalMessageComponent, ClientSessionComponent, HostPeer>(client, peer);
                    client.Local = local;
                    client.JoinProduct().Forget(); // 首次 Call 入队后立即返回，让 Init 安装 Runner。
                }
                else
                {
                    ClientSessionComponent client = self.CreateClientScene();
                    IPAddress address = IPAddress.Parse(self.Config.Address);
                    var service = new KService(address.AddressFamily, NetworkProtocol.UDP, ServiceType.Outer);
                    try
                    {
                        NetComponent net = ((Scene)client.IScene).AddComponent<NetComponent, AService, bool>(service, false);
                        Session session = net.Create(new IPEndPoint(address, self.Config.Port));
                        session.AddComponent<ProductClientBinding, ClientSessionComponent>(client);
                        client.Session = session;
                    }
                    catch
                    {
                        service.Dispose();
                        throw;
                    }
                    client.JoinProduct().Forget();
                }
            }
            catch (Exception e)
            {
                self.Failure = $"ProductInitializeFailed: {e.Message}";
                RuntimeInstanceContext.Fail(self.Config, self.Failure);
                self.StopProduct();
                throw;
            }
        }

        private static ClientSessionComponent CreateClientScene(this ProductRuntimeComponent self)
        {
            Scene scene = EntitySceneFactory.CreateScene(self.Root(), IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "ProductClient");
            self.ClientScene = scene;
            ClientSessionComponent client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(self);
            client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            self.Client = client;
            return client;
        }

    }
}
#endif
