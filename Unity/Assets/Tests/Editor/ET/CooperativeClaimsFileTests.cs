using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private (byte[] Ticket, byte[] Authority, string MatchId, string TicketId) ClaimFileFixture(SoloSaveRepository repository, SoloSavePayload payload)
        {
            var own = OfflineProfiles.OwnIdentity(repository);
            string match = Guid.NewGuid().ToString("N");
            string ticket = Guid.NewGuid().ToString("N");
            var authority = new ProgressAuthorityValue
            {
                AuthorityId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = Guid.NewGuid().ToString("N")
            };
            using var response = (ProgressRepositoryResponse)Finish(repository.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(),
                ProductMessages.Create<ProgressRepositoryRequest>(request =>
                {
                    request.Operation = (int)ProgressRepositoryOperation.PrepareResult;
                    request.MatchId = match;
                    request.Profiles.Add(own.ProfileId);
                    request.Keys.Add(own.KeyId);
                    request.TicketIds.Add(ticket);
                    request.Document = MemoryPackHelper.Serialize(new CooperativeSuccessValue
                    {
                        SaveResultId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = authority.WorldId,
                        CompletedLevelId = CurrentLevel.ConfigId, NextLevelId = CurrentLevel.NextLevelConfigId,
                        Score = CurrentLevel.Score, Stars = CurrentLevel.Stars, CompletedUtcTicks = DateTime.UtcNow.Ticks,
                        Payload = MemoryPackHelper.Serialize(payload), Authority = MemoryPackHelper.Serialize(authority)
                    });
                }), false));
            Assert.That(response.Error, Is.Zero, response.Message);
            byte[] document = (byte[])response.Documents.Single().Clone();
            var verified = ProgressClaimCodec.Ticket(document, repository);
            return (document, verified.Success.Authority, match, ticket);
        }

        private byte[] ClaimCloseFixture(SoloSaveRepository repository, byte[] authority, string match)
        {
            return OfflineProfiles.Sign(repository, OfflineProfiles.NormalClose, MemoryPackHelper.Serialize(new ProgressCloseValue
            {
                ReceiptId = Guid.NewGuid().ToString("N"), MatchId = match, ProfileId = repository.SaveProfile(), Authority = authority
            }));
        }

        [Test]
        [Category("CooperativeClaimsFiles")]
        public void CooperativeClaimAtomicFailureRetryResponseLossAndLaterCheckpointReturnOriginalRecord()
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var fixture = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistTicket(repository, fixture.Ticket, fixture.MatchId);
                string blocked = Path.Combine(repository.DirectoryPath, fixture.TicketId + ".ccsave");
                Directory.CreateDirectory(blocked);
                Assert.Throws<IOException>(() => ProgressClaimFiles.Claim(repository, fixture.TicketId));
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(Directory.EnumerateFiles(repository.DirectoryPath, "*.tmp"), Is.Empty);
                Directory.Delete(blocked);
                var original = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                repository.Select(original.SlotId, out var claimedHeader);
                Assert.That(claimedHeader.Receipts.Single().ResultId, Is.EqualTo(original.ResultId));
                Assert.That(claimedHeader.RootSlotId, Is.EqualTo(original.SlotId));
                string originalSlot = original.SlotId;
                repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(ProgressClaimFiles.Claim(repository, fixture.TicketId).SlotId, Is.EqualTo(originalSlot));
                var checkpoint = CopySaveValue(claimedHeader);
                checkpoint.SlotId = Guid.NewGuid().ToString("N");
                checkpoint.ResultId = Guid.NewGuid().ToString("N");
                checkpoint.SavedUtcTicks += 10;
                repository.Commit(checkpoint, payload);
                Assert.That(ProgressClaimFiles.Claim(repository, fixture.TicketId).SlotId, Is.EqualTo(originalSlot));
                Assert.That(repository.Browse().Length, Is.EqualTo(2));
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Claimed));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsFiles")]
        public void CooperativeClaimCloseBeforeTicketKeepsClosedAndAfterClaimPreservesRecord()
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var first = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistClose(repository, ClaimCloseFixture(repository, first.Authority, first.MatchId), first.MatchId);
                ProgressClaimFiles.PersistTicket(repository, first.Ticket, first.MatchId);
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Closed));
                Assert.That(Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.Claim(repository, first.TicketId)).Message, Is.EqualTo("ClaimClosed"));
                var second = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistTicket(repository, second.Ticket, second.MatchId);
                Assert.That(Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistClose(repository,
                    ClaimCloseFixture(repository, first.Authority, first.MatchId), second.MatchId)).Message, Is.EqualTo("WrongMatch"));
                var saved = ProgressClaimFiles.Claim(repository, second.TicketId);
                ProgressClaimFiles.PersistClose(repository, ClaimCloseFixture(repository, second.Authority, second.MatchId), second.MatchId);
                Assert.That(ProgressClaimFiles.Claim(repository, second.TicketId).SlotId, Is.EqualTo(saved.SlotId));
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsFiles")]
        public void CooperativeClaimRejectsForgedWrongPurposePlatformAndRecipientWithoutFiles()
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var fixture = ClaimFileFixture(repository, payload);
                byte[] forged = (byte[])fixture.Ticket.Clone();
                forged[forged.Length - 1] ^= 1;
                Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistTicket(repository, forged, fixture.MatchId));
                byte[] platform = (byte[])fixture.Ticket.Clone();
                platform[13] = (byte)'X';
                Assert.That(Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistTicket(repository, platform, fixture.MatchId)).Message, Is.EqualTo("WrongPlatform"));
                Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistTicket(repository,
                    ClaimCloseFixture(repository, fixture.Authority, fixture.MatchId), fixture.MatchId));
                var value = ProgressClaimCodec.Ticket(fixture.Ticket, repository).Ticket;
                value.EligibleProfileId = Guid.NewGuid().ToString("N");
                byte[] wrongRecipient = OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(value));
                Assert.That(Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistTicket(repository, wrongRecipient, fixture.MatchId)).Message, Is.EqualTo("IneligibleProfile"));
                Assert.That(ProgressClaimFiles.Browse(repository), Is.Empty);
                Assert.That(repository.Browse(), Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsFiles")]
        public void CooperativeClaimOtherTrustedHostCannotCloseCurrentHostsMatchEvenWithAValidSignature()
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            SoloSaveRepository other = null;
            try
            {
                var fixture = ClaimFileFixture(repository, payload);
                var own = OfflineProfiles.OwnIdentity(repository);
                other = CreateFiber().Root.AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
                var otherIdentity = OfflineProfiles.OwnIdentity(other);
                OfflineProfiles.TrustIdentity(repository, otherIdentity);
                var authority = ProgressClaimCodec.Authority(fixture.Authority, repository).Value;
                authority.AuthorityId = Guid.NewGuid().ToString("N");
                authority.HostProfileId = otherIdentity.ProfileId; authority.HostKeyId = otherIdentity.KeyId;
                byte[] otherAuthority = OfflineProfiles.Sign(other, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(authority));
                byte[] forgedScope = OfflineProfiles.Sign(other, OfflineProfiles.NormalClose, MemoryPackHelper.Serialize(new ProgressCloseValue
                {
                    ReceiptId = Guid.NewGuid().ToString("N"), MatchId = fixture.MatchId, ProfileId = own.ProfileId, Authority = otherAuthority
                }));
                // 验签有效仍不能扩大到当前连接确认的Host；回执先到也不得抢占Match。
                Assert.That(Assert.Throws<InvalidOperationException>(() => ProgressClaimFiles.PersistClose(repository, forgedScope, fixture.MatchId, own)).Message,
                    Is.EqualTo("WrongMatchHost"));
                ProgressClaimFiles.PersistTicket(repository, fixture.Ticket, fixture.MatchId, own);
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(Directory.Exists(Path.Combine(repository.DirectoryPath, "Closures")), Is.False);
            }
            finally { other?.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
