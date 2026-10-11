using Game;
using GameFramework;

namespace ET
{
    [ComponentOf]
    public sealed class AsyncOwnerComponent : Entity, IAwake, IDestroy, IAsyncOwner
    {
        public AsyncScope AsyncScope { get; set; }
        public ResourceContainer Resources { get; set; }
    }

    [EntitySystemOf(typeof(AsyncOwnerComponent))]
    public static partial class AsyncOwnerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AsyncOwnerComponent self)
        {
            EntityRef<Entity> ownerRef = self.Parent;
            EntityRef<AsyncOwnerComponent> componentRef = self;
            long instanceId = self.Parent.InstanceId;
            var parent = Init.Instance?.AsyncScope;
            self.AsyncScope = new AsyncScope(AsyncRuntime.Current,
                () => (Entity)ownerRef != null && ((Entity)ownerRef).InstanceId == instanceId && (AsyncOwnerComponent)componentRef != null,
                parent);
        }

        [EntitySystem]
        private static void Destroy(this AsyncOwnerComponent self)
        {
            var old = self.AsyncScope;
            var resources = self.Resources;
            self.AsyncScope = null;
            self.Resources = null;
            old?.Stop();
            if (resources != null) { resources.UnloadAllAssets(); ReferencePool.Release(resources); }
        }

        public static AsyncOwnerComponent GetAsyncOwner(this Entity self)
        {
            if (self == null || self.IsDisposed) throw new System.ObjectDisposedException(nameof(Entity));
            return self.GetComponent<AsyncOwnerComponent>() ?? self.AddComponent<AsyncOwnerComponent>();
        }

        public static ResourceContainer GetOwnedResources(this AsyncOwnerComponent self)
        {
            if (self.Resources == null) self.Resources = ResourceContainer.Create(self);
            return self.Resources;
        }
    }
}
