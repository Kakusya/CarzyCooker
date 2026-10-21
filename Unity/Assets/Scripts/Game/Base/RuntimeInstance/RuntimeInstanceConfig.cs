using System;
using System.Net;

namespace Game
{
    public enum RuntimeTestDestination { None, Catalog, Run }

    public enum RuntimeInstanceRole
    {
        Legacy,
        Host,
        Client,
        Solo
    }

    // 每轮启动意图；不包含工作区职责、产品身份或实际监听结果。
    public sealed class RuntimeInstanceConfig
    {
        public string RunId { get; }
        public string InstanceId { get; }
        public RuntimeInstanceRole Role { get; }
        public string Address { get; }
        public int Port { get; }
        public bool ShowModeMenu { get; }
        public bool StartInRoom { get; }
        public string EntryFeedback { get; }
        public RuntimeTestDestination TestDestination { get; }
        // Run时为目标，其他入口仅记住本次应用内的选择，不授予测试开局权限。
        public int TestLevelId { get; }
        public bool IsDevelopmentTest => TestDestination == RuntimeTestDestination.Run;
        public static bool DevelopmentTestsAvailable => UnityEngine.Application.isEditor || UnityEngine.Debug.isDebugBuild;

        private RuntimeInstanceConfig(string runId, string instanceId, RuntimeInstanceRole role, string address, int port,
            bool showModeMenu, bool startInRoom, string entryFeedback,
            RuntimeTestDestination testDestination = RuntimeTestDestination.None, int testLevelId = 0)
        {
            RunId = runId;
            InstanceId = instanceId;
            Role = role;
            Address = address;
            Port = port;
            ShowModeMenu = showModeMenu;
            StartInRoom = startInRoom;
            EntryFeedback = entryFeedback;
            TestDestination = testDestination;
            TestLevelId = testLevelId;
        }

        public static RuntimeInstanceConfig Create(string runId, string instanceId, string role, string listen = null, string connect = null,
            bool showModeMenu = false, bool startInRoom = false, string entryFeedback = null,
            RuntimeTestDestination testDestination = RuntimeTestDestination.None, int testLevelId = 0)
        {
            ValidateId(runId, nameof(runId));
            ValidateId(instanceId, nameof(instanceId));
            RuntimeInstanceRole parsedRole = role switch
            {
                "legacy" => RuntimeInstanceRole.Legacy,
                "host" => RuntimeInstanceRole.Host,
                "client" => RuntimeInstanceRole.Client,
                "solo" => RuntimeInstanceRole.Solo,
                _ => throw new ArgumentException("role 必须是 legacy、solo、host 或 client。")
            };
            if (testDestination is not (RuntimeTestDestination.None or RuntimeTestDestination.Catalog or RuntimeTestDestination.Run) || testLevelId < 0)
            {
                throw new ArgumentException("InvalidDevelopmentTestIntent");
            }
            if (testDestination != RuntimeTestDestination.None)
            {
                ValidateDevelopmentAccess(parsedRole, UnityEngine.Application.isEditor, UnityEngine.Debug.isDebugBuild);
                if (testDestination == RuntimeTestDestination.Run && (testLevelId == 0 || showModeMenu) ||
                    testDestination == RuntimeTestDestination.Catalog && !showModeMenu)
                {
                    throw new ArgumentException("InvalidDevelopmentTestDestination");
                }
            }
            if (showModeMenu && parsedRole != RuntimeInstanceRole.Solo || startInRoom && parsedRole is not (RuntimeInstanceRole.Host or RuntimeInstanceRole.Client))
            {
                throw new ArgumentException("模式选择只用于初始单机上下文，合作房间需要 Host 或 Client。 ");
            }
            if (parsedRole is RuntimeInstanceRole.Legacy or RuntimeInstanceRole.Solo)
            {
                if (!string.IsNullOrEmpty(listen) || !string.IsNullOrEmpty(connect))
                {
                    throw new ArgumentException("legacy／solo 不接受 endpoint。 ");
                }
                return new RuntimeInstanceConfig(runId, instanceId, parsedRole, null, 0, showModeMenu, startInRoom, entryFeedback, testDestination, testLevelId);
            }
            if (parsedRole == RuntimeInstanceRole.Host && !string.IsNullOrEmpty(connect) ||
                parsedRole == RuntimeInstanceRole.Client && !string.IsNullOrEmpty(listen))
            {
                throw new ArgumentException("endpoint 与 role 冲突。 ");
            }
            string endpoint = parsedRole == RuntimeInstanceRole.Host ? listen : connect;
            if (parsedRole == RuntimeInstanceRole.Host && string.IsNullOrEmpty(endpoint))
            {
                endpoint = "127.0.0.1:0";
            }
            int split = endpoint?.LastIndexOf(':') ?? -1;
            if (split <= 0 || !IPAddress.TryParse(endpoint.Substring(0, split).Trim('[', ']'), out IPAddress address) ||
                !int.TryParse(endpoint.Substring(split + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int port) ||
                port < 0 || port > 65535 || !IPAddress.IsLoopback(address) || parsedRole == RuntimeInstanceRole.Client && port == 0)
            {
                throw new ArgumentException("需要 IP 格式的 loopback endpoint；client 端口必须非零。 ");
            }
            return new RuntimeInstanceConfig(runId, instanceId, parsedRole, address.ToString(), port, showModeMenu, startInRoom, entryFeedback, testDestination, testLevelId);
        }

        public static void ValidateDevelopmentAccess(RuntimeInstanceRole role, bool editor, bool developmentBuild)
        {
            if (!(editor || developmentBuild) || role != RuntimeInstanceRole.Solo)
            {
                throw new InvalidOperationException("DevelopmentTestRequiresSoloDevelopmentEnvironment");
            }
        }

        public static void RequireDevelopmentAccess(RuntimeInstanceRole role)
            => ValidateDevelopmentAccess(role, UnityEngine.Application.isEditor, UnityEngine.Debug.isDebugBuild);

        public RuntimeInstanceConfig WithTestSelection(int levelId) => new(RunId, InstanceId, Role, Address, Port,
            ShowModeMenu, StartInRoom, EntryFeedback, TestDestination, levelId);

        private static void ValidateId(string value, string name)
        {
            if (string.IsNullOrEmpty(value) || value == "." || value == "..")
            {
                throw new ArgumentException("运行标识不能为空或仅为点。", name);
            }
            foreach (char c in value)
            {
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '.' || c == '_' || c == '-'))
                {
                    throw new ArgumentException("运行标识只接受字母、数字、点、下划线和短横线。", name);
                }
            }
        }
    }
}
