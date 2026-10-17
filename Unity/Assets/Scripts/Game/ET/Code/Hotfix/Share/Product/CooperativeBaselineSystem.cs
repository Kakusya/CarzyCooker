#if !DOTNET
using System;
using System.IO;
using System.Linq;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(CooperativeSaveBinding))]
    [FriendOf(typeof(CooperativeSaveBinding))]
    public static partial class CooperativeSaveBindingSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeSaveBinding self)
        {
            self.Baseline = default;
            self.Authorization = null;
            self.RootProfileId = self.RootSlotId = null;
            self.RootRevision = 0;
        }

        public static void ClearCooperativeBaseline(this Match match)
        {
            var binding = match.GetComponent<CooperativeSaveBinding>();
            if (binding == null)
            {
                return;
            }
            SelectedSaveBaseline baseline = binding.Baseline;
            binding.Authorization = null;
            binding.Baseline = default;
            match.RemoveComponent<CooperativeSaveBinding>();
            baseline?.Dispose();
        }
    }

    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(CooperativeSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    [FriendOf(typeof(SoloSaveRepository))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(CooperativeClaimState))]
    public static class CooperativeBaselineSystem
    {
        public static byte[] Authorize(SoloSaveRepository repository, string slot, string matchId, OfflineProfileIdentity host)
        {
            OfflineProfiles.EncodeIdentity(host);
            if (!SoloSaveValidation.IsIdentifier(matchId))
            {
                throw new InvalidOperationException("WrongMatch");
            }
            var payload = repository.Select(slot, out var header);
            if (header.RecordKind != SoloSaveRecordKind.Continue)
            {
                throw new InvalidOperationException("ChapterCompleted");
            }
            var value = new CooperativeBaselineValue
            {
                GrantId = Guid.NewGuid().ToString("N"), MatchId = matchId,
                HostProfileId = host.ProfileId, HostKeyId = host.KeyId, DelegateMatchResults = true,
                RootProfileId = header.Version >= 5 ? header.RootProfileId : header.ProfileId,
                RootSlotId = header.Version >= 5 ? header.RootSlotId : header.SlotId,
                RootRevision = header.Version >= 5 ? header.RootRevision : header.Revision,
                Record = SoloSaveFile.Encode(header, payload)
            };
            return OfflineProfiles.Sign(repository, OfflineProfiles.BaselineReadRun, MemoryPackHelper.Serialize(value));
        }

        public static CooperativeBaselineValue ReadValue(OfflineVerifiedDocument signed, out SoloSaveHeader header, out SoloSavePayload payload)
            => CooperativeBaselineValues.ReadValue(signed, out header, out payload);

        public static string ProveProfile(this HostSessionComponent authority, HostPeer peer, string matchId, int action, byte[] proof, out string challenge, out string profile)
        {
            challenge = profile = null;
            Match match = authority.Match;
            if (match == null || match.MatchId != matchId || peer.MemberId == 0)
            {
                return "WrongMatch";
            }
            Participant participant = match.GetChild<Participant>(peer.MemberId);
            if (participant == null || !participant.Online || (HostPeer)participant.BoundPeer != peer)
            {
                return "EntryClosed";
            }
            if (action == 1)
            {
                byte[] random = new byte[32];
                using (var generator = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    generator.GetBytes(random);
                }
                peer.ProfileChallenge = Convert.ToBase64String(random);
                challenge = peer.ProfileChallenge;
                return null;
            }
            if (action != 2 || peer.ProfileChallenge == null)
            {
                return "ProfileChallengeRequired";
            }
            SoloSaveRepository repository = authority.Root().GetComponent<SoloSaveRepository>();
            if (repository == null)
            {
                return "ProfileNotTrusted";
            }
            var signed = OfflineProfiles.Verify(proof, OfflineProfiles.ProfileProof, repository);
            var value = (ProductProfileProofValue)MemoryPackHelper.Deserialize(typeof(ProductProfileProofValue), signed.Body, 0, signed.Body.Length);
            if (value == null || value.MatchId != match.MatchId || value.BindingGeneration != peer.Generation || value.Challenge != peer.ProfileChallenge)
            {
                return "ProfileChallengeChanged";
            }
            if (participant.ProfileId != null && (participant.ProfileId != signed.Signer.ProfileId || participant.ProfileKeyId != signed.Signer.KeyId))
            {
                return "ProfileKeyConflict";
            }
            peer.ProfileChallenge = null;
            peer.ProfileProved = true;
            participant.ProfileId = signed.Signer.ProfileId;
            participant.ProfileKeyId = signed.Signer.KeyId;
            profile = participant.ProfileId;
            match.InvalidateRoomReadiness();
            ++match.RoomVersion;
            authority.BroadcastRoom();
            return null;
        }

        public static string LoadCooperativeBaseline(this HostSessionComponent authority, HostPeer peer, ProductCommandContext context, long roomVersion, byte[] authorization)
        {
            Match match = authority.Match;
            if (match?.GetComponent<CooperativeClaimState>()?.Closing == true)
            {
                return "MatchClosing";
            }
            ProductRuntimeComponent runtime = authority.Runtime;
            if (runtime == null || match == null || runtime.Stage is ProductStage.Stopping or ProductStage.Stopped or ProductStage.Failed)
            {
                return "EntryClosed";
            }
            if (!peer.CanControlRestaurant() || runtime.Config.Role != RuntimeInstanceRole.Host)
            {
                return "HostControlOnly";
            }
            if (context.MatchId != match.MatchId)
            {
                return "WrongMatch";
            }
            var repository = authority.Root().GetComponent<SoloSaveRepository>();
            var participant = match.GetChild<Participant>(peer.MemberId);
            if (repository == null || participant?.ProfileId == null || !peer.ProfileProved)
            {
                return "ProfileProofRequired";
            }
            var own = OfflineProfiles.OwnIdentity(repository);
            if (participant.ProfileId != own.ProfileId || participant.ProfileKeyId != own.KeyId)
            {
                return "WrongProfile";
            }
            var signed = OfflineProfiles.Verify(authorization, OfflineProfiles.BaselineReadRun, repository);
            var value = ReadValue(signed, out var header, out var payload);
            if (value.MatchId != match.MatchId || value.HostProfileId != participant.ProfileId || value.HostKeyId != participant.ProfileKeyId)
            {
                return "GrantScopeDenied";
            }
            bool same = roomVersion == peer.LastBaselineRoomVersion && peer.LastBaselineSignature != null && signed.Signature.SequenceEqual(peer.LastBaselineSignature);
            string prior = peer.CheckProductCommand(context, ProductCommandKind.Baseline, same, out bool replay);
            if (prior != null || replay)
            {
                return prior;
            }
            if (roomVersion != match.RoomVersion)
            {
                return "RoomChanged";
            }
            if ((RestaurantRuntime)match.Restaurant != null)
            {
                return "RestaurantAlreadyOpen";
            }
            long nextVersion = checked(match.RoomVersion + 1);
            peer.RecordProductCommand(context, ProductCommandKind.Baseline);
            peer.LastBaselineSignature = signed.Signature;
            peer.LastBaselineRoomVersion = roomVersion;
            try
            {
                match.ClearCooperativeBaseline();
                var binding = match.AddComponent<CooperativeSaveBinding>();
                binding.Baseline = match.AddChild<SelectedSaveBaseline, SoloSaveHeader, SoloSavePayload>(header, payload);
                binding.Authorization = (byte[])authorization.Clone();
                binding.RootProfileId = value.RootProfileId;
                binding.RootSlotId = value.RootSlotId;
                binding.RootRevision = value.RootRevision;
                DRLevelConfig first = Tables.Instance.DTLevelConfig.Get(header.NextLevelId);
                match.RoomMenuVariant = first.MenuVariant;
                match.PublishRoomProposal(first.MenuVariant, 0, first.Id);
                match.RoomVersion = nextVersion;
                peer.RecordProductResult(context.CommandId, GridResultStatus.Executed, null);
            }
            catch
            {
                match.ClearCooperativeBaseline();
                match.InvalidateRoomReadiness(false);
                peer.RecordProductResult(context.CommandId, GridResultStatus.Rejected, "RestaurantInitializeFailed");
                throw;
            }
            authority.BroadcastRoom();
            return null;
        }

    }
}
#endif
