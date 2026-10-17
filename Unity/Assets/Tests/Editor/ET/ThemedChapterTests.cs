using System;
using System.IO;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test, Category("ThemedChapterClaims")]
        public void ThemedChapterActualHostAndKcpBothClaimEachSharedResultAndTerminalCompletionOnce()
        {
            StartRoomFixture(); UseOptionalBuffFixture();
            var repository = TestSaveRepository(out string directory);
            SoloSaveRepository other = null;
            try
            {
                ProveFixtureProfile(host.Client, repository);
                var remote = JoinRoomFixture();
                other = remote.Root().AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
                OfflineProfiles.TrustIdentity(repository, OfflineProfiles.OwnIdentity(other));
                OfflineProfiles.TrustIdentity(other, OfflineProfiles.OwnIdentity(repository));
                ProveFixtureProfile(remote, other); ContentRoomStart(3);
                for (int stage = 1; stage <= 3; ++stage)
                {
                    int completed = 2800 + stage;
                    ContentComplete();
                    if (stage == 1) { RenovationChoose(8); }
                    PumpUntil(() => ProgressClaimFiles.Browse(repository).Any(ticket => ticket.CompletedLevelId == completed) &&
                        ProgressClaimFiles.Browse(other).Any(ticket => ticket.CompletedLevelId == completed), "Both actual profiles receive themed shared result", 300, true);
                    var ownTicket = ProgressClaimFiles.Browse(repository).Single(ticket => ticket.CompletedLevelId == completed);
                    var peerTicket = ProgressClaimFiles.Browse(other).Single(ticket => ticket.CompletedLevelId == completed);
                    var own = ProgressClaimFiles.Claim(repository, ownTicket.TicketId);
                    var peer = ProgressClaimFiles.Claim(other, peerTicket.TicketId);
                    Assert.That(own.ResultId, Is.EqualTo(peer.ResultId)); Assert.That(own.SlotId, Is.Not.EqualTo(peer.SlotId));
                    Assert.That(ProgressClaimFiles.Claim(repository, ownTicket.TicketId).SlotId, Is.EqualTo(own.SlotId));
                    Assert.That(ProgressClaimFiles.Claim(other, peerTicket.TicketId).SlotId, Is.EqualTo(peer.SlotId));
                    repository.Select(own.SlotId, out var header); other.Select(peer.SlotId, out var peerHeader);
                    Assert.That(header.ChapterId, Is.EqualTo(23)); Assert.That(peerHeader.ChapterId, Is.EqualTo(23));
                    Assert.That(header.RecordKind, Is.EqualTo(stage == 3 ? SoloSaveRecordKind.ChapterCompleted : SoloSaveRecordKind.Continue));
                    if (stage < 3)
                    {
                        long previous = CurrentLevel.Id;
                        using (var response = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
                        {
                            Assert.That(response.Error, Is.Zero, response.Message);
                        }
                        PumpUntil(() => CurrentLevel.Id != previous, "Root repository finishes shared continuation before installing the next stage", 300, true);
                        Pump(20);
                    }
                }
                Assert.That(repository.Browse().Length, Is.EqualTo(3)); Assert.That(other.Browse().Length, Is.EqualTo(3));
            }
            finally { other?.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test, Category("ThemedChapterCore")]
        public void ThemedChapterThreeConfiguredRoutesHaveClosedMenusAndKeepOriginalChapterIdentities()
        {
            RestaurantContentChapterConfigHasTwoThreeStageRoutesInBothModesWithoutChangingOldReferences();
            Assert.That(Tables.Instance.DTRestaurantChapterConfig.DataList.Count, Is.EqualTo(3));
            var old = Tables.Instance.DTRestaurantChapterConfig.Get(1);
            Assert.That(new[] { old.SoloChapterId, old.CooperativeChapterId, old.SoloFirstLevelId, old.CooperativeFirstLevelId }, Is.EqualTo(new[] { 11, 21, 1101, 2101 }));
            old = Tables.Instance.DTRestaurantChapterConfig.Get(2);
            Assert.That(new[] { old.SoloChapterId, old.CooperativeChapterId, old.SoloFirstLevelId, old.CooperativeFirstLevelId }, Is.EqualTo(new[] { 12, 22, 1501, 2501 }));
            Assert.That(ProductMenuCatalog.Includes(ProductMenuVariant.SteamTea, ProductMenuVariant.Cafe), Is.False);
            Assert.That(ProductMenuCatalog.SupportsDrinks(ProductMenuVariant.SteamTea), Is.True);
            foreach (int first in new[] { 1801, 2801 })
            {
                for (int stage = 0; stage < 3; ++stage)
                {
                    var level = Tables.Instance.DTLevelConfig.Get(first + stage);
                    Assert.That(level.AllowedDishIds, Is.EqualTo(new[] { 12 }));
                    Assert.That(level.AllowedDrinkIds, Is.EqualTo(Enumerable.Range(13, stage + 1).ToArray()));
                    Assert.That(RestaurantChapters.Find(level).Id, Is.EqualTo(3));
                }
            }
        }

        [TestCase(false), TestCase(true), Category("ThemedChapterCore")]
        public void ThemedChapterActualSoloAndKcpZeroStarThreeStagesKeepOneKitchenAndRejectFourthLevel(bool cooperative)
        {
            RestaurantContentChapterRealLocalAndKcpSelectThreeLevelsAndReplayAsAnIndependentKitchen(cooperative, 3);
        }

        [TestCase(false), TestCase(true), Category("ThemedChapterFiles")]
        public void ThemedChapterActualSoloAndSignedClaimCheckpointsCarryTeaMilkAndTerminalRecordWithoutRefill(bool cooperative)
        {
            StartContentChapter(cooperative, 3);
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative) { ProveFixtureProfile(host.Client, repository); }
                TeaBrew(host.Client); ContentComplete(); RenovationChoose(8);
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload); Pump(15);
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2802 : 1802));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(TeaStation(50).ItemId, Is.Zero, "Used leaf stays consumed across the new chapter checkpoint");
                Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
                var tea = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single();
                Assert.That(tea.RemainingPortions, Is.EqualTo(3));
                TeaMilk(host.Client, tea);
                ContentComplete();
                long retiredLevel = CurrentLevel.Id;
                using (var response = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.SaveAndContinue)))
                {
                    Assert.That(response.Error, Is.Zero, response.Message);
                }
                PumpUntil(() => CurrentLevel.Id != retiredLevel, "Actual second checkpoint installs the third stage once", 300, true);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2803 : 1803));
                Assert.That(TeaStation(52).ItemId, Is.Zero); Assert.That(TeaStation(53).ItemId, Is.Zero);
                Assert.That(Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single(value => value.RequiredCapability == GridStationCapability.MixTea).RemainingPortions, Is.EqualTo(2));
                ContentComplete(); Assert.That(Renovation.Candidates, Is.Empty);
                using (var response = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.FinishChapter)))
                {
                    Assert.That(response.Error, Is.Zero, response.Message);
                }
                PumpUntil(() => (Scene)host.HostScene == null && repository.Browse().Any(value => value.RecordKind == SoloSaveRecordKind.ChapterCompleted), "Terminal theme publication returns to selector", 300, true);
                var completed = repository.Browse().Single(value => value.RecordKind == SoloSaveRecordKind.ChapterCompleted);
                var end = repository.Select(completed.SlotId, out completed);
                Assert.That(completed.ChapterId, Is.EqualTo(cooperative ? 23 : 13));
                Assert.That(completed.NextLevelId, Is.Zero); Assert.That(completed.ChapterStage, Is.EqualTo(3));
                Assert.That(Assert.Throws<InvalidOperationException>(() => host.InitializeSavedProduct(SaveProfileId, completed, end)).Message, Is.EqualTo("ChapterCompleted"));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                host.InitializeSavedProduct(SaveProfileId, menuVariant: ProductMenuVariant.SteamTea, contentChapterId: 3); Pump(15);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1801));
                Assert.That(TeaStation(50).ItemId, Is.Not.Zero); Assert.That(Grid.AutoProcesses, Is.Empty);
                Assert.That(Renovation.AppliedStations, Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
