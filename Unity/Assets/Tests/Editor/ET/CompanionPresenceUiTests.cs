#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private void CompanionPresenceReadChats(CoopUiRemote remote)
        {
            var grid = remote?.Grid ?? Grid;
            var client = remote?.Client ?? Client;
            var presentation = remote?.Presentation ?? Presentation;
            var hud = remote?.Hud ?? Hud;
            long command = client.NextCommandId, tick = grid.Tick, elapsed = grid.LevelElapsedTicks;
            var items = grid.Items.Keys.OrderBy(id => id).ToArray();
            int completed = grid.Companions.Values.Single().CompletedCount;
            var definition = Tables.Instance.DTCompanionConfig.Get(1);
            string[] labels = { "聊聊备菜", "聊聊合作", "聊聊收店" };
            string[] texts = { definition.PreparationChat, definition.CooperationChat, definition.ClosingChat };
            for (int index = 0; index < labels.Length; ++index)
            {
                Pointer(hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == labels[index]));
                Assert.That(hud.DetailsText.text, Does.Contain("小麦").And.Contain(texts[index]));
            }
            Pointer(hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "结束聊天"));
            Assert.That(presentation.CompanionChat, Is.EqualTo(CompanionChatTopic.None));
            Assert.That(client.NextCommandId, Is.EqualTo(command));
            Assert.That(grid.Tick, Is.EqualTo(tick)); Assert.That(grid.LevelElapsedTicks, Is.EqualTo(elapsed));
            Assert.That(grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(grid.Companions.Values.Single().CompletedCount, Is.EqualTo(completed));
            Assert.That(grid.LevelPhase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(grid.Orders, Is.Empty);
        }

        private IEnumerator CompanionPresenceUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartCoopHostLauncher();
            }
            else
            {
                yield return StartLauncher();
            }
            CompanionPresenceReadChats(null);
            var staleChat = CaptureButtonCallback(Button("聊聊备菜"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Companion managed HUD reopens");
            staleChat(); Assert.That(Presentation.CompanionChat, Is.EqualTo(CompanionChatTopic.None));
            yield return CoopFrontUiPrepareSoup();
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "KCP sees same preparing companion");
                CompanionPresenceReadChats(remote);
            }
            try
            {
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(Command("开始营业"), remote); yield return HotUiSwitch(remote, false);
                var grid = remote?.Grid ?? Grid; var presentation = remote?.Presentation ?? Presentation;
                var client = remote?.Client ?? Client;
                yield return HotUiWait(remote, () => grid.Companions.Values.Single().CompletedCount > 0 && !string.IsNullOrEmpty(presentation.CompanionFeedback), "Actual companion work commits before positive feedback", 25);
                Assert.That(presentation.CompanionFeedback, Does.Contain("小麦").And.Contain(Tables.Instance.DTCompanionConfig.Get(1).WorkFeedback));
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(Command("暂停"), remote); yield return HotUiSwitch(remote, false);
                long revision = presentation.CompanionFeedbackRevision;
                int completed = grid.Companions.Values.Single().CompletedCount;
                for (int repeat = 0; repeat < 3; ++repeat)
                {
                    EventSystem.Instance.Publish((Scene)client.IScene, new FrontWorkOutcomeApplied());
                    EventSystem.Instance.Publish((Scene)client.IScene, new GridProjectionApplied { Version = grid.Version });
                }
                Assert.That(presentation.CompanionFeedbackRevision, Is.EqualTo(revision));
                Assert.That(grid.Companions.Values.Single().CompletedCount, Is.EqualTo(completed));
                presentation.CloseGridHud(); yield return null; presentation.OpenGridHud();
                yield return HotUiWait(remote, () => presentation.Ready, "No history replay on operating HUD reopen");
                Assert.That(presentation.CompanionFeedback, Is.Null);
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(Command("恢复"), remote); yield return HotUiSwitch(remote, false);
                yield return HotUiWait(remote, () => grid.Orders.Values.Any(order => order.DishId == 1 && order.State == OrderState.Waiting), "Real soup order");
                var order = grid.Orders.Values.First(value => value.DishId == 1 && value.State == OrderState.Waiting);
                string table = grid.Stations[order.TableStationId].Name;
                yield return HotUiCommand(remote, "拿 碗台 ·", true); yield return HotUiCommand(remote, "前往 " + table);
                yield return HotUiCommand(remote, "交付 番茄蛋花汤 → " + table);
                yield return HotUiWait(remote, () => grid.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Player clear target actually appears", 20);
                long clear = grid.FrontWorks.Values.Single(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available).Id;
                completed = grid.Companions.Values.Single().CompletedCount; revision = presentation.CompanionFeedbackRevision;
                yield return HotUiCommand(remote, "清桌 " + table, true);
                yield return HotUiWait(remote, () => grid.FrontWorks[clear].State == FrontWorkState.Completed, "Player clearing really completes", 20);
                if (grid.Companions.Values.Single().CompletedCount == completed)
                {
                    Assert.That(presentation.CompanionFeedbackRevision, Is.EqualTo(revision), "Player clearing alone creates no companion feedback");
                }
                yield return HotUiWait(remote, () => presentation.CompanionFeedback?.Contains(Tables.Instance.DTCompanionConfig.Get(1).GrowthFeedback) == true, "Third actual work shows existing growth feedback", 35);
                Assert.That(grid.Companions.Values.Single().CompletedCount, Is.GreaterThanOrEqualTo(3));
                Assert.That(grid.Companions.Values.Single().Name, Is.EqualTo("小麦"));
                yield return HotUiSwitch(remote, true);
                yield return ContentUiPump(Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Actual natural close before next level", 90), remote);
                long previous = Grid.LevelId;
                yield return ContentUiPump(Command("下一关"), remote);
                yield return HotUiWait(remote, () => grid.LevelId != previous && grid.LevelPhase == LevelPhase.Preparing, "Both clients enter next level");
                yield return HotUiSwitch(remote, false);
                Assert.That(grid.Companions.Values.Single().Name, Is.EqualTo("小麦"));
                Assert.That(grid.Companions.Values.Single().DefinitionId, Is.EqualTo(1));
                Assert.That(grid.Companions.Values.Single().CompletedCount, Is.Zero);
                Assert.That(presentation.CompanionFeedback, Is.Null);
                staleChat(); Assert.That(presentation.CompanionChat, Is.EqualTo(CompanionChatTopic.None));
                CompanionPresenceReadChats(remote);
            }
            finally
            {
                remote?.Dispose();
            }
        }

        [UnityTest, Category("CompanionPresenceUi"), Timeout(360000)]
        public IEnumerator CompanionPresenceSoloReadOnlyChatCommittedFeedbackReopenAndNextLevel() => CompanionPresenceUiJourney(false);

        [UnityTest, Category("CompanionPresenceUi"), Timeout(360000)]
        public IEnumerator CompanionPresenceKcpSameFactsReadOnlyChatCommittedFeedbackAndNextLevel() => CompanionPresenceUiJourney(true);

        [UnityTest, Category("CompanionPresencePanels")]
        public IEnumerator CompanionPresenceChatUsesActualDetailsCloseAndRecipeSwitchWithoutCommands()
        {
            yield return StartLauncher();
            long command = Client.NextCommandId;
            int items = Grid.Items.Count;
            Pointer(Button("聊聊备菜"));
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf, Is.True);
            Pointer(Button("收起详情"));
            Assert.That(Presentation.CompanionChat, Is.EqualTo(CompanionChatTopic.None));
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf, Is.False);
            Pointer(Button("聊聊合作"));
            Pointer(Button("菜谱 番茄蛋花汤"));
            Assert.That(Presentation.CompanionChat, Is.EqualTo(CompanionChatTopic.None));
            Assert.That(Presentation.RecipeVisible, Is.True);
            Assert.That(Hud.DetailsText.text, Does.Contain("番茄"));
            Pointer(Button("关闭菜谱"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            Assert.That(Grid.Companions.Values.Single().CompletedCount, Is.Zero);
        }
    }
}
#endif
