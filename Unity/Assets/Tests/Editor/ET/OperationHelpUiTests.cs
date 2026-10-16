#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("EntryHelp")]
        public IEnumerator EntryHelpSoloTopicsRecipeContainerAndActualDragKeepCommandsExplicitAcrossReopen()
        {
            yield return StartLauncher();
            yield return Wait(() => Grid.BuffDrawVersion > 0, "Original preparation is available");
            long command = Client.NextCommandId;
            Pointer(Button("操作帮助"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Help));
            Assert.That(Hud.DetailsText.text, Does.Contain("选择并执行").And.Contain("释放不追加点击"));
            foreach (string caption in new[] { "帮助：拿取与放置", "帮助：容器与内容", "帮助：加工与接续", "帮助：当前阶段", "帮助：角色权限" })
            {
                Pointer(Button(caption));
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Reading help cannot issue a business command");
            }
            Assert.That(Hud.DetailsText.text, Does.Contain("当前：单机"));
            Pointer(Button("帮助：前往菜谱"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Recipes));
            Pointer(Button("菜谱 番茄蛋花汤"));
            Assert.That(Presentation.RecipeVisible, Is.True);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("操作帮助"));
            Assert.That(Presentation.RecipeVisible, Is.False, "Help and recipe details share one owned area");
            Pointer(Button("关闭操作帮助")); Pointer(Button("全部动作"));
            yield return Command("拿 食材台 ·", true);
            yield return Command("添加 碗台 ·", true);
            yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            long[] contents = Grid.Items[bowl].Contents.ToArray();
            Assert.That(contents.Length, Is.EqualTo(1));
            command = Client.NextCommandId;
            Pointer(Button("操作帮助")); Pointer(Button("帮助：容器与内容"));
            Assert.That(Hud.DetailsText.text, Does.Contain("内部内容").And.Contain("整锅搬走"));
            Assert.That(Grid.Items[bowl].Contents, Is.EqualTo(contents));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("关闭操作帮助"));
            yield return Command("放 交接台 ·", true);
            Assert.That(Grid.Items[bowl].Contents, Is.EqualTo(contents), "Actual container placement carries the original contents");
            yield return Command("拿 交接台 ·", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl));
            Assert.That(Grid.Items[bowl].Contents, Is.EqualTo(contents));
            yield return UnlockUiStartOperating();
            command = Client.NextCommandId;
            Pointer(Button("操作帮助")); Pointer(Button("帮助：当前阶段"));
            Assert.That(Hud.DetailsText.text, Does.Contain("当前：营业").And.Contain("不会自动暂停"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("关闭操作帮助"));
            yield return Command("暂停");
            long tick = Grid.Tick;
            command = Client.NextCommandId;
            Pointer(Button("操作帮助")); Pointer(Button("帮助：当前阶段"));
            Assert.That(Hud.DetailsText.text, Does.Contain("已暂停").And.Contain("不会自动恢复"));
            var retiredTopic = CaptureButtonCallback(Button("帮助：角色权限"));
            var retiredClose = CaptureButtonCallback(Button("关闭操作帮助"));
            int mono = Hud.GetInstanceID();
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Help uses the original managed pooled HUD lifecycle");
            retiredTopic(); retiredClose();
            Assert.That(Hud.GetInstanceID(), Is.EqualTo(mono));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Grid.LevelPaused, Is.True); Assert.That(Grid.Tick, Is.EqualTo(tick));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("恢复");
            Vector2 point = Board.InputScreenRect.center;
            float cameraY = Board.BoardCamera.transform.position.y;
            command = Client.NextCommandId;
            Presentation.ProcessGridPointer(point, true, true, false);
            Presentation.ProcessGridPointer(point + new Vector2(0, 160), false, true, false);
            Presentation.ProcessGridPointer(point + new Vector2(0, 160), false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Actual drag release cannot append a click");
            Assert.That(Board.BoardCamera.transform.position.y, Is.Not.EqualTo(cameraY), "Actual board view pans");
        }

        [UnityTest, Category("EntryHelp")]
        public IEnumerator EntryHelpActualKcpClientReadsRoleAndPausedStateWithoutGainingHostControls()
        {
            yield return StartCoopHostLauncher();
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases the shared Editor HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP Client owns a managed HUD");
                Button Find(string caption, bool prefix = false) => ClaimUiButton(remote.Hud, caption, prefix);
                long command = remote.Client.NextCommandId;
                Pointer(Find("操作帮助")); Pointer(Find("帮助：角色权限"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("当前：合作队员").And.Contain("由房主控制"));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => new[] { "暂停", "恢复", "开始营业", "下一关" }.Contains(button.GetComponentInChildren<Text>().text)), Is.False);
                var pause = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Pause));
                yield return WaitCoopUi(remote, () => pause.Status != UniTaskStatus.Pending, "Actual Host local mailbox pauses the original world");
                using (var response = (LevelCommandResponse)pause.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitCoopUi(remote, () => remote.Grid.LevelPaused, "Client sees actual paused projection");
                Pointer(Find("帮助：当前阶段"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("已暂停").And.Contain("不会自动恢复"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                var retired = CaptureButtonCallback(Find("帮助：前往菜谱"));
                remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Client help closes and reopens through original owner");
                retired();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.LevelPaused, Is.True);
                var resume = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Resume));
                yield return WaitCoopUi(remote, () => resume.Status != UniTaskStatus.Pending, "Only Host explicitly resumes");
                using (var response = (LevelCommandResponse)resume.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitCoopUi(remote, () => !remote.Grid.LevelPaused, "Remote sees committed resume");
                Pointer(Find("全部动作"));
                Pointer(Find("拿 碗台 ·", true));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(++command));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, "Original KCP pickup still works after help");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Not.Zero);
            }
        }
    }
}
#endif
