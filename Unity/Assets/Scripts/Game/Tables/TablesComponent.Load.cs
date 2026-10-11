using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Luban;
using Luban.SimpleJSON;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Extension;
using UnityGameFramework.Runtime;

namespace Game
{
    public enum TablesLoadType : byte
    {
        Undefined = 0,
        Bytes,
        Json,
        Code,
    }
    
    public partial class TablesComponent : GameFrameworkComponent, IAsyncOwner
    {
        public AsyncScope AsyncScope { get; private set; }
        private AsyncScope GetLoadScope()
        {
            if (AsyncScope == null) AsyncScope = new AsyncScope(AsyncRuntime.Current, () => this != null);
            return AsyncScope;
        }
        /// <summary>
        /// Luban加载类型
        /// </summary>
        [ShowInInspector, ReadOnly]
        public TablesLoadType LoadType { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            TablesMemory.Clear();
        }

        private void OnDestroy()
        {
            var scope = AsyncScope;
            AsyncScope = null;
            scope?.Stop();
            TablesMemory.Clear();
        }

        /// <summary>
        /// 加载所有的配置表Table
        /// </summary>
        /// <returns>是否是</returns>
        public UniTask LoadAllAsync() => GetLoadScope().RunAsync(_ => LoadAllCoreAsync());

        private async UniTask LoadAllCoreAsync()
        {
            Type tablesType = this.GetType();
            MethodInfo loadMethodInfo = tablesType.GetMethod("LoadAsync");
            if (loadMethodInfo == null)
            {
                LoadType = TablesLoadType.Code;
                return;
            }
            Type loaderReturnType = loadMethodInfo.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
            if (loaderReturnType == typeof (UniTask<ByteBuf>))
            {
                LoadType = TablesLoadType.Bytes;
                async UniTask<ByteBuf> LoadByteBuf(string file)
                {
                    return await this.ReadAssetAsync<TextAsset, ByteBuf>(AssetUtility.GetLubanAsset(file, false), asset => new ByteBuf(asset.bytes));
                }
                Func<string, UniTask<ByteBuf>> func = LoadByteBuf;
                await (UniTask)loadMethodInfo.Invoke(this, new object[] { func });
            }
            else
            {
                LoadType = TablesLoadType.Json;
                async UniTask<JSONNode> LoadJson(string file)
                {
                    return await this.ReadAssetAsync<TextAsset, JSONNode>(AssetUtility.GetLubanAsset(file, true), asset => JSON.Parse(asset.text));
                }
                Func<string, UniTask<JSONNode>> func = LoadJson;
                await (UniTask)loadMethodInfo.Invoke(this, new object[] { func });
            }
        }
    }
}
