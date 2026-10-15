using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    // v2-v4实际18成员Header；v1另用既有13成员夹具，不用新Header冒充旧文件。
    [MemoryPackable]
    public sealed partial class ClaimsLegacyHeader : Object
    {
        public int Version { get; set; }
        public string Platform { get; set; } = "PC";
        public string SlotId { get; set; }
        public string ProfileId { get; set; }
        public int Revision { get; set; } = 1;
        public string ResultId { get; set; }
        public string SourceSlotId { get; set; } = "";
        public int SourceRevision { get; set; }
        public int CompletedLevelId { get; set; } = 1;
        public int NextLevelId { get; set; } = 2;
        public int Score { get; set; }
        public int Stars { get; set; }
        public long SavedUtcTicks { get; set; }
        public SoloSaveRecordKind RecordKind { get; set; }
        public int ChapterId { get; set; }
        public int ChapterStage { get; set; }
        public int ServedOrders { get; set; }
        public int UnfulfilledOrders { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [Category("CooperativeClaimsCompatibility")]
        public void CooperativeClaimsV5ReadsActualOldHeaderLayoutsWithoutRewritingOrInventingReceipts(int version)
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var legacy = new ClaimsLegacyHeader
                {
                    Version = version, SlotId = Guid.NewGuid().ToString("N"), ProfileId = SaveProfileId,
                    ResultId = Guid.NewGuid().ToString("N"), SavedUtcTicks = DateTime.UtcNow.Ticks
                };
                byte[] head = version == 1 ? MemoryPackHelper.Serialize(new SoloChapterLegacyHeader
                {
                    SlotId = legacy.SlotId, ProfileId = legacy.ProfileId, ResultId = legacy.ResultId,
                    CompletedLevelId = 1, NextLevelId = 2, SavedUtcTicks = legacy.SavedUtcTicks
                }) : MemoryPackHelper.Serialize(legacy);
                byte[] body = MemoryPackHelper.Serialize(payload);
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body);
                }
                string path = Path.Combine(repository.DirectoryPath, legacy.SlotId + ".ccsave");
                byte[] bytes = memory.ToArray();
                File.WriteAllBytes(path, bytes);
                var restored = repository.Select(legacy.SlotId, out var header);
                Assert.That(header.Version, Is.EqualTo(version));
                Assert.That(header.ClaimedResults, Is.Empty);
                Assert.That(header.ClaimSlots, Is.Empty);
                Assert.That(header.ClaimRevisions, Is.Empty);
                Assert.That(header.RootSlotId, Is.EqualTo(legacy.SlotId));
                Assert.That(restored.Items.Length, Is.EqualTo(payload.Items.Length));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsCompatibility")]
        public void CooperativeClaimLoadedSoloCheckpointCarriesOriginalReceiptAndDoesNotReopenClaim()
        {
            StartSolo();
            var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var fixture = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistTicket(repository, fixture.Ticket, fixture.MatchId);
                var receipt = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                repository.Select(receipt.SlotId, out var claimed);
                repository.Dispose();
                host.StopProduct();
                StartSavedFixture(claimed, payload);
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                CompleteForSave();
                using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.SaveAndContinue)))
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                AdvanceTicks(1);
                PumpUntil(() => SaveResult?.State == SoloSaveState.Committed, "Real Solo save after reading a claimed checkpoint", 200, true);
                var checkpoint = repository.Browse().Single(header => header.SlotId != claimed.SlotId);
                Assert.That(checkpoint.SourceSlotId, Is.EqualTo(claimed.SlotId));
                Assert.That(checkpoint.RootSlotId, Is.EqualTo(claimed.RootSlotId));
                Assert.That(checkpoint.Receipts.Single().SlotId, Is.EqualTo(claimed.Receipts.Single().SlotId));
                Assert.That(checkpoint.Receipts.Single().Revision, Is.EqualTo(claimed.Receipts.Single().Revision));
                Assert.That(ProgressClaimFiles.Claim(repository, fixture.TicketId).SlotId, Is.EqualTo(claimed.SlotId));
                Assert.That(repository.Browse().Length, Is.EqualTo(2));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
