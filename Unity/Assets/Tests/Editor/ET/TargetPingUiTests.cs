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
        private static Text PingCaption(MonoGridKitchenBoard board, int member)
            => board.GetComponentsInChildren<Text>(false).SingleOrDefault(text => text.text.StartsWith("成员" + member + " → "));

        [UnityTest, Category("TargetPingsUi"), Timeout(150000)]
        public IEnumerator EntryTargetPingActualHostKcpThreeTargetsFollowOriginalPotAndPooledRecoveryCannotReplay()
        {
            yield return StartCoopHostLauncher();
            CreateSaveUiRepository(Runtime, out _, out _);
            var stove = Grid.Stations.Values.Single(value => value.Name == "灶台");
            long pot = stove.ItemId;
            GridCell ownCell = Grid.Actors[Client.MemberId].Cell;
            long command = Client.NextCommandId;
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, stove.Id);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("标记工位 · 灶台");
            yield return Wait(() => PingCaption(Board, Client.MemberId) != null, "Original Board shows sender and station label");
            Assert.That(PingCaption(Board, Client.MemberId).text, Does.Contain("灶台"));
            Assert.That(Grid.Actors[Client.MemberId].Cell, Is.EqualTo(ownCell));
            AssertInspectionGeometry(Board, stove);
            Pointer(Button("关闭工位说明"));
            Pointer(Button("物品查找")); Pointer(LocatorButton(pot));
            var highlight = Presentation.ItemLocatorHighlight(Grid);
            yield return Command("标记物品 · 煮锅", true);
            Assert.That(Grid.TargetPings.Single().Kind, Is.EqualTo(TargetPingKind.Item));
            Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.EqualTo(highlight), "Shared marker cannot replace the local item highlight");
            var stale = CaptureButtonCallback(Button("标记物品 · 煮锅", true));
            command = Client.NextCommandId;
            Presentation.CloseGridHud();
            yield return Wait(() => PingCaption(Board, Client.MemberId) == null, "Closing HUD clears owned marker visuals");
            Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Host managed HUD reopens");
            stale(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases display for actual Client");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.TargetPings.Count == 1, "Actual KCP joins with current Host marker");
                var board = ((GridKitchenBoard)remote.Presentation.Board).View;
                command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "查看标记 · 成员 " + Client.MemberId + " ·", true));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Presentation.ItemLocatorSelectedId, Is.EqualTo(pot));
                IEnumerator Do(string caption, bool prefix = false)
                {
                    long expected = remote.Client.NextCommandId + 1;
                    var button = remote.Hud.GetComponentsInChildren<Button>(false).SingleOrDefault(value =>
                        prefix ? value.GetComponentInChildren<Text>().text.StartsWith(caption) : value.GetComponentInChildren<Text>().text == caption);
                    Assert.That(button, Is.Not.Null, "Missing original UI action: " + caption + "; page=" + remote.Presentation.OverviewPage + "; group=" + remote.Presentation.ActionGroup);
                    Pointer(button);
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == expected && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                yield return Do("标记物品 · 煮锅", true);
                yield return WaitCoopUi(remote, () => remote.Grid.TargetPings.Count == 2, "Each sender owns its own one marker");
                Pointer(ClaimUiButton(remote.Hud, "全部动作"));
                yield return Do("直接拿锅");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(pot));
                var marker = remote.Grid.TargetPings.Single(value => value.MemberId == remote.Client.MemberId);
                Assert.That(GridPresentationSystem.TargetPingCell(remote.Grid, marker), Is.EqualTo(remote.Grid.Actors[remote.Client.MemberId].Cell));
                yield return WaitCoopUi(remote, () => PingCaption(board, remote.Client.MemberId) != null, "Sender label follows held original pot");
                yield return Do("放 交接台 ·", true);
                Assert.That(remote.Grid.Items[pot].HolderId, Is.EqualTo(remote.Grid.Stations.Values.Single(value => value.Name == "交接台").Id));
                yield return Do("取消我的目标标记");
                Pointer(ClaimUiButton(remote.Hud, "工位说明"));
                var table = remote.Grid.Stations[remote.Grid.Tables.Keys.First()];
                InspectStationRow(remote.Hud, remote.Grid, table.Id);
                yield return Do("标记餐桌 · " + table.Name);
                Assert.That(remote.Grid.TargetPings.Single(value => value.MemberId == remote.Client.MemberId).Kind, Is.EqualTo(TargetPingKind.Table));
                var retired = CaptureButtonCallback(ClaimUiButton(remote.Hud, "标记餐桌 · " + table.Name));
                long binding = remote.Client.BindingGeneration;
                ProductRuntimeComponent runtime = remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected && Grid.TargetPings.All(value => value.MemberId != remote.Client.MemberId), "Actual disconnect clears only this sender");
                Pointer(ClaimUiButton(remote.Hud, "手动恢复"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed && remote.Client.BindingGeneration > binding, "New binding receives current markers without own replay");
                Assert.That(remote.Grid.TargetPings.All(value => value.MemberId != remote.Client.MemberId), Is.True);
                command = remote.Client.NextCommandId; retired();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.Items[pot].HolderId, Is.EqualTo(remote.Grid.Stations.Values.Single(value => value.Name == "交接台").Id));
            }
        }
    }
}
#endif
