using System;
using CommandLine;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game;

namespace ET
{
    [DisallowMultipleComponent]
    public class Init : MonoBehaviour, IAsyncOwner
    {
        public static Init Instance { get; private set; }

        private class Runner : MonoBehaviour
        {
            public Init Owner;
            private void Update()
            {
                if (Owner == null || Owner.m_Stopped)
                {
                    return;
                }
                TimeInfo.Instance.Update();
                FiberManager.Instance.Update();
            }

            private void LateUpdate()
            {
                if (Owner == null || Owner.m_Stopped)
                {
                    return;
                }
                FiberManager.Instance.LateUpdate();
            }

            private void OnDestroy()
            {
                Owner?.Shutdown();
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                if (Owner != null && !Owner.m_Stopped)
                {
                    EventSystem.Instance?.Invoke(new OnApplicationPause(pauseStatus));
                }
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                if (Owner != null && !Owner.m_Stopped)
                {
                    EventSystem.Instance?.Invoke(new OnApplicationFocus(hasFocus));
                }
            }
        }

        private Runner m_RunnerComponent;
        private World m_World;
        private bool m_Stopped;
        private RuntimeInstanceConfig m_Config;
        private UnhandledExceptionEventHandler m_ExceptionHandler;
        public AsyncScope AsyncScope { get; private set; }

        public bool IsCurrent(World world) => !m_Stopped && Instance == this && ReferenceEquals(m_World, world);
        public World OwnedWorld => m_World;

        private void Awake()
        {
            Instance = this;
            m_Stopped = false;
            AsyncScope = new AsyncScope(AsyncRuntime.Current, () => !m_Stopped && Instance == this, GameEntry.RuntimeOwner.AsyncScope);
            m_Config = RuntimeInstanceContext.Config;
#if UNITY_ET_VIEW && UNITY_EDITOR
            Entity.SetRootView(this.transform);
#endif
        }

        private void Start()
        {
            this.RunAsync(_ => StartAsync());
        }

        private void OnDestroy()
        {
            Shutdown();
            if (this.m_RunnerComponent != null)
            {
                Runner runner = this.m_RunnerComponent;
                this.m_RunnerComponent = null;
                DestroyImmediate(runner);
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Shutdown()
        {
            if (m_Stopped)
            {
                return;
            }
            m_Stopped = true;
            var oldScope = AsyncScope;
            AsyncScope = null;
            oldScope?.Stop();
            World world = m_World;
            m_World = null;
            ProductBootstrapBridge.ClearInitialization(world);
            if (m_ExceptionHandler != null)
            {
                AppDomain.CurrentDomain.UnhandledException -= m_ExceptionHandler;
                m_ExceptionHandler = null;
            }
            try
            {
                ProductBootstrapBridge.StopCurrent();
                EventSystem.Instance?.Invoke(new OnShutdown());
            }
            finally
            {
                world?.Dispose();
            }
        }

        private async UniTask StartAsync()
        {
            m_ExceptionHandler = (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            AppDomain.CurrentDomain.UnhandledException += m_ExceptionHandler;
            m_World = World.Instance;
            ProductBootstrapBridge.RegisterInitialization(m_World, Shutdown);
            try
            {

                // 命令行参数
                string[] args = "".Split(" ");
                Parser.Default.ParseArguments<Options>(args)
                        .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                        .WithParsed((o) => World.Instance.AddSingleton(o));
                Options.Instance.StartConfig = "Localhost";

                World.Instance.AddSingleton<Logger, ILog>(new UnityLogger());
                World.Instance.AddSingleton<TimeInfo, ITimeNow>(new UnityTimeNow());
                World.Instance.AddSingleton<FiberManager>();
                World.Instance.AddSingleton<ConfigComponent, IConfigReader>(new ConfigReader());
                World.Instance.AddSingleton<CodeLoaderComponent, ICodeLoader>(new CodeLoader());

                await CodeLoaderComponent.Instance.StartAsync();
                if (m_Stopped || Instance != this)
                {
                    return;
                }
                this.m_RunnerComponent = this.gameObject.AddComponent<Runner>();
                this.m_RunnerComponent.Owner = this;
            }
            catch (Exception e)
            {
                if (!m_Stopped)
                {
                    RuntimeInstanceContext.Fail(m_Config, $"ETInitializationFailed: {e.Message}");
                    Log.Warning($"ET initialization failed: {e.Message}");
                    Shutdown();
                }
            }
        }
    }
}
