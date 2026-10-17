#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private readonly List<CoopUiRemote> ownedCoopUiRemotes = new();
        private IEnumerator StartCoopHostLauncher()
        {
            Assert.That(Init.Instance, Is.Null, "Do not replace another active ET entry");
            Assert.That(FiberManager.Instance, Is.Null, "Do not replace another active World");
            RuntimeInstanceContext.Reset();
            RuntimeInstanceContext.Prepare(RuntimeInstanceConfig.Create("coop-core-ui", "host", "host", "127.0.0.1:0"));
            Application.runInBackground = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Launcher.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return Wait(() => Game.GameEntry.RuntimeOwner is Component, "Owned ordinary Host Launcher starts");
            ownedRoot = ((Component)Game.GameEntry.RuntimeOwner).transform.root.gameObject;
            yield return Wait(() => Init.Instance != null, "Host ET entry starts");
            ownedInit = Init.Instance;
            yield return PreparePublishedRoomUi();
            yield return Wait(() => Presentation?.Ready == true, "Host managed HUD ready");
            Assert.That(Runtime.Config.Role, Is.EqualTo(RuntimeInstanceRole.Host));
            Assert.That(Grid.CanControlRestaurant, Is.True);
        }

        // 与现有 ProductSessionTests 一样，临时 Fiber 只持有真实远端 Session/投影。
        // 它不进入全局 Launcher 桥，不建立权威，泵与退出都恢复原 Fiber/同步上下文。
        private sealed class CoopUiRemote : IDisposable
        {
            private static readonly FieldInfo CurrentFiber = typeof(Fiber).GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            private static readonly MethodInfo UpdateFiber = typeof(Fiber).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo LateUpdateFiber = typeof(Fiber).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            private readonly Fiber fiber;
            private EntityRef<ProductRuntimeComponent> runtimeRef;
            private bool disposed;
            public ClientSessionComponent Client => ((ProductRuntimeComponent)runtimeRef)?.Client ?? default;
            public GridClientComponent Grid => Client.GetComponent<GridClientComponent>();
            public GridPresentationComponent Presentation => ((Scene)Client.IScene).GetComponent<GridPresentationComponent>();
            public MonoUIGridKitchen Hud => ((UIGridKitchen)Presentation.Hud).View;

            public SoloSaveRepository UseOwnedPreferencesRepository(string directory, string profile)
            {
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    fiber.Root.RemoveComponent<SoloSaveRepository>();
                    var repository = fiber.Root.AddComponent<SoloSaveRepository, string, string>(directory, profile);
                    Presentation.RefreshGridHud();
                    return repository;
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            public void KeepMapHeadlessWhileHostOwnsDisplay()
            {
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    Scene scene = (Scene)Client.IScene;
                    var display = scene.GetComponent<GridPresentationComponent>() ?? scene.AddComponent<GridPresentationComponent>();
                    display.GetAsyncOwner().AsyncScope.Stop();
                    display.CloseGridHud();
                    GridKitchenBoard board = display.Board;
                    if (board != null)
                    {
                        board.Visible = false;
                    }
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            public CoopUiRemote(string endpoint)
            {
                fiber = (Fiber)Activator.CreateInstance(typeof(Fiber), BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { 92001, 0, SceneType.ProductRoot, "CoopUiRemote" }, null);
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    fiber.Root.AddComponent<TimerComponent>();
                    fiber.Root.AddComponent<CoroutineLockComponent>();
                    fiber.Root.AddComponent<ProcessInnerSender>();
                    var config = RuntimeInstanceConfig.Create("coop-core-ui", "client", "client", connect: endpoint);
                    var runtime = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
                    runtimeRef = runtime;
                    Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "CoopUiRemote");
                    runtime.ClientScene = scene;
                    runtime.Client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(runtime);
                    Client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
                    var service = new KService(AddressFamily.InterNetwork, NetworkProtocol.UDP, ServiceType.Outer);
                    NetComponent net = scene.AddComponent<NetComponent, AService, bool>(service, false);
                    Session session = net.Create(NetworkHelper.ToIPEndPoint(endpoint));
                    session.AddComponent<ProductClientBinding, ClientSessionComponent>(Client);
                    Client.Session = session;
                    Client.JoinProduct().Forget();
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
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            public void Dispose()
            {
                if (disposed) { return; }
                disposed = true;
                object previous = CurrentFiber.GetValue(null);
                SynchronizationContext previousContext = SynchronizationContext.Current;
                try
                {
                    CurrentFiber.SetValue(null, fiber);
                    SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                    fiber.Root.GetComponent<ProductRuntimeComponent>()?.StopProduct();
                    fiber.Dispose();
                }
                finally
                {
                    CurrentFiber.SetValue(null, previous);
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }
        }

        private IEnumerator WaitCoopUi(CoopUiRemote remote, Func<bool> condition, string reason, float seconds = 15)
        {
            float started = Time.realtimeSinceStartup;
            while (!condition())
            {
                remote.Tick();
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(seconds), reason + "; " + remote.Presentation?.Failure + "; " + remote.Client.Stage);
                yield return null;
            }
        }

        [UnityTest]
        [NUnit.Framework.Category("PerFrameIntegration")]
        public IEnumerator CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners()
        {
            yield return StartCoopHostLauncher();
            ClientSessionComponent hostClient = Client;
            GridClientComponent hostGrid = Grid;
            var retiredHostPause = CaptureButtonCallback(Button("暂停"));
            ((Scene)hostClient.IScene).RemoveComponent<GridPresentationComponent>();
            yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Real KCP Client managed HUD ready");
                Assert.That(remote.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
                Assert.That(remote.Grid.Confirmed, Is.True);
                Assert.That(remote.Grid.WorldId, Is.EqualTo(hostGrid.WorldId));
                Assert.That(remote.Grid.Items, Is.Not.SameAs(hostGrid.Items));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(((UIGridKitchen)remote.Presentation.Hud).Parent, Is.SameAs(((Scene)remote.Client.IScene).GetComponent<UIComponent>()));
                string[] labels = remote.Hud.GetComponentsInChildren<Button>(false).Select(value => value.GetComponentInChildren<Text>().text).ToArray();
                Assert.That(labels.Any(value => value is "暂停" or "恢复" or "开始营业" or "下一关" || value.StartsWith("选择 ") || value.StartsWith("调整 ")), Is.False);
                Assert.That(labels.Any(value => value.StartsWith("采购 ")), Is.True);
                long hostCommand = hostClient.NextCommandId;
                retiredHostPause();
                Assert.That(hostClient.NextCommandId, Is.EqualTo(hostCommand), "Old Host callback cannot address the remote reused Mono");

                Button take = remote.Hud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text.StartsWith("拿 碗台 ·"));
                var retiredTake = CaptureButtonCallback(take);
                long command = remote.Client.NextCommandId + 1;
                int pooledMono = remote.Hud.GetInstanceID();
                Pointer(take);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command), "One remote pointer issues one physical command");
                remote.Presentation.CloseGridHud();
                retiredTake();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return null;
                remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, "Accepted remote pickup continues across display close");
                Assert.That(remote.Hud.GetInstanceID(), Is.EqualTo(pooledMono));
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Not.Zero);
                retiredTake();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command), "Retired callback cannot emit requests after the same Mono is reopened");
                Button place = remote.Hud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text.StartsWith("放 交接台 ·"));
                Pointer(place);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(++command));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, "Reopened current remote HUD still executes");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Zero);
                Assert.That(remote.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
        }
    }
}
#endif
