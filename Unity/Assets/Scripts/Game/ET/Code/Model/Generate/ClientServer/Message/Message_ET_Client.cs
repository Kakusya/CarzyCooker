// This is an automatically generated class by Share.Tool. Please do not modify it.

using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // proto file : ET-Client/ClientMessage.proto (line:6)
    [MemoryPackable]
    [Message(Message_ET_Client.Main2NetClient_Login)]
    [ResponseType(nameof(NetClient2Main_Login))]
    public partial class Main2NetClient_Login: MessageObject, IRequest
    {
        public static Main2NetClient_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Main2NetClient_Login), isFromPool) as Main2NetClient_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int OwnerFiberId { get; set; }
        /// <summary>
        /// 账号
        /// </summary>
        [MemoryPackOrder(2)]
        public string Account { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [MemoryPackOrder(3)]
        public string Password { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.OwnerFiberId = default;
            this.Account = default;
            this.Password = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ClientMessage.proto (line:14)
    [MemoryPackable]
    [Message(Message_ET_Client.NetClient2Main_Login)]
    public partial class NetClient2Main_Login: MessageObject, IResponse
    {
        public static NetClient2Main_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(NetClient2Main_Login), isFromPool) as NetClient2Main_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long PlayerId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 测试枚举
    // proto file : ET-Client/ClientMessage.proto (line:24)
    public enum TestEnum    {
    // 测试A
        A = 1,
        B = 2,
    }

    // proto file : ET-Client/LockStepOuter.proto (line:5)
    [MemoryPackable]
    [Message(Message_ET_Client.C2G_Match)]
    [ResponseType(nameof(G2C_Match))]
    public partial class C2G_Match: MessageObject, ISessionRequest
    {
        public static C2G_Match Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Match), isFromPool) as C2G_Match;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:10)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_Match)]
    public partial class G2C_Match: MessageObject, ISessionResponse
    {
        public static G2C_Match Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Match), isFromPool) as G2C_Match;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 匹配成功，通知客户端切换场景
    /// </summary>
    // proto file : ET-Client/LockStepOuter.proto (line:18)
    [MemoryPackable]
    [Message(Message_ET_Client.Match2G_NotifyMatchSuccess)]
    public partial class Match2G_NotifyMatchSuccess: MessageObject, IMessage
    {
        public static Match2G_NotifyMatchSuccess Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Match2G_NotifyMatchSuccess), isFromPool) as Match2G_NotifyMatchSuccess;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        /// <summary>
        /// 房间的ActorId
        /// </summary>
        [MemoryPackOrder(1)]
        public ActorId ActorId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ActorId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 客户端通知房间切换场景完成
    /// </summary>
    // proto file : ET-Client/LockStepOuter.proto (line:26)
    [MemoryPackable]
    [Message(Message_ET_Client.C2Room_ChangeSceneFinish)]
    public partial class C2Room_ChangeSceneFinish: MessageObject, IRoomMessage
    {
        public static C2Room_ChangeSceneFinish Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2Room_ChangeSceneFinish), isFromPool) as C2Room_ChangeSceneFinish;
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.PlayerId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:31)
    [MemoryPackable]
    [Message(Message_ET_Client.LockStepUnitInfo)]
    public partial class LockStepUnitInfo: MessageObject
    {
        public static LockStepUnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LockStepUnitInfo), isFromPool) as LockStepUnitInfo;
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(1)]
        public TrueSync.TSVector Position { get; set; }
        [MemoryPackOrder(2)]
        public TrueSync.TSQuaternion Rotation { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.PlayerId = default;
            this.Position = default;
            this.Rotation = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 房间通知客户端进入战斗
    /// </summary>
    // proto file : ET-Client/LockStepOuter.proto (line:39)
    [MemoryPackable]
    [Message(Message_ET_Client.Room2C_Start)]
    public partial class Room2C_Start: MessageObject, IMessage
    {
        public static Room2C_Start Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Room2C_Start), isFromPool) as Room2C_Start;
        }

        [MemoryPackOrder(0)]
        public long StartTime { get; set; }
        [MemoryPackOrder(1)]
        public List<LockStepUnitInfo> UnitInfo { get; set; } = new List<LockStepUnitInfo>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.StartTime = default;
            this.UnitInfo.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:45)
    [MemoryPackable]
    [Message(Message_ET_Client.FrameMessage)]
    public partial class FrameMessage: MessageObject, IMessage
    {
        public static FrameMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrameMessage), isFromPool) as FrameMessage;
        }

        [MemoryPackOrder(0)]
        public int Frame { get; set; }
        [MemoryPackOrder(1)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(2)]
        public LSInput Input { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Frame = default;
            this.PlayerId = default;
            this.Input = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:52)
    [MemoryPackable]
    [Message(Message_ET_Client.OneFrameInputs)]
    public partial class OneFrameInputs: MessageObject, IMessage
    {
        public static OneFrameInputs Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(OneFrameInputs), isFromPool) as OneFrameInputs;
        }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(1)]
        public Dictionary<long, LSInput> Inputs { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Inputs.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:57)
    [MemoryPackable]
    [Message(Message_ET_Client.Room2C_AdjustUpdateTime)]
    public partial class Room2C_AdjustUpdateTime: MessageObject, IMessage
    {
        public static Room2C_AdjustUpdateTime Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Room2C_AdjustUpdateTime), isFromPool) as Room2C_AdjustUpdateTime;
        }

        [MemoryPackOrder(0)]
        public int DiffTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.DiffTime = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:62)
    [MemoryPackable]
    [Message(Message_ET_Client.C2Room_CheckHash)]
    public partial class C2Room_CheckHash: MessageObject, IRoomMessage
    {
        public static C2Room_CheckHash Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2Room_CheckHash), isFromPool) as C2Room_CheckHash;
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(1)]
        public int Frame { get; set; }
        [MemoryPackOrder(2)]
        public long Hash { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.PlayerId = default;
            this.Frame = default;
            this.Hash = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:69)
    [MemoryPackable]
    [Message(Message_ET_Client.Room2C_CheckHashFail)]
    public partial class Room2C_CheckHashFail: MessageObject, IMessage
    {
        public static Room2C_CheckHashFail Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Room2C_CheckHashFail), isFromPool) as Room2C_CheckHashFail;
        }

        [MemoryPackOrder(0)]
        public int Frame { get; set; }
        [MemoryPackOrder(1)]
        public byte[] LSWorldBytes { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Frame = default;
            this.LSWorldBytes = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/LockStepOuter.proto (line:75)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_Reconnect)]
    public partial class G2C_Reconnect: MessageObject, IMessage
    {
        public static G2C_Reconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Reconnect), isFromPool) as G2C_Reconnect;
        }

        [MemoryPackOrder(0)]
        public long StartTime { get; set; }
        [MemoryPackOrder(1)]
        public List<LockStepUnitInfo> UnitInfos { get; set; } = new List<LockStepUnitInfo>();
        [MemoryPackOrder(2)]
        public int Frame { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.StartTime = default;
            this.UnitInfos.Clear();
            this.Frame = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:4)
    [MemoryPackable]
    [Message(Message_ET_Client.HttpGetRouterResponse)]
    public partial class HttpGetRouterResponse: MessageObject
    {
        public static HttpGetRouterResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(HttpGetRouterResponse), isFromPool) as HttpGetRouterResponse;
        }

        [MemoryPackOrder(0)]
        public List<string> Realms { get; set; } = new List<string>();
        [MemoryPackOrder(1)]
        public List<string> Routers { get; set; } = new List<string>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Realms.Clear();
            this.Routers.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:10)
    [MemoryPackable]
    [Message(Message_ET_Client.RouterSync)]
    public partial class RouterSync: MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RouterSync), isFromPool) as RouterSync;
        }

        [MemoryPackOrder(0)]
        public uint ConnectId { get; set; }
        [MemoryPackOrder(1)]
        public string Address { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.ConnectId = default;
            this.Address = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:17)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_TestRequest)]
    [ResponseType(nameof(M2C_TestResponse))]
    public partial class C2M_TestRequest: MessageObject, ILocationRequest
    {
        public static C2M_TestRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRequest), isFromPool) as C2M_TestRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string request { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.request = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:23)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_TestResponse)]
    public partial class M2C_TestResponse: MessageObject, IResponse
    {
        public static M2C_TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestResponse), isFromPool) as M2C_TestResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public string response { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.response = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:32)
    [MemoryPackable]
    [Message(Message_ET_Client.C2G_EnterMap)]
    [ResponseType(nameof(G2C_EnterMap))]
    public partial class C2G_EnterMap: MessageObject, ISessionRequest
    {
        public static C2G_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_EnterMap), isFromPool) as C2G_EnterMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:37)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_EnterMap)]
    public partial class G2C_EnterMap: MessageObject, ISessionResponse
    {
        public static G2C_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_EnterMap), isFromPool) as G2C_EnterMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        /// <summary>
        /// 自己的UnitId
        /// </summary>
        [MemoryPackOrder(3)]
        public long MyId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MyId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:46)
    [MemoryPackable]
    [Message(Message_ET_Client.MoveInfo)]
    public partial class MoveInfo: MessageObject
    {
        public static MoveInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(MoveInfo), isFromPool) as MoveInfo;
        }

        [MemoryPackOrder(0)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new List<Unity.Mathematics.float3>();
        [MemoryPackOrder(1)]
        public Unity.Mathematics.quaternion Rotation { get; set; }
        [MemoryPackOrder(2)]
        public int TurnSpeed { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Points.Clear();
            this.Rotation = default;
            this.TurnSpeed = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:53)
    [MemoryPackable]
    [Message(Message_ET_Client.UnitInfo)]
    public partial class UnitInfo: MessageObject
    {
        public static UnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(UnitInfo), isFromPool) as UnitInfo;
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 Position { get; set; }
        [MemoryPackOrder(4)]
        public Unity.Mathematics.float3 Forward { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(5)]
        public Dictionary<int, long> KV { get; set; } = new();
        [MemoryPackOrder(6)]
        public MoveInfo MoveInfo { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.UnitId = default;
            this.ConfigId = default;
            this.Type = default;
            this.Position = default;
            this.Forward = default;
            this.KV.Clear();
            this.MoveInfo = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:64)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_CreateUnits)]
    public partial class M2C_CreateUnits: MessageObject, IMessage
    {
        public static M2C_CreateUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateUnits), isFromPool) as M2C_CreateUnits;
        }

        [MemoryPackOrder(0)]
        public List<UnitInfo> Units { get; set; } = new List<UnitInfo>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Units.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:69)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_CreateMyUnit)]
    public partial class M2C_CreateMyUnit: MessageObject, IMessage
    {
        public static M2C_CreateMyUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateMyUnit), isFromPool) as M2C_CreateMyUnit;
        }

        [MemoryPackOrder(0)]
        public UnitInfo Unit { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Unit = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:74)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_StartSceneChange)]
    public partial class M2C_StartSceneChange: MessageObject, IMessage
    {
        public static M2C_StartSceneChange Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_StartSceneChange), isFromPool) as M2C_StartSceneChange;
        }

        [MemoryPackOrder(0)]
        public long SceneInstanceId { get; set; }
        [MemoryPackOrder(1)]
        public string SceneName { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.SceneInstanceId = default;
            this.SceneName = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:80)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_RemoveUnits)]
    public partial class M2C_RemoveUnits: MessageObject, IMessage
    {
        public static M2C_RemoveUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_RemoveUnits), isFromPool) as M2C_RemoveUnits;
        }

        [MemoryPackOrder(0)]
        public List<long> Units { get; set; } = new List<long>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Units.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:85)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_PathfindingResult)]
    public partial class C2M_PathfindingResult: MessageObject, ILocationMessage
    {
        public static C2M_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_PathfindingResult), isFromPool) as C2M_PathfindingResult;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Position = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:91)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_Stop)]
    public partial class C2M_Stop: MessageObject, ILocationMessage
    {
        public static C2M_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_Stop), isFromPool) as C2M_Stop;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:96)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_PathfindingResult)]
    public partial class M2C_PathfindingResult: MessageObject, IMessage
    {
        public static M2C_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_PathfindingResult), isFromPool) as M2C_PathfindingResult;
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }
        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }
        [MemoryPackOrder(2)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new List<Unity.Mathematics.float3>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Id = default;
            this.Position = default;
            this.Points.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:103)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_Stop)]
    public partial class M2C_Stop: MessageObject, IMessage
    {
        public static M2C_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Stop), isFromPool) as M2C_Stop;
        }

        [MemoryPackOrder(0)]
        public int Error { get; set; }
        [MemoryPackOrder(1)]
        public long Id { get; set; }
        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 Position { get; set; }
        [MemoryPackOrder(3)]
        public Unity.Mathematics.quaternion Rotation { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Error = default;
            this.Id = default;
            this.Position = default;
            this.Rotation = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:112)
    [MemoryPackable]
    [Message(Message_ET_Client.C2G_Ping)]
    [ResponseType(nameof(G2C_Ping))]
    public partial class C2G_Ping: MessageObject, ISessionRequest
    {
        public static C2G_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Ping), isFromPool) as C2G_Ping;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:117)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_Ping)]
    public partial class G2C_Ping: MessageObject, ISessionResponse
    {
        public static G2C_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Ping), isFromPool) as G2C_Ping;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long Time { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Time = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:126)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_Test)]
    public partial class G2C_Test: MessageObject, ISessionMessage
    {
        public static G2C_Test Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Test), isFromPool) as G2C_Test;
        }

        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:131)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload: MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_Reload), isFromPool) as C2M_Reload;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public string Password { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Account = default;
            this.Password = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:138)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_Reload)]
    public partial class M2C_Reload: MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Reload), isFromPool) as M2C_Reload;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:146)
    [MemoryPackable]
    [Message(Message_ET_Client.C2R_Login)]
    [ResponseType(nameof(R2C_Login))]
    public partial class C2R_Login: MessageObject, ISessionRequest
    {
        public static C2R_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_Login), isFromPool) as C2R_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        /// <summary>
        /// 帐号
        /// </summary>
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [MemoryPackOrder(2)]
        public string Password { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Account = default;
            this.Password = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:153)
    [MemoryPackable]
    [Message(Message_ET_Client.R2C_Login)]
    public partial class R2C_Login: MessageObject, ISessionResponse
    {
        public static R2C_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_Login), isFromPool) as R2C_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public string Address { get; set; }
        [MemoryPackOrder(4)]
        public long Key { get; set; }
        [MemoryPackOrder(5)]
        public long GateId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Address = default;
            this.Key = default;
            this.GateId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:164)
    [MemoryPackable]
    [Message(Message_ET_Client.C2G_LoginGate)]
    [ResponseType(nameof(G2C_LoginGate))]
    public partial class C2G_LoginGate: MessageObject, ISessionRequest
    {
        public static C2G_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_LoginGate), isFromPool) as C2G_LoginGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        /// <summary>
        /// 帐号
        /// </summary>
        [MemoryPackOrder(1)]
        public long Key { get; set; }
        [MemoryPackOrder(2)]
        public long GateId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Key = default;
            this.GateId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:171)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_LoginGate)]
    public partial class G2C_LoginGate: MessageObject, ISessionResponse
    {
        public static G2C_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_LoginGate), isFromPool) as G2C_LoginGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long PlayerId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:179)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage: MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_TestHotfixMessage), isFromPool) as G2C_TestHotfixMessage;
        }

        [MemoryPackOrder(0)]
        public string Info { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.Info = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:185)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase: MessageObject, ILocationRequest
    {
        public static C2M_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRobotCase), isFromPool) as C2M_TestRobotCase;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int N { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.N = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:191)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_TestRobotCase)]
    public partial class M2C_TestRobotCase: MessageObject, ILocationResponse
    {
        public static M2C_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestRobotCase), isFromPool) as M2C_TestRobotCase;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public int N { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.N = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:199)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_TestRobotCase2)]
    public partial class C2M_TestRobotCase2: MessageObject, ILocationMessage
    {
        public static C2M_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRobotCase2), isFromPool) as C2M_TestRobotCase2;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int N { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.N = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:205)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_TestRobotCase2)]
    public partial class M2C_TestRobotCase2: MessageObject, ILocationMessage
    {
        public static M2C_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestRobotCase2), isFromPool) as M2C_TestRobotCase2;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int N { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.N = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:212)
    [MemoryPackable]
    [Message(Message_ET_Client.C2M_TransferMap)]
    [ResponseType(nameof(M2C_TransferMap))]
    public partial class C2M_TransferMap: MessageObject, ILocationRequest
    {
        public static C2M_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TransferMap), isFromPool) as C2M_TransferMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:217)
    [MemoryPackable]
    [Message(Message_ET_Client.M2C_TransferMap)]
    public partial class M2C_TransferMap: MessageObject, ILocationResponse
    {
        public static M2C_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TransferMap), isFromPool) as M2C_TransferMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:225)
    [MemoryPackable]
    [Message(Message_ET_Client.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark: MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Benchmark), isFromPool) as C2G_Benchmark;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/OuterMessage.proto (line:230)
    [MemoryPackable]
    [Message(Message_ET_Client.G2C_Benchmark)]
    public partial class G2C_Benchmark: MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Benchmark), isFromPool) as G2C_Benchmark;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 本机产品会话原型 v1。追加文件以保持既有 Opcode；内部消息不允许从 wire 接入。
    // proto file : ET-Client/ZProductSession.proto (line:5)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductRosterMember)]
    public partial class ProductRosterMember: MessageObject
    {
        public static ProductRosterMember Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductRosterMember), isFromPool) as ProductRosterMember;
        }

        [MemoryPackOrder(0)]
        public int MemberId { get; set; }
        [MemoryPackOrder(1)]
        public bool Online { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MemberId = default;
            this.Online = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:12)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductJoinRequest)]
    [ResponseType(nameof(ProductJoinResponse))]
    public partial class ProductJoinRequest: MessageObject, IRequest
    {
        public static ProductJoinRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductJoinRequest), isFromPool) as ProductJoinRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:18)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductJoinResponse)]
    public partial class ProductJoinResponse: MessageObject, IResponse
    {
        public static ProductJoinResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductJoinResponse), isFromPool) as ProductJoinResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public string MatchId { get; set; }
        [MemoryPackOrder(4)]
        public int MemberId { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long RosterVersion { get; set; }
        [MemoryPackOrder(7)]
        public List<ProductRosterMember> Members { get; set; } = new List<ProductRosterMember>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MatchId = default;
            this.MemberId = default;
            this.BindingGeneration = default;
            this.RosterVersion = default;
            this.Members.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:31)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductProbeRequest)]
    [ResponseType(nameof(ProductProbeResponse))]
    public partial class ProductProbeRequest: MessageObject, IRequest
    {
        public static ProductProbeRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductProbeRequest), isFromPool) as ProductProbeRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string MatchId { get; set; }
        [MemoryPackOrder(2)]
        public int Sequence { get; set; }
        [MemoryPackOrder(3)]
        public string Payload { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.MatchId = default;
            this.Sequence = default;
            this.Payload = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:39)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductProbeResponse)]
    public partial class ProductProbeResponse: MessageObject, IResponse
    {
        public static ProductProbeResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductProbeResponse), isFromPool) as ProductProbeResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public int MemberId { get; set; }
        [MemoryPackOrder(4)]
        public int Sequence { get; set; }
        [MemoryPackOrder(5)]
        public string Payload { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MemberId = default;
            this.Sequence = default;
            this.Payload = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:50)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductLeaveRequest)]
    [ResponseType(nameof(ProductLeaveResponse))]
    public partial class ProductLeaveRequest: MessageObject, IRequest
    {
        public static ProductLeaveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductLeaveRequest), isFromPool) as ProductLeaveRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string MatchId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.MatchId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:56)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductLeaveResponse)]
    public partial class ProductLeaveResponse: MessageObject, IResponse
    {
        public static ProductLeaveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductLeaveResponse), isFromPool) as ProductLeaveResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public bool Committed { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Committed = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:64)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductRosterSnapshot)]
    public partial class ProductRosterSnapshot: MessageObject, IMessage
    {
        public static ProductRosterSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductRosterSnapshot), isFromPool) as ProductRosterSnapshot;
        }

        [MemoryPackOrder(0)]
        public string MatchId { get; set; }
        [MemoryPackOrder(1)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(2)]
        public long RosterVersion { get; set; }
        [MemoryPackOrder(3)]
        public List<ProductRosterMember> Members { get; set; } = new List<ProductRosterMember>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MatchId = default;
            this.BindingGeneration = default;
            this.RosterVersion = default;
            this.Members.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:73)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductHeartbeatRequest)]
    [ResponseType(nameof(ProductHeartbeatResponse))]
    public partial class ProductHeartbeatRequest: MessageObject, IRequest
    {
        public static ProductHeartbeatRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductHeartbeatRequest), isFromPool) as ProductHeartbeatRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:80)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductHeartbeatResponse)]
    public partial class ProductHeartbeatResponse: MessageObject, IResponse
    {
        public static ProductHeartbeatResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductHeartbeatResponse), isFromPool) as ProductHeartbeatResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:87)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductMatchClosed)]
    public partial class ProductMatchClosed: MessageObject, IMessage
    {
        public static ProductMatchClosed Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductMatchClosed), isFromPool) as ProductMatchClosed;
        }

        [MemoryPackOrder(0)]
        public string MatchId { get; set; }
        [MemoryPackOrder(1)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(2)]
        public string Reason { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MatchId = default;
            this.BindingGeneration = default;
            this.Reason = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:95)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductControlRequest)]
    [ResponseType(nameof(ProductControlResponse))]
    public partial class ProductControlRequest: MessageObject, IRequest
    {
        public static ProductControlRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductControlRequest), isFromPool) as ProductControlRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Action { get; set; }
        [MemoryPackOrder(2)]
        public long PeerId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(4)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(5)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(6)]
        public string MatchId { get; set; }
        [MemoryPackOrder(7)]
        public int Sequence { get; set; }
        [MemoryPackOrder(8)]
        public string Payload { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Action = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.BindingGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.Sequence = default;
            this.Payload = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZProductSession.proto (line:108)
    [MemoryPackable]
    [Message(Message_ET_Client.ProductControlResponse)]
    public partial class ProductControlResponse: MessageObject, IResponse
    {
        public static ProductControlResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductControlResponse), isFromPool) as ProductControlResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public string MatchId { get; set; }
        [MemoryPackOrder(4)]
        public int MemberId { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long RosterVersion { get; set; }
        [MemoryPackOrder(7)]
        public List<ProductRosterMember> Members { get; set; } = new List<ProductRosterMember>();
        [MemoryPackOrder(8)]
        public int Sequence { get; set; }
        [MemoryPackOrder(9)]
        public string Payload { get; set; }
        [MemoryPackOrder(10)]
        public bool Committed { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MatchId = default;
            this.MemberId = default;
            this.BindingGeneration = default;
            this.RosterVersion = default;
            this.Members.Clear();
            this.Sequence = default;
            this.Payload = default;
            this.Committed = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:5)
    [MemoryPackable]
    [Message(Message_ET_Client.GridCommandRequest)]
    [ResponseType(nameof(GridCommandResponse))]
    public partial class GridCommandRequest: MessageObject, IRequest
    {
        public static GridCommandRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridCommandRequest), isFromPool) as GridCommandRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long CommandId { get; set; }
        [MemoryPackOrder(7)]
        public int Action { get; set; }
        [MemoryPackOrder(8)]
        public int X { get; set; }
        [MemoryPackOrder(9)]
        public int Y { get; set; }
        [MemoryPackOrder(10)]
        public long TargetId { get; set; }
        [MemoryPackOrder(11)]
        public long ExpectedItemId { get; set; }
        [MemoryPackOrder(12)]
        public long ExpectedTargetItemId { get; set; }
        [MemoryPackOrder(13)]
        public long ExpectedProcessId { get; set; }
        [MemoryPackOrder(14)]
        public long LevelId { get; set; }
        [MemoryPackOrder(15)]
        public long LevelGeneration { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.CommandId = default;
            this.Action = default;
            this.X = default;
            this.Y = default;
            this.TargetId = default;
            this.ExpectedItemId = default;
            this.ExpectedTargetItemId = default;
            this.ExpectedProcessId = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:25)
    [MemoryPackable]
    [Message(Message_ET_Client.GridCommandResponse)]
    public partial class GridCommandResponse: MessageObject, IResponse
    {
        public static GridCommandResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridCommandResponse), isFromPool) as GridCommandResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 内部可信转交，不进入 wire 白名单。
    // proto file : ET-Client/ZZGridKitchen.proto (line:36)
    [MemoryPackable]
    [Message(Message_ET_Client.GridSubmitRequest)]
    [ResponseType(nameof(GridSubmitResponse))]
    public partial class GridSubmitRequest: MessageObject, IRequest
    {
        public static GridSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridSubmitRequest), isFromPool) as GridSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long CommandId { get; set; }
        [MemoryPackOrder(10)]
        public int Action { get; set; }
        [MemoryPackOrder(11)]
        public int X { get; set; }
        [MemoryPackOrder(12)]
        public int Y { get; set; }
        [MemoryPackOrder(13)]
        public long TargetId { get; set; }
        [MemoryPackOrder(14)]
        public long ExpectedItemId { get; set; }
        [MemoryPackOrder(15)]
        public long ExpectedTargetItemId { get; set; }
        [MemoryPackOrder(16)]
        public long ExpectedProcessId { get; set; }
        [MemoryPackOrder(17)]
        public long LevelId { get; set; }
        [MemoryPackOrder(18)]
        public long LevelGeneration { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.CommandId = default;
            this.Action = default;
            this.X = default;
            this.Y = default;
            this.TargetId = default;
            this.ExpectedItemId = default;
            this.ExpectedTargetItemId = default;
            this.ExpectedProcessId = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:59)
    [MemoryPackable]
    [Message(Message_ET_Client.GridSubmitResponse)]
    public partial class GridSubmitResponse: MessageObject, IResponse
    {
        public static GridSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridSubmitResponse), isFromPool) as GridSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:68)
    [MemoryPackable]
    [Message(Message_ET_Client.GridPathCell)]
    public partial class GridPathCell: MessageObject
    {
        public static GridPathCell Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridPathCell), isFromPool) as GridPathCell;
        }

        [MemoryPackOrder(0)]
        public int X { get; set; }
        [MemoryPackOrder(1)]
        public int Y { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.X = default;
            this.Y = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:74)
    [MemoryPackable]
    [Message(Message_ET_Client.GridActorState)]
    public partial class GridActorState: MessageObject
    {
        public static GridActorState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridActorState), isFromPool) as GridActorState;
        }

        [MemoryPackOrder(0)]
        public int MemberId { get; set; }
        [MemoryPackOrder(1)]
        public long Generation { get; set; }
        [MemoryPackOrder(2)]
        public int X { get; set; }
        [MemoryPackOrder(3)]
        public int Y { get; set; }
        [MemoryPackOrder(4)]
        public int NextX { get; set; }
        [MemoryPackOrder(5)]
        public int NextY { get; set; }
        [MemoryPackOrder(6)]
        public long StepStartTick { get; set; }
        [MemoryPackOrder(7)]
        public int Motion { get; set; }
        [MemoryPackOrder(8)]
        public int Facing { get; set; }
        [MemoryPackOrder(9)]
        public long HandItemId { get; set; }
        [MemoryPackOrder(10)]
        public int GoalX { get; set; }
        [MemoryPackOrder(11)]
        public int GoalY { get; set; }
        [MemoryPackOrder(12)]
        public List<GridPathCell> Path { get; set; } = new List<GridPathCell>();
        [MemoryPackOrder(13)]
        public long LastCommandId { get; set; }
        [MemoryPackOrder(14)]
        public int LastStatus { get; set; }
        [MemoryPackOrder(15)]
        public string LastReason { get; set; }
        [MemoryPackOrder(16)]
        public long WorkId { get; set; }
        [MemoryPackOrder(17)]
        public long FrontWorkId { get; set; }
        [MemoryPackOrder(18)]
        public long FrontClaimSerial { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MemberId = default;
            this.Generation = default;
            this.X = default;
            this.Y = default;
            this.NextX = default;
            this.NextY = default;
            this.StepStartTick = default;
            this.Motion = default;
            this.Facing = default;
            this.HandItemId = default;
            this.GoalX = default;
            this.GoalY = default;
            this.Path.Clear();
            this.LastCommandId = default;
            this.LastStatus = default;
            this.LastReason = default;
            this.WorkId = default;
            this.FrontWorkId = default;
            this.FrontClaimSerial = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:97)
    [MemoryPackable]
    [Message(Message_ET_Client.GridStationState)]
    public partial class GridStationState: MessageObject
    {
        public static GridStationState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridStationState), isFromPool) as GridStationState;
        }

        [MemoryPackOrder(0)]
        public long StationId { get; set; }
        [MemoryPackOrder(1)]
        public int X { get; set; }
        [MemoryPackOrder(2)]
        public int Y { get; set; }
        [MemoryPackOrder(3)]
        public string Name { get; set; }
        [MemoryPackOrder(4)]
        public long ItemId { get; set; }
        [MemoryPackOrder(5)]
        public int Kind { get; set; }
        [MemoryPackOrder(6)]
        public long AutoProcessId { get; set; }
        [MemoryPackOrder(7)]
        public int DefinitionId { get; set; }
        [MemoryPackOrder(8)]
        public int Orientation { get; set; }
        [MemoryPackOrder(9)]
        public int Capabilities { get; set; }
        [MemoryPackOrder(10)]
        public int ItemSlotCapacity { get; set; }
        [MemoryPackOrder(11)]
        public List<GridPathCell> Footprint { get; set; } = new List<GridPathCell>();
        [MemoryPackOrder(12)]
        public List<GridPathCell> Approaches { get; set; } = new List<GridPathCell>();
        [MemoryPackOrder(13)]
        public long BakeProcessId { get; set; }
        [MemoryPackOrder(14)]
        public List<long> PoolItemIds { get; set; } = new List<long>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.StationId = default;
            this.X = default;
            this.Y = default;
            this.Name = default;
            this.ItemId = default;
            this.Kind = default;
            this.AutoProcessId = default;
            this.DefinitionId = default;
            this.Orientation = default;
            this.Capabilities = default;
            this.ItemSlotCapacity = default;
            this.Footprint.Clear();
            this.Approaches.Clear();
            this.BakeProcessId = default;
            this.PoolItemIds.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:116)
    [MemoryPackable]
    [Message(Message_ET_Client.GridItemState)]
    public partial class GridItemState: MessageObject
    {
        public static GridItemState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridItemState), isFromPool) as GridItemState;
        }

        [MemoryPackOrder(0)]
        public long ItemId { get; set; }
        [MemoryPackOrder(1)]
        public string Name { get; set; }
        [MemoryPackOrder(2)]
        public int Location { get; set; }
        [MemoryPackOrder(3)]
        public long HolderId { get; set; }
        [MemoryPackOrder(4)]
        public int X { get; set; }
        [MemoryPackOrder(5)]
        public int Y { get; set; }
        [MemoryPackOrder(6)]
        public List<long> Contents { get; set; } = new List<long>();
        [MemoryPackOrder(7)]
        public int DefinitionId { get; set; }
        [MemoryPackOrder(8)]
        public long AutoProcessId { get; set; }
        [MemoryPackOrder(9)]
        public long BakeProcessId { get; set; }
        [MemoryPackOrder(10)]
        public int Hygiene { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.ItemId = default;
            this.Name = default;
            this.Location = default;
            this.HolderId = default;
            this.X = default;
            this.Y = default;
            this.Contents.Clear();
            this.DefinitionId = default;
            this.AutoProcessId = default;
            this.BakeProcessId = default;
            this.Hygiene = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:131)
    [MemoryPackable]
    [Message(Message_ET_Client.GridWorldSnapshot)]
    public partial class GridWorldSnapshot: MessageObject, IMessage
    {
        public static GridWorldSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridWorldSnapshot), isFromPool) as GridWorldSnapshot;
        }

        [MemoryPackOrder(0)]
        public string MatchId { get; set; }
        [MemoryPackOrder(1)]
        public string WorldId { get; set; }
        [MemoryPackOrder(2)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(3)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int MemberId { get; set; }
        [MemoryPackOrder(5)]
        public long Version { get; set; }
        [MemoryPackOrder(6)]
        public long Tick { get; set; }
        [MemoryPackOrder(7)]
        public int Width { get; set; }
        [MemoryPackOrder(8)]
        public int Height { get; set; }
        [MemoryPackOrder(9)]
        public int KitchenBottom { get; set; }
        [MemoryPackOrder(10)]
        public int TickMilliseconds { get; set; }
        [MemoryPackOrder(11)]
        public int StepTicks { get; set; }
        [MemoryPackOrder(12)]
        public List<GridActorState> Actors { get; set; } = new List<GridActorState>();
        [MemoryPackOrder(13)]
        public List<GridStationState> Stations { get; set; } = new List<GridStationState>();
        [MemoryPackOrder(14)]
        public List<GridItemState> Items { get; set; } = new List<GridItemState>();
        [MemoryPackOrder(15)]
        public List<GridManualProcessState> Processes { get; set; } = new List<GridManualProcessState>();
        [MemoryPackOrder(16)]
        public long LevelId { get; set; }
        [MemoryPackOrder(17)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(18)]
        public List<GridAutoProcessState> AutoProcesses { get; set; } = new List<GridAutoProcessState>();
        [MemoryPackOrder(19)]
        public int LevelPhase { get; set; }
        [MemoryPackOrder(20)]
        public bool LevelPaused { get; set; }
        [MemoryPackOrder(21)]
        public int LevelOperatingTicks { get; set; }
        [MemoryPackOrder(22)]
        public int LevelElapsedTicks { get; set; }
        [MemoryPackOrder(23)]
        public int LevelScore { get; set; }
        [MemoryPackOrder(24)]
        public int LevelStars { get; set; }
        [MemoryPackOrder(25)]
        public int LevelConfigId { get; set; }
        [MemoryPackOrder(26)]
        public int NextLevelConfigId { get; set; }
        [MemoryPackOrder(27)]
        public long LastCommandId { get; set; }
        [MemoryPackOrder(28)]
        public int LastCommandKind { get; set; }
        [MemoryPackOrder(29)]
        public int LastCommandStatus { get; set; }
        [MemoryPackOrder(30)]
        public string LastCommandReason { get; set; }
        [MemoryPackOrder(31)]
        public long LayoutVersion { get; set; }
        [MemoryPackOrder(32)]
        public List<GridBakeProcessState> BakeProcesses { get; set; } = new List<GridBakeProcessState>();
        [MemoryPackOrder(33)]
        public List<int> AllowedDishIds { get; set; } = new List<int>();
        [MemoryPackOrder(34)]
        public List<GridCompanionState> Companions { get; set; } = new List<GridCompanionState>();
        [MemoryPackOrder(35)]
        public List<GridFrontWorkState> FrontWorks { get; set; } = new List<GridFrontWorkState>();
        [MemoryPackOrder(36)]
        public List<GridCustomerState> Customers { get; set; } = new List<GridCustomerState>();
        [MemoryPackOrder(37)]
        public List<GridOrderState> Orders { get; set; } = new List<GridOrderState>();
        [MemoryPackOrder(38)]
        public List<GridTableState> Tables { get; set; } = new List<GridTableState>();
        [MemoryPackOrder(39)]
        public List<long> WaitingCustomers { get; set; } = new List<long>();
        [MemoryPackOrder(40)]
        public int SkippedArrivals { get; set; }
        [MemoryPackOrder(41)]
        public int BuffSeed { get; set; }
        [MemoryPackOrder(42)]
        public long BuffDrawVersion { get; set; }
        [MemoryPackOrder(43)]
        public List<int> BuffCandidates { get; set; } = new List<int>();
        [MemoryPackOrder(44)]
        public int SelectedBuffId { get; set; }
        [MemoryPackOrder(45)]
        public bool RequireBuffChoice { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.MemberId = default;
            this.Version = default;
            this.Tick = default;
            this.Width = default;
            this.Height = default;
            this.KitchenBottom = default;
            this.TickMilliseconds = default;
            this.StepTicks = default;
            this.Actors.Clear();
            this.Stations.Clear();
            this.Items.Clear();
            this.Processes.Clear();
            this.LevelId = default;
            this.LevelGeneration = default;
            this.AutoProcesses.Clear();
            this.LevelPhase = default;
            this.LevelPaused = default;
            this.LevelOperatingTicks = default;
            this.LevelElapsedTicks = default;
            this.LevelScore = default;
            this.LevelStars = default;
            this.LevelConfigId = default;
            this.NextLevelConfigId = default;
            this.LastCommandId = default;
            this.LastCommandKind = default;
            this.LastCommandStatus = default;
            this.LastCommandReason = default;
            this.LayoutVersion = default;
            this.BakeProcesses.Clear();
            this.AllowedDishIds.Clear();
            this.Companions.Clear();
            this.FrontWorks.Clear();
            this.Customers.Clear();
            this.Orders.Clear();
            this.Tables.Clear();
            this.WaitingCustomers.Clear();
            this.SkippedArrivals = default;
            this.BuffSeed = default;
            this.BuffDrawVersion = default;
            this.BuffCandidates.Clear();
            this.SelectedBuffId = default;
            this.RequireBuffChoice = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:181)
    [MemoryPackable]
    [Message(Message_ET_Client.GridCommandResult)]
    public partial class GridCommandResult: MessageObject, IMessage
    {
        public static GridCommandResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridCommandResult), isFromPool) as GridCommandResult;
        }

        [MemoryPackOrder(0)]
        public string MatchId { get; set; }
        [MemoryPackOrder(1)]
        public string WorldId { get; set; }
        [MemoryPackOrder(2)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(3)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(4)]
        public long CommandId { get; set; }
        [MemoryPackOrder(5)]
        public int Status { get; set; }
        [MemoryPackOrder(6)]
        public string Reason { get; set; }
        [MemoryPackOrder(7)]
        public long Version { get; set; }
        [MemoryPackOrder(8)]
        public long LevelId { get; set; }
        [MemoryPackOrder(9)]
        public long LevelGeneration { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.CommandId = default;
            this.Status = default;
            this.Reason = default;
            this.Version = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:195)
    [MemoryPackable]
    [Message(Message_ET_Client.GridManualProcessState)]
    public partial class GridManualProcessState: MessageObject
    {
        public static GridManualProcessState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridManualProcessState), isFromPool) as GridManualProcessState;
        }

        [MemoryPackOrder(0)]
        public long ProcessId { get; set; }
        [MemoryPackOrder(1)]
        public int RecipeId { get; set; }
        [MemoryPackOrder(2)]
        public long InputItemId { get; set; }
        [MemoryPackOrder(3)]
        public long TargetItemId { get; set; }
        [MemoryPackOrder(4)]
        public long StationId { get; set; }
        [MemoryPackOrder(5)]
        public int RequiredTicks { get; set; }
        [MemoryPackOrder(6)]
        public int ProgressTicks { get; set; }
        [MemoryPackOrder(7)]
        public int State { get; set; }
        [MemoryPackOrder(8)]
        public int WorkerMemberId { get; set; }
        [MemoryPackOrder(9)]
        public long WorkerGeneration { get; set; }
        [MemoryPackOrder(10)]
        public long OutputItemId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.ProcessId = default;
            this.RecipeId = default;
            this.InputItemId = default;
            this.TargetItemId = default;
            this.StationId = default;
            this.RequiredTicks = default;
            this.ProgressTicks = default;
            this.State = default;
            this.WorkerMemberId = default;
            this.WorkerGeneration = default;
            this.OutputItemId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZGridKitchen.proto (line:210)
    [MemoryPackable]
    [Message(Message_ET_Client.GridAutoProcessState)]
    public partial class GridAutoProcessState: MessageObject
    {
        public static GridAutoProcessState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridAutoProcessState), isFromPool) as GridAutoProcessState;
        }

        [MemoryPackOrder(0)]
        public long ProcessId { get; set; }
        [MemoryPackOrder(1)]
        public int RecipeId { get; set; }
        [MemoryPackOrder(2)]
        public long PotItemId { get; set; }
        [MemoryPackOrder(3)]
        public long StartStationId { get; set; }
        [MemoryPackOrder(4)]
        public long StartedTick { get; set; }
        [MemoryPackOrder(5)]
        public int RequiredTicks { get; set; }
        [MemoryPackOrder(6)]
        public int ProgressTicks { get; set; }
        [MemoryPackOrder(7)]
        public int OutputDefinitionId { get; set; }
        [MemoryPackOrder(8)]
        public int TotalPortions { get; set; }
        [MemoryPackOrder(9)]
        public int RemainingPortions { get; set; }
        [MemoryPackOrder(10)]
        public int ServingContainerDefinitionId { get; set; }
        [MemoryPackOrder(11)]
        public int State { get; set; }
        [MemoryPackOrder(12)]
        public List<long> InputItemIds { get; set; } = new List<long>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.ProcessId = default;
            this.RecipeId = default;
            this.PotItemId = default;
            this.StartStationId = default;
            this.StartedTick = default;
            this.RequiredTicks = default;
            this.ProgressTicks = default;
            this.OutputDefinitionId = default;
            this.TotalPortions = default;
            this.RemainingPortions = default;
            this.ServingContainerDefinitionId = default;
            this.State = default;
            this.InputItemIds.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 单机类型化控制，入口与内部提交都不开放 wire。
    // proto file : ET-Client/ZZRestaurantLevel.proto (line:6)
    [MemoryPackable]
    [Message(Message_ET_Client.LevelCommandRequest)]
    [ResponseType(nameof(LevelCommandResponse))]
    public partial class LevelCommandRequest: MessageObject, IRequest
    {
        public static LevelCommandRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LevelCommandRequest), isFromPool) as LevelCommandRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long CommandId { get; set; }
        [MemoryPackOrder(7)]
        public long LevelId { get; set; }
        [MemoryPackOrder(8)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(9)]
        public int Action { get; set; }
        [MemoryPackOrder(10)]
        public int NextLevelConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.CommandId = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.Action = default;
            this.NextLevelConfigId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZRestaurantLevel.proto (line:21)
    [MemoryPackable]
    [Message(Message_ET_Client.LevelCommandResponse)]
    public partial class LevelCommandResponse: MessageObject, IResponse
    {
        public static LevelCommandResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LevelCommandResponse), isFromPool) as LevelCommandResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZRestaurantLevel.proto (line:31)
    [MemoryPackable]
    [Message(Message_ET_Client.LevelSubmitRequest)]
    [ResponseType(nameof(LevelSubmitResponse))]
    public partial class LevelSubmitRequest: MessageObject, IRequest
    {
        public static LevelSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LevelSubmitRequest), isFromPool) as LevelSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long CommandId { get; set; }
        [MemoryPackOrder(10)]
        public long LevelId { get; set; }
        [MemoryPackOrder(11)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(12)]
        public int Action { get; set; }
        [MemoryPackOrder(13)]
        public int NextLevelConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.CommandId = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.Action = default;
            this.NextLevelConfigId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZRestaurantLevel.proto (line:49)
    [MemoryPackable]
    [Message(Message_ET_Client.LevelSubmitResponse)]
    public partial class LevelSubmitResponse: MessageObject, IResponse
    {
        public static LevelSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LevelSubmitResponse), isFromPool) as LevelSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 仅本地；不进入产品 wire 白名单。
    // proto file : ET-Client/ZZStationLayout.proto (line:6)
    [MemoryPackable]
    [Message(Message_ET_Client.StationLayoutRequest)]
    [ResponseType(nameof(StationLayoutResponse))]
    public partial class StationLayoutRequest: MessageObject, IRequest
    {
        public static StationLayoutRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(StationLayoutRequest), isFromPool) as StationLayoutRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long LevelId { get; set; }
        [MemoryPackOrder(7)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long CommandId { get; set; }
        [MemoryPackOrder(9)]
        public long StationId { get; set; }
        [MemoryPackOrder(10)]
        public long ExpectedLayoutVersion { get; set; }
        [MemoryPackOrder(11)]
        public int X { get; set; }
        [MemoryPackOrder(12)]
        public int Y { get; set; }
        [MemoryPackOrder(13)]
        public int Orientation { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.StationId = default;
            this.ExpectedLayoutVersion = default;
            this.X = default;
            this.Y = default;
            this.Orientation = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZStationLayout.proto (line:24)
    [MemoryPackable]
    [Message(Message_ET_Client.StationLayoutResponse)]
    public partial class StationLayoutResponse: MessageObject, IResponse
    {
        public static StationLayoutResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(StationLayoutResponse), isFromPool) as StationLayoutResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZStationLayout.proto (line:34)
    [MemoryPackable]
    [Message(Message_ET_Client.StationLayoutSubmitRequest)]
    [ResponseType(nameof(StationLayoutSubmitResponse))]
    public partial class StationLayoutSubmitRequest: MessageObject, IRequest
    {
        public static StationLayoutSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(StationLayoutSubmitRequest), isFromPool) as StationLayoutSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long LevelId { get; set; }
        [MemoryPackOrder(10)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(11)]
        public long CommandId { get; set; }
        [MemoryPackOrder(12)]
        public long StationId { get; set; }
        [MemoryPackOrder(13)]
        public long ExpectedLayoutVersion { get; set; }
        [MemoryPackOrder(14)]
        public int X { get; set; }
        [MemoryPackOrder(15)]
        public int Y { get; set; }
        [MemoryPackOrder(16)]
        public int Orientation { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.StationId = default;
            this.ExpectedLayoutVersion = default;
            this.X = default;
            this.Y = default;
            this.Orientation = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZStationLayout.proto (line:55)
    [MemoryPackable]
    [Message(Message_ET_Client.StationLayoutSubmitResponse)]
    public partial class StationLayoutSubmitResponse: MessageObject, IResponse
    {
        public static StationLayoutSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(StationLayoutSubmitResponse), isFromPool) as StationLayoutSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 排在既有协议源之后，保留所有旧 Opcode。
    // proto file : ET-Client/ZZToastBake.proto (line:5)
    [MemoryPackable]
    [Message(Message_ET_Client.GridBakeProcessState)]
    public partial class GridBakeProcessState: MessageObject
    {
        public static GridBakeProcessState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridBakeProcessState), isFromPool) as GridBakeProcessState;
        }

        [MemoryPackOrder(0)]
        public long ProcessId { get; set; }
        [MemoryPackOrder(1)]
        public int RecipeId { get; set; }
        [MemoryPackOrder(2)]
        public long StationId { get; set; }
        [MemoryPackOrder(3)]
        public long InputItemId { get; set; }
        [MemoryPackOrder(4)]
        public int OutputDefinitionId { get; set; }
        [MemoryPackOrder(5)]
        public int RequiredTicks { get; set; }
        [MemoryPackOrder(6)]
        public int ProgressTicks { get; set; }
        [MemoryPackOrder(7)]
        public long OutputItemId { get; set; }
        [MemoryPackOrder(8)]
        public int State { get; set; }
        [MemoryPackOrder(9)]
        public long StartedTick { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.ProcessId = default;
            this.RecipeId = default;
            this.StationId = default;
            this.InputItemId = default;
            this.OutputDefinitionId = default;
            this.RequiredTicks = default;
            this.ProgressTicks = default;
            this.OutputItemId = default;
            this.State = default;
            this.StartedTick = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // 仅产品本地入口；不进入 UDP/KCP wire 白名单。
    // proto file : ET-Client/ZZZFrontWork.proto (line:6)
    [MemoryPackable]
    [Message(Message_ET_Client.FrontWorkRequest)]
    [ResponseType(nameof(FrontWorkResponse))]
    public partial class FrontWorkRequest: MessageObject, IRequest
    {
        public static FrontWorkRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrontWorkRequest), isFromPool) as FrontWorkRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long LevelId { get; set; }
        [MemoryPackOrder(7)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long CommandId { get; set; }
        [MemoryPackOrder(9)]
        public int Action { get; set; }
        [MemoryPackOrder(10)]
        public long WorkId { get; set; }
        [MemoryPackOrder(11)]
        public long ClaimSerial { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.Action = default;
            this.WorkId = default;
            this.ClaimSerial = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:21)
    [MemoryPackable]
    [Message(Message_ET_Client.FrontWorkResponse)]
    public partial class FrontWorkResponse: MessageObject, IResponse
    {
        public static FrontWorkResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrontWorkResponse), isFromPool) as FrontWorkResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:30)
    [MemoryPackable]
    [Message(Message_ET_Client.FrontWorkSubmitRequest)]
    [ResponseType(nameof(FrontWorkSubmitResponse))]
    public partial class FrontWorkSubmitRequest: MessageObject, IRequest
    {
        public static FrontWorkSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrontWorkSubmitRequest), isFromPool) as FrontWorkSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long LevelId { get; set; }
        [MemoryPackOrder(10)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(11)]
        public long CommandId { get; set; }
        [MemoryPackOrder(12)]
        public int Action { get; set; }
        [MemoryPackOrder(13)]
        public long WorkId { get; set; }
        [MemoryPackOrder(14)]
        public long ClaimSerial { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.Action = default;
            this.WorkId = default;
            this.ClaimSerial = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:48)
    [MemoryPackable]
    [Message(Message_ET_Client.FrontWorkSubmitResponse)]
    public partial class FrontWorkSubmitResponse: MessageObject, IResponse
    {
        public static FrontWorkSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrontWorkSubmitResponse), isFromPool) as FrontWorkSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:57)
    [MemoryPackable]
    [Message(Message_ET_Client.CompanionWorkRequest)]
    [ResponseType(nameof(CompanionWorkResponse))]
    public partial class CompanionWorkRequest: MessageObject, IRequest
    {
        public static CompanionWorkRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CompanionWorkRequest), isFromPool) as CompanionWorkRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long LevelId { get; set; }
        [MemoryPackOrder(6)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(7)]
        public long RoleId { get; set; }
        [MemoryPackOrder(8)]
        public long RoleInstanceId { get; set; }
        [MemoryPackOrder(9)]
        public long RequestId { get; set; }
        [MemoryPackOrder(10)]
        public int Action { get; set; }
        [MemoryPackOrder(11)]
        public long WorkId { get; set; }
        [MemoryPackOrder(12)]
        public long ClaimSerial { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.RoleId = default;
            this.RoleInstanceId = default;
            this.RequestId = default;
            this.Action = default;
            this.WorkId = default;
            this.ClaimSerial = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:73)
    [MemoryPackable]
    [Message(Message_ET_Client.CompanionWorkResponse)]
    public partial class CompanionWorkResponse: MessageObject, IResponse
    {
        public static CompanionWorkResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CompanionWorkResponse), isFromPool) as CompanionWorkResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long RequestId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RequestId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:81)
    [MemoryPackable]
    [Message(Message_ET_Client.CompanionWorkResult)]
    public partial class CompanionWorkResult: MessageObject, IMessage
    {
        public static CompanionWorkResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CompanionWorkResult), isFromPool) as CompanionWorkResult;
        }

        [MemoryPackOrder(0)]
        public long RoleId { get; set; }
        [MemoryPackOrder(1)]
        public long RoleInstanceId { get; set; }
        [MemoryPackOrder(2)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(3)]
        public long RequestId { get; set; }
        [MemoryPackOrder(4)]
        public long WorkId { get; set; }
        [MemoryPackOrder(5)]
        public long ClaimSerial { get; set; }
        [MemoryPackOrder(6)]
        public int Status { get; set; }
        [MemoryPackOrder(7)]
        public string Reason { get; set; }
        [MemoryPackOrder(8)]
        public int EventKind { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RoleId = default;
            this.RoleInstanceId = default;
            this.LevelGeneration = default;
            this.RequestId = default;
            this.WorkId = default;
            this.ClaimSerial = default;
            this.Status = default;
            this.Reason = default;
            this.EventKind = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:94)
    [MemoryPackable]
    [Message(Message_ET_Client.GridCompanionState)]
    public partial class GridCompanionState: MessageObject
    {
        public static GridCompanionState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridCompanionState), isFromPool) as GridCompanionState;
        }

        [MemoryPackOrder(0)]
        public long RoleId { get; set; }
        [MemoryPackOrder(1)]
        public long CompanionId { get; set; }
        [MemoryPackOrder(2)]
        public int DefinitionId { get; set; }
        [MemoryPackOrder(3)]
        public string Name { get; set; }
        [MemoryPackOrder(4)]
        public int X { get; set; }
        [MemoryPackOrder(5)]
        public int Y { get; set; }
        [MemoryPackOrder(6)]
        public int NextX { get; set; }
        [MemoryPackOrder(7)]
        public int NextY { get; set; }
        [MemoryPackOrder(8)]
        public long StepStartTick { get; set; }
        [MemoryPackOrder(9)]
        public int Motion { get; set; }
        [MemoryPackOrder(10)]
        public long WorkId { get; set; }
        [MemoryPackOrder(11)]
        public long ClaimSerial { get; set; }
        [MemoryPackOrder(12)]
        public int CompletedCount { get; set; }
        [MemoryPackOrder(13)]
        public List<GridPathCell> Path { get; set; } = new List<GridPathCell>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RoleId = default;
            this.CompanionId = default;
            this.DefinitionId = default;
            this.Name = default;
            this.X = default;
            this.Y = default;
            this.NextX = default;
            this.NextY = default;
            this.StepStartTick = default;
            this.Motion = default;
            this.WorkId = default;
            this.ClaimSerial = default;
            this.CompletedCount = default;
            this.Path.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:111)
    [MemoryPackable]
    [Message(Message_ET_Client.GridFrontWorkState)]
    public partial class GridFrontWorkState: MessageObject
    {
        public static GridFrontWorkState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridFrontWorkState), isFromPool) as GridFrontWorkState;
        }

        [MemoryPackOrder(0)]
        public long WorkId { get; set; }
        [MemoryPackOrder(1)]
        public int Kind { get; set; }
        [MemoryPackOrder(2)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(3)]
        public long StationId { get; set; }
        [MemoryPackOrder(4)]
        public long ItemId { get; set; }
        [MemoryPackOrder(5)]
        public int State { get; set; }
        [MemoryPackOrder(6)]
        public double Progress { get; set; }
        [MemoryPackOrder(7)]
        public int WorkerKind { get; set; }
        [MemoryPackOrder(8)]
        public long WorkerId { get; set; }
        [MemoryPackOrder(9)]
        public long ClaimSerial { get; set; }
        [MemoryPackOrder(10)]
        public int FrozenTicks { get; set; }
        [MemoryPackOrder(11)]
        public int OperationX { get; set; }
        [MemoryPackOrder(12)]
        public int OperationY { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.WorkId = default;
            this.Kind = default;
            this.CustomerId = default;
            this.StationId = default;
            this.ItemId = default;
            this.State = default;
            this.Progress = default;
            this.WorkerKind = default;
            this.WorkerId = default;
            this.ClaimSerial = default;
            this.FrozenTicks = default;
            this.OperationX = default;
            this.OperationY = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:127)
    [MemoryPackable]
    [Message(Message_ET_Client.GridCustomerState)]
    public partial class GridCustomerState: MessageObject
    {
        public static GridCustomerState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridCustomerState), isFromPool) as GridCustomerState;
        }

        [MemoryPackOrder(0)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(1)]
        public int ArrivalSequence { get; set; }
        [MemoryPackOrder(2)]
        public int Phase { get; set; }
        [MemoryPackOrder(3)]
        public long TableStationId { get; set; }
        [MemoryPackOrder(4)]
        public long OrderId { get; set; }
        [MemoryPackOrder(5)]
        public int X { get; set; }
        [MemoryPackOrder(6)]
        public int Y { get; set; }
        [MemoryPackOrder(7)]
        public int NextX { get; set; }
        [MemoryPackOrder(8)]
        public int NextY { get; set; }
        [MemoryPackOrder(9)]
        public long StepStartTick { get; set; }
        [MemoryPackOrder(10)]
        public int Motion { get; set; }
        [MemoryPackOrder(11)]
        public int RemainingEatTicks { get; set; }
        [MemoryPackOrder(12)]
        public List<GridPathCell> Path { get; set; } = new List<GridPathCell>();
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.CustomerId = default;
            this.ArrivalSequence = default;
            this.Phase = default;
            this.TableStationId = default;
            this.OrderId = default;
            this.X = default;
            this.Y = default;
            this.NextX = default;
            this.NextY = default;
            this.StepStartTick = default;
            this.Motion = default;
            this.RemainingEatTicks = default;
            this.Path.Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:143)
    [MemoryPackable]
    [Message(Message_ET_Client.GridOrderState)]
    public partial class GridOrderState: MessageObject
    {
        public static GridOrderState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridOrderState), isFromPool) as GridOrderState;
        }

        [MemoryPackOrder(0)]
        public long OrderId { get; set; }
        [MemoryPackOrder(1)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(2)]
        public long TableStationId { get; set; }
        [MemoryPackOrder(3)]
        public int DishId { get; set; }
        [MemoryPackOrder(4)]
        public int Score { get; set; }
        [MemoryPackOrder(5)]
        public int State { get; set; }
        [MemoryPackOrder(6)]
        public long DeadlineTick { get; set; }
        [MemoryPackOrder(7)]
        public long ServedItemId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.OrderId = default;
            this.CustomerId = default;
            this.TableStationId = default;
            this.DishId = default;
            this.Score = default;
            this.State = default;
            this.DeadlineTick = default;
            this.ServedItemId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZFrontWork.proto (line:154)
    [MemoryPackable]
    [Message(Message_ET_Client.GridTableState)]
    public partial class GridTableState: MessageObject
    {
        public static GridTableState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GridTableState), isFromPool) as GridTableState;
        }

        [MemoryPackOrder(0)]
        public long StationId { get; set; }
        [MemoryPackOrder(1)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(2)]
        public int State { get; set; }
        [MemoryPackOrder(3)]
        public int SeatX { get; set; }
        [MemoryPackOrder(4)]
        public int SeatY { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.StationId = default;
            this.CustomerId = default;
            this.State = default;
            this.SeatX = default;
            this.SeatY = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZServe.proto (line:5)
    [MemoryPackable]
    [Message(Message_ET_Client.ServeOrderRequest)]
    [ResponseType(nameof(ServeOrderResponse))]
    public partial class ServeOrderRequest: MessageObject, IRequest
    {
        public static ServeOrderRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ServeOrderRequest), isFromPool) as ServeOrderRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long LevelId { get; set; }
        [MemoryPackOrder(7)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long CommandId { get; set; }
        [MemoryPackOrder(9)]
        public long OrderId { get; set; }
        [MemoryPackOrder(10)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(11)]
        public long TableStationId { get; set; }
        [MemoryPackOrder(12)]
        public long ItemId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.OrderId = default;
            this.CustomerId = default;
            this.TableStationId = default;
            this.ItemId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZServe.proto (line:21)
    [MemoryPackable]
    [Message(Message_ET_Client.ServeOrderResponse)]
    public partial class ServeOrderResponse: MessageObject, IResponse
    {
        public static ServeOrderResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ServeOrderResponse), isFromPool) as ServeOrderResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZServe.proto (line:30)
    [MemoryPackable]
    [Message(Message_ET_Client.ServeOrderSubmitRequest)]
    [ResponseType(nameof(ServeOrderSubmitResponse))]
    public partial class ServeOrderSubmitRequest: MessageObject, IRequest
    {
        public static ServeOrderSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ServeOrderSubmitRequest), isFromPool) as ServeOrderSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long LevelId { get; set; }
        [MemoryPackOrder(10)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(11)]
        public long CommandId { get; set; }
        [MemoryPackOrder(12)]
        public long OrderId { get; set; }
        [MemoryPackOrder(13)]
        public long CustomerId { get; set; }
        [MemoryPackOrder(14)]
        public long TableStationId { get; set; }
        [MemoryPackOrder(15)]
        public long ItemId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.OrderId = default;
            this.CustomerId = default;
            this.TableStationId = default;
            this.ItemId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZServe.proto (line:49)
    [MemoryPackable]
    [Message(Message_ET_Client.ServeOrderSubmitResponse)]
    public partial class ServeOrderSubmitResponse: MessageObject, IResponse
    {
        public static ServeOrderSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ServeOrderSubmitResponse), isFromPool) as ServeOrderSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZZLevelBuff.proto (line:4)
    [MemoryPackable]
    [Message(Message_ET_Client.ChooseBuffRequest)]
    [ResponseType(nameof(ChooseBuffResponse))]
    public partial class ChooseBuffRequest: MessageObject, IRequest
    {
        public static ChooseBuffRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ChooseBuffRequest), isFromPool) as ChooseBuffRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(2)]
        public string MatchId { get; set; }
        [MemoryPackOrder(3)]
        public string WorldId { get; set; }
        [MemoryPackOrder(4)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(5)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(6)]
        public long LevelId { get; set; }
        [MemoryPackOrder(7)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long CommandId { get; set; }
        [MemoryPackOrder(9)]
        public long DrawVersion { get; set; }
        [MemoryPackOrder(10)]
        public int ChoiceDefId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.DrawVersion = default;
            this.ChoiceDefId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZZLevelBuff.proto (line:18)
    [MemoryPackable]
    [Message(Message_ET_Client.ChooseBuffResponse)]
    public partial class ChooseBuffResponse: MessageObject, IResponse
    {
        public static ChooseBuffResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ChooseBuffResponse), isFromPool) as ChooseBuffResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZZLevelBuff.proto (line:27)
    [MemoryPackable]
    [Message(Message_ET_Client.ChooseBuffSubmitRequest)]
    [ResponseType(nameof(ChooseBuffSubmitResponse))]
    public partial class ChooseBuffSubmitRequest: MessageObject, IRequest
    {
        public static ChooseBuffSubmitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ChooseBuffSubmitRequest), isFromPool) as ChooseBuffSubmitRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PeerId { get; set; }
        [MemoryPackOrder(2)]
        public long PeerInstanceId { get; set; }
        [MemoryPackOrder(3)]
        public long PeerGeneration { get; set; }
        [MemoryPackOrder(4)]
        public int ProtocolVersion { get; set; }
        [MemoryPackOrder(5)]
        public string MatchId { get; set; }
        [MemoryPackOrder(6)]
        public string WorldId { get; set; }
        [MemoryPackOrder(7)]
        public long WorldGeneration { get; set; }
        [MemoryPackOrder(8)]
        public long BindingGeneration { get; set; }
        [MemoryPackOrder(9)]
        public long LevelId { get; set; }
        [MemoryPackOrder(10)]
        public long LevelGeneration { get; set; }
        [MemoryPackOrder(11)]
        public long CommandId { get; set; }
        [MemoryPackOrder(12)]
        public long DrawVersion { get; set; }
        [MemoryPackOrder(13)]
        public int ChoiceDefId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.PeerId = default;
            this.PeerInstanceId = default;
            this.PeerGeneration = default;
            this.ProtocolVersion = default;
            this.MatchId = default;
            this.WorldId = default;
            this.WorldGeneration = default;
            this.BindingGeneration = default;
            this.LevelId = default;
            this.LevelGeneration = default;
            this.CommandId = default;
            this.DrawVersion = default;
            this.ChoiceDefId = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

    // proto file : ET-Client/ZZZZZLevelBuff.proto (line:44)
    [MemoryPackable]
    [Message(Message_ET_Client.ChooseBuffSubmitResponse)]
    public partial class ChooseBuffSubmitResponse: MessageObject, IResponse
    {
        public static ChooseBuffSubmitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ChooseBuffSubmitResponse), isFromPool) as ChooseBuffSubmitResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long CommandId { get; set; }
        [MemoryPackOrder(4)]
        public int Status { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool) { return; }
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CommandId = default;
            this.Status = default;
            ObjectPool.Instance.Recycle(this);
        }
    }

}
