#if !DOTNET
using System;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(Match))]
    public static partial class ProductRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ProductRuntimeComponent self, RuntimeInstanceConfig config)
        {
            self.Config = config;
            self.Stage = ProductStage.Initializing;
            self.Failure = null;
            self.ListenEndpoint = null;
            self.Sender = self.Root().GetComponent<ProcessInnerSender>();
            self.HostScene = default;
            self.ClientScene = default;
            self.Authority = default;
            self.Client = default;
        }

        [EntitySystem]
        private static void Destroy(this ProductRuntimeComponent self)
        {
            self.StopProduct();
            ProcessInnerSender sender = self.Sender;
            sender?.Stop();
            ProductBootstrapBridge.Clear(self.Config);
            self.Stage = ProductStage.Stopped;
        }

        public static void Report(this ProductRuntimeComponent self, ProductStage stage)
        {
            if (self.Stage is ProductStage.Stopping or ProductStage.Stopped or ProductStage.Failed)
            {
                return;
            }
            self.Stage = stage;
            RuntimeInstanceContext.ReportProductStage(self.Config, stage.ToString());
        }

        public static void FailProduct(this ProductRuntimeComponent self, string reason)
        {
            if (self.IsDisposed || self.Root().IsDisposed || self.Stage is ProductStage.Stopping or ProductStage.Stopped or ProductStage.Failed)
            {
                return;
            }
            self.Failure = reason;
            self.Stage = ProductStage.Failed;
            RuntimeInstanceContext.Fail(self.Config, reason);
            Log.Warning(reason);
            EntityRef<ProductRuntimeComponent> selfRef = self;
            self.Fiber().ThreadSynchronizationContext.Post(() =>
            {
                ProductRuntimeComponent current = selfRef;
                if (current != null)
                {
                    current.StopProduct();
                    ProductBootstrapBridge.ScheduleStopInitialization(current.Config);
                }
            });
        }

        public static void StopProduct(this ProductRuntimeComponent self)
        {
            if (self.Stage is ProductStage.Stopping or ProductStage.Stopped)
            {
                return;
            }
            self.Stage = ProductStage.Stopping;
            HostSessionComponent authority = self.Authority;
            Match match = authority?.Match ?? default;
            ((RestaurantRuntime)(match?.Restaurant ?? default))?.StopRestaurant();
            match.ProductKitchen()?.StopGrid();
            if (authority != null)
            {
                foreach (HostPeer peer in authority.Children.Values.Cast<HostPeer>().ToArray())
                {
                    peer.EntryOpen = false;
                    peer.Closing = true;
                    try
                    {
                        ProductDelivery.Send(peer, ProductMessages.Create<ProductMatchClosed>(message => { message.MatchId = match?.MatchId; message.BindingGeneration = peer.Generation; message.Reason = "HostStopped"; }));
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Product close notification unavailable: {e.Message}");
                    }
                }
            }
            ClientSessionComponent client = self.Client;
            if (client != null)
            {
                client.Stage = ProductClientStage.Stopping;
                EventSystem.Instance.Publish((Scene)client.IScene, new ProductClientLeft());
            }
            ProcessInnerSender sender = self.Sender;
            sender?.Stop();
            Scene host = self.HostScene;
            Scene clientScene = self.ClientScene;
            host?.Dispose();
            clientScene?.Dispose();
            self.HostScene = default;
            self.ClientScene = default;
            self.Authority = default;
            self.Client = default;
            self.Stage = ProductStage.Stopped;
            ProductBootstrapBridge.Clear(self.Config);
            RuntimeInstanceContext.ReportProductStage(self.Config, "Stopped");
        }
    }

    [Invoke((long)SceneType.ProductRoot)]
    public sealed class FiberInit_ProductRoot : AInvokeHandler<FiberInit, UniTask>
    {
        public override UniTask Handle(FiberInit args)
        {
            Scene root = args.Fiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            ProductRuntimeComponent runtime = root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(RuntimeInstanceContext.Config);
            try
            {
                runtime.InitializeProduct();
            }
            catch (Exception e)
            {
                // 现有 FiberManager 的失败 Invoke 不结清创建等待；这里保留真实失败再结束初始化。
                RuntimeInstanceContext.Fail(RuntimeInstanceContext.Config, $"ProductInitializeFailed: {e.Message}");
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
