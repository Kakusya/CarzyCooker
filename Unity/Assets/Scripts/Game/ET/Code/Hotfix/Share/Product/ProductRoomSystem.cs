#if !DOTNET
using System;
using System.Linq;
using Game;

namespace ET
{
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(CooperativeSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(CooperativeClaimState))]
    public static class ProductRoomSystem
    {
        public static string CommitRoom(this HostSessionComponent authority, HostPeer peer, ProductCommandContext context, ProductRoomIntent intent)
        {
            Match match = authority.Match;
            ProductRuntimeComponent runtime = authority.Runtime;
            if (runtime == null || match == null || runtime.Stage is ProductStage.Stopping or ProductStage.Stopped or ProductStage.Failed)
            {
                return "EntryClosed";
            }
            if (peer == null || !peer.EntryOpen || peer.Closing)
            {
                return "EntryClosed";
            }
            bool ready = intent.Action is ProductRoomAction.Ready or ProductRoomAction.CancelReady;
            if (runtime.Config.Role != RuntimeInstanceRole.Host || !ready && !peer.CanControlRestaurant())
            {
                return "HostControlOnly";
            }
            if (match.GetComponent<CooperativeClaimState>()?.Closing == true)
            {
                return "MatchClosing";
            }
            if (context.MatchId != match.MatchId)
            {
                return "WrongMatch";
            }
            if (context.BindingGeneration != peer.Generation)
            {
                return "EntryClosed";
            }
            string error = peer.CheckProductCommand(context, ProductCommandKind.Room, intent.Equals(peer.LastRoomCommand), out bool replay);
            if (error != null || replay)
            {
                return error;
            }
            if (ready)
            {
                error = authority.CommitRoomReady(peer, context, intent);
                if (error == null || error == "RoomSourceChanged")
                {
                    authority.BroadcastRoom();
                }
                return error;
            }
            if (intent.Version != match.RoomVersion)
            {
                return "RoomChanged";
            }
            if (!ProductMenuCatalog.IsValid(intent.MenuVariant) || intent.Action is not (ProductRoomAction.StartRestaurant or ProductRoomAction.CloseRestaurant or ProductRoomAction.PublishProposal))
            {
                return "InvalidRoomCommand";
            }
            RestaurantRuntime old = match.Restaurant;
            if (intent.Action is ProductRoomAction.StartRestaurant or ProductRoomAction.PublishProposal && old != null)
            {
                return "RestaurantAlreadyOpen";
            }
            if (intent.Action == ProductRoomAction.CloseRestaurant && old == null)
            {
                return "RestaurantUnavailable";
            }
            DRLevelConfig selectedFirst = null;
            if (intent.Action is ProductRoomAction.StartRestaurant or ProductRoomAction.PublishProposal)
            {
                var selected = intent.ContentChapterId == 0 ? null : Tables.Instance.DTRestaurantChapterConfig.GetOrDefault(intent.ContentChapterId);
                if (intent.ContentChapterId != 0 && (selected == null || selected.MenuVariant != intent.MenuVariant))
                {
                    return "InvalidContentChapter";
                }
                selectedFirst = RestaurantChapters.First(intent.ContentChapterId, RuntimeInstanceRole.Host, intent.MenuVariant);
                if (intent.Action == ProductRoomAction.StartRestaurant)
                {
                    error = authority.CheckRoomProposal(intent);
                    if (error != null)
                    {
                        if (error == "RoomSourceChanged")
                        {
                            authority.BroadcastRoom();
                        }
                        return error;
                    }
                    if (!match.AllRoomMembersReady())
                    {
                        return "MembersNotReady";
                    }
                    SelectedSaveBaseline baseline = match.GetComponent<CooperativeSaveBinding>()?.Baseline ?? default;
                    if (baseline != null)
                    {
                        selectedFirst = Tables.Instance.DTLevelConfig.Get(baseline.Header.NextLevelId);
                    }
                }
            }
            else if (intent.ContentChapterId != 0)
            {
                return "InvalidContentChapter";
            }
            long nextVersion = checked(match.RoomVersion + 1);
            peer.RecordProductCommand(context, ProductCommandKind.Room);
            peer.LastRoomCommand = intent;
            try
            {
                if (intent.Action == ProductRoomAction.PublishProposal)
                {
                    match.ClearCooperativeBaseline();
                    match.PublishRoomProposal(intent.MenuVariant, intent.ContentChapterId, selectedFirst.Id);
                }
                else if (intent.Action == ProductRoomAction.StartRestaurant)
                {
                    DRLevelConfig first = selectedFirst;
                    long id = IdGenerater.Instance.GenerateId();
                    RestaurantRuntime restaurant;
                    try
                    {
                        restaurant = match.AddChildWithId<RestaurantRuntime, DRLevelConfig>(id, first);
                    }
                    catch
                    {
                        match.GetChild<RestaurantRuntime>(id)?.Dispose();
                        throw;
                    }
                    match.Restaurant = restaurant;
                    match.RoomMenuVariant = intent.MenuVariant;
                    match.InvalidateRoomReadiness(false);
                    foreach (Participant member in match.Children.Values.OfType<Participant>())
                    {
                        if (member.Online)
                        {
                            ((GridKitchenComponent)restaurant.Kitchen).QueueGridMember(member.MemberId, member.BindingGeneration, true);
                        }
                    }
                }
                else
                {
                    old.StopRestaurant();
                    old.Dispose();
                    match.Restaurant = default;
                    match.ClearCooperativeBaseline();
                    match.InvalidateRoomReadiness(false);
                }
                match.RoomVersion = nextVersion;
                peer.RecordProductResult(context.CommandId, GridResultStatus.Executed, null);
            }
            catch (Exception exception)
            {
                peer.RecordProductResult(context.CommandId, GridResultStatus.Rejected, "RestaurantInitializeFailed");
                Log.Warning("Room transition failed: " + exception.Message);
                return "RestaurantInitializeFailed";
            }
            authority.BroadcastRoom();
            return null;
        }

        public static void FinishCooperativeChapter(this HostSessionComponent authority, RestaurantRuntime restaurant)
        {
            Match match = authority.Match;
            if (match == null || (RestaurantRuntime)match.Restaurant != restaurant)
            {
                return;
            }
            Level level = restaurant.CurrentLevel;
            if (!level.IsChapterTerminal || level.Phase != LevelPhase.Completed)
            {
                return;
            }
            long nextVersion = checked(match.RoomVersion + 1);
            restaurant.StopRestaurant();
            restaurant.Dispose();
            match.Restaurant = default;
            match.ClearCooperativeBaseline();
            match.InvalidateRoomReadiness(false);
            match.RoomVersion = nextVersion;
            authority.BroadcastRoom();
        }

        public static void BroadcastRoom(this HostSessionComponent authority)
        {
            Match match = authority.Match;
            GridKitchenComponent kitchen = match.ProductKitchen();
            var repository = authority.Root().GetComponent<SoloSaveRepository>();
            byte[] hostIdentity = OfflineProfiles.AvailablePublicIdentity(repository);
            SelectedSaveBaseline baseline = match.GetComponent<CooperativeSaveBinding>()?.Baseline ?? default;
            foreach (HostPeer peer in authority.Children.Values.OfType<HostPeer>())
            {
                if (!peer.EntryOpen || peer.Closing || peer.MemberId <= 0)
                {
                    continue;
                }
                ProductRoomSnapshot snapshot = ProductMessages.Create<ProductRoomSnapshot>(message =>
                {
                    message.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                    message.MatchId = match.MatchId;
                    message.MemberId = peer.MemberId;
                    message.BindingGeneration = peer.Generation;
                    message.RoomVersion = match.RoomVersion;
                    message.WorldId = kitchen?.WorldId;
                    message.WorldGeneration = kitchen?.WorldGeneration ?? 0;
                    message.MenuVariant = (int)match.RoomMenuVariant;
                    message.CanControlRoom = ((ProductRuntimeComponent)authority.Runtime).Config.Role == RuntimeInstanceRole.Host && peer.CanControlRestaurant();
                    message.VerifiedProfileId = peer.ProfileProved ? match.GetChild<Participant>(peer.MemberId)?.ProfileId : null;
                    message.HostIdentity = hostIdentity == null ? null : (byte[])hostIdentity.Clone();
                    message.BaselineOwnerId = baseline?.Header.ProfileId;
                    message.BaselineSlotId = baseline?.Header.SlotId;
                    message.BaselineRevision = baseline?.Header.Revision ?? 0;
                });
                match.CopyRoomReadiness(snapshot);
                try
                {
                    ProductDelivery.Send(peer, snapshot);
                }
                catch (Exception exception)
                {
                    // 通知失败不改写已提交的房间结果，其他接收者继续。
                    Log.Warning("Room notification unavailable: " + exception.Message);
                }
            }
        }
    }
}
#endif
