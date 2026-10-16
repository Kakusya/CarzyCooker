#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class FrontWorkPeerHandler : ProductActorRequestHandler<HostPeer, FrontWorkRequest, FrontWorkResponse>
    {
        protected override async UniTask Run(HostPeer peer, FrontWorkRequest request, FrontWorkResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<FrontWorkSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.BindingGeneration = request.BindingGeneration;
                value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration; value.CommandId = request.CommandId;
                value.Action = request.Action; value.WorkId = request.WorkId; value.ClaimSerial = request.ClaimSerial;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (FrontWorkSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    public sealed class FrontWorkSubmitHandler : ProductActorRequestHandler<Entity, FrontWorkSubmitRequest, FrontWorkSubmitResponse>
    {
        protected override UniTask Run(Entity target, FrontWorkSubmitRequest request, FrontWorkSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            if (target is not HostSessionComponent authority)
            { response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly"; response.Status = (int)GridResultStatus.Rejected; return UniTask.CompletedTask; }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId); Match match = authority.Match;
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : match.ProductKitchen() == null ? "RestaurantUnavailable"
                : match.ProductKitchen().QueuePlayerFrontWork(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId), new FrontWorkIntent((FrontWorkAction)request.Action, request.WorkId, request.ClaimSerial));
            response.Error = error == null ? 0 : ProductError.InvalidRequest; response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(Level))]
    public sealed class CompanionWorkHandler : ProductActorRequestHandler<Entity, CompanionWorkRequest, CompanionWorkResponse>
    {
        protected override UniTask Run(Entity target, CompanionWorkRequest request, CompanionWorkResponse response)
        {
            response.RequestId = request.RequestId;
            if (target is not HostSessionComponent authority)
            { response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly"; response.Status = (int)GridResultStatus.Rejected; return UniTask.CompletedTask; }
            Match match = authority.Match; GridKitchenComponent kitchen = match.ProductKitchen(); Level level = kitchen.ProductLevel();
            CompanionRole role = level?.GetChild<CompanionRole>(request.RoleId);
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : role == null || role.InstanceId != request.RoleInstanceId || role.Parent != level || role.LevelGeneration != request.LevelGeneration ? "RoleChanged"
                : kitchen.QueueCompanionFrontWork(role, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, role.LevelGeneration, request.RequestId), new FrontWorkIntent((FrontWorkAction)request.Action, request.WorkId, request.ClaimSerial));
            response.Error = error == null ? 0 : ProductError.InvalidRequest; response.Message = error ?? role?.LastWorkReason;
            response.Status = (int)(error == null ? role.LastResultStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(CompanionRole))]
    public sealed class CompanionWorkResultHandler : MessageHandler<CompanionRole, CompanionWorkResult>
    {
        protected override UniTask Run(CompanionRole role, CompanionWorkResult message)
        {
            try
            {
                if (message.RoleId != role.Id || message.RoleInstanceId != role.InstanceId || message.LevelGeneration != role.LevelGeneration ||
                    message.RequestId != role.LastCommittedRequestId) return UniTask.CompletedTask;
                if ((CompanionNotificationKind)message.EventKind is CompanionNotificationKind.RequestResult or CompanionNotificationKind.Finished)
                {
                    role.LastNotificationKind = (CompanionNotificationKind)message.EventKind;
                }
            }
            finally { message.Dispose(); }
            return UniTask.CompletedTask;
        }
    }

    public readonly struct FrontWorkReceipt
    {
        public readonly int Error;
        public readonly GridResultStatus Status;
        public readonly string Reason;
        public FrontWorkReceipt(int error, GridResultStatus status, string reason) { Error = error; Status = status; Reason = reason; }
    }

    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(Match))]
    public static class CompanionRequests
    {
        public static CompanionWorkRequest CreateCompanionWorkRequest(this CompanionRole self, FrontWorkIntent intent)
        {
            Level level = self.GetParent<Level>(); GridKitchenComponent kitchen = level.GetParent<RestaurantRuntime>().GetComponent<GridKitchenComponent>();
            Match match = kitchen.Match; long request = checked(++self.NextRequestId);
            return ProductMessages.Create<CompanionWorkRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = match.MatchId; value.WorldId = kitchen.WorldId;
                value.WorldGeneration = kitchen.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.RoleId = self.Id; value.RoleInstanceId = self.InstanceId; value.RequestId = request;
                value.Action = (int)intent.Action; value.WorkId = intent.WorkId; value.ClaimSerial = intent.ClaimSerial;
            });
        }

        public static UniTask<FrontWorkReceipt> RequestCompanionWork(this CompanionRole self, FrontWorkIntent intent)
        {
            var owner = self.GetAsyncOwner();
            if (!owner.AsyncScope.IsActive) throw new ObjectDisposedException(nameof(CompanionRole));
            if (self.RequestInFlight) throw new InvalidOperationException("Companion request already in flight");
            HostSessionComponent authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            if (authority == null) throw new InvalidOperationException("Companion authority unavailable");
            ActorId target = authority.GetActorId(); ProcessInnerSender sender = self.Root().GetComponent<ProcessInnerSender>();
            CompanionWorkRequest request = self.CreateCompanionWorkRequest(intent);
            EntityRef<CompanionRole> original = self; self.RequestInFlight = true;
            try
            {
                return owner.AsyncScope.RunAsync(context => context.CallLocalValueAsync(sender, target, request,
                    response => { var value = (CompanionWorkResponse)response; return new FrontWorkReceipt(value.Error, (GridResultStatus)value.Status, value.Message); },
                    () => { CompanionRole role = original; if (role != null) role.RequestInFlight = false; }));
            }
            catch { request.Dispose(); self.RequestInFlight = false; throw; }
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class FrontWorkClientCommands
    {
        public static FrontWorkRequest CreateFrontWorkCommand(this GridClientComponent self, FrontWorkAction action, long work, long serial = 0)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            ProductCommandContext context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.FrontWork; self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<FrontWorkRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.BindingGeneration = context.BindingGeneration; value.LevelId = context.LevelId;
                value.LevelGeneration = context.LevelGeneration; value.CommandId = context.CommandId; value.Action = (int)action; value.WorkId = work; value.ClaimSerial = serial;
            });
        }
    }
}
#endif
