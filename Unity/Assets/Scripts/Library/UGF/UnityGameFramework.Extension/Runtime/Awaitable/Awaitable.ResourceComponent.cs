using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Extension
{
    public static partial class Awaitable
    {
        /// <summary>Explicit-owner foundation API. Cancellation ends the wait before native completion.</summary>
        public static UniTask<T> LoadAssetAsync<T>(this ResourceComponent resourceComponent, string assetName, int priority = 0,
            CancellationToken cancellationToken = default, Action<float> updateEvent = null, Action<string> dependencyAssetEvent = null) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            TipsSubscribeEvent();
            if (typeof(T).IsSubclassOf(typeof(UnityEngine.Component)))
                throw new GameFrameworkException("Load the GameObject before fetching its components.");
#endif
            if (!IsValid || cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<T>(cancellationToken);
            var manager = resourceComponent.ResourceManager;
            var runtimeBase = UnityGameFramework.Runtime.GameEntry.GetComponent<BaseComponent>();
            var completion = new UniTaskCompletionSource<T>();
            bool finished = false;
            bool canceled = false;
            CancellationTokenRegistration registration = default;
            void Detach()
            {
                registration.Dispose();
                if (runtimeBase != null) runtimeBase.ShuttingDown -= Cancel;
            }
            void Cancel()
            {
                if (finished || canceled) return;
                canceled = true;
                Detach();
                completion.TrySetCanceled(cancellationToken);
            }
            void Unload(object asset)
            {
                if (runtimeBase != null && !runtimeBase.ModulesStopped) manager.UnloadAsset(asset);
            }
            if (runtimeBase == null || runtimeBase.ModulesStopped) return UniTask.FromCanceled<T>();
            runtimeBase.ShuttingDown += Cancel;
            registration = cancellationToken.Register(Cancel);
            var callbacks = new LoadAssetCallbacks(
                (_, asset, duration, data) =>
                {
                    if (finished) return;
                    finished = true;
                    Detach();
                    if (canceled) { Unload(asset); return; }
                    if (!(asset is T typed))
                    {
                        Unload(asset);
                        completion.TrySetException(new GameFrameworkException($"Load '{assetName}' expected {typeof(T)}, got {asset?.GetType()}."));
                        return;
                    }
                    completion.TrySetResult(typed);
                },
                (_, status, message, data) =>
                {
                    if (finished) return;
                    finished = true;
                    Detach();
                    if (!canceled) completion.TrySetException(new GameFrameworkException(message));
                },
                updateEvent == null ? null : (LoadAssetUpdateCallback)((_, progress, data) => { if (!finished && !canceled) updateEvent(progress); }),
                dependencyAssetEvent == null ? null : (LoadAssetDependencyAssetCallback)((_, name, loaded, total, data) => { if (!finished && !canceled) dependencyAssetEvent(name); }));
            try { manager.LoadAsset(assetName, typeof(T), priority, callbacks); }
            catch { Detach(); throw; }
            return completion.Task;
        }

        /// <summary>Update a resource group. Do not call StopUpdateResources while awaiting this API.</summary>
        public static UniTask<bool> UpdateResourcesAsync(this ResourceComponent resourceComponent, string resourceGroupName)
        {
            var completion = AutoResetUniTaskCompletionSource<bool>.Create();
            resourceComponent.UpdateResources(resourceGroupName, (group, result) => completion.TrySetResult(result));
            return completion.Task;
        }
    }
}