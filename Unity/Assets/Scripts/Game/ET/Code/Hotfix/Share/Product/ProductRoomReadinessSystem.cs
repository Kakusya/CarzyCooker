#if !DOTNET
using System;
using System.IO;
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(ProductRoomReadiness))]
    [FriendOf(typeof(ProductRoomReadiness))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(CooperativeSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    [FriendOf(typeof(SoloSaveRepository))]
    public static partial class ProductRoomReadinessSystem
    {
        [EntitySystem]
        private static void Awake(this ProductRoomReadiness self)
        {
            self.ProposalVersion = 0; self.HasProposal = false;
            self.MenuVariant = ProductMenuVariant.Basic; self.ContentChapterId = 0;
            self.FirstLevelConfigId = 0;
            self.Ready.Clear();
        }

        public static void InvalidateRoomReadiness(this Match match, bool keepProposal = true)
        {
            var state = match.GetComponent<ProductRoomReadiness>();
            state.ProposalVersion = checked(state.ProposalVersion + 1);
            state.Ready.Clear();
            if (!keepProposal)
            {
                state.HasProposal = false;
            }
        }

        public static void PublishRoomProposal(this Match match, ProductMenuVariant menu, int chapter, int firstLevel)
        {
            match.InvalidateRoomReadiness();
            var state = match.GetComponent<ProductRoomReadiness>();
            state.HasProposal = true; state.MenuVariant = menu; state.ContentChapterId = chapter;
            state.FirstLevelConfigId = firstLevel;
        }

        public static bool AllRoomMembersReady(this Match match)
        {
            var state = match.GetComponent<ProductRoomReadiness>();
            if (!state.HasProposal)
            {
                return false;
            }
            int connected = 0;
            foreach (var member in match.Children.Values.OfType<Participant>())
            {
                if (!member.Online)
                {
                    continue;
                }
                ++connected;
                HostPeer peer = member.BoundPeer;
                if (peer == null || !peer.EntryOpen || peer.Closing || peer.Generation != member.BindingGeneration ||
                    !state.Ready.Contains((member.MemberId, member.BindingGeneration)))
                {
                    return false;
                }
            }
            return connected > 0;
        }

        public static string CheckRoomProposal(this HostSessionComponent authority, ProductRoomIntent intent)
        {
            Match match = authority.Match;
            var state = match.GetComponent<ProductRoomReadiness>();
            if (!state.HasProposal)
            {
                return "RoomProposalRequired";
            }
            if (state.ProposalVersion != intent.ProposalVersion || state.MenuVariant != intent.MenuVariant || state.ContentChapterId != intent.ContentChapterId)
            {
                return "RoomProposalChanged";
            }
            SelectedSaveBaseline baseline = match.GetComponent<CooperativeSaveBinding>()?.Baseline ?? default;
            if (baseline == null)
            {
                return null;
            }
            try
            {
                var repository = authority.Root().GetComponent<SoloSaveRepository>();
                var binding = match.GetComponent<CooperativeSaveBinding>();
                var signed = OfflineProfiles.Verify(binding.Authorization, OfflineProfiles.BaselineReadRun, repository);
                CooperativeBaselineValues.ReadValue(signed, out var authorized, out _);
                if (authorized.SlotId != baseline.Header.SlotId || authorized.Revision != baseline.Header.Revision || authorized.ResultId != baseline.Header.ResultId)
                {
                    throw new InvalidOperationException("SourceChanged");
                }
                // 本机来源可重新读取当前修订；外部授权仍以原已签名快照为能力边界。
                if (repository.ProfileId == baseline.Header.ProfileId)
                {
                    repository.Select(baseline.Header.SlotId, out var current);
                    if (current.Revision != baseline.Header.Revision || current.ResultId != baseline.Header.ResultId)
                    {
                        throw new InvalidOperationException("SourceChanged");
                    }
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException or System.Security.Cryptography.CryptographicException)
            {
                match.InvalidateRoomReadiness(false);
                ++match.RoomVersion;
                return "RoomSourceChanged";
            }
            return null;
        }

        public static string CommitRoomReady(this HostSessionComponent authority, HostPeer peer, ProductCommandContext context, ProductRoomIntent intent)
        {
            Match match = authority.Match;
            if ((RestaurantRuntime)match.Restaurant != null)
            {
                return "RestaurantAlreadyOpen";
            }
            var member = match.GetChild<Participant>(peer.MemberId);
            if (member?.Online != true || (HostPeer)member.BoundPeer != peer || member.BindingGeneration != context.BindingGeneration)
            {
                return "EntryClosed";
            }
            string error = authority.CheckRoomProposal(intent);
            if (error != null)
            {
                return error;
            }
            var state = match.GetComponent<ProductRoomReadiness>();
            peer.RecordProductCommand(context, ProductCommandKind.Room);
            peer.LastRoomCommand = intent;
            if (intent.Action == ProductRoomAction.Ready)
            {
                state.Ready.Add((member.MemberId, member.BindingGeneration));
            }
            else
            {
                state.Ready.Remove((member.MemberId, member.BindingGeneration));
            }
            ++match.RoomVersion;
            peer.RecordProductResult(context.CommandId, GridResultStatus.Executed, null);
            return null;
        }

        public static void CopyRoomReadiness(this Match match, ProductRoomSnapshot snapshot)
        {
            var state = match.GetComponent<ProductRoomReadiness>();
            snapshot.HasProposal = state.HasProposal; snapshot.ProposalVersion = state.ProposalVersion;
            snapshot.ProposalMenu = (int)state.MenuVariant; snapshot.ProposalChapter = state.ContentChapterId;
            snapshot.AllReady = match.AllRoomMembersReady();
            snapshot.ProposalFirstLevel = state.FirstLevelConfigId;
            foreach (var member in match.Children.Values.OfType<Participant>().OrderBy(member => member.MemberId))
            {
                snapshot.ReadyMembers.Add(ProductMessages.Create<ProductRosterMember>(value =>
                {
                    value.MemberId = member.MemberId; value.Online = member.Online; value.ReadyBinding = member.BindingGeneration;
                    value.Ready = member.Online && state.Ready.Contains((member.MemberId, member.BindingGeneration));
                }));
            }
        }
    }
}
#endif
