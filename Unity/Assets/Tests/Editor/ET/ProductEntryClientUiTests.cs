#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        // 与现有 CoopUiRemote 对称的同 Editor 服务端 fixture。只建真实 ET/KCP 树，
        // 不注册全局启动桥；普通 Launcher 的 Client 仍使用完整真实启动/切换入口。
        private sealed class EntryUiHost : IDisposable
        {
            private static readonly FieldInfo CurrentFiber = typeof(Fiber).GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            private static readonly MethodInfo UpdateFiber = typeof(Fiber).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo LateUpdateFiber = typeof(Fiber).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            private readonly Fiber fiber;
            private EntityRef<ProductRuntimeComponent> runtimeRef;
            private bool disposed;
            public ProductRuntimeComponent Runtime => runtimeRef;
            public ClientSessionComponent Client => Runtime.Client;

            public EntryUiHost(string endpoint)
            {
                fiber = (Fiber)Activator.CreateInstance(typeof(Fiber), BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { 92002, 0, SceneType.ProductRoot, "EntryUiHost" }, null);
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    fiber.Root.AddComponent<TimerComponent>();
                    fiber.Root.AddComponent<CoroutineLockComponent>();
                    fiber.Root.AddComponent<ProcessInnerSender>();
                    var config = RuntimeInstanceConfig.Create("entry-ui", "fixture-host", "host", endpoint);
                    ProductRuntimeComponent runtime = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
                    runtimeRef = runtime;
                    Scene hostScene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductHost, "EntryUiHost");
                    runtime.HostScene = hostScene;
                    Match match = hostScene.AddChild<Match>();
                    match.RoomMenuVariant = ProductMenuVariant.Drink;
                    HostSessionComponent authority = hostScene.AddComponent<HostSessionComponent, Match, ProductRuntimeComponent>(match, runtime);
                    authority.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
                    runtime.Authority = authority;
                    var service = new KService(NetworkHelper.ToIPEndPoint(endpoint), NetworkProtocol.UDP, ServiceType.Outer);
                    hostScene.AddComponent<NetComponent, AService, bool>(service, true);
                    runtime.ListenEndpoint = ((UdpTransport)service.Transport).LocalEndPoint.ToString();
                    Scene local = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "EntryUiHostLocal");
                    runtime.ClientScene = local;
                    ClientSessionComponent client = local.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(runtime);
                    client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
                    runtime.Client = client;
                    HostPeer peer = ProductAuthority.CreatePeer(authority);
                    peer.AddComponent<LocalDelivery, ClientSessionComponent>(client);
                    client.Local = local.AddComponent<LocalMessageComponent, ClientSessionComponent, HostPeer>(client, peer);
                    runtime.Report(ProductStage.HostListening);
                    client.JoinProduct().Forget();
                }
                catch
                {
                    fiber.Dispose();
                    throw;
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            public void Tick()
            {
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    UpdateFiber.Invoke(fiber, null);
                    LateUpdateFiber.Invoke(fiber, null);
                    // 本 fixture 的本地 Host 只用于控制；释放其显示 owner，保留真实成员/邮箱。
                    ((Scene)Runtime.ClientScene).RemoveComponent<GridPresentationComponent>();
                    fiber.Root.RemoveComponent<ProductEntryPresentation>();
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    Runtime?.StopProduct();
                    fiber.Dispose();
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }
        }

        private IEnumerator WaitEntryHost(EntryUiHost host, Func<bool> condition, string reason, float seconds = 12)
        {
            float started = UnityEngine.Time.realtimeSinceStartup;
            while (!condition())
            {
                host.Tick();
                Assert.That(UnityEngine.Time.realtimeSinceStartup - started, Is.LessThan(seconds), reason + "; " + Runtime?.Failure);
                yield return null;
            }
        }

        [UnityTest]
        [Category("ProductEntryClient")]
        public IEnumerator ProductEntryUiJoinTimeoutReturnsModesAndCanStartSolo()
        {
            yield return StartModeLauncher();
            using (var target = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                target.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                EntryHud.ShowEntryAddress(target.LocalEndPoint.ToString());
                Pointer(EntryButton("加入合作房间"));
                yield return Wait(() => Runtime != null && Runtime.Config.Role == RuntimeInstanceRole.Client, "Real Client attempts a silent UDP endpoint");
                yield return Wait(() => Runtime != null && Runtime.Config.ShowModeMenu && Entry != null &&
                    ((UIGridKitchen)Entry.Hud)?.Available == true && !string.IsNullOrEmpty(Entry.Failure), "Real Join timeout returns to mode selection", 20);
                ownedInit = Init.Instance;
                Assert.That(Entry.Failure, Does.Contain("ProductJoinFailed"));
                Assert.That((Scene)Runtime.ClientScene, Is.Null);
                Assert.That((Scene)Runtime.HostScene, Is.Null);
            }
            Pointer(EntryButton("单机餐厅"));
            yield return Wait(() => Runtime.Root().GetComponent<SoloSavePresentation>() is SoloSavePresentation selection &&
                ((UIGridKitchen)selection.Hud)?.Available == true, "Failed join leaves a working Solo choice");
            var selector = Runtime.Root().GetComponent<SoloSavePresentation>();
            yield return StartPreviewAndConfirm(((UIGridKitchen)selector.Hud).View.GetComponentsInChildren<Button>(false)
                .Single(button => button.GetComponentInChildren<Text>().text == "内存试玩"));
            yield return Wait(() => Presentation?.Ready == true, "Solo starts after the actual timeout");
            Assert.That(((Scene)Runtime.HostScene).GetComponent<NetComponent>(), Is.Null);
        }

        [UnityTest]
        [Category("ProductEntryClient")]
        public IEnumerator ProductEntryUiOrdinaryClientAddressJoinsRealHostRoomAndOpensOnlyProjection()
        {
            yield return StartModeLauncher();
            string endpoint;
            using (var reserved = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                endpoint = reserved.LocalEndPoint.ToString();
                EntryHud.ShowEntryAddress(endpoint);
                Pointer(EntryButton("加入合作房间"));
                yield return Wait(() => Runtime != null && Runtime.Config.Role == RuntimeInstanceRole.Client && Client != null,
                    "Ordinary Client has its new independent runtime");
            }
            ownedInit = Init.Instance;
            using (var server = new EntryUiHost(endpoint))
            {
                yield return WaitEntryHost(server, () => Client.Stage == ProductClientStage.Joined &&
                    Client.GetComponent<ProductRoomClient>().Current.Version > 0 && Entry != null &&
                    ((UIGridKitchen)Entry.Hud)?.Available == true && server.Client.Stage == ProductClientStage.Joined,
                    "Native Client mode completes real KCP Join");
                Assert.That((HostSessionComponent)Runtime.Authority, Is.Null);
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                Assert.That(Presentation, Is.Null);
                Assert.That(Client.Roster.Count, Is.EqualTo(2));
                Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text == "开启内存餐厅"), Is.False);
                var start = server.Client.CallProduct(server.Client.CreateRoomCommand(ProductRoomAction.StartRestaurant));
                yield return WaitEntryHost(server, () => start.Status != UniTaskStatus.Pending, "Actual Host local ordered control starts the kitchen");
                using (var response = (ProductRoomControlResponse)start.GetAwaiter().GetResult())
                {
                    Assert.That(response.Error, Is.Zero, response.Message);
                }
                yield return WaitEntryHost(server, () => Presentation?.Ready == true, "Ordinary Client installs the authoritative world before map input");
                Assert.That(Grid.CanControlRestaurant, Is.False);
                Assert.That((RestaurantRuntime)((Match)((HostSessionComponent)server.Runtime.Authority).Match).Restaurant, Is.Not.Null);
                long command = Client.NextCommandId + 1;
                Pointer(Button("拿 碗台 ·", true));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                yield return WaitEntryHost(server, () => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed,
                    "Ordinary Client pointer performs a real KCP pickup");
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
            }
        }
    }
}
#endif
