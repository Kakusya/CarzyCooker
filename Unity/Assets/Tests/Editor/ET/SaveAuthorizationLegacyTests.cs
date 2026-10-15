using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    // 真实v5的27成员布局，不能用包含第28成员的v6类型伪装旧文件。
    [MemoryPackable]
    public sealed partial class AuthorizationLegacyV5Header : Object
    {
        public int Version { get; set; } = 5;
        public string Platform { get; set; } = "PC";
        public string SlotId { get; set; }
        public string ProfileId { get; set; }
        public int Revision { get; set; } = 1;
        public string ResultId { get; set; }
        public string SourceSlotId { get; set; } = "";
        public int SourceRevision { get; set; }
        public int CompletedLevelId { get; set; }
        public int NextLevelId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public long SavedUtcTicks { get; set; }
        public SoloSaveRecordKind RecordKind { get; set; }
        public int ChapterId { get; set; }
        public int ChapterStage { get; set; }
        public int ServedOrders { get; set; }
        public int UnfulfilledOrders { get; set; }
        public string SourceProfileId { get; set; } = "";
        public string RootProfileId { get; set; }
        public string RootSlotId { get; set; }
        public int RootRevision { get; set; } = 1;
        public string SourceResultId { get; set; } = "";
        public string AuthorizationId { get; set; } = "";
        public string[] ClaimedResults { get; set; }
        public string[] ClaimSlots { get; set; }
        public int[] ClaimRevisions { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        [Test]
        [Category("SaveAuthorizationCompatibility")]
        public void SaveAuthorizationReadsActualV5ReceiptWithoutRewritingAndDuplicateClaimReturnsOriginalMetadata()
        {
            StartSolo(); var payload = CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var fixture = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistTicket(repository, fixture.Ticket, fixture.MatchId);
                var result = ProgressClaimCodec.Ticket(fixture.Ticket, repository).Success;
                var legacy = new AuthorizationLegacyV5Header
                {
                    SlotId = fixture.TicketId, ProfileId = SaveProfileId, ResultId = result.SaveResultId,
                    CompletedLevelId = result.CompletedLevelId, NextLevelId = result.NextLevelId,
                    Score = result.Score, Stars = result.Stars, SavedUtcTicks = result.CompletedUtcTicks,
                    RootProfileId = SaveProfileId, RootSlotId = fixture.TicketId,
                    ClaimedResults = new[] { result.SaveResultId + ":" + SaveProfileId },
                    ClaimSlots = new[] { fixture.TicketId }, ClaimRevisions = new[] { 1 }
                };
                byte[] head = MemoryPackHelper.Serialize(legacy), body = MemoryPackHelper.Serialize(payload);
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length);
                    writer.Write(head); writer.Write(body);
                }
                string path = Path.Combine(repository.DirectoryPath, fixture.TicketId + ".ccsave");
                byte[] bytes = memory.ToArray(); File.WriteAllBytes(path, bytes);
                repository.Select(fixture.TicketId, out var read);
                Assert.That(read.Version, Is.EqualTo(5));
                Assert.That(read.Receipts.Single().ResultId, Is.EqualTo(result.SaveResultId));
                var duplicate = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                Assert.That(duplicate.Kind, Is.EqualTo(ProgressCommitKind.Claim));
                Assert.That(duplicate.SlotId, Is.EqualTo(fixture.TicketId));
                Assert.That(duplicate.Revision, Is.EqualTo(1));
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
