using System;
using GameFramework.Resource;
using UnityGameFramework.Runtime;

namespace Game
{
    public sealed class ResourceComponentBackend : IAssetBackend
    {
        private readonly IResourceManager m_Manager;
        private readonly BaseComponent m_Base;
        public ResourceComponentBackend(ResourceComponent resource, BaseComponent runtimeBase)
        {
            m_Manager = resource.ResourceManager ?? throw new InvalidOperationException("Resource backend is not initialized.");
            m_Base = runtimeBase ?? throw new ArgumentNullException(nameof(runtimeBase));
        }
        public void Load(string assetName, Type type, int priority, LoadAssetCallbacks callbacks)
        {
            if (m_Base == null || m_Base.ModulesStopped) throw new ObjectDisposedException(nameof(ResourceComponentBackend));
            m_Manager.LoadAsset(assetName, type, priority, callbacks);
        }
        public void Unload(object asset)
        {
            // Once the original modules stop, their pools own bundle/asset release. Never unspawn a dead pool.
            if (m_Base != null && !m_Base.ModulesStopped) m_Manager.UnloadAsset(asset);
        }
    }
}
