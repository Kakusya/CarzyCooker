//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace Game
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour, IAsyncOwner
    {
        private AsyncRuntime m_AsyncRuntime;
        public AsyncScope AsyncScope { get; private set; }
        public static IAsyncOwner RuntimeOwner { get; private set; }

        private void Start()
        {
            InitBuiltinComponents();
            m_AsyncRuntime = AsyncRuntime.Begin();
            AsyncScope = new AsyncScope(m_AsyncRuntime);
            RuntimeOwner = this;
            Base.ShuttingDown += m_AsyncRuntime.Stop;
            InitExtensionComponents();
            InitGameComponents();

            UnityGameFramework.Runtime.GameEntry.Initialize();
        }

        private void OnDestroy()
        {
            m_AsyncRuntime?.Stop();
            if (ReferenceEquals(RuntimeOwner, this)) RuntimeOwner = null;
        }

        private void OnApplicationQuit()
        {
            if (UnityGameFramework.Runtime.GameEntry.IsInitialized)
            {
                UnityGameFramework.Runtime.GameEntry.Shutdown(UnityGameFramework.Runtime.ShutdownType.Quit);
            }
        }
    }
}
