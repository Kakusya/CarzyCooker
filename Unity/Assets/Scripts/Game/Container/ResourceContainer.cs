using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;

namespace Game
{
    public sealed class ResourceContainer : IReference
    {
        public object Owner { get; private set; }
        private IAsyncOwner m_Owner;
        private IAssetBackend m_Backend;
        private Round m_Round;

        private sealed class Round
        {
            internal readonly AsyncScope Scope;
            internal readonly IAssetBackend Backend;
            internal readonly List<UnityEngine.Object> Assets = new List<UnityEngine.Object>();
            internal Round(AsyncScope owner, IAssetBackend backend)
            {
                Backend = backend;
                Scope = new AsyncScope(owner.Runtime, parent: owner);
                Scope.Stopped += Release;
            }
            private void Release()
            {
                var assets = Assets.ToArray();
                Assets.Clear();
                foreach (var asset in assets) Backend.Unload(asset);
            }
        }

        public static ResourceContainer Create(object owner)
        {
            if (!(owner is IAsyncOwner asyncOwner)) throw new InvalidOperationException("ResourceContainer requires an async owner.");
            return Create(asyncOwner, new ResourceComponentBackend(GameEntry.Resource, GameEntry.Base));
        }

        public static ResourceContainer Create(IAsyncOwner owner, IAssetBackend backend)
        {
            if (owner?.AsyncScope == null || !owner.AsyncScope.IsActive) throw new InvalidOperationException("ResourceContainer requires an active owner.");
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            var container = ReferencePool.Acquire<ResourceContainer>();
            container.Owner = owner;
            container.m_Owner = owner;
            container.m_Backend = backend;
            return container;
        }

        public void Clear()
        {
            Round old = m_Round;
            m_Round = null;
            m_Owner = null;
            m_Backend = null;
            Owner = null;
            // Clear invalidates pending requests. Normal assets must be unloaded before pool release.
            if (old != null) { old.Assets.Clear(); old.Scope.Stop(); }
        }

        private Round GetRound()
        {
            AsyncScope owner = m_Owner?.AsyncScope;
            if (owner == null || !owner.IsActive) throw new ObjectDisposedException(nameof(ResourceContainer));
            if (m_Round == null || !m_Round.Scope.IsActive) m_Round = new Round(owner, m_Backend);
            return m_Round;
        }

        public void LoadAsset<T>(string assetName, Action<T> onLoadSuccess, Action onLoadFailure = null, int priority = 0,
            Action<float> updateEvent = null, Action<string> dependencyAssetEvent = null) where T : UnityEngine.Object
        {
            Round round = GetRound();
            var task = LoadAssetAsync<T>(assetName, priority, updateEvent, dependencyAssetEvent);
            ObserveCallback(task, round.Scope, onLoadSuccess, onLoadFailure).Forget();
        }

        private static async UniTaskVoid ObserveCallback<T>(UniTask<T> task, AsyncScope owner, Action<T> success, Action failure)
        {
            try
            {
                T asset = await task;
                owner?.Apply(() => success?.Invoke(asset));
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                // The task boundary already observed the underlying failure.
                owner?.Runtime.Report(error);
                owner?.Apply(() => failure?.Invoke());
            }
        }

        public UniTask<T> LoadAssetAsync<T>(string assetName, int priority = 0,
            Action<float> updateEvent = null, Action<string> dependencyAssetEvent = null) where T : UnityEngine.Object
        {
            Round round = GetRound();
            return m_Owner.RunAsync(_ => round.Scope.RunAsync(context =>
                Load<T>(round, context, assetName, priority, updateEvent, dependencyAssetEvent), false));
        }

        private static UniTask<T> Load<T>(Round round, AsyncContext context, string assetName, int priority,
            Action<float> update, Action<string> dependency) where T : UnityEngine.Object
        {
            var completion = new OwnedCompletion<T>();
            bool finished = false;
            CancellationTokenRegistration registration = default;
            registration = context.CancellationToken.Register(() =>
            {
                registration.Dispose();
                completion.Cancel(context.CancellationToken);
            });
            void Success(string _, object asset, float duration, object data)
            {
                if (finished) return;
                finished = true;
                registration.Dispose();
                // Ownership precedes type validation: a stale wrong-type result never reaches business.
                if (!context.IsActive) { round.Backend.Unload(asset); completion.Cancel(context.CancellationToken); return; }
                if (!(asset is T typed))
                {
                    round.Backend.Unload(asset);
                    completion.Fail(new GameFrameworkException($"Load asset '{assetName}' expected {typeof(T)}, got {asset?.GetType()}."));
                    return;
                }
                round.Assets.Add(typed);
                completion.Succeed(typed);
            }
            void Failure(string _, LoadResourceStatus status, string message, object data)
            {
                if (finished) return;
                finished = true;
                registration.Dispose();
                if (context.IsActive) completion.Fail(new GameFrameworkException($"Load '{assetName}': {status}: {message}"));
                else completion.Cancel(context.CancellationToken);
            }
            var callbacks = new LoadAssetCallbacks(Success, Failure,
                update == null ? null : (LoadAssetUpdateCallback)((_, value, data) => { if (!finished) context.Apply(() => update(value)); }),
                dependency == null ? null : (LoadAssetDependencyAssetCallback)((_, name, loaded, total, data) => { if (!finished) context.Apply(() => dependency(name)); }));
            try { round.Backend.Load(assetName, typeof(T), priority, callbacks); }
            catch { registration.Dispose(); throw; }
            return completion.Task;
        }

        public void UnloadAsset(UnityEngine.Object asset)
        {
            Round round = m_Round;
            if (round != null && round.Assets.Remove(asset)) round.Backend.Unload(asset);
        }

        // Explicit transfers retain the original backend release responsibility at the receiver.
        public bool TransferAsset(UnityEngine.Object asset) => m_Round != null && m_Round.Assets.Remove(asset);
        public void UnloadAllAssets() => UnloadAllAssets(false);
        public void UnloadAllAssets(bool isShutdown)
        {
            Round old = m_Round;
            m_Round = null;
            old?.Scope.Stop();
        }
    }
}
