#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("EntrySettlement")]
        public IEnumerator SoloChapterUiTerminalFailureRetryReturnReadOnlyCompletedFileAndIndependentNewRun()
        {
            yield return StartLauncher(false, false);
            var runtime = Runtime; var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                var checkpoint = SelectionHeader(profile); checkpoint.CompletedLevelId = 102; checkpoint.NextLevelId = 103;
                checkpoint.ChapterId = 1; checkpoint.ChapterStage = 2;
                repository.Commit(checkpoint, SelectionFixture());
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 1, "Real terminal checkpoint visible");
                yield return InspectAndContinueSave(checkpoint.SlotId);
                yield return Wait(() => Presentation?.Ready == true && Grid.LevelConfigId == 103, "Real file restores third level");
                Assert.That(Hud.PhaseText.text, Does.Contain("第 3/3 关"));
                string buff = Hud.GetComponentsInChildren<Button>(false).First(b => b.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
                yield return Command(buff); yield return Command("开始营业");
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Terminal front naturally drains", 75);
                Assert.That(Hud.SettlementText.text, Does.Contain("大关完成").And.Contain("未满足"));
                Assert.That(Grid.LevelStars, Is.Zero);
                Assert.That(Hud.SettlementText.text, Does.Contain("尚未保存").And.Contain("返回选档，无下一关"));
                long readCommand = Client.NextCommandId;
                Pointer(Button("结算详情"));
                Assert.That(Hud.DetailsText.text, Does.Contain("零星不阻止完成").And.Not.Contain("下一关菜单："));
                Pointer(Button("关闭结算详情"));
                Assert.That(Client.NextCommandId, Is.EqualTo(readCommand));
                Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(b => b.GetComponentInChildren<Text>().text.StartsWith("装修 ")), Is.False);
                string path = repository.DirectoryPath; string blocker = Path.Combine(path, "blocked"); File.WriteAllText(blocker, "owned UI test blocker");
                repository.DirectoryPath = blocker; long terminalLevel = Grid.LevelId;
                Pointer(Button("保存完成并返回选档"));
                yield return Wait(() => Grid.SaveState == SoloSaveState.Failed, "Terminal save failure retains settlement");
                Assert.That(Grid.LevelId, Is.EqualTo(terminalLevel)); Assert.That(Hud.SettlementText.text, Does.Contain("保存失败"));
                repository.DirectoryPath = path; var oldCallback = CaptureButtonCallback(Button("保存完成并返回选档"));
                long count = Client.NextCommandId; var retiredClient = Client;
                var finish = Button("保存完成并返回选档"); Pointer(finish); Pointer(finish);
                Assert.That(retiredClient.NextCommandId, Is.EqualTo(count + 1));
                yield return Wait(() => Client == null && Selection?.Started == false && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 2,
                    "Committed finish returns managed selector with repository alive");
                var complete = Selection.Headers.Single(h => h.RecordKind == SoloSaveRecordKind.ChapterCompleted);
                Assert.That(complete.NextLevelId, Is.Zero); Assert.That(repository.Browse().Length, Is.EqualTo(2));
                var label = SelectionHud.GetComponentsInChildren<Button>(false).Single(b => b.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                Pointer(label);
                yield return Wait(() => Selection.ViewedResult?.SlotId == complete.SlotId && !Selection.LoadingSelection, "Completed record read only");
                Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That(SelectionHud.SettlementText.text, Does.Contain("已完成").And.Contain("成功"));
                Selection.CloseSoloSelection(); Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 2, "Selection reopens from durable records");
                oldCallback(); Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That(repository.Browse().Length, Is.EqualTo(2));
                StartPreviewAndConfirm(SelectionButton("内存试玩"));
                yield return Wait(() => Presentation?.Ready == true && Grid.LevelConfigId == 201, "New independent chapter starts with first drink menu");
                Assert.That(Grid.SaveEnabled, Is.False); Assert.That(Grid.Items.Count, Is.EqualTo(10));
                oldCallback(); Assert.That(Grid.LevelConfigId, Is.EqualTo(201)); Assert.That(repository.Browse().Length, Is.EqualTo(2));
            }
            finally { runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory); }
        }
    }
}
#endif
