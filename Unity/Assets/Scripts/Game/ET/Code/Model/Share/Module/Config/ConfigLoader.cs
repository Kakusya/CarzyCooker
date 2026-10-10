using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Luban;
using Luban.SimpleJSON;

namespace ET
{
    [Invoke]
    public class LubanLoadAllAsyncHandler : AInvokeHandler<ConfigComponent.LoadAll, UniTask>
    {
        public override async UniTask Handle(ConfigComponent.LoadAll arg)
        {
            ConfigComponent config = ConfigComponent.Instance;
            Tables tables = World.Instance.AddSingleton<Tables>();
            Type tablesType = typeof (Tables);
            MethodInfo loadMethodInfo = tablesType.GetMethod("LoadAsync");
            if (loadMethodInfo == null)
            {
                return;
            }
            Type loaderReturnType = loadMethodInfo.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
            if (loaderReturnType == typeof (UniTask<ByteBuf>))
            {
                async UniTask<ByteBuf> LoadByteBuf(string file)
                {
                    byte[] bytes = await config.ReadBytesAsync(file);
                    if (!ReferenceEquals(config, ConfigComponent.Instance))
                    {
                        throw new OperationCanceledException("Configuration owner stopped");
                    }
                    return new ByteBuf(bytes);
                }
                Func<string, UniTask<ByteBuf>> func = LoadByteBuf;
                await (UniTask)loadMethodInfo.Invoke(tables, new object[] { func });
            }
            else
            {
                async UniTask<JSONNode> LoadJson(string file)
                {
                    string json = await config.ReadTextAsync(file);
                    if (!ReferenceEquals(config, ConfigComponent.Instance))
                    {
                        throw new OperationCanceledException("Configuration owner stopped");
                    }
                    return JSON.Parse(json);
                }
                Func<string, UniTask<JSONNode>> func = LoadJson;
                await (UniTask)loadMethodInfo.Invoke(tables, new object[] { func });
            }
        }
    }
    
    [Invoke]
    public class LubanLoadOneAsyncHandler: AInvokeHandler<ConfigComponent.ReloadOne, UniTask>
    {
        public override async UniTask Handle(ConfigComponent.ReloadOne arg)
        {
            Type tablesType = typeof (Tables);
            MethodInfo refreshMethodInfo = tablesType.GetMethod("Refresh");
            if (refreshMethodInfo == null)
            {
#if UNITY_HOTFIX
                await ConfigComponent.Instance.ReadBytesAsync("Config");
                object obj = Activator.CreateInstance(tablesType);
                ((ISingletonAwake)obj).Awake();
                World.Instance.AddSingleton((ASingleton)obj);
#endif
            }
            else
            {
                await Tables.Instance.GetDataTable(arg.ConfigName).LoadAsync();
                refreshMethodInfo.Invoke(Tables.Instance, null);
            }
        }
    }
    
    [Invoke]
    public class LubanReloadAllAsyncHandler: AInvokeHandler<ConfigComponent.ReloadAll, UniTask>
    {
        public override async UniTask Handle(ConfigComponent.ReloadAll arg)
        {
            Type tablesType = typeof (Tables);
            MethodInfo refreshMethodInfo = tablesType.GetMethod("Refresh");
            if (refreshMethodInfo == null)
            {
#if UNITY_HOTFIX
                await ConfigComponent.Instance.ReadBytesAsync("Config");
                object obj = Activator.CreateInstance(tablesType);
                ((ISingletonAwake)obj).Awake();
                World.Instance.AddSingleton((ASingleton)obj);
#endif
            }
            else
            {
                foreach (var dataTable in Tables.Instance.DataTables)
                {
                    await dataTable.LoadAsync();
                }
                refreshMethodInfo.Invoke(Tables.Instance, null);
            }
        }
    }
}
