using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    // Exact original member order, used to produce a real pre-chapter binary file.
    [MemoryPackable]
    public sealed partial class SoloChapterLegacyHeader : Object
    {
        public int Version { get; set; } = 1;
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
    }

    public sealed partial class ProductSessionTests
    {
        private void ChapterNaturalComplete()
        {
            if (CurrentLevel.Phase == LevelPhase.Preparing) LevelDo(LevelControlAction.Start);
            for (int i = 0; i < 2200 && CurrentLevel.Phase != LevelPhase.Completed; ++i) { AdvanceTicks(1); PumpMessages(3); }
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(CurrentLevel.Children.Values.OfType<Order>().Count(o => o.State == OrderState.Served)));
            Assert.That(CurrentLevel.UnfulfilledOrders, Is.EqualTo(CurrentLevel.Children.Values.OfType<Order>().Count(o => o.State == OrderState.Expired)));
        }

        [Test]
        public void SoloChapterConfigExplicitThreeStagesAndMissingNextNeverBecomeTerminal()
        {
            var rows = Tables.Instance.DTLevelConfig.DataList;
            RestaurantConfig.ValidateLevelDefinitions(rows, GridKitchenSystem.IsInitialGridWalkable);
            var chapter = rows.Where(row => row.ChapterId == 1 && row.MenuVariant == 0).OrderBy(row => row.ChapterStage).ToArray();
            Assert.That(chapter.Select(row => row.Id), Is.EqualTo(new[] { 101, 102, 103 }));
            Assert.That(chapter.Select(row => row.NextLevelConfigId), Is.EqualTo(new[] { 102, 103, 0 }));
            Assert.That(chapter.Select(row => row.IsChapterTerminal), Is.EqualTo(new[] { false, false, true }));
            Assert.That(chapter.All(row => row.AllowedDishIds.SequenceEqual(new[] { 1, 2 })), Is.True);
            Assert.That(chapter.Last().AllowedRenovationIds, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => RestaurantConfig.ValidateLevelDefinitions(rows.Where(row => row.Id != 102), GridKitchenSystem.IsInitialGridWalkable));
            Assert.That(Tables.Instance.DTLevelConfig.Get(2).NextLevelConfigId, Is.EqualTo(2));
            StartHost(); Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1)); Assert.That(CurrentLevel.IsChapterTerminal, Is.False);
        }

        [Test]
        public void SoloChapterThreeNaturalZeroStarLevelsTerminalRejectsContinueAndNoRenovation()
        {
            StartSolo(liveFront: true);
            for (int stage = 1; stage <= 3; ++stage)
            {
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(100 + stage));
                ChapterNaturalComplete(); Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.Stars, Is.Zero);
                Assert.That(CurrentLevel.UnfulfilledOrders, Is.GreaterThan(0));
                if (stage < 3) { LevelDo(LevelControlAction.Continue); continue; }
                long level = CurrentLevel.Id; int count = CurrentLevel.UnfulfilledOrders;
                var client = (ClientSessionComponent)host.Client;
                using (var rejected = LevelReply(client, client.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.Continue)))
                    Assert.That(rejected.Message, Is.EqualTo("ChapterCompleted"));
                AdvanceTicks(30); PumpMessages(8);
                Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(CurrentLevel.UnfulfilledOrders, Is.EqualTo(count));
                Assert.That(Renovation.Candidates, Is.Empty); Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
                Assert.That(client.GetComponent<GridClientComponent>().UnfulfilledOrders, Is.EqualTo(count));
            }
        }

        [Test]
        public void SoloChapterTerminalFileFailureRetryCommitsOnceAndReturnsWithRootSenderAlive()
        {
            StartSolo();
            for (int i = 0; i < 2; ++i) { ChapterNaturalComplete(); LevelDo(LevelControlAction.Continue); }
            ChapterNaturalComplete(); BindCurrentSave();
            var restaurant = Restaurant; var kitchen = Grid; var sender = host.Root().GetComponent<ProcessInnerSender>();
            var repository = TestSaveRepository(out string directory); string realPath = repository.DirectoryPath;
            try
            {
                string blocker = Path.Combine(realPath, "blocked"); File.WriteAllText(blocker, "owned test blocker"); repository.DirectoryPath = blocker;
                void Finish()
                {
                    var client = (ClientSessionComponent)host.Client;
                    using var reply = LevelReply(client, client.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.FinishChapter));
                    Assert.That(reply.Error, Is.Zero, reply.Message); AdvanceTicks(1); PumpMessages(8);
                }
                Finish(); SoloSaveResult result = SaveResult;
                Assert.That(result, Is.Not.Null, FrontPeer.LastCommandReason);
                byte[] immutable = (byte[])result.Record.Clone(); string resultId = result.ResultId;
                AdvanceTicks(1); PumpMessages(8);
                Assert.That(result.State, Is.EqualTo(SoloSaveState.Failed)); Assert.That(CurrentLevel.ConfigId, Is.EqualTo(103));
                Assert.That(restaurant.IsDisposed, Is.False); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
                repository.DirectoryPath = realPath; Finish(); Assert.That(SaveResult.ResultId, Is.EqualTo(resultId));
                Assert.That(SaveResult.Record, Is.EqualTo(immutable));
                AdvanceTicks(2); PumpMessages(12);
                Assert.That(restaurant.IsDisposed && kitchen.IsDisposed, Is.True);
                Assert.That((Scene)host.HostScene, Is.Null); Assert.That((Scene)host.ClientScene, Is.Null);
                Assert.That(sender.IsStopped, Is.False); Assert.That(host.Root().GetComponent<SoloSaveRepository>(), Is.SameAs(repository));
                var header = repository.Browse().Single(); Assert.That(header.RecordKind, Is.EqualTo(SoloSaveRecordKind.ChapterCompleted));
                Assert.That(header.NextLevelId, Is.Zero); Assert.That(header.Version, Is.EqualTo(SoloSaveValidation.FormatVersion)); Assert.That(header.ResultId, Is.EqualTo(resultId));
                var payload = repository.Select(header.SlotId, out var selected);
                Assert.Throws<InvalidOperationException>(() => host.InitializeSavedProduct(SaveProfileId, selected, payload));
                Assert.That((Scene)host.HostScene, Is.Null);
                host.InitializeProduct(); Pump(8); Assert.That(CurrentLevel.ConfigId, Is.EqualTo(101));
                Assert.That(Grid.WorldId, Is.Not.EqualTo(kitchen.WorldId)); Assert.That(repository.Browse().Length, Is.EqualTo(1));
            }
            finally { repository.DirectoryPath = realPath; repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloChapterReadsActualThirteenMemberV1FileWithoutChangingOriginalNextSemantics()
        {
            StartSolo(); var payload = CompleteForSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                var legacy = new SoloChapterLegacyHeader
                {
                    SlotId = Guid.NewGuid().ToString("N"), ResultId = Guid.NewGuid().ToString("N"), ProfileId = SaveProfileId,
                    CompletedLevelId = 1, NextLevelId = 2, SavedUtcTicks = DateTime.UtcNow.Ticks
                };
                byte[] head = MemoryPackHelper.Serialize(legacy), body = MemoryPackHelper.Serialize(payload);
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                { writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body); }
                byte[] original = memory.ToArray(); string path = Path.Combine(repository.DirectoryPath, legacy.SlotId + ".ccsave");
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) file.Write(original, 0, original.Length);
                Assert.That(repository.Browse().Single().Version, Is.EqualTo(1));
                var restored = repository.Select(legacy.SlotId, out var selected);
                Assert.That(selected.RecordKind, Is.EqualTo(SoloSaveRecordKind.Continue)); Assert.That(selected.NextLevelId, Is.EqualTo(2));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                host.StopProduct(); StartSavedFixture(selected, restored); PumpMessages(8);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(2)); Assert.That(CurrentLevel.IsChapterTerminal, Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
