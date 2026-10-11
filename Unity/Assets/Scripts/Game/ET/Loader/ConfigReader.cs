using Cysharp.Threading.Tasks;
using Game;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Extension;

namespace ET
{
    public class ConfigReader : IConfigReader
    {
        private readonly IAsyncOwner m_Owner = Init.Instance;
        public async UniTask<byte[]> ReadBytesAsync(string file)
        {
            return await m_Owner.ReadAssetAsync<TextAsset, byte[]>(GetLubanAssetPath(file, false), asset => asset.bytes);
        }

        public async UniTask<string> ReadTextAsync(string file)
        {
            return await m_Owner.ReadAssetAsync<TextAsset, string>(GetLubanAssetPath(file, true), asset => asset.text);
        }
        
        private string GetLubanAssetPath(string fileName, bool isJson)
        {
            if (Define.CodeMode == CodeMode.Client)
            {
                if (isJson)
                {
                    return AssetUtility.GetETAsset(Utility.Text.Format("Client/Luban/{0}.json", fileName));
                }
                else
                {
                    return AssetUtility.GetETAsset(Utility.Text.Format("Client/Luban/{0}.bytes", fileName));
                }
            }
            else
            {
                if (isJson)
                {
                    return AssetUtility.GetETAsset(Utility.Text.Format("ClientServer/Luban/{0}.json", fileName));
                }
                else
                {
                    return AssetUtility.GetETAsset(Utility.Text.Format("ClientServer/Luban/{0}.bytes", fileName));
                }
            }
        }
    }
}
