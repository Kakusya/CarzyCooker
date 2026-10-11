using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityGameFramework.Extension;
using UnityGameFramework.Runtime;

namespace Game
{
    public static class HybridCLRHelper
    {
        public static readonly string ConfigAsset = "Assets/Res/HybridCLR/HybridCLRConfig.asset";
        
        public static UniTask LoadAsync()
        {
            return GameEntry.RuntimeOwner.ReadAssetAsync<HybridCLRConfig, bool>(ConfigAsset, aotGroup =>
            {
                foreach (TextAsset textAsset in aotGroup.AotAssemblies)
                    RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.Consistent);
                Log.Info("HybridCLR load metadata.");
                return true;
            }).AsUniTask();
        }
    }
}
