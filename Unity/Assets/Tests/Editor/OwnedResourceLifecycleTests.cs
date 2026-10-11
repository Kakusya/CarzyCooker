using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game;
using GameFramework.Resource;
using NUnit.Framework;
using UnityEngine;

namespace CarzyCooker.Tests
{
    public sealed class OwnedResourceLifecycleTests
    {
        private AsyncRuntime m_Runtime;
        private readonly List<ResourceContainer> m_Containers = new List<ResourceContainer>();
        private readonly List<UnityEngine.Object> m_Objects = new List<UnityEngine.Object>();
        [SetUp] public void Setup() => m_Runtime = new AsyncRuntime { ErrorObserver = _ => { } };
        [TearDown] public void Cleanup()
        {
            m_Runtime.Stop();
            foreach (var container in m_Containers) { container.UnloadAllAssets(); GameFramework.ReferencePool.Release(container); }
            m_Containers.Clear();
            foreach (var obj in m_Objects) UnityEngine.Object.DestroyImmediate(obj);
            m_Objects.Clear();
        }
        private ResourceContainer Container(IAsyncOwner owner, IAssetBackend backend)
        {
            var container = ResourceContainer.Create(owner, backend);
            m_Containers.Add(container);
            return container;
        }
        private TextAsset Asset() { var asset = new TextAsset("owned"); m_Objects.Add(asset); return asset; }

        [Test] [Category("OwnedFinal")]
        public void T6_PublicWidgetCloseReentryPreservesNewResourceRound()
        {
            m_Runtime.Stop();
            m_Runtime = AsyncRuntime.Begin();
            var backend = new Backend();
            var go = new GameObject("OwnedReentryWidget", typeof(RectTransform), typeof(OwnedMonoExWidget));
            var widget = go.GetComponent<OwnedMonoExWidget>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var open = typeof(AUIWidget).GetMethod("OnOpen", flags);
            var field = typeof(AExUIWidget).GetField("m_ResourceContainer", flags);
            try
            {
                typeof(AUIWidget).GetMethod("OnInit", flags).Invoke(widget, new object[] { null });
                widget.SetInitialized(true);
                open.Invoke(widget, new object[] { null });
                field.SetValue(widget, ResourceContainer.Create(widget, backend));
                var old = widget.LoadAssetAsync<TextAsset>("old");
                UniTask<TextAsset> next = default;
                old.GetAwaiter().OnCompleted(() =>
                {
                    open.Invoke(widget, new object[] { null });
                    field.SetValue(widget, ResourceContainer.Create(widget, backend));
                    next = widget.LoadAssetAsync<TextAsset>("new");
                });
                typeof(AExUIWidget).GetMethod("OnClose", flags).Invoke(widget, new object[] { false, null });
                Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
                Assert.That(widget.AsyncScope.IsActive, Is.True);
                backend.Success(0, Asset());
                Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
                backend.Success(1, Asset());
                Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Succeeded));
                Assert.That(backend.Released, Is.EqualTo(1));
                typeof(AExUIWidget).GetMethod("OnClose", flags).Invoke(widget, new object[] { false, null });
                Assert.That(backend.Released, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            Assert.That(backend.Released, Is.EqualTo(2));
        }

        // Controls native notifications only. No scope/registration/release logic lives in this double.
        private sealed class Backend : IAssetBackend
        {
            public readonly List<LoadAssetCallbacks> Requests = new List<LoadAssetCallbacks>();
            public int Released;
            public void Load(string name, Type type, int priority, LoadAssetCallbacks callbacks) => Requests.Add(callbacks);
            public void Unload(object asset) => Released++;
            public void Success(int index, object asset) => Requests[index].LoadAssetSuccessCallback("asset", asset, 0, null);
            public void Failure(int index) => Requests[index].LoadAssetFailureCallback("asset", LoadResourceStatus.AssetError, "backend failed", null);
            public void Progress(int index)
            {
                Requests[index].LoadAssetUpdateCallback?.Invoke("asset", 0.5f, null);
                Requests[index].LoadAssetDependencyAssetCallback?.Invoke("asset", "dependency", 1, 2, null);
            }
        }

        [Test] public void T1_T5_UnconsumedAssetIsRegisteredAndReleasedOnClose()
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            var result = container.LoadAssetAsync<TextAsset>("asset");
            backend.Success(0, Asset());
            Assert.That(result.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(owner.PendingCount, Is.Zero);
            Assert.That(backend.Released, Is.Zero);
            owner.Stop();
            Assert.That(backend.Released, Is.EqualTo(1));
            owner.Stop();
            Assert.That(backend.Released, Is.EqualTo(1));
        }

