#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game
{
    // SessionState 跨进入 Play 的域重载保留，Editor 退出后自然失效。
    public static class RuntimeInstanceRequest
    {
        [Serializable]
        private sealed class Request
        {
            public string RunId;
            public string InstanceId;
            public string Role;
            public string Listen;
            public string Connect;
            public RuntimeTestDestination TestDestination;
            public int TestLevelId;
        }

        public static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;

        public static string Key(string projectPath, string kind)
        {
            return GameFramework.Utility.Text.Format("CarzyCooker.Instance.{0}.{1}", Path.GetFullPath(projectPath).TrimEnd('/', '\\').Replace('\\', '/').ToUpperInvariant(), kind);
        }

        public static void Submit(RuntimeInstanceConfig config, string projectPath = null)
        {
            Request request = new Request
            {
                RunId = config.RunId,
                InstanceId = config.InstanceId,
                Role = config.Role.ToString().ToLowerInvariant(),
                TestDestination = config.TestDestination,
                TestLevelId = config.TestLevelId
            };
            if (config.Role is RuntimeInstanceRole.Host or RuntimeInstanceRole.Client)
            {
                string address = config.Address.Contains(":") ? GameFramework.Utility.Text.Format("[{0}]", config.Address) : config.Address;
                string endpoint = GameFramework.Utility.Text.Format("{0}:{1}", address, config.Port);
                if (config.Role == RuntimeInstanceRole.Host)
                {
                    request.Listen = endpoint;
                }
                else
                {
                    request.Connect = endpoint;
                }
            }
            SessionState.SetString(Key(projectPath ?? ProjectPath, "Request"), JsonUtility.ToJson(request));
        }

        public static RuntimeInstanceConfig Consume(string projectPath)
        {
            string key = Key(projectPath, "Request");
            string json = SessionState.GetString(key, "");
            SessionState.EraseString(key);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            Request request = JsonUtility.FromJson<Request>(json);
            return RuntimeInstanceConfig.Create(request.RunId, request.InstanceId, request.Role, request.Listen, request.Connect,
                testDestination: request.TestDestination, testLevelId: request.TestLevelId);
        }

        public static void Clear(string projectPath, bool preserveInterruption = false)
        {
            SessionState.EraseString(Key(projectPath, "Request"));
            SessionState.EraseBool(Key(projectPath, "Active"));
            if (!preserveInterruption)
            {
                SessionState.EraseBool(Key(projectPath, "Interrupted"));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Handoff()
        {
            try
            {
                if (SessionState.GetBool(Key(ProjectPath, "Interrupted"), false))
                {
                    RuntimeInstanceContext.Fail("PlayDomainReloadInterrupted");
                    return;
                }
                RuntimeInstanceConfig config = Consume(ProjectPath);
                if (config != null)
                {
                    RuntimeInstanceContext.Prepare(config);
                    SessionState.SetBool(Key(ProjectPath, "Active"), true);
                }
            }
            catch (Exception e)
            {
                RuntimeInstanceContext.Fail(GameFramework.Utility.Text.Format("InstanceHandoffFailed: {0}", e.Message));
            }
        }
    }
}
#endif
