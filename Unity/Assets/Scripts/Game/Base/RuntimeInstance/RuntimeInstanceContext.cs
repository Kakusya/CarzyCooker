using UnityEngine;

namespace Game
{
    public enum ProductStoragePlatform { Unsupported = 0, PC = 1, Android = 2 }

    public enum RuntimeInstanceState
    {
        NotRequested,
        Preparing,
        Prepared,
        Failed,
        Stopping,
        Stopped
    }

    public static class RuntimeInstanceContext
    {
        // Unity 平台事实由壳层提供，ET Model 不引用 UnityEngine。
        public static int ProductPlatformId => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.Android ? 2 : 1;
        public static ProductStoragePlatform StoragePlatform => StoragePlatformFor(Application.platform);
        public static ProductStoragePlatform StoragePlatformFor(RuntimePlatform platform) => platform switch
        {
            RuntimePlatform.Android => ProductStoragePlatform.Android,
            RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer or
                RuntimePlatform.LinuxEditor or RuntimePlatform.LinuxPlayer => ProductStoragePlatform.PC,
            _ => ProductStoragePlatform.Unsupported
        };
        public static string StoragePlatformName(ProductStoragePlatform platform) => platform switch
        {
            ProductStoragePlatform.PC => "PC",
            ProductStoragePlatform.Android => "Android",
            _ => throw new System.InvalidOperationException("当前平台只支持内存试玩")
        };
        public static RuntimeInstanceConfig Config { get; private set; }
        public static RuntimeInstanceState State { get; private set; }
        public static string Failure { get; private set; }
        public static string ProductStage { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Config = null;
            Failure = null;
            ProductStage = null;
            State = RuntimeInstanceState.NotRequested;
        }

        public static void Prepare(RuntimeInstanceConfig config)
        {
            if (State != RuntimeInstanceState.NotRequested)
            {
                throw new System.InvalidOperationException("本轮上下文已建立。 ");
            }
            State = RuntimeInstanceState.Preparing;
            Config = config ?? throw new System.ArgumentNullException(nameof(config));
            State = RuntimeInstanceState.Prepared;
        }

        public static void Fail(string reason)
        {
            Failure ??= reason;
            State = RuntimeInstanceState.Failed;
        }

        public static void Fail(RuntimeInstanceConfig owner, string reason)
        {
            if (ReferenceEquals(owner, Config) && State != RuntimeInstanceState.Stopping && State != RuntimeInstanceState.Stopped)
            {
                Fail(reason);
            }
        }

        public static void ReportProductStage(RuntimeInstanceConfig owner, string stage)
        {
            if (!ReferenceEquals(owner, Config) || State != RuntimeInstanceState.Prepared)
            {
                return;
            }
            ProductStage = stage;
            UnityGameFramework.Runtime.Log.Info("[Instance {0}/{1}] {2}", owner.RunId, owner.InstanceId, stage);
        }

        public static void Stop()
        {
            if (Failure != null)
            {
                State = RuntimeInstanceState.Failed;
                return;
            }
            State = RuntimeInstanceState.Stopping;
            State = RuntimeInstanceState.Stopped;
        }
    }
}
