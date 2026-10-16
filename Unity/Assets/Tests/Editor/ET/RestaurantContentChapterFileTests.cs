using System;
using System.IO;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantContentChapterFiles")]
        public void RestaurantContentChapterActualCheckpointAndCompletionKeepChosenIdentityAcrossSoloAndClaimReentry(bool cooperative)
        {
            StartContentChapter(cooperative, 2);
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(host.Client, repository);
                }
                ContentComplete(); RenovationChoose(8);
                var first = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(first.SlotId, out first);
                int next = first.NextLevelId; string path = Path.Combine(repository.DirectoryPath, first.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(first.CompletedLevelId)).Id, Is.EqualTo(2));
                Assert.That(RestaurantChapters.Label(next), Does.Contain("进阶小馆").And.Contain("2/3"));
                host.StopProduct(); repository.Dispose(); StartSavedFixture(first, payload); Pump(15);
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(next)); Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(CurrentLevel.ConfigId)).Id, Is.EqualTo(2));
                ContentComplete();
                long level = CurrentLevel.Id;
                using (var response = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.SaveAndContinue)))
                {
                    Assert.That(response.Error, Is.Zero, response.Message);
                }
                PumpUntil(() => CurrentLevel.Id != level, "Actual chapter checkpoint save and third-level continuation", 300, true);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(next + 1));
                ContentComplete();
                using (var response = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.FinishChapter)))
                {
                    Assert.That(response.Error, Is.Zero, response.Message);
                }
                PumpUntil(() => (Scene)host.HostScene == null && repository.Browse().Any(header => header.RecordKind == SoloSaveRecordKind.ChapterCompleted),
                    "Actual terminal publication retires the restaurant", 300, true);
                var completed = repository.Browse().Single(header => header.RecordKind == SoloSaveRecordKind.ChapterCompleted);
                var completePayload = repository.Select(completed.SlotId, out completed);
                Assert.That(completed.ChapterId, Is.EqualTo(cooperative ? 22 : 12)); Assert.That(completed.ChapterStage, Is.EqualTo(3));
                Assert.That(completed.NextLevelId, Is.Zero);
                Assert.That(RestaurantChapters.Label(completed.CompletedLevelId), Does.Contain("进阶小馆").And.Contain("3/3"));
                Assert.That(Assert.Throws<InvalidOperationException>(() => host.InitializeSavedProduct(SaveProfileId, completed, completePayload)).Message, Is.EqualTo("ChapterCompleted"));
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                host.InitializeSavedProduct(SaveProfileId, menuVariant: ProductMenuVariant.Growth, contentChapterId: 1); Pump(15);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1101)); Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Renovation.AppliedStations, Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test, Category("RestaurantContentChapterFiles")]
        public void RestaurantContentChapterActualExistingV9CheckpointKeepsItsOriginalChainAndInventory()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = File.ReadAllBytes(path);
                var selected = repository.Select(header.SlotId, out var read);
                host.StopProduct(); StartSavedFixture(read, selected); Pump(15);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(102)); Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(102)), Is.Null);
                Assert.That(CurrentLevel.AllowedDishIds, Is.EquivalentTo(new[] { 1, 2 }));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.Items.Values.All(value => ((GridItem)value).DefinitionId < 22), Is.True);
                Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
