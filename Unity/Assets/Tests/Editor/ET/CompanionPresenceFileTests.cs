using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [TestCase(false), TestCase(true), Category("CompanionPresenceFiles")]
        public void CompanionPresenceActualSoloAndClaimFileKeepStableDefinitionAndResetLevelGrowth(bool cooperative)
        {
            StartContentChapter(cooperative, 1);
            var definition = Tables.Instance.DTCompanionConfig.Get(1);
            Assert.That(Tables.Instance.DTCompanionConfig.DataList.Count, Is.EqualTo(1));
            Assert.That(definition.Name, Is.EqualTo("小麦"));
            Assert.That(new[] { definition.PreparationChat, definition.CooperationChat, definition.ClosingChat }, Has.All.Not.Empty);
            Assert.That(new[] { definition.AskTicks, definition.ClearTicks, definition.WashTicks }, Is.EqualTo(new[] { 30, 20, 61 }));
            Companion original = Restaurant.Children.Values.OfType<Companion>().Single();
            Assert.That(original.DefinitionId, Is.EqualTo(1));
            int definitionId = original.DefinitionId;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(host.Client, repository);
                }
                ContentComplete();
                Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[original.Id], Is.GreaterThan(0));
                RenovationChoose(8);
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] bytes = File.ReadAllBytes(path);
                host.StopProduct(); StartSavedFixture(header, payload); Pump(15);
                Companion restored = Restaurant.Children.Values.OfType<Companion>().Single();
                Assert.That(restored.DefinitionId, Is.EqualTo(definitionId));
                Assert.That(restored.Name, Is.EqualTo("小麦"));
                Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
                Assert.That(CurrentLevel.Children.Values.OfType<CompanionRole>().Single().Companion, Is.EqualTo((EntityRef<Companion>)restored));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
                Assert.That(header.Version, Is.EqualTo(12), "Presentation adds no character save format");
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
