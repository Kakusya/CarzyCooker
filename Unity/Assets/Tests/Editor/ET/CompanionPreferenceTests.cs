using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(CompanionPreferenceState))]
    public sealed partial class ProductSessionTests
    {
        private CompanionPreferenceState PreferenceState => Restaurant.GetComponent<CompanionPreferenceState>();

        private void PreferenceDo(CompanionWorkPreference preference, string rejected = null)
        {
            ClientSessionComponent client = host.Client;
            var task = client.CallProduct(client.GetComponent<GridClientComponent>().CreateCompanionPreference(preference));
            PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            using var reply = (CompanionPreferenceResponse)task.GetAwaiter().GetResult();
            Assert.That(reply.Error == 0, Is.EqualTo(rejected == null), reply.Message);
            Assert.That(reply.Message, Is.EqualTo(rejected));
            if (rejected == null)
            {
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
                Assert.That(PreferenceState.Value, Is.EqualTo(preference));
                Assert.That(client.GetComponent<GridClientComponent>().CompanionPreference, Is.EqualTo(preference));
            }
        }

        [TestCase(CompanionWorkPreference.Automatic), TestCase(CompanionWorkPreference.AskFirst), TestCase(CompanionWorkPreference.WashFirst)]
        [Category("CompanionPreference")]
        public void EntryCompanionPreferenceActualCandidatesFallbackAndPendingApproachingWorkingSwitchKeepOriginalWork(CompanionWorkPreference preference)
        {
            ConfigureFrontFlow(interval: 100000, wait: 2000, firstArrival: 100000);
            StartSolo();
            PreferenceDo(preference);
            LevelDo(LevelControlAction.Start);
            var first = InitialSeatedCustomer(); var second = InitialSeatedCustomer(2, 12);
            var ask = CurrentLevel.GetChild<FrontWork>(first.AskWorkId);
            var askSecond = CurrentLevel.GetChild<FrontWork>(second.AskWorkId);
            var wash = InitialWashTarget();
            FrontWork expected = preference == CompanionWorkPreference.WashFirst ? wash : ask;
            var role = FrontRole;
            role.NextScheduleTick = 0;
            Grid.ScheduleCompanions(CurrentLevel);
            Assert.That(role.RequestInFlight, Is.True, "Original owner is awaiting its own real local claim");
            var opposite = preference == CompanionWorkPreference.WashFirst ? CompanionWorkPreference.AskFirst : CompanionWorkPreference.WashFirst;
            PreferenceDo(opposite);
            Assert.That(expected.State, Is.EqualTo(FrontWorkState.Available), "Preference messages do not advance the business step or replace a queued claim");
            AdvanceTicks(2); PumpMessages(8);
            Assert.That(role.WorkId, Is.EqualTo(expected.Id));
            Assert.That(expected.State, Is.EqualTo(FrontWorkState.Approaching));
            long serial = expected.ClaimSerial;
            var path = role.Path.ToArray(); var next = role.NextCell;
            PreferenceDo(preference);
            Assert.That(role.WorkId, Is.EqualTo(expected.Id));
            Assert.That(expected.ClaimSerial, Is.EqualTo(serial));
            Assert.That(role.Path, Is.EqualTo(path));
            Assert.That(role.NextCell, Is.EqualTo(next));
            FrontWait(() => expected.State == FrontWorkState.Working, 400);
            AdvanceTicks(5);
            double progress = expected.Progress; int frozen = expected.FrozenTicks;
            var growth = CurrentLevel.GetComponent<CompanionGrowth>();
            Assert.That(growth.CompletedByCompanion, Is.Empty);
            PreferenceDo(opposite);
            Assert.That(expected.Progress, Is.EqualTo(progress));
            Assert.That(expected.FrozenTicks, Is.EqualTo(frozen));
            Assert.That(expected.ClaimSerial, Is.EqualTo(serial));
            Assert.That(growth.CompletedByCompanion, Is.Empty);
            LevelDo(LevelControlAction.Pause);
            progress = expected.Progress;
            PreferenceDo(preference, "Paused");
            AdvanceTicks(20);
            Assert.That(expected.Progress, Is.EqualTo(progress));
            Assert.That(PreferenceState.Value, Is.EqualTo(opposite));
            LevelDo(LevelControlAction.Resume);
            FrontWait(() => expected.State == FrontWorkState.Completed, 400);
            // Keep the same selected class after it has run out, so the remaining category must be used.
            PreferenceDo(preference);
            FrontWait(() => ask.State == FrontWorkState.Completed && askSecond.State == FrontWorkState.Completed && wash.State == FrontWorkState.Completed, 1200);
            Assert.That(Grid.Item(wash.ItemId).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Companion companion = role.Companion;
            Assert.That(growth.CompletedByCompanion[companion.Id], Is.EqualTo(3));
            PreferenceDo((CompanionWorkPreference)99, "InvalidCompanionPreference");
            Assert.That(PreferenceState.Value, Is.EqualTo(preference));
        }

        [Test, Category("CompanionPreference")]
        public void EntryCompanionPreferenceActualNaturalCloseNextLevelStaleCommandAndSuccessFileRestoreAutomatic()
        {
            StartContentChapter(false, 1);
            PreferenceDo(CompanionWorkPreference.WashFirst);
            ContentComplete();
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion.Values.Sum(), Is.GreaterThan(0));
            PreferenceDo(CompanionWorkPreference.AskFirst, "WrongPhase");
            RenovationChoose(Renovation.Candidates.First());
            var repository = TestSaveRepository(out string directory);
            try
            {
                var header = ExpansionFile(repository, false, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                string file = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] bytes = File.ReadAllBytes(file);
                var stale = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CreateCompanionPreference(CompanionWorkPreference.AskFirst);
                LevelDo(LevelControlAction.Continue);
                Assert.That(PreferenceState.Value, Is.EqualTo(CompanionWorkPreference.WashFirst));
                Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
                var task = ((ClientSessionComponent)host.Client).CallProduct(stale); PumpMessages(8);
                using var denied = (CompanionPreferenceResponse)task.GetAwaiter().GetResult();
                Assert.That(denied.Message, Is.EqualTo("WrongLevel"));
                Assert.That(PreferenceState.Value, Is.EqualTo(CompanionWorkPreference.WashFirst));
                host.StopProduct(); StartSavedFixture(header, payload); Pump(15);
                Assert.That(PreferenceState.Value, Is.EqualTo(CompanionWorkPreference.Automatic));
                Assert.That(((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CompanionPreference, Is.EqualTo(CompanionWorkPreference.Automatic));
                Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
                Assert.That(File.ReadAllBytes(file), Is.EqualTo(bytes));
                PreferenceDo(CompanionWorkPreference.WashFirst);
                host.StopProduct(); StartSavedFixture(null, null); Pump(15);
                Assert.That(PreferenceState.Value, Is.EqualTo(CompanionWorkPreference.Automatic));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
