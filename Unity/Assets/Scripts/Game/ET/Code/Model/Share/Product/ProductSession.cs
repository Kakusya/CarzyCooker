#if !DOTNET
using System.Collections.Generic;
using Game;

namespace ET
{
    public enum ProductStage
    {
        Initializing,
        HostListening,
        Joining,
        HostReady,
        ClientReady,
        Failed,
        Stopping,
        Stopped,
        SoloReady
    }

    public enum ProductClientStage
    {
        Connecting,
        Joining,
        Joined,
        Failed,
        Stopping,
        Stopped
    }

    public enum ProductControlAction
    {
        Join = 1,
        Probe = 2,
        Leave = 3,
        Close = 4
    }

    public static class ProductSessionRules
    {
        public const int ProtocolVersion = 12;
        public const int MaxPayloadBytes = 1024;
        public const int RequestTimeout = 4000;
        public const int HeartbeatInterval = 2000;
        public const int HeartbeatTimeout = 2000;
        public const int ResponseCloseGrace = 100;
    }

    [UniqueId(205000, 205099)]
    public static class ProductError
    {
        public const int IncompatibleProtocol = 205001;
        public const int PeerClosed = 205002;
        public const int NotJoined = 205003;
        public const int InvalidRequest = 205004;
        public const int WrongMatch = 205005;
        public const int RuntimeStopped = 205006;
    }

    // 客户端独立值投影，不能保留池化 wire DTO 或 Host Participant 引用。
    public readonly struct ProductMemberSnapshot
    {
        public int MemberId { get; }
        public bool Online { get; }

        public ProductMemberSnapshot(int memberId, bool online)
        {
            MemberId = memberId;
            Online = online;
        }
    }

    [ComponentOf(typeof(Scene))]
    public sealed class ProductRuntimeComponent : Entity, IAwake<RuntimeInstanceConfig>, IDestroy
    {
        public RuntimeInstanceConfig Config;
        public ProductStage Stage;
        public string Failure;
        public string ListenEndpoint;
        public EntityRef<ProcessInnerSender> Sender;
        public EntityRef<Scene> HostScene;
        public EntityRef<Scene> ClientScene;
        public EntityRef<HostSessionComponent> Authority;
        public EntityRef<ClientSessionComponent> Client;
    }

    [ChildOf(typeof(Scene))]
    public sealed class Match : Entity, IAwake
    {
        public string MatchId;
        public long RosterVersion;
        // 仅由 HostSession 有序入口分配；本 Match 内不回绕或复用。
        public int NextMemberId;
        public EntityRef<RestaurantRuntime> Restaurant;
    }

    [ChildOf(typeof(Match))]
    public sealed class Participant : Entity, IAwake
    {
        public int MemberId;
        public bool Online;
        public EntityRef<HostPeer> BoundPeer;
        public long BindingGeneration;
    }

    [ComponentOf(typeof(Scene))]
    public sealed class HostSessionComponent : Entity, IAwake<Match, ProductRuntimeComponent>, IUpdate
    {
        public EntityRef<Match> Match;
        public EntityRef<ProductRuntimeComponent> Runtime;
        public long NextBindingGeneration;
    }

    [ChildOf(typeof(HostSessionComponent))]
    public sealed class HostPeer : Entity, IAwake<HostSessionComponent, long>
    {
        public EntityRef<HostSessionComponent> Authority;
        public long Generation;
        public int MemberId;
        public bool EntryOpen;
        public bool Closing;
        public bool CloseFinished;
        // 入口在 Call 前增加，整个原调用/回送结束后减少；不是第二份请求等待表。
        public int InFlight;
        // 仅最近命令的入口记账，最终世界事实仍由领域 owner 保管。
        public ProductCommandContext LastCommandContext;
        public ProductCommandKind LastCommandKind;
        public GridIntent LastGridCommand;
        public LevelControlIntent LastLevelCommand;
        public GridStationLayoutIntent LastLayoutCommand;
        public FrontWorkIntent LastFrontWorkCommand;
        public ServeOrderIntent LastServeCommand;
        public BuffChoiceIntent LastBuffCommand;
        public GridResultStatus LastCommandStatus;
        public string LastCommandReason;
    }

    [ComponentOf(typeof(HostPeer))]
    public sealed class LocalDelivery : Entity, IAwake<ClientSessionComponent>
    {
        public EntityRef<ClientSessionComponent> Client;
    }

    [ComponentOf(typeof(HostPeer))]
    public sealed class RemoteDelivery : Entity, IAwake<Session>
    {
        public EntityRef<Session> Session;
    }

    [ComponentOf(typeof(Session))]
    public sealed class ProductPeerBinding : Entity, IAwake<HostPeer>, IDestroy
    {
        public EntityRef<HostPeer> Peer;
        public long Generation;
    }

    [ComponentOf(typeof(Session))]
    public sealed class ProductClientBinding : Entity, IAwake<ClientSessionComponent>, IDestroy
    {
        public EntityRef<ClientSessionComponent> Client;
    }

    [ComponentOf(typeof(Scene))]
    public sealed class ClientSessionComponent : Entity, IAwake<ProductRuntimeComponent>, IDestroy
    {
        public EntityRef<ProductRuntimeComponent> Runtime;
        public ProductClientStage Stage;
        public string MatchId;
        public int MemberId;
        public long BindingGeneration;
        public long RosterVersion;
        public readonly Dictionary<int, ProductMemberSnapshot> Roster = new();
        public EntityRef<LocalMessageComponent> Local;
        public EntityRef<Session> Session;
        public bool HeartbeatStarted;
        public bool HeartbeatInFlight;
        public int NextProbeSequence;
        public string LastProbePayload;
        public int LastProbeMemberId;
        public long NextCommandId;
        // Joining 下候选只属于当前入口，Join 确认后才安装到正式投影。
        public string CandidateMatchId;
        public long CandidateGeneration;
        public long CandidateVersion;
        public readonly Dictionary<int, ProductMemberSnapshot> CandidateRoster = new();
    }

    [ComponentOf(typeof(Scene))]
    public sealed class LocalMessageComponent : Entity, IAwake<ClientSessionComponent, HostPeer>
    {
        public EntityRef<ClientSessionComponent> Client;
        public EntityRef<HostPeer> Peer;
    }

    public struct ProductMemberChanged
    {
        public string MatchId;
        public long RosterVersion;
    }

    public struct ProductRosterApplied
    {
        public long RosterVersion;
    }

    public struct ProductClientJoined
    {
        public int MemberId;
    }

    public struct ProductClientLeft
    {
    }
}
#endif
