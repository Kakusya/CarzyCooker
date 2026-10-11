using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game;
using GameFramework.Resource;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGameFramework.Runtime;

namespace CarzyCooker.Tests
{
    [Category("OwnedIntegration")]
    public sealed class OwnedNativeResourceTests
    {
        private const string AssetPath = "Assets/Res/ET/ClientServer/Luban/dtaiconfig.bytes";
        private sealed class NativeBackend : IAssetBackend, IDisposable
        {
            private readonly GameObject m_Root = new GameObject("OwnedNativeBackend");
            private readonly DefaultLoadResourceAgentHelper m_Helper;
            private readonly DefaultResourceHelper m_ResourceHelper;
            private readonly string m_Path;
            private LoadAssetCallbacks m_Callbacks;
            private Type m_Type;
            private bool m_Stopped;
            public AssetBundle Bundle;
            private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
            public AssetBundleCreateRequest Creating => (AssetBundleCreateRequest)typeof(DefaultLoadResourceAgentHelper).GetField("m_FileAssetBundleCreateRequest", Fields).GetValue(m_Helper);
            public AssetBundleRequest Reading => (AssetBundleRequest)typeof(DefaultLoadResourceAgentHelper).GetField("m_AssetBundleRequest", Fields).GetValue(m_Helper);

            public NativeBackend(string path)
            {
                m_Path = path;
                m_Helper = m_Root.AddComponent<DefaultLoadResourceAgentHelper>();
                m_ResourceHelper = m_Root.AddComponent<DefaultResourceHelper>();
                m_Helper.LoadResourceAgentHelperUpdate += (_, e) => { };
                m_Helper.LoadResourceAgentHelperError += (_, e) => Assert.Fail(e.ErrorMessage);
                m_Helper.LoadResourceAgentHelperReadFileComplete += (_, e) =>
                {
                    Bundle = (AssetBundle)e.Resource;
                    m_Helper.Reset();
                    m_Helper.LoadAsset(Bundle, AssetPath, m_Type, false);
                };
                m_Helper.LoadResourceAgentHelperLoadComplete += (_, e) =>
                {
                    m_Callbacks.LoadAssetSuccessCallback(AssetPath, e.Asset, 0, null);
                    m_Helper.Reset();
                };
            }
            public void Load(string name, Type type, int priority, LoadAssetCallbacks callbacks)
            {
                m_Callbacks = callbacks;
                m_Type = type;
                m_Helper.ReadFile(m_Path);
            }
            public void Unload(object asset)
            {
                if (m_Stopped) Assert.Fail("Retired operation accessed the stopped backend.");
                m_ResourceHelper.Release(Bundle);
            }
            public void NativeUpdate() => typeof(DefaultLoadResourceAgentHelper).GetMethod("Update", Fields).Invoke(m_Helper, null);
            public void Shutdown()
            {
                if (m_Stopped) return;
                m_Stopped = true;
                m_Helper.Reset();
                if (Bundle != null) m_ResourceHelper.Release(Bundle);
            }
            public void Dispose() { Shutdown(); UnityEngine.Object.DestroyImmediate(m_Root); }
        }

        [UnityTest] public IEnumerator OwnedNative_CreateAndAssetReadRetireWithoutOwnedUpdatesThenReload()
        {
            string output = Path.Combine("Temp", "OwnedAsyncLifecycle", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            var manifest = BuildPipeline.BuildAssetBundles(output,
                new[] { new AssetBundleBuild { assetBundleName = "owned", assetNames = new[] { AssetPath } } },
                BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);
            Assert.That(manifest, Is.Not.Null);
            string path = Path.GetFullPath(Path.Combine(output, "owned"));
            var containers = new System.Collections.Generic.List<ResourceContainer>();
            var runtime = new AsyncRuntime();
            try
            {
                using (var backend = new NativeBackend(path))
                {
                    var scope = new AsyncScope(runtime);
                    var container = ResourceContainer.Create(scope, backend);
                    containers.Add(container);
                    var task = container.LoadAssetAsync<TextAsset>(AssetPath);
                    var creating = backend.Creating;
                    Assert.That(creating, Is.Not.Null);
                    Assert.That(creating.isDone, Is.False, "A real in-flight bundle create is required.");
                    scope.Stop();
                    Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
                    backend.Shutdown();
                    Assert.That(DefaultLoadResourceAgentHelper.PendingNativeCount, Is.GreaterThan(0));
                    while (!creating.isDone) yield return null;
                    yield return null;
                    Assert.That(DefaultLoadResourceAgentHelper.PendingNativeCount, Is.Zero);
                    Assert.That(creating.assetBundle == null, Is.True, "The untransferred bundle must be unloaded.");
                }

                using (var backend = new NativeBackend(path))
                {
                    var scope = new AsyncScope(runtime);
                    var container = ResourceContainer.Create(scope, backend);
                    containers.Add(container);
                    var task = container.LoadAssetAsync<TextAsset>(AssetPath);
                    while (!backend.Creating.isDone) yield return null;
                    backend.NativeUpdate();
                    var reading = backend.Reading;
                    Assert.That(reading, Is.Not.Null);
                    Assert.That(reading.isDone, Is.False, "A real in-flight asset read is required.");
                    runtime.Stop();
                    Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
                    backend.Shutdown();
                    Assert.That(backend.Bundle != null, Is.True, "Manager release waits for its native asset read.");
                    while (!reading.isDone) yield return null;
                    yield return null;
                    Assert.That(DefaultLoadResourceAgentHelper.PendingNativeCount, Is.Zero);
                    Assert.That(backend.Bundle == null, Is.True);
                    Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
                }

                runtime = new AsyncRuntime();
                using (var backend = new NativeBackend(path))
                {
                    var scope = new AsyncScope(runtime);
                    var container = ResourceContainer.Create(scope, backend);
                    containers.Add(container);
                    var task = container.LoadAssetAsync<TextAsset>(AssetPath);
                    while (task.Status == UniTaskStatus.Pending)
                    {
                        yield return null;
                        backend.NativeUpdate();
                    }
                    Assert.That(task.GetAwaiter().GetResult(), Is.Not.Null);
                    Assert.That(scope.PendingCount, Is.Zero);
                    scope.Stop();
                    Assert.That(backend.Bundle == null, Is.True);
                }
            }
            finally { runtime.Stop(); foreach (var container in containers) { container.UnloadAllAssets(); GameFramework.ReferencePool.Release(container); } }
        }
    }
}
