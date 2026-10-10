using System;
using System.Net;

namespace Game
{
    public enum RuntimeInstanceRole
    {
        Legacy,
        Host,
        Client
    }

    // 每轮启动意图；不包含工作区职责、产品身份或实际监听结果。
    public sealed class RuntimeInstanceConfig
    {
        public string RunId { get; }
        public string InstanceId { get; }
        public RuntimeInstanceRole Role { get; }
        public string Address { get; }
        public int Port { get; }

        private RuntimeInstanceConfig(string runId, string instanceId, RuntimeInstanceRole role, string address, int port)
        {
            RunId = runId;
            InstanceId = instanceId;
            Role = role;
            Address = address;
            Port = port;
        }

        public static RuntimeInstanceConfig Create(string runId, string instanceId, string role, string listen = null, string connect = null)
        {
            ValidateId(runId, nameof(runId));
            ValidateId(instanceId, nameof(instanceId));
            RuntimeInstanceRole parsedRole = role switch
            {
                "legacy" => RuntimeInstanceRole.Legacy,
                "host" => RuntimeInstanceRole.Host,
                "client" => RuntimeInstanceRole.Client,
                _ => throw new ArgumentException("role 必须是 legacy、host 或 client。")
            };
            if (parsedRole == RuntimeInstanceRole.Legacy)
            {
                if (!string.IsNullOrEmpty(listen) || !string.IsNullOrEmpty(connect))
                {
                    throw new ArgumentException("legacy 不接受 endpoint。 ");
                }
                return new RuntimeInstanceConfig(runId, instanceId, parsedRole, null, 0);
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
            return new RuntimeInstanceConfig(runId, instanceId, parsedRole, address.ToString(), port);
        }

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
