using Cysharp.Threading.Tasks;

using System;
#if !DOTNET
using Game;
#endif

namespace ET
{
    public struct EntryEvent1
    {
    }   
    
    public struct EntryEvent2
    {
    } 
    
    public struct EntryEvent3
    {
    }
    
    public static class Entry
    {
        public static void Init()
        {
            
        }
        
        public static void Start()
        {
            StartAsync().Forget();
        }
        
        private static async UniTaskVoid StartAsync()
        {
            CodeLoaderComponent loader = CodeLoaderComponent.Instance;
#if !DOTNET
            World world = ProductBootstrapBridge.CurrentWorld;
            RuntimeInstanceConfig config = RuntimeInstanceContext.Config;
            try
            {
#endif
            WinPeriod.Init();

            // 注册Mongo type
            MongoRegister.Init();
            // 注册Entity序列化器
            EntitySerializeRegister.Init();
            World.Instance.AddSingleton<IdGenerater>();
            World.Instance.AddSingleton<OpcodeType>();
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<MessageQueue>();
            World.Instance.AddSingleton<NetServices>();
            World.Instance.AddSingleton<NavmeshComponent>();
            World.Instance.AddSingleton<LogMsg>();

            // 创建需要reload的code singleton
            CodeTypes.Instance.CreateCode();

            await ConfigComponent.Instance.LoadAllAsync();
#if !DOTNET
            if (CodeLoaderComponent.Instance != loader || !ProductBootstrapBridge.IsCurrentInitialization(world))
            {
                return;
            }
            if (config != null && config.Role != RuntimeInstanceRole.Legacy)
            {
                if (!ReferenceEquals(config, RuntimeInstanceContext.Config) || RuntimeInstanceContext.State != RuntimeInstanceState.Prepared)
                {
                    return;
                }
                if (!CodeTypes.Instance.GetTypes().ContainsKey("ET.FiberInit_ProductRoot"))
                {
                    throw new InvalidOperationException("ProductBootstrapUnavailable");
                }
                await FiberManager.Instance.Create(SchedulerType.Main, ConstFiberId.Main, 0, SceneType.ProductRoot, "ProductRoot");
                if (RuntimeInstanceContext.State == RuntimeInstanceState.Failed)
                {
                    ProductBootstrapBridge.StopInitialization(world);
                }
                return;
            }
#endif
            await FiberManager.Instance.Create(SchedulerType.Main, ConstFiberId.Main, 0, SceneType.Main, "");
#if !DOTNET
            }
            catch (Exception e)
            {
                if (ProductBootstrapBridge.IsCurrentInitialization(world))
                {
                    RuntimeInstanceContext.Fail(config, $"ETEntryFailed: {e.Message}");
                    Log.Warning($"ET Entry failed: {e}");
                    ProductBootstrapBridge.StopInitialization(world);
                }
            }
#endif
        }
    }
}
