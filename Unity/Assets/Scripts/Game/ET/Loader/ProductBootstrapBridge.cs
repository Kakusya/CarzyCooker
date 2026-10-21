using System;
using Game;
using Cysharp.Threading.Tasks;

namespace ET
{
    // 仅为稳定装载层提供根定位和同步关闭交接，业务状态仍只归 ProductRuntime 的 ET 树。
    public static class ProductBootstrapBridge
    {
        [StaticField]
        private static EntityRef<Entity> s_Root;
        [StaticField]
        private static RuntimeInstanceConfig s_Owner;
        [StaticField]
        private static Action s_Stop;
        [StaticField]
        private static World s_InitializationWorld;
        [StaticField]
        private static Action s_StopInitialization;
        [StaticField]
        private static bool s_SwitchPending;

        public static World CurrentWorld => s_InitializationWorld;

        public static bool ReturnToModeMenu(RuntimeInstanceConfig owner, string feedback = null)
            => SwitchInitialization(owner, RuntimeInstanceConfig.Create(Guid.NewGuid().ToString("N"), "launcher", "solo",
                showModeMenu: true, entryFeedback: feedback));

        public static bool SwitchInitialization(RuntimeInstanceConfig owner, RuntimeInstanceConfig next)
        {
            World world = s_InitializationWorld;
            if (s_SwitchPending || next == null || world == null || !ReferenceEquals(owner, RuntimeInstanceContext.Config))
            {
                return false;
            }
            s_SwitchPending = true;
            if (next.TestLevelId == 0 && owner.TestLevelId > 0)
            {
                next = next.WithTestSelection(owner.TestLevelId);
            }
            SwitchAfterFiberFrame(owner, world, next).Forget();
            return true;
        }

        private static async UniTaskVoid SwitchAfterFiberFrame(RuntimeInstanceConfig owner, World world, RuntimeInstanceConfig next)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                if (!ReferenceEquals(owner, RuntimeInstanceContext.Config) || !IsCurrentInitialization(world))
                {
                    return;
                }
                // 退出捕获的完整 ET 运行后，才建立新的不可变启动上下文。
                GameEntry.CodeRunner.StopRun();
                RuntimeInstanceContext.Reset();
                RuntimeInstanceContext.Prepare(next);
                GameEntry.CodeRunner.StartRun("ET.Init");
            }
            catch (Exception exception)
            {
                RuntimeInstanceContext.Fail(next, "ProductModeSwitchFailed: " + exception.Message);
                UnityEngine.Debug.LogException(exception);
            }
            finally
            {
                s_SwitchPending = false;
            }
        }

        public static void RegisterInitialization(World world, Action stop)
        {
            s_InitializationWorld = world;
            s_StopInitialization = stop;
        }

        public static bool IsCurrentInitialization(World world) => world != null && ReferenceEquals(world, s_InitializationWorld);

        public static void StopInitialization(World world)
        {
            if (IsCurrentInitialization(world))
            {
                s_StopInitialization?.Invoke();
            }
        }

        public static void ClearInitialization(World world)
        {
            if (IsCurrentInitialization(world))
            {
                s_InitializationWorld = null;
                s_StopInitialization = null;
            }
        }

        public static void ScheduleStopInitialization(RuntimeInstanceConfig owner)
        {
            World world = s_InitializationWorld;
            if (world == null || !ReferenceEquals(owner, RuntimeInstanceContext.Config))
            {
                return;
            }
            StopAfterFiberFrame(owner, world).Forget();
        }

        private static async UniTaskVoid StopAfterFiberFrame(RuntimeInstanceConfig owner, World world)
        {
            // 在 Runner 的 Fiber 遍历结束后关闭捕获 World，不能销毁调度器正在遍历的集合。
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (ReferenceEquals(owner, RuntimeInstanceContext.Config) && IsCurrentInitialization(world))
            {
                StopInitialization(world);
            }
        }

        public static Entity Root => s_Root;

        public static void Register(RuntimeInstanceConfig owner, Entity root, Action stop)
        {
            if (!ReferenceEquals(owner, RuntimeInstanceContext.Config) || RuntimeInstanceContext.State != RuntimeInstanceState.Prepared)
            {
                throw new InvalidOperationException("Product bootstrap owner is no longer current");
            }
            if (Root != null)
            {
                throw new InvalidOperationException("A product root is already registered");
            }
            s_Owner = owner;
            s_Root = root;
            s_Stop = stop;
        }

        public static void StopCurrent()
        {
            if (!ReferenceEquals(s_Owner, RuntimeInstanceContext.Config))
            {
                return;
            }
            Action stop = s_Stop;
            s_Stop = null;
            s_Root = default;
            s_Owner = null;
            stop?.Invoke();
        }

        public static void Clear(RuntimeInstanceConfig owner)
        {
            if (!ReferenceEquals(s_Owner, owner))
            {
                return;
            }
            s_Stop = null;
            s_Root = default;
            s_Owner = null;
        }
    }
}
