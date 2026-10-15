#if !DOTNET
using System;
using System.Linq;
using Game;

namespace ET
{
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(CooperativeSaveBinding))]
    public static partial class RestaurantRuntimeSystem
    {
        private static void FreezeCooperativeEligibility(this RestaurantRuntime self, Level level)
        {
            var authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            if (((ProductRuntimeComponent)authority.Runtime).Config.Role != RuntimeInstanceRole.Host)
            {
                return;
            }
            Match match = self.Match;
            foreach (var member in match.Children.Values.OfType<Participant>())
            {
                HostPeer peer = member.BoundPeer;
                if (member.Online && peer != null && peer.EntryOpen && !peer.Closing && peer.ProfileProved &&
                    peer.Generation == member.BindingGeneration && SoloSaveValidation.IsIdentifier(member.ProfileId))
                {
                    level.EligibleProfiles[member.ProfileId] = member.ProfileKeyId;
                }
            }
            if (level.EligibleProfiles.Count > 0 && match.GetComponent<CooperativeClaimState>() == null)
            {
                match.AddComponent<CooperativeClaimState>();
            }
        }

        private static void AdvanceCooperativeResults(this RestaurantRuntime self, Level level)
        {
            if (level.Phase != LevelPhase.Completed || level.EligibleProfiles.Count == 0 || (CooperativeSaveResult)level.ClaimResult != null)
            {
                return;
            }
            var renovation = self.GetComponent<RestaurantRenovation>();
            if (!level.IsChapterTerminal && (!renovation.Prepared || !renovation.SelectionFinalized))
            {
                return;
            }
            Match match = self.Match;
            GridKitchenComponent kitchen = self.Kitchen;
            byte[] payload = MemoryPackHelper.Serialize(SoloSaveCapture.Kitchen(self));
            var result = match.AddChild<CooperativeSaveResult>();
            result.ResultId = Guid.NewGuid().ToString("N");
            result.WorldId = kitchen.WorldId;
            result.LevelId = level.Id;
            result.LevelGeneration = level.Generation;
            var authority = new ProgressAuthorityValue
            {
                AuthorityId = Guid.NewGuid().ToString("N"), MatchId = match.MatchId, WorldId = kitchen.WorldId,
                BaselineAuthorization = match.GetComponent<CooperativeSaveBinding>()?.Authorization
            };
            result.UnsignedSuccess = MemoryPackHelper.Serialize(new CooperativeSuccessValue
            {
                SaveResultId = result.ResultId, MatchId = match.MatchId, WorldId = kitchen.WorldId,
                CompletedLevelId = level.ConfigId, NextLevelId = level.NextLevelConfigId,
                Score = level.Score, Stars = level.Stars, ServedOrders = level.ServedOrders, UnfulfilledOrders = level.UnfulfilledOrders,
                CompletedUtcTicks = DateTime.UtcNow.Ticks, Payload = payload,
                Authority = MemoryPackHelper.Serialize(authority)
            });
            foreach (var eligible in level.EligibleProfiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var claim = result.AddChild<CooperativeClaim>();
                claim.TicketId = Guid.NewGuid().ToString("N");
                claim.ProfileId = eligible.Key;
                claim.KeyId = eligible.Value;
                result.Claims.Add(claim.ProfileId, claim);
            }
            level.ClaimResult = result;
            match.GetComponent<CooperativeClaimState>().Results.Add(result.ResultId, result);
            result.PrepareCooperativeResult();
            kitchen.Dirty = true;
        }

        private static bool CooperativeSavePending(this RestaurantRuntime self, Level level)
        {
            CooperativeSaveResult result = level.ClaimResult;
            return result?.PendingControl != null && !result.HostSaveCommitted;
        }

        private static string BeginCooperativeSave(this RestaurantRuntime self, Level level, LevelControlEnvelope control)
        {
            if (level.Phase != LevelPhase.Completed)
            {
                return "SuccessRequired";
            }
            CooperativeSaveResult result = level.ClaimResult;
            if (result == null)
            {
                return "SettlementChoiceRequired";
            }
            if (result.HostSaveCommitted)
            {
                return self.ApplyLevelControl(level, new LevelControlIntent(level.IsChapterTerminal ? LevelControlAction.FinishChapter : LevelControlAction.Continue, level.NextLevelConfigId));
            }
            HostPeer peer = control.Peer;
            Match match = self.Match;
            Participant member = match.GetChild<Participant>(peer.MemberId);
            if (!peer.ProfileProved || member?.ProfileId == null || !result.Claims.ContainsKey(member.ProfileId))
            {
                return "HostProfileRequired";
            }
            result.PendingControl = control;
            result.HostSaveRequired = true;
            if (result.SignedSuccess == null)
            {
                if (!result.Preparing)
                {
                    result.PrepareCooperativeResult();
                }
                return null;
            }
            if (result.State != SoloSaveState.Writing)
            {
                result.WriteCooperativeHostClaim(member.ProfileId);
            }
            return null;
        }
    }

    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class CooperativeSaveResultSystem
    {
        public static void PrepareCooperativeResult(this CooperativeSaveResult self)
        {
            if (self.Preparing || self.SignedSuccess != null)
            {
                return;
            }
            self.Preparing = true;
            var request = ProductMessages.Create<ProgressRepositoryRequest>(value =>
            {
                value.Operation = (int)ProgressRepositoryOperation.PrepareResult;
                value.MatchId = self.GetParent<Match>().MatchId;
                value.Document = self.UnsignedSuccess;
                foreach (var reference in self.Claims.Values)
                {
                    CooperativeClaim claim = reference;
                    value.Profiles.Add(claim.ProfileId);
                    value.Keys.Add(claim.KeyId);
                    value.TicketIds.Add(claim.TicketId);
                }
            });
            EntityRef<CooperativeSaveResult> original = self;
            var repository = self.Root().GetComponent<SoloSaveRepository>();
            EntityRef<ProcessInnerSender> senderRef = self.Root().GetComponent<ProcessInnerSender>();
            var authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            var target = authority.GetActorId();
            string matchId = self.GetParent<Match>().MatchId;
            long id = self.Id, instance = self.InstanceId;
            self.GetAsyncOwner().Run(async context =>
            {
                int error = 0; string failure = null; byte[] success = null; byte[][] tickets = null;
                try
                {
                    if (repository == null)
                    {
                        throw new InvalidOperationException("SaveRepositoryUnavailable");
                    }
                    using var reply = (ProgressRepositoryResponse)await ((ProcessInnerSender)senderRef).Call(repository.GetActorId(), request, false);
                    error = reply.Error; failure = reply.Message;
                    success = reply.Document == null ? null : (byte[])reply.Document.Clone();
                    tickets = reply.Documents.Select(bytes => (byte[])bytes.Clone()).ToArray();
                }
                catch (Exception exception) { error = ProductError.InvalidRequest; failure = exception.Message; }
                context.ThrowIfStopped(); self = original;
                var sender = (ProcessInnerSender)senderRef;
                if (self == null || sender == null || sender.IsStopped)
                {
                    return;
                }
                using var response = (ProgressPreparedResponse)await sender.Call(target, ProductMessages.Create<ProgressPreparedRequest>(value =>
                {
                    value.MatchId = matchId; value.ResultEntityId = id; value.ResultInstanceId = instance;
                    value.Error = error; value.Failure = failure; value.Success = success;
                    if (tickets != null)
                    {
                        value.Tickets.AddRange(tickets);
                    }
                }), false);
            });
        }

        public static void WriteCooperativeHostClaim(this CooperativeSaveResult self, string profile)
        {
            CooperativeClaim claim = self.Claims[profile];
            byte[] ticket = claim.Document;
            string ticketId = claim.TicketId, matchId = self.GetParent<Match>().MatchId;
            long id = self.Id, instance = self.InstanceId;
            self.State = SoloSaveState.Writing; self.Failure = null;
            EntityRef<CooperativeSaveResult> original = self;
            EntityRef<ProcessInnerSender> senderRef = self.Root().GetComponent<ProcessInnerSender>();
            var repositoryActor = self.Root().GetComponent<SoloSaveRepository>().GetActorId();
            var authorityActor = ((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId();
            self.GetAsyncOwner().Run(async context =>
            {
                int error = 0; string failure = null; byte[] receipt = null;
                try
                {
                    using var persisted = (ProgressRepositoryResponse)await ((ProcessInnerSender)senderRef).Call(repositoryActor,
                        ProductMessages.Create<ProgressRepositoryRequest>(value => { value.Operation = (int)ProgressRepositoryOperation.PersistTicket; value.MatchId = matchId; value.Document = ticket; }), false);
                    error = persisted.Error; failure = persisted.Message;
                    if (error == 0)
                    {
                        using var saved = (ProgressRepositoryResponse)await ((ProcessInnerSender)senderRef).Call(repositoryActor,
                            ProductMessages.Create<ProgressRepositoryRequest>(value => { value.Operation = (int)ProgressRepositoryOperation.Claim; value.Id = ticketId; }), false);
                        error = saved.Error; failure = saved.Message;
                        receipt = saved.Receipt == null ? null : (byte[])saved.Receipt.Clone();
                    }
                }
                catch (Exception exception) { error = ProductError.InvalidRequest; failure = exception.Message; }
                context.ThrowIfStopped(); self = original;
                var sender = (ProcessInnerSender)senderRef;
                if (self == null || sender == null || sender.IsStopped)
                {
                    return;
                }
                using var response = (ProgressHostSavedResponse)await sender.Call(authorityActor, ProductMessages.Create<ProgressHostSavedRequest>(value =>
                {
                    value.MatchId = matchId; value.ResultEntityId = id; value.ResultInstanceId = instance;
                    value.Error = error; value.Failure = failure; value.Receipt = receipt;
                }), false);
            });
        }
    }
}
#endif
