using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game
{
    public abstract class AEntity : EntityLogic, IAsyncOwner
    {
        public AsyncScope AsyncScope { get; private set; }
        protected virtual void StopAsyncScope()
        {
            var old = AsyncScope;
            AsyncScope = null;
            old?.Stop();
        }
        protected virtual void OnDestroy() => StopAsyncScope();
        protected override void OnRecycle()
        {
            StopAsyncScope();
            base.OnRecycle();
        }
        public int Id => Entity.Id;
        private Transform m_CachedParentTransform;

#if UNITY_EDITOR
        private string m_GameObjectName;
#endif

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CachedParentTransform = CachedTransform.parent;
#if UNITY_EDITOR
            m_GameObjectName = CachedTransform.name;
#endif
        }

        protected override void OnShow(object userData)
        {
            StopAsyncScope();
            AsyncScope = new AsyncScope(AsyncRuntime.Current);
            base.OnShow(userData);
#if UNITY_EDITOR
            Name = Utility.Text.Format("[Entity {0} {1}]", Id, m_GameObjectName);
#endif
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            CompleteHide(isShutdown, userData, AsyncScope);
        }

        protected void CompleteHide(bool isShutdown, object userData, AsyncScope exitedScope)
        {
            if (ReferenceEquals(AsyncScope, exitedScope)) StopAsyncScope();
            exitedScope?.Stop();
            if (AsyncScope != null && !ReferenceEquals(AsyncScope, exitedScope)) return;
            if (m_CachedParentTransform != CachedTransform.parent)
            {
                CachedTransform.SetParent(m_CachedParentTransform, true);
            }
#if UNITY_EDITOR
            Name = m_GameObjectName;
#endif
            base.OnHide(isShutdown, userData);
        }
    }
}