        [Test] [Category("OwnedRemaining")]
        public void T1_ValidFailureIsObservedOnceAndWrongTypeReleasesBeforeFailure()
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            int failures = 0;
            m_Runtime.ErrorObserver = _ => failures++;
            var failed = container.LoadAssetAsync<TextAsset>("failed");
            backend.Failure(0);
            Assert.That(failed.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.That(owner.PendingCount, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
            var wrong = container.LoadAssetAsync<Texture2D>("wrong");
            backend.Success(1, Asset());
            Assert.That(wrong.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.That(backend.Released, Is.EqualTo(1));
            Assert.That(failures, Is.EqualTo(2));
            owner.Stop();
        }

        [TestCase(false)] [TestCase(true)]
        public void T2_T3_CloseBeforeCallbackSuppressesAllOldNotifications(bool callback)
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            int writes = 0;
            UniTask<TextAsset> result = default;
            if (callback) container.LoadAsset<TextAsset>("asset", _ => writes++, () => writes++, updateEvent: _ => writes++, dependencyAssetEvent: _ => writes++);
            else result = container.LoadAssetAsync<TextAsset>("asset", updateEvent: _ => writes++, dependencyAssetEvent: _ => writes++);
            owner.Stop();
            Assert.That(owner.PendingCount, Is.Zero);
            if (!callback) Assert.That(result.Status, Is.EqualTo(UniTaskStatus.Canceled));
            backend.Progress(0);
            // Stale mismatched type must be released before any type error is created.
            var wrong = new Texture2D(1, 1); m_Objects.Add(wrong);
            Assert.DoesNotThrow(() => backend.Success(0, wrong));
            Assert.That(writes, Is.Zero);
            Assert.That(backend.Released, Is.EqualTo(1));
        }

        [Test] public void T3_LateFailureIsSilent()
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            int failures = 0;
            m_Runtime.ErrorObserver = _ => failures++;
            var result = container.LoadAssetAsync<TextAsset>("asset");
            owner.Stop();
            backend.Failure(0);
            Assert.That(failures, Is.Zero);
            Assert.That(result.Status, Is.EqualTo(UniTaskStatus.Canceled));
        }

        [Test] public void T4_UnloadAndClearInvalidateOnlyTheOldResourceRound()
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            var ordinary = owner.RunAsync(_ => new UniTaskCompletionSource<int>().Task);
            var old = container.LoadAssetAsync<TextAsset>("old");
            container.UnloadAllAssets();
            Assert.That(old.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(ordinary.Status, Is.EqualTo(UniTaskStatus.Pending));
            var next = container.LoadAssetAsync<TextAsset>("new");
            backend.Success(0, Asset());
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
            backend.Success(1, Asset());
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            container.UnloadAllAssets();
            var third = container.LoadAssetAsync<TextAsset>("third");
            container.Clear();
            backend.Success(2, Asset());
            Assert.That(third.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(backend.Released, Is.EqualTo(3));
        }

        [Test] public void T6_CompletionReentryDoesNotClearNewRound()
        {
            var owner = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var container = Container(owner, backend);
            UniTask<TextAsset> next = default;
            container.LoadAsset<TextAsset>("old", _ =>
            {
                container.UnloadAllAssets();
                next = container.LoadAssetAsync<TextAsset>("new");
            });
            backend.Success(0, Asset());
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
            backend.Success(1, Asset());
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(backend.Released, Is.EqualTo(1));
            container.UnloadAllAssets();
            Assert.That(backend.Released, Is.EqualTo(2));
        }

        [Test] public void T7_T8_SharedBackendAndRuntimeStopReleaseEachReferenceOnce()
        {
            var first = new AsyncScope(m_Runtime);
            var second = new AsyncScope(m_Runtime);
            var backend = new Backend();
            var one = Container(first, backend);
            var two = Container(second, backend);
            var old = one.LoadAssetAsync<TextAsset>("shared");
            var next = two.LoadAssetAsync<TextAsset>("shared");
            first.Stop();
            var shared = Asset();
            backend.Success(0, shared);
            backend.Success(1, shared);
            Assert.That(old.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(backend.Released, Is.EqualTo(1));
            var never = two.LoadAssetAsync<TextAsset>("never");
            m_Runtime.Stop();
            Assert.That(never.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(backend.Released, Is.EqualTo(2));
        }
    }
}
