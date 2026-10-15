#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    public readonly struct VerifiedProgressAuthority
    {
        public readonly ProgressAuthorityValue Value;
        public readonly OfflineProfileIdentity Host;
        public readonly SoloSaveHeader Source;
        public VerifiedProgressAuthority(ProgressAuthorityValue value, OfflineProfileIdentity host, SoloSaveHeader source)
        {
            Value = value; Host = host; Source = source;
        }
    }

    public readonly struct VerifiedProgressTicket
    {
        public readonly ProgressTicketValue Ticket;
        public readonly CooperativeSuccessValue Success;
        public readonly SoloSavePayload Payload;
        public readonly VerifiedProgressAuthority Authority;
        public VerifiedProgressTicket(ProgressTicketValue ticket, CooperativeSuccessValue success, SoloSavePayload payload, VerifiedProgressAuthority authority)
        {
            Ticket = ticket; Success = success; Payload = payload; Authority = authority;
        }
    }

    public static class ProgressClaimCodec
    {
        public static T Decode<T>(byte[] bytes) where T : class
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > OfflineProfiles.MaxDocumentBytes)
            {
                throw new InvalidOperationException("InvalidAuthorizationLength");
            }
            return (T)MemoryPackHelper.Deserialize(typeof(T), bytes, 0, bytes.Length);
        }

        public static VerifiedProgressAuthority Authority(byte[] document, SoloSaveRepository repository)
        {
            var signed = OfflineProfiles.Verify(document, OfflineProfiles.MatchAuthority, repository);
            var value = Decode<ProgressAuthorityValue>(signed.Body);
            if (value == null || !SoloSaveValidation.IsIdentifier(value.AuthorityId) || !SoloSaveValidation.IsIdentifier(value.MatchId) ||
                !SoloSaveValidation.IsIdentifier(value.WorldId) || value.HostProfileId != signed.Signer.ProfileId || value.HostKeyId != signed.Signer.KeyId)
            {
                throw new InvalidOperationException("InvalidMatchAuthority");
            }
            SoloSaveHeader source = null;
            if (value.BaselineAuthorization?.Length > 0)
            {
                var grant = OfflineProfiles.Verify(value.BaselineAuthorization, OfflineProfiles.BaselineReadRun, repository);
                var baseline = CooperativeBaselineSystem.ReadValue(grant, out source, out _);
                if (baseline.MatchId != value.MatchId || baseline.HostProfileId != value.HostProfileId || baseline.HostKeyId != value.HostKeyId)
                {
                    throw new InvalidOperationException("GrantScopeDenied");
                }
            }
            return new VerifiedProgressAuthority(value, signed.Signer, source);
        }

        public static VerifiedProgressTicket Ticket(byte[] document, SoloSaveRepository repository)
        {
            var signed = OfflineProfiles.Verify(document, OfflineProfiles.ClaimTicket, repository);
            var ticket = Decode<ProgressTicketValue>(signed.Body);
            var own = OfflineProfiles.OwnIdentity(repository);
            if (ticket == null || !SoloSaveValidation.IsIdentifier(ticket.TicketId) || ticket.EligibleProfileId != own.ProfileId || ticket.EligibleKeyId != own.KeyId)
            {
                throw new InvalidOperationException("IneligibleProfile");
            }
            var successSigned = OfflineProfiles.Verify(ticket.Success, OfflineProfiles.SuccessResult, repository);
            var success = Decode<CooperativeSuccessValue>(successSigned.Body);
            if (success == null)
            {
                throw new InvalidOperationException("InvalidSuccessResult");
            }
            var authority = Authority(success.Authority, repository);
            if (signed.Signer.ProfileId != authority.Host.ProfileId || signed.Signer.KeyId != authority.Host.KeyId ||
                successSigned.Signer.ProfileId != authority.Host.ProfileId || successSigned.Signer.KeyId != authority.Host.KeyId ||
                success.MatchId != authority.Value.MatchId || success.WorldId != authority.Value.WorldId ||
                !SoloSaveValidation.IsIdentifier(success.SaveResultId) || success.CompletedUtcTicks <= 0 || success.CompletedUtcTicks > DateTime.MaxValue.Ticks)
            {
                throw new InvalidOperationException("InvalidSuccessResult");
            }
            SoloSavePayload payload = Decode<SoloSavePayload>(success.Payload);
            var probe = Header(success, authority, own.ProfileId, Guid.NewGuid().ToString("N"), Array.Empty<ProgressCommitReceipt>(), validationOnly: true);
            RestaurantUnlockValues.AdaptLegacy(probe, payload);
            KitchenGeometryConfig.AdaptLegacy(probe, payload);
            SoloSaveValidation.Payload(probe, payload, own.ProfileId);
            return new VerifiedProgressTicket(ticket, success, payload, authority);
        }

        public static ProgressCloseValue Close(byte[] document, SoloSaveRepository repository)
        {
            var signed = OfflineProfiles.Verify(document, OfflineProfiles.NormalClose, repository);
            var value = Decode<ProgressCloseValue>(signed.Body);
            var own = OfflineProfiles.OwnIdentity(repository);
            if (value == null || !SoloSaveValidation.IsIdentifier(value.ReceiptId) || value.ProfileId != own.ProfileId)
            {
                throw new InvalidOperationException("IneligibleProfile");
            }
            var authority = Authority(value.Authority, repository);
            if (value.MatchId != authority.Value.MatchId || signed.Signer.ProfileId != authority.Host.ProfileId || signed.Signer.KeyId != authority.Host.KeyId)
            {
                throw new InvalidOperationException("InvalidCloseReceipt");
            }
            return value;
        }

        public static SoloSaveHeader Header(CooperativeSuccessValue success, VerifiedProgressAuthority authority, string profile, string slot,
            ProgressCommitReceipt[] receipts, bool validationOnly = false)
        {
            var definition = Tables.Instance.DTLevelConfig.GetOrDefault(success.CompletedLevelId);
            if (definition == null || success.NextLevelId != definition.NextLevelConfigId)
            {
                throw new InvalidOperationException("InvalidNextLevel");
            }
            var header = new SoloSaveHeader
            {
                Version = validationOnly ? success.ContentVersion == 0 ? 7 : success.ContentVersion : SoloSaveFormat.Current,
                SlotId = slot, ProfileId = profile, ResultId = success.SaveResultId,
                CompletedLevelId = success.CompletedLevelId, NextLevelId = success.NextLevelId,
                Score = success.Score, Stars = success.Stars, SavedUtcTicks = success.CompletedUtcTicks,
                RecordKind = definition.IsChapterTerminal ? SoloSaveRecordKind.ChapterCompleted : SoloSaveRecordKind.Continue,
                ChapterId = definition.ChapterId, ChapterStage = definition.ChapterStage,
                ServedOrders = success.ServedOrders, UnfulfilledOrders = success.UnfulfilledOrders,
                SourceResultId = success.SaveResultId, AuthorizationId = authority.Value.AuthorityId,
                Receipts = receipts
            };
            if (authority.Source != null)
            {
                var source = authority.Source;
                header.SourceProfileId = source.ProfileId;
                header.SourceSlotId = source.SlotId;
                header.SourceRevision = source.Revision;
                header.RootProfileId = source.Version >= 5 ? source.RootProfileId : source.ProfileId;
                header.RootSlotId = source.Version >= 5 ? source.RootSlotId : source.SlotId;
                header.RootRevision = source.Version >= 5 ? source.RootRevision : source.Revision;
            }
            return header;
        }

        public static string ConsumeKey(string result, string profile)
        {
            if (!SoloSaveValidation.IsIdentifier(result) || !SoloSaveValidation.IsIdentifier(profile))
            {
                throw new InvalidOperationException("InvalidClaimIdentity");
            }
            return result + ":" + profile;
        }
    }
}
#endif
