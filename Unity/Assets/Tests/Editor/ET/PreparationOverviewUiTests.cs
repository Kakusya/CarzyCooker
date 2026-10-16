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
        [UnityTest, Category("EntryExperience"), Timeout(150000)]
        public IEnumerator EntryPreparationOverviewSoloShowsOriginalGatesAndStartsWithoutPreproductionAcrossNextLevel()
        {
            yield return StartLauncher();
            yield return Wait(() => Grid.BuffDrawVersion > 0, "Original buff preparation completes");
            long command = Client.NextCommandId;
            Pointer(Button("开店准备总览"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Preparation));
            Assert.That(Hud.DetailsText.text, Does.Contain("本关菜单").And.Contain("不是额外门槛"));
            Assert.That(Grid.RequireBuffChoice, Is.True);
            Assert.That(Hud.DetailsText.text, Does.Contain("必须先选择"));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "开始营业"), Is.False);
            // UI输入分支：临时只改变本端投影，不向权威提交；恢复后再验证真实选择和开店。
            long draw = Grid.BuffDrawVersion;
            try
            {
                Grid.BuffDrawVersion = 0;
                Presentation.RefreshGridHud();
                Assert.That(Hud.DetailsText.text, Does.Contain("加成准备中"));
                Grid.BuffDrawVersion = draw;
                Grid.RequireBuffChoice = false;
                Presentation.RefreshGridHud();
                Assert.That(Hud.DetailsText.text, Does.Contain("加成可选"));
                Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "开始营业"), Is.True);
            }
            finally
            {
                Grid.BuffDrawVersion = draw; Grid.RequireBuffChoice = true;
                Presentation.RefreshGridHud();
            }
            Pointer(Button("准备：查看菜谱"));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Recipes));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Pointer(Button("菜谱 番茄蛋花汤"));
            Assert.That(Presentation.RecipeVisible, Is.True);
            Pointer(Button("开店准备总览"));
            Assert.That(Presentation.RecipeVisible, Is.False);
            Pointer(Button("准备：查看补货"));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Supply));
            Pointer(Button("开店准备总览"));
            Pointer(Button("准备：查看布局"));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Layout));
            Pointer(Button("全部动作"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Read-only guidance does not prepare items or send actions");
            Pointer(Button("开店准备总览"));
            yield return Command("暂停");
            long tick = Grid.Tick;
            Assert.That(Hud.DetailsText.text, Does.Contain("暂停"));
            yield return null; yield return null;
            Assert.That(Grid.Tick, Is.EqualTo(tick));
            var retired = CaptureButtonCallback(Button("关闭准备总览"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Overview managed pool reopen");
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            retired();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            yield return Command("恢复");
            Pointer(Button("开店准备总览"));
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 "))
                .GetComponentInChildren<Text>().text;
            yield return Command(buff);
            Assert.That(Hud.DetailsText.text, Does.Contain("加成已选"));
            Assert.That(Grid.AutoProcesses, Is.Empty);
            var oldOverview = CaptureButtonCallback(Button("关闭准备总览"));
            long level = Grid.LevelId;
            yield return Command("开始营业");
            Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Operating));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            oldOverview();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Original level naturally completes", 90);
            yield return Wait(() => Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "下一关"), "Original memory continuation is available");
            Pointer(Button("下一关"));
            yield return Wait(() => Grid.LevelId != level && Grid.LevelPhase == LevelPhase.Preparing && Grid.BuffDrawVersion > 0,
                "Original continuation installs the next preparation");
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            oldOverview();
            Pointer(Button("开店准备总览"));
            Assert.That(Hud.DetailsText.text, Does.Contain("必须先选择"));
            Assert.That(Grid.SelectedBuffId, Is.Zero);
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryPreparationOverviewActualKcpClientReadsPausedRoomAndHostStartsWithoutPreparedFood()
        {
            yield return StartCoopHostLauncher();
            yield return Command("暂停");
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases UI while retaining owner");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Real KCP Client gets original paused projection");
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "开店准备总览"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("等待房主").And.Contain("没有预制成品"));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "开始营业"), Is.False);
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Pointer(ClaimUiButton(remote.Hud, "准备：查看补货"));
                Assert.That(remote.Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Supply));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                remote.Presentation.CloseGridHud();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Client releases UI");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host resumes original controls");
                yield return ContentUiPump(Command("恢复"), remote);
                Pointer(Button("开店准备总览"));
                if (Grid.RequireBuffChoice && Grid.SelectedBuffId == 0)
                {
                    string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 "))
                        .GetComponentInChildren<Text>().text;
                    yield return ContentUiPump(Command(buff), remote);
                }
                Assert.That(Grid.AutoProcesses, Is.Empty);
                command = Client.NextCommandId;
                yield return ContentUiPump(Command("开始营业"), remote);
                Assert.That(Client.NextCommandId, Is.EqualTo(command + 1));
                yield return WaitCoopUi(remote, () => remote.Grid.LevelPhase == LevelPhase.Operating, "Remote observes Host's actual open");
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            }
        }
    }
}
#endif
