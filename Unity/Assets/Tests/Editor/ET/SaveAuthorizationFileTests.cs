using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private enum AuthorizationScopeConflict { Recipient = 1, Key = 2, Root = 3, Revision = 4 }

        private SoloSaveRepository OtherAuthorizationRepository(string directory)
        {
            return CreateFiber().Root.AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
        }

        private (string TicketId, byte[] Document) AuthorizedFileTicket(SoloSaveRepository recipient, SoloSaveRepository sourceOwner,
            string sourceSlot, SoloSavePayload payload)
        {
            var hostIdentity = OfflineProfiles.OwnIdentity(recipient);
            OfflineProfiles.TrustIdentity(sourceOwner, hostIdentity);
            OfflineProfiles.TrustIdentity(recipient, OfflineProfiles.OwnIdentity(sourceOwner));
            var fixture = ClaimFileFixture(recipient, payload);
            var verified = ProgressClaimCodec.Ticket(fixture.Ticket, recipient);
            var authority = verified.Authority.Value;
            authority.BaselineAuthorization = CooperativeBaselineSystem.Authorize(sourceOwner, sourceSlot, fixture.MatchId, hostIdentity);
            var success = verified.Success;
            success.Authority = OfflineProfiles.Sign(recipient, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(authority));
            var ticket = verified.Ticket;
            ticket.Success = OfflineProfiles.Sign(recipient, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(success));
            byte[] document = OfflineProfiles.Sign(recipient, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(ticket));
            ProgressClaimFiles.PersistTicket(recipient, document, fixture.MatchId, hostIdentity);
            return (ticket.TicketId, document);
        }

        [Test]
        [Category("SaveAuthorizationFiles")]
        public void SaveAuthorizationImportCreatesIndependentOwnerLineageAndContinuesAfterSourceOwnerGoesOffline()
        {
            StartSolo(); var payload = CompleteForSave();
            var owner = TestSaveRepository(out string directory);
            SoloSaveRepository receiver = null;
            try
            {
                var source = SaveHeader(); owner.Commit(source, payload);
                string sourcePath = Path.Combine(owner.DirectoryPath, source.SlotId + ".ccsave");
                byte[] sourceBytes = File.ReadAllBytes(sourcePath);
                receiver = OtherAuthorizationRepository(directory);
                OfflineProfiles.TrustIdentity(receiver, OfflineProfiles.OwnIdentity(owner));
                byte[] document = SaveAuthorizationFiles.ExportImport(owner, source.SlotId);
                var receipt = SaveAuthorizationFiles.Import(receiver, document);
                Assert.That(receipt.ProfileId, Is.EqualTo(receiver.SaveProfile()));
                Assert.That(receipt.ProfileId, Is.Not.EqualTo(source.ProfileId));
                Assert.That(receipt.SlotId, Is.Not.EqualTo(source.SlotId));
                Assert.That(SaveAuthorizationFiles.Import(receiver, document).SlotId, Is.EqualTo(receipt.SlotId));
                var restored = receiver.Select(receipt.SlotId, out var branch);
                Assert.That(branch.SourceProfileId, Is.EqualTo(source.ProfileId));
                Assert.That(branch.SourceSlotId, Is.EqualTo(source.SlotId));
                Assert.That(branch.RootSlotId, Is.EqualTo(source.RootSlotId));
                Assert.That(branch.AuthorizationId, Is.EqualTo(receipt.GrantId));
                Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(sourceBytes));
                string profile = receiver.SaveProfile();
                owner.Dispose(); owner = null;
                receiver.Dispose(); receiver = null;
                host.StopProduct();
                var config = Game.RuntimeInstanceConfig.Create("authorized-branch-test", "test", "solo", null);
                Game.RuntimeInstanceContext.Reset(); Game.RuntimeInstanceContext.Prepare(config);
                var fiber = CreateFiber();
                host = fiber.Root.AddComponent<ProductRuntimeComponent, Game.RuntimeInstanceConfig>(config);
                receiver = fiber.Root.AddComponent<SoloSaveRepository, string, string>(directory, profile);
                host.InitializeSavedProduct(profile, branch, restored);
                PumpUntil(() => ((ClientSessionComponent)host.Client)?.GetComponent<GridClientComponent>().Confirmed == true,
                    "Imported branch enters a real independent restaurant", 120, true);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(branch.NextLevelId));
                CompleteForSave();
                using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.SaveAndContinue)))
                {
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                }
                AdvanceTicks(1);
                PumpUntil(() => SaveResult?.State == SoloSaveState.Committed, "Imported Owner independently saves next checkpoint", 200, true);
                Assert.That(receiver.Browse().Length, Is.EqualTo(2));
                Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(sourceBytes));
                Assert.That(SaveAuthorizationFiles.Import(receiver, document).SlotId, Is.EqualTo(receipt.SlotId));
            }
            finally { receiver?.Dispose(); owner?.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("SaveAuthorizationFiles")]
        public void SaveAuthorizationReadRunExpiryLocalRejectionPlatformAndSignatureNeverCreateBranch()
        {
            StartSolo(); var payload = CompleteForSave();
            var owner = TestSaveRepository(out string directory);
            SoloSaveRepository receiver = null;
            try
            {
                var source = SaveHeader(); owner.Commit(source, payload);
                receiver = OtherAuthorizationRepository(directory);
                var identity = OfflineProfiles.OwnIdentity(receiver);
                OfflineProfiles.TrustIdentity(owner, identity);
                OfflineProfiles.TrustIdentity(receiver, OfflineProfiles.OwnIdentity(owner));
                byte[] readRun = CooperativeBaselineSystem.Authorize(owner, source.SlotId, Guid.NewGuid().ToString("N"), identity);
                Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Import(receiver, readRun));
                byte[] document = SaveAuthorizationFiles.ExportImport(owner, source.SlotId);
                var value = SaveAuthorizationFiles.ReadImport(document, receiver).Grant;
                value.ExpiresUtcTicks = DateTime.UtcNow.AddDays(-1).Ticks;
                byte[] expired = OfflineProfiles.Sign(owner, OfflineProfiles.ImportBranch, MemoryPackHelper.Serialize(value));
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Import(receiver, expired)).Message, Is.EqualTo("GrantExpired"));
                byte[] platform = (byte[])document.Clone(); platform[13] = (byte)'A';
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Import(receiver, platform)).Message, Is.EqualTo("WrongPlatform"));
                byte[] forged = (byte[])document.Clone(); forged[forged.Length - 1] ^= 1;
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Import(receiver, forged)).Message, Is.EqualTo("InvalidSignature"));
                string rejectedId = SaveAuthorizationFiles.ReadImport(document).Grant.GrantId;
                SaveAuthorizationFiles.Reject(receiver, rejectedId);
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Import(receiver, document)).Message, Is.EqualTo("GrantRejected"));
                Assert.That(receiver.Browse(), Is.Empty);
                Assert.That(owner.Browse().Length, Is.EqualTo(1));
                Assert.That(Directory.EnumerateFiles(receiver.DirectoryPath, "*.tmp", SearchOption.AllDirectories), Is.Empty);
            }
            finally { receiver?.Dispose(); owner.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("SaveAuthorizationFiles")]
        public void SaveAuthorizationForeignOwnerAtomicReplaceFailureRetryAndLaterReplaceRetainOriginalReceiptWithoutBackup()
        {
            StartSolo(); var payload = CompleteForSave();
            var owner = TestSaveRepository(out string directory);
            SoloSaveRepository receiver = null;
            try
            {
                var target = SaveHeader(); owner.Commit(target, payload);
                receiver = OtherAuthorizationRepository(directory);
                var ticket = AuthorizedFileTicket(receiver, owner, target.SlotId, payload);
                byte[] grant = SaveAuthorizationFiles.ExportReplace(owner, target.SlotId, OfflineProfiles.OwnIdentity(receiver));
                var preview = SaveAuthorizationFiles.PreviewReplace(receiver, ticket.TicketId, grant);
                string path = Path.Combine(owner.DirectoryPath, target.SlotId + ".ccsave");
                byte[] old = File.ReadAllBytes(path);
                // 允许重新读取，禁止删除/重命名：真实失败发生在已暂存后的File.Replace发布点。
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Assert.Throws<IOException>(() => SaveAuthorizationFiles.Replace(receiver, ticket.TicketId, grant, preview));
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(old));
                    Assert.That(ProgressClaimFiles.Browse(receiver).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                }
                Assert.That(Directory.EnumerateFiles(owner.DirectoryPath, "*.tmp"), Is.Empty);
                var first = SaveAuthorizationFiles.Replace(receiver, ticket.TicketId, grant, preview);
                Assert.That(first.ProfileId, Is.EqualTo(owner.SaveProfile()));
                Assert.That(first.Revision, Is.EqualTo(2));
                Assert.That(receiver.Browse(), Is.Empty, "Foreign target keeps its Owner rather than becoming receiver's record");
                owner.Select(target.SlotId, out var replaced);
                Assert.That(replaced.ProfileId, Is.EqualTo(target.ProfileId));
                Assert.That(replaced.Revision, Is.EqualTo(2));
                var secondTicket = AuthorizedFileTicket(receiver, owner, target.SlotId, payload);
                byte[] nextGrant = SaveAuthorizationFiles.ExportReplace(owner, target.SlotId, OfflineProfiles.OwnIdentity(receiver));
                var second = SaveAuthorizationFiles.Replace(receiver, secondTicket.TicketId, nextGrant,
                    SaveAuthorizationFiles.PreviewReplace(receiver, secondTicket.TicketId, nextGrant));
                Assert.That(second.Revision, Is.EqualTo(3));
                var repeated = ProgressClaimFiles.Claim(receiver, ticket.TicketId);
                Assert.That(repeated.SlotId, Is.EqualTo(first.SlotId));
                Assert.That(repeated.Revision, Is.EqualTo(2), "Immutable receipt is returned even though current target is now revision3");
                Assert.That(SaveAuthorizationFiles.Replace(receiver, ticket.TicketId, grant, preview).Revision, Is.EqualTo(2));
                owner.Select(target.SlotId, out var actual);
                Assert.That(actual.Revision, Is.EqualTo(3));
                Assert.That(actual.Receipts.Length, Is.EqualTo(2));
                Assert.That(Directory.EnumerateFiles(owner.DirectoryPath, "*.ccsave").Count(), Is.EqualTo(1));
                Assert.That(Directory.EnumerateFiles(owner.DirectoryPath, "*.bak", SearchOption.AllDirectories), Is.Empty);
                Assert.That(Directory.EnumerateFiles(owner.DirectoryPath, "*.tmp", SearchOption.AllDirectories), Is.Empty);
            }
            finally { receiver?.Dispose(); owner.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("SaveAuthorizationFiles")]
        public void SaveAuthorizationReplacementScopeRevisionAndStaleConfirmationRejectWithoutTouchingTargetOrEligibility()
        {
            StartSolo(); var payload = CompleteForSave();
            var owner = TestSaveRepository(out string directory);
            SoloSaveRepository receiver = null;
            try
            {
                var target = SaveHeader(); owner.Commit(target, payload);
                receiver = OtherAuthorizationRepository(directory);
                var ticket = AuthorizedFileTicket(receiver, owner, target.SlotId, payload);
                byte[] document = SaveAuthorizationFiles.ExportReplace(owner, target.SlotId, OfflineProfiles.OwnIdentity(receiver));
                string path = Path.Combine(owner.DirectoryPath, target.SlotId + ".ccsave");
                byte[] old = File.ReadAllBytes(path);
                var grant = SaveAuthorizationFiles.ReadReplace(document, receiver).Grant;
                foreach (var conflict in new[] { AuthorizationScopeConflict.Recipient, AuthorizationScopeConflict.Key,
                    AuthorizationScopeConflict.Root, AuthorizationScopeConflict.Revision })
                {
                    var invalid = CopySaveValue(grant);
                    switch (conflict)
                    {
                        case AuthorizationScopeConflict.Recipient: invalid.RecipientProfileId = Guid.NewGuid().ToString("N"); break;
                        case AuthorizationScopeConflict.Key: invalid.RecipientKeyId = Guid.NewGuid().ToString("N"); break;
                        case AuthorizationScopeConflict.Root: invalid.RootSlotId = Guid.NewGuid().ToString("N"); break;
                        case AuthorizationScopeConflict.Revision: invalid.ExpectedRevision = 2; break;
                    }
                    byte[] signed = OfflineProfiles.Sign(owner, OfflineProfiles.ReplaceSave, MemoryPackHelper.Serialize(invalid));
                    Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.PreviewReplace(receiver, ticket.TicketId, signed));
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(old));
                }
                var preview = SaveAuthorizationFiles.PreviewReplace(receiver, ticket.TicketId, document);
                var stale = CopySaveValue(preview); stale.TargetResultId = Guid.NewGuid().ToString("N");
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Replace(receiver, ticket.TicketId, document, stale)).Message, Is.EqualTo("TargetChanged"));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(old));
                Assert.That(ProgressClaimFiles.Browse(receiver).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(Directory.EnumerateFiles(owner.DirectoryPath, "*.tmp"), Is.Empty);
                var otherTicket = AuthorizedFileTicket(receiver, owner, target.SlotId, payload);
                SaveAuthorizationFiles.Replace(receiver, otherTicket.TicketId, document,
                    SaveAuthorizationFiles.PreviewReplace(receiver, otherTicket.TicketId, document));
                byte[] newer = File.ReadAllBytes(path);
                Assert.That(Assert.Throws<InvalidOperationException>(() => SaveAuthorizationFiles.Replace(receiver, ticket.TicketId, document, preview)).Message,
                    Is.EqualTo("RevisionConflict"));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(newer));
                Assert.That(ProgressClaimFiles.Browse(receiver).Single(row => row.TicketId == ticket.TicketId).State, Is.EqualTo(ProgressReceiptState.Available));
            }
            finally { receiver?.Dispose(); owner.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
