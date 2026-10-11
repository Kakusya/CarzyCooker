using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    // Native handles only, not a resource pool. No callbacks here touch a GF module or GameEntry.
    // Keep manager-owned bundles (including dependencies) alive until all current native asset reads end.
    internal static class NativeResourceRetirement
    {
        private static readonly HashSet<AsyncOperation> s_Pending = new HashSet<AsyncOperation>();
        private static readonly HashSet<AssetBundle> s_Releases = new HashSet<AssetBundle>();
        internal static int PendingCount => s_Pending.Count;

        internal static void Track(AsyncOperation operation)
        {
            if (operation == null || operation.isDone || !s_Pending.Add(operation)) return;
            operation.completed += Complete;
        }
        private static void Complete(AsyncOperation operation)
        {
            operation.completed -= Complete;
            s_Pending.Remove(operation);
            if (s_Pending.Count != 0) return;
            var bundles = new List<AssetBundle>(s_Releases);
            s_Releases.Clear();
            foreach (var bundle in bundles) if (bundle != null) bundle.Unload(true);
        }
        internal static void Release(AssetBundle bundle)
        {
            if (bundle == null) return;
            if (s_Pending.Count == 0) bundle.Unload(true);
            else s_Releases.Add(bundle);
        }
        internal static void Retire(AssetBundleCreateRequest request)
        {
            if (request == null) return;
            if (request.isDone)
            {
                Release(request.assetBundle);
                return;
            }
            Track(request);
            // This bundle was never transferred into the manager's resource/dependency pool.
            request.completed += RetiredCreateComplete;
        }
        private static void RetiredCreateComplete(AsyncOperation operation)
        {
            operation.completed -= RetiredCreateComplete;
            Release(((AssetBundleCreateRequest)operation).assetBundle);
        }
    }
}
