#if !DOTNET
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ET
{
    public readonly struct VerifiedImportBranch
    {
        public readonly ImportBranchGrantValue Grant;
        public readonly SoloSaveHeader Header;
        public readonly SoloSavePayload Payload;
        public readonly OfflineProfileIdentity Signer;
        public VerifiedImportBranch(ImportBranchGrantValue grant, SoloSaveHeader header, SoloSavePayload payload, OfflineProfileIdentity signer)
        {
            Grant = grant; Header = header; Payload = payload; Signer = signer;
        }
    }

    [FriendOf(typeof(SoloSaveRepository))]
    public static class SaveAuthorizationFiles
    {
        public static (SaveAuthorizationKind Kind, string GrantId, OfflineProfileIdentity Signer, string Summary) Inspect(byte[] document)
        {
            OfflineVerifiedDocument signed;
            try
            {
                signed = OfflineProfiles.Verify(document, OfflineProfiles.ImportBranch);
            }
            catch (InvalidOperationException exception) when (exception.Message == "GrantScopeDenied")
            {
                var replacement = ReadReplace(document);
                var grant = replacement.Grant;
                return (SaveAuthorizationKind.Replace, grant.GrantId, replacement.Signer,
                    $"有损替换授权\n目标Owner：{grant.TargetProfileId}\nSlot：{grant.TargetSlotId} · 预期修订 {grant.ExpectedRevision}\n接收Profile：{grant.RecipientProfileId}\n允许来源根：{grant.RootProfileId}/{grant.RootSlotId} · {grant.RootRevision}\n不会授予其他目标或远程撤回权");
            }
            var source = ReadImport(document);
            return (SaveAuthorizationKind.ImportBranch, source.Grant.GrantId, signed.Signer,
                $"独立分支导入授权\n来源：{source.Header.ProfileId}/{source.Header.SlotId} · 修订 {source.Header.Revision}\n根：{source.Header.RootProfileId}/{source.Header.RootSlotId} · {source.Header.RootRevision}\n接收者新建自己的记录，源档不改变；已导入分支不能远程撤回");
        }

        public static byte[] ExportImport(SoloSaveRepository repository, string slot, long? expiresUtcTicks = null)
        {
            var payload = repository.Select(slot, out var header);
            long expiry = expiresUtcTicks ?? DateTime.MaxValue.Ticks;
            CheckExpiry(expiry);
            return OfflineProfiles.Sign(repository, OfflineProfiles.ImportBranch, MemoryPackHelper.Serialize(new ImportBranchGrantValue
            {
                GrantId = Guid.NewGuid().ToString("N"), ExpiresUtcTicks = expiry, Record = SoloSaveFile.Encode(header, payload)
            }));
        }

        public static VerifiedImportBranch ReadImport(byte[] document, SoloSaveRepository repository = null)
        {
            var signed = OfflineProfiles.Verify(document, OfflineProfiles.ImportBranch, repository);
            var value = ProgressClaimCodec.Decode<ImportBranchGrantValue>(signed.Body);
            if (value == null || !SoloSaveValidation.IsIdentifier(value.GrantId) || value.ExpiresUtcTicks <= 0 ||
                value.ExpiresUtcTicks > DateTime.MaxValue.Ticks || value.Record == null ||
                value.Record.Length > SoloSaveFile.MaxPayloadBytes + SoloSaveFile.MaxHeaderBytes + 16)
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            using var stream = new MemoryStream(value.Record, false);
            var payload = SoloSaveFile.ReadPayload(stream, signed.Signer.ProfileId, out var header);
            return new VerifiedImportBranch(value, header, payload, signed.Signer);
        }

        public static ProgressCommitReceipt Import(SoloSaveRepository repository, byte[] document)
        {
            var source = ReadImport(document, repository);
            var own = OfflineProfiles.OwnIdentity(repository);
            var prior = ProgressReceiptIndex.Find(repository, ProgressCommitKind.ImportBranch, source.Grant.GrantId, own.ProfileId);
            if (prior != null)
            {
                return prior;
            }
            CheckUsable(repository, source.Grant.GrantId, source.Grant.ExpiresUtcTicks);
            byte[] encoded = MemoryPackHelper.Serialize(source.Header);
            var target = ProgressClaimCodec.Decode<SoloSaveHeader>(encoded);
            target.Version = SoloSaveValidation.FormatVersion;
            target.ProfileId = own.ProfileId; target.SlotId = Guid.NewGuid().ToString("N"); target.Revision = 1;
            target.SourceProfileId = source.Header.ProfileId; target.SourceSlotId = source.Header.SlotId; target.SourceRevision = source.Header.Revision;
            target.RootProfileId = source.Header.RootProfileId; target.RootSlotId = source.Header.RootSlotId; target.RootRevision = source.Header.RootRevision;
            target.SourceResultId = source.Header.ResultId; target.AuthorizationId = source.Grant.GrantId; target.SavedUtcTicks = DateTime.UtcNow.Ticks;
            target.ClaimedResults = Array.Empty<string>(); target.ClaimSlots = Array.Empty<string>(); target.ClaimRevisions = Array.Empty<int>();
            var receipt = new ProgressCommitReceipt
            {
                Kind = ProgressCommitKind.ImportBranch, ResultId = target.ResultId, GrantId = source.Grant.GrantId,
                ConsumerProfileId = own.ProfileId, ProfileId = own.ProfileId, SlotId = target.SlotId, Revision = 1
            };
            var receipts = ProgressReceiptIndex.Read(repository);
            ProgressReceiptIndex.Add(receipts, receipt);
            // 外部包中的本端消费事实不跨设备继承；只带本设备已经发布的事实。
            target.Receipts = ProgressReceiptIndex.Values(receipts);
            repository.CommitWithReceipt(target, source.Payload);
            Changed(repository);
            return ProgressReceiptValues.Copy(receipt);
        }

        public static byte[] ExportReplace(SoloSaveRepository repository, string slot, OfflineProfileIdentity recipient,
            long? expiresUtcTicks = null, SoloSaveHeader allowedSource = null)
        {
            OfflineProfiles.RequireTrusted(repository, recipient);
            repository.Select(slot, out var target);
            long expiry = expiresUtcTicks ?? DateTime.MaxValue.Ticks;
            CheckExpiry(expiry);
            var root = allowedSource ?? target;
            SoloSaveValidation.Header(root, root.ProfileId);
            return OfflineProfiles.Sign(repository, OfflineProfiles.ReplaceSave, MemoryPackHelper.Serialize(new ReplaceSaveGrantValue
            {
                GrantId = Guid.NewGuid().ToString("N"), ExpiresUtcTicks = expiry,
                TargetProfileId = target.ProfileId, TargetSlotId = target.SlotId, ExpectedRevision = target.Revision,
                RootProfileId = root.RootProfileId, RootSlotId = root.RootSlotId, RootRevision = root.RootRevision,
                RecipientProfileId = recipient.ProfileId, RecipientKeyId = recipient.KeyId
            }));
        }

        public static (ReplaceSaveGrantValue Grant, OfflineProfileIdentity Signer) ReadReplace(byte[] document, SoloSaveRepository repository = null)
        {
            var signed = OfflineProfiles.Verify(document, OfflineProfiles.ReplaceSave, repository);
            var value = ProgressClaimCodec.Decode<ReplaceSaveGrantValue>(signed.Body);
            if (value == null || !SoloSaveValidation.IsIdentifier(value.GrantId) || value.ExpiresUtcTicks <= 0 || value.ExpiresUtcTicks > DateTime.MaxValue.Ticks ||
                value.TargetProfileId != signed.Signer.ProfileId || !SoloSaveValidation.IsIdentifier(value.TargetSlotId) || value.ExpectedRevision <= 0 ||
                !SoloSaveValidation.IsIdentifier(value.RootProfileId) || !SoloSaveValidation.IsIdentifier(value.RootSlotId) || value.RootRevision <= 0 ||
                !SoloSaveValidation.IsIdentifier(value.RecipientProfileId) || !SoloSaveValidation.IsIdentifier(value.RecipientKeyId))
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            return (value, signed.Signer);
        }

        public static ProgressReplacePreview PreviewReplace(SoloSaveRepository repository, string ticketId, byte[] document)
        {
            var ticket = ProgressClaimFiles.Read(repository, ticketId);
            if (ProgressReceiptIndex.Find(repository, ProgressCommitKind.Claim, ticket.Success.SaveResultId, repository.ProfileId) != null)
            {
                throw new InvalidOperationException("AlreadyClaimed");
            }
            ProgressClaimFiles.EnsureOpen(repository, ticket);
            var grant = ReadReplace(document, repository).Grant;
            CheckReplaceScope(repository, ticket, grant);
            repository.SelectOwnerRecord(grant.TargetProfileId, grant.TargetSlotId, out var target);
            if (target.Revision != grant.ExpectedRevision)
            {
                throw new InvalidOperationException("RevisionConflict");
            }
            return new ProgressReplacePreview
            {
                TicketId = ticketId, GrantId = grant.GrantId, ProfileId = target.ProfileId, SlotId = target.SlotId,
                Revision = target.Revision, TargetResultId = target.ResultId, ResultId = ticket.Success.SaveResultId,
                ConsumerProfileId = repository.ProfileId
            };
        }

        public static ProgressCommitReceipt Replace(SoloSaveRepository repository, string ticketId, byte[] document, ProgressReplacePreview confirmed)
        {
            var ticket = ProgressClaimFiles.Read(repository, ticketId);
            var prior = ProgressReceiptIndex.Find(repository, ProgressCommitKind.Claim, ticket.Success.SaveResultId, repository.ProfileId);
            if (prior != null)
            {
                return prior;
            }
            var current = PreviewReplace(repository, ticketId, document);
            if (confirmed == null || confirmed.TicketId != current.TicketId || confirmed.GrantId != current.GrantId ||
                confirmed.ProfileId != current.ProfileId || confirmed.SlotId != current.SlotId || confirmed.Revision != current.Revision ||
                confirmed.TargetResultId != current.TargetResultId || confirmed.ResultId != current.ResultId || confirmed.ConsumerProfileId != current.ConsumerProfileId)
            {
                throw new InvalidOperationException("TargetChanged");
            }
            var grant = ReadReplace(document, repository).Grant;
            repository.SelectOwnerRecord(current.ProfileId, current.SlotId, out var old);
            var receipts = ProgressReceiptIndex.Read(repository);
            foreach (var receipt in old.Receipts)
            {
                ProgressReceiptIndex.Add(receipts, receipt);
            }
            var committed = new ProgressCommitReceipt
            {
                Kind = ProgressCommitKind.Claim, ResultId = ticket.Success.SaveResultId, GrantId = grant.GrantId,
                ConsumerProfileId = repository.ProfileId, ProfileId = old.ProfileId, SlotId = old.SlotId, Revision = checked(old.Revision + 1)
            };
            ProgressReceiptIndex.Add(receipts, committed);
            var target = ProgressClaimCodec.Header(ticket.Success, ticket.Authority, old.ProfileId, old.SlotId, ProgressReceiptIndex.Values(receipts));
            target.Revision = committed.Revision;
            repository.ReplaceWithReceipt(target, ticket.Payload, current.Revision);
            Changed(repository);
            return ProgressReceiptValues.Copy(committed);
        }

        private static void CheckReplaceScope(SoloSaveRepository repository, VerifiedProgressTicket ticket, ReplaceSaveGrantValue grant)
        {
            var own = OfflineProfiles.OwnIdentity(repository);
            var source = ticket.Authority.Source;
            if (grant.RecipientProfileId != own.ProfileId || grant.RecipientKeyId != own.KeyId || source == null ||
                source.RootProfileId != grant.RootProfileId || source.RootSlotId != grant.RootSlotId || source.RootRevision != grant.RootRevision)
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            CheckUsable(repository, grant.GrantId, grant.ExpiresUtcTicks);
        }

        public static void Reject(SoloSaveRepository repository, string grantId)
        {
            string target = RejectionPath(repository, grantId);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target))
            {
                return;
            }
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(grantId);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static string RejectionPath(SoloSaveRepository repository, string grant)
        {
            if (!SoloSaveValidation.IsIdentifier(grant))
            {
                throw new InvalidOperationException("InvalidGrantIdentity");
            }
            return Path.Combine(repository.DirectoryPath, "RejectedGrants", grant + ".ccdeny");
        }

        private static void CheckUsable(SoloSaveRepository repository, string grant, long expiry)
        {
            if (File.Exists(RejectionPath(repository, grant)))
            {
                throw new InvalidOperationException("GrantRejected");
            }
            CheckExpiry(expiry);
        }

        private static void CheckExpiry(long expiry)
        {
            if (expiry <= DateTime.UtcNow.Ticks || expiry > DateTime.MaxValue.Ticks)
            {
                throw new InvalidOperationException("GrantExpired");
            }
        }

        private static void Changed(SoloSaveRepository repository) => EventSystem.Instance.Publish((Scene)repository.IScene, new ProgressInboxChanged());
    }
}
#endif
