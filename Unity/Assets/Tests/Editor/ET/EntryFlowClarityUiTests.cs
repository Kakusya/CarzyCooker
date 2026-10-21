#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("EntryFlowClarity")]
        public IEnumerator EntryFlowHomeHasThreePrimaryActionsAndRetainsSecondaryNavigation()
        {
            yield return StartModeLauncher();
            yield return Wait(() => Entry.RecordState != SoloRecordReadState.Loading, "Home read finishes without a world");
            var home = Runtime;
            string[] main = EntryHud.ControlPanelRectTransform.GetComponentsInChildren<Button>(false)
                .Select(button => button.GetComponentInChildren<Text>().text).ToArray();
            Assert.That(main, Is.EqualTo(new[] { "开始新游戏", "继续游戏", "合作游玩" }));
            var retired = CaptureButtonCallback(EntryButton("开始新游戏"));
            yield return ReachAndClick(EntryButton("设置"));
            Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Settings));
            Assert.That(EntryButton("本机设置"), Is.Not.Null);
            Assert.That(EntryButton("声音"), Is.Not.Null);
            retired();
            Assert.That(Selection, Is.Null);
            yield return ReachAndClick(EntryButton("返回开始页"));
            yield return ReachAndClick(EntryButton("进度管理"));
            Assert.That(EntryButton("合作进度领取"), Is.Not.Null);
            Assert.That(EntryButton("存档与授权"), Is.Not.Null);
            yield return ReachAndClick(EntryButton("返回开始页"));
            yield return ReachAndClick(EntryButton("更多玩法"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "More play reaches the real menu trials");
            Assert.That(Selection.Page, Is.EqualTo(SoloEntryPage.TrialMenus));
            Assert.That(SelectionButton("内存试玩"), Is.Not.Null);
            Assert.That((Scene)home.HostScene, Is.Null);
            Assert.That((ClientSessionComponent)home.Client, Is.Null);
            string missing = Path.Combine(Path.GetTempPath(), "cc-entry-readonly-" + System.Guid.NewGuid().ToString("N"));
            home.Root().RemoveComponent<SoloSaveRepository>();
            Selection.InitializePreferences(missing);
            Assert.That(home.Root().GetComponent<SoloSaveRepository>(), Is.Null);
            Assert.That(Directory.Exists(missing), Is.False, "Reading default preferences never creates a local identity or directory");
        }

        [UnityTest, Category("EntryFlowClarity")]
        public IEnumerator EntryFlowInlineChaptersCancelRetiredChoiceAndStartExactChapter()
        {
            yield return StartModeLauncher();
            var preferences = CreateSaveUiRepository(Runtime, out _, out _);
            preferences.Preferences.FontSize = ProductFontSize.Largest;
            yield return ReachAndClick(EntryButton("开始新游戏"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Inline chapter page");
            Assert.That(Selection.Page, Is.EqualTo(SoloEntryPage.Chapters));
            Assert.That(Selection.PreviewChapterId, Is.EqualTo(1));
            var viewport = (RectTransform)SelectionHud.transform;
            var preview = SelectionHud.SettlementPanelRectTransform;
            Assert.That(preview.anchorMin.x > .4f, Is.EqualTo(viewport.rect.width > 900 && viewport.rect.width > viewport.rect.height),
                "Portrait canvas logical width must not activate the wide two-column preview");
            SelectionHud.ApplyLocalFontSize(ProductFontSize.Largest);
            SelectionHud.ApplySafeArea(new Rect(12, 18, Screen.width - 24, Screen.height - 36), new Vector2(Screen.width, Screen.height));
            Assert.That(((RectTransform)SelectionHud.transform).anchorMin.x, Is.GreaterThan(0));
            foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList)
            {
                if (Selection.PreviewChapterId != chapter.Id)
                {
                    yield return ReachAndClick(SelectionButton("选择 · " + chapter.Name));
                }
                Assert.That(SelectionHud.SettlementText.text, Does.Contain(chapter.Name).And.Contain("共 3 关"));
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                Assert.That(SelectionHud.SettlementText.fontSize, Is.GreaterThan(24), "The saved largest font remains active after selecting another chapter");
            }
            var retired = CaptureButtonCallback(SelectionButton("确认开始"));
            var oldRuntime = Runtime;
            yield return ReachAndClick(SelectionButton("返回开始方式"));
            yield return Wait(() => Runtime != oldRuntime && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Cancel returns to the true home");
            ownedInit = Init.Instance;
            retired();
            Assert.That((Scene)Runtime.HostScene, Is.Null);
            yield return ReachAndClick(EntryButton("开始新游戏"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Fresh inline preview");
            yield return ReachAndClick(SelectionButton("选择 · 进阶小馆"));
            var once = CaptureButtonCallback(SelectionButton("确认开始"));
            yield return ReachAndClick(SelectionButton("确认开始"));
            once();
            yield return Wait(() => Presentation?.Ready == true, "Explicit start installs one actual chapter");
            Assert.That(Grid.LevelConfigId, Is.EqualTo(Tables.Instance.DTRestaurantChapterConfig.Get(2).SoloFirstLevelId));
            Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Preparing));
            yield return ReadableCommand("拿 碗台 ·", true);
        }

        [UnityTest, Category("EntryFlowClarity")]
        public IEnumerator EntryFlowRecordsFailureRetryEmptyAndExplicitOldSaveRestore()
        {
            yield return StartModeLauncher();
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            string originalDirectory = repository.DirectoryPath;
            try
            {
                yield return Wait(() => Entry.RecordRepository == repository.InstanceId && Entry.RecordState == SoloRecordReadState.Ready, "Home reads the owned empty repository");
                Assert.That(Entry.ContinueRecordCount, Is.Zero);
                yield return ReachAndClick(EntryButton("继续游戏"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.RecordState == SoloRecordReadState.Ready, "Empty is a successful read");
                Assert.That(SelectionHud.SettlementText.text, Does.Contain("没有可继续"));
                string blocked = Path.Combine(directory, "blocked-directory");
                File.WriteAllText(blocked, "owned test file");
                repository.DirectoryPath = blocked;
                Pointer(SelectionButton("刷新记录"));
                yield return Wait(() => Selection.RecordState == SoloRecordReadState.Failed, "Directory IO failure is not an empty list");
                Assert.That(SelectionHud.SettlementText.text, Does.Contain("读取失败"));
                repository.DirectoryPath = originalDirectory;
                var header = SelectionHeader(profile);
                var other = SelectionHeader(profile);
                repository.Commit(header, SelectionFixture());
                repository.Commit(other, SelectionFixture());
                var completed = InspectionCompletedHeader(profile);
                repository.Commit(completed, SelectionFixture());
                Pointer(SelectionButton("刷新记录"));
                yield return Wait(() => Selection.RecordState == SoloRecordReadState.Ready && Selection.Headers.Length == 3, "Retry reads all real files");
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                Pointer(SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录")));
                yield return Wait(() => Selection.InspectedSave?.SlotId == completed.SlotId && !Selection.LoadingSelection, "Completed records remain read only");
                Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "确认继续此记录"), Is.False);
                yield return ReachAndClick(SelectionButton("返回记录列表"));
                yield return null;
                yield return InspectAndContinueSave(header.SlotId);
                yield return Wait(() => Presentation?.Ready == true, "The explicitly selected old record restores");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(header.NextLevelId));
                Assert.That(Grid.Items.Count, Is.EqualTo(SelectionFixture().Items.Length));
                Assert.That(repository.Browse().Length, Is.EqualTo(3));
                yield return ReadableCommand("拿 碗台 ·", true);
            }
            finally
            {
                repository.DirectoryPath = originalDirectory;
                repository.Dispose();
                RemoveOwnSaveUiFiles(directory);
            }
        }
    }
}
#endif
