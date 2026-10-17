#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(CompanionPreferenceState))]
    [FriendOf(typeof(CompanionPreferenceState))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class CompanionPreferenceSystem
    {
        [EntitySystem]
        private static void Awake(this CompanionPreferenceState self) => self.Value = CompanionWorkPreference.Automatic;

        public static string CommitCompanionPreference(this GridKitchenComponent kitchen, HostPeer peer, ProductCommandContext context, int value)
        {
            Level level = kitchen.ProductLevel();
            if (kitchen.Stopped || level == null || peer == null || !peer.EntryOpen || !kitchen.HasBinding(peer.MemberId, peer.Generation))
            {
                return "EntryClosed";
            }
            if (!peer.CanControlRestaurant())
            {
                return "HostControlOnly";
            }
            string error = context.Validate(((Match)kitchen.Match).MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (error != null)
            {
                return error;
            }
            if (!Enum.IsDefined(typeof(CompanionWorkPreference), value))
            {
                return "InvalidCompanionPreference";
            }
            var preference = (CompanionWorkPreference)value;
            error = peer.CheckProductCommand(context, ProductCommandKind.CompanionPreference, preference == peer.LastCompanionPreference, out bool replay);
            if (error != null || replay)
            {
                return error;
            }
            if (level.Paused)
            {
                return "Paused";
            }
            if (level.Phase is not (LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining))
            {
                return "WrongPhase";
            }
            peer.RecordProductCommand(context, ProductCommandKind.CompanionPreference);
            peer.LastCompanionPreference = preference;
            level.GetParent<RestaurantRuntime>().GetComponent<CompanionPreferenceState>().Value = preference;
            peer.RecordProductResult(context, ProductCommandKind.CompanionPreference, GridResultStatus.Executed, null);
            kitchen.Dirty = true;
            kitchen.CommitGridState();
            return null;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class CompanionPreferencePeerHandler : ProductActorRequestHandler<HostPeer, CompanionPreferenceRequest, CompanionPreferenceResponse>
    {
        protected override async UniTask Run(HostPeer peer, CompanionPreferenceRequest request, CompanionPreferenceResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen || !peer.CanControlRestaurant())
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = peer.EntryOpen ? "HostControlOnly" : "EntryClosed";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            var inner = ProductMessages.Create<CompanionPreferenceSubmitRequest>(value =>
            {
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation;
                value.ProtocolVersion = request.ProtocolVersion; value.MatchId = request.MatchId; value.WorldId = request.WorldId;
                value.WorldGeneration = request.WorldGeneration; value.LevelId = request.LevelId; value.LevelGeneration = request.LevelGeneration;
                value.BindingGeneration = request.BindingGeneration; value.CommandId = request.CommandId; value.Preference = request.Preference;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (CompanionPreferenceSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class CompanionPreferenceSubmitHandler : ProductActorRequestHandler<Entity, CompanionPreferenceSubmitRequest, CompanionPreferenceSubmitResponse>
    {
        protected override UniTask Run(Entity target, CompanionPreferenceSubmitRequest request, CompanionPreferenceSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            response.Status = (int)GridResultStatus.Rejected;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly";
                return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            GridKitchenComponent kitchen = ((Match)authority.Match).ProductKitchen();
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                    peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : kitchen == null ? "RestaurantUnavailable"
                : kitchen.CommitCompanionPreference(peer, new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration,
                    request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId), request.Preference);
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer?.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridClientComponent))]
    public static class CompanionPreferenceClientCommands
    {
        public static CompanionPreferenceRequest CreateCompanionPreference(this GridClientComponent self, CompanionWorkPreference preference)
        {
            if (!self.Confirmed)
            {
                throw new InvalidOperationException("Grid projection is not ready");
            }
            var context = self.GetParent<ClientSessionComponent>().CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            self.LastCommandId = context.CommandId; self.LastCommandKind = ProductCommandKind.CompanionPreference;
            self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<CompanionPreferenceRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId;
                value.WorldGeneration = context.WorldGeneration; value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration;
                value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId; value.Preference = (int)preference;
            });
        }
    }
}
#endif
