#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
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
        [UnityTest]
        [Category("EntryExperience")]
        public IEnumerator EntryActionNavigationFiltersOriginalButtonsWithoutCommandsAndKeepsCurrentTarget()
        {
            yield return StartLauncher();
            yield return Wait(() => Grid.BuffDrawVersion > 0, "Original preparation has settled");
            long command = Client.NextCommandId;
            var originalButtons = Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false)
                .Select(button => button.GetInstanceID()).OrderBy(id => id).ToArray();
            Button take = Button("拿 碗台 ·", true);
            var oldTake = CaptureButtonCallback(take);
            Pointer(Button("补货动作"));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Supply));
            Assert.That(Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false)
                .Any(button => button.GetComponentInChildren<Text>().text.StartsWith("采购 ")), Is.True);
            Assert.That(take.gameObject.activeInHierarchy, Is.False);
            oldTake();
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Hidden action cannot be invoked from its old callback");
            Pointer(Button("前厅动作"));
            Assert.That(Presentation.SelectedStationId, Is.Zero);
            Pointer(Button("全部动作"));
            Assert.That(Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false)
                .Select(button => button.GetInstanceID()).OrderBy(id => id).ToArray(), Is.EqualTo(originalButtons),
                "Browsing groups preserves the same original action buttons");
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            var scroll = Hud.TargetsRectTransform.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null, "The original action list remains scrollable");
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = .35f;
            yield return null; yield return null;
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.35f).Within(.01f), "Projection refresh keeps the current action scroll position");
            yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            long station = Presentation.SelectedStationId;
            Pointer(Button("当前目标动作"));
            Assert.That(Button("放 碗台 ·", true).transform.parent, Is.SameAs(Hud.PrimaryActionsRectTransform));
            Pointer(Button("补货动作"));
            Assert.That(Presentation.SelectedStationId, Is.EqualTo(station));
            Assert.That(Button("放 碗台 ·", true).gameObject.activeInHierarchy, Is.True, "Current target remains available");
            yield return Command("放 碗台 ·", true);
            Assert.That(Grid.Stations[station].ItemId, Is.EqualTo(bowl));
            yield return Command("暂停");
            long pausedTick = Grid.Tick;
            command = Client.NextCommandId;
            Pointer(Button("菜谱入口"));
            Pointer(Button("布局与装修"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Tick, Is.EqualTo(pausedTick));
            var retired = CaptureButtonCallback(Button("前厅动作"));
            GridPresentationComponent presentation = Presentation;
            int pooled = Hud.GetInstanceID();
            presentation.CloseGridHud();
            yield return null;
            presentation.OpenGridHud();
            yield return Wait(() => presentation.Ready, "Filtered HUD reopens through original owner");
            GridActionGroup group = presentation.ActionGroup;
            retired();
            Assert.That(presentation.ActionGroup, Is.EqualTo(group));
            Assert.That(Hud.GetInstanceID(), Is.EqualTo(pooled));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("全部动作"));
            yield return Command("恢复");
            Vector2 point = Board.InputScreenRect.center;
            command = Client.NextCommandId;
            presentation.ProcessGridPointer(point, true, true, false);
            presentation.ProcessGridPointer(point + new Vector2(0, 200), false, true, false);
            presentation.ProcessGridPointer(point + new Vector2(0, 200), false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Group navigation keeps drag separate from input");
        }

        [UnityTest]
        [Category("EntryExperience")]
        public IEnumerator EntryActionNavigationActualKcpFiltersAndExecutesOneOriginalPickup()
        {
            yield return StartCoopHostLauncher();
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"),
                "Host releases the shared Editor display before the remote HUD is opened");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP managed HUD ready");
                long command = remote.Client.NextCommandId;
                Button RemoteButton(string caption, bool prefix = false) => remote.Hud.GetComponentsInChildren<Button>(false)
                    .Single(button => prefix ? button.GetComponentInChildren<Text>().text.StartsWith(caption)
                        : button.GetComponentInChildren<Text>().text == caption);
                Pointer(RemoteButton("菜谱入口"));
                Assert.That(remote.Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Recipes));
                Assert.That(remote.Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false)
                    .Any(button => button.GetComponentInChildren<Text>().text.StartsWith("菜谱 ")), Is.True);
                Pointer(RemoteButton("补货动作"));
                Pointer(RemoteButton("全部动作"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Pointer(RemoteButton("拿 碗台 ·", true));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(++command));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command &&
                    remote.Grid.LastStatus == GridResultStatus.Executed, "Grouped remote original pickup commits");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Not.Zero);
                var retired = CaptureButtonCallback(RemoteButton("前厅动作"));
                remote.Presentation.CloseGridHud();
                yield return null;
                remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote grouped HUD reopens");
                GridActionGroup group = remote.Presentation.ActionGroup;
                retired();
                Assert.That(remote.Presentation.ActionGroup, Is.EqualTo(group));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
            }
        }
    }
}
#endif
