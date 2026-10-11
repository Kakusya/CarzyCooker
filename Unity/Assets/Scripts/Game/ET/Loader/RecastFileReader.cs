using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityGameFramework.Extension;

namespace ET
{
    [Invoke]
    public class RecastFileReader: AInvokeHandler<NavmeshComponent.RecastFileLoader, UniTask<byte[]>>
    {
        public override async UniTask<byte[]> Handle(NavmeshComponent.RecastFileLoader args)
        {
            IAsyncOwner owner = Init.Instance;
            return await owner.ReadAssetAsync<TextAsset, byte[]>(AssetUtility.GetETAsset($"ClientServer/Recast/{args.Name}.bytes"), asset => asset.bytes);
        }
    }
}