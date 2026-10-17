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
        private static SpriteRenderer[] InspectionMarkers(MonoGridKitchenBoard board, bool includeInactive = false) =>
            board.GetComponentsInChildren<SpriteRenderer>(includeInactive).Where(renderer =>
                renderer.name.StartsWith("Station footprint ") || renderer.name.StartsWith("Station approach ")).ToArray();

        private void InspectStationRow(MonoUIGridKitchen hud, GridClientComponent grid, long station)
        {
            int index = grid.Stations.Keys.OrderBy(id => id).Select((id, offset) => (id, offset)).Single(value => value.id == station).offset + 1;
            Pointer(ClaimUiButton(hud, "工位 " + index + " ·", true));
        }

        private static void AssertInspectionGeometry(MonoGridKitchenBoard board, GridStationProjection station)
        {
            var footprint = InspectionMarkers(board).Where(marker => marker.name.StartsWith("Station footprint ")).ToArray();
            var approaches = InspectionMarkers(board).Where(marker => marker.name.StartsWith("Station approach ")).ToArray();
            Assert.That(footprint.Select(marker => marker.transform.localPosition), Is.EquivalentTo(station.Footprint.Select(board.CellToWorld)), "Blue marks must occupy the actual committed footprint");
            Assert.That(approaches.Select(marker => marker.transform.localPosition), Is.EquivalentTo(station.Approaches.Select(board.CellToWorld)), "Green marks must occupy actual committed operation cells");
            Assert.That(footprint.All(marker => marker.color.b > marker.color.g), Is.True);
            Assert.That(approaches.All(marker => marker.color.g > marker.color.b), Is.True);
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryStationInspectionSoloRotatesMovesReusesMarkersAndReturnsToOriginalBaking()
        {
            yield return StartLauncher();
            long oven = Grid.Stations.Values.Single(station => station.Name == "烤箱").Id;
            long command = Client.NextCommandId;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, oven); yield return null;
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf, Is.False, "Selecting a station exposes the map markers");
            AssertInspectionGeometry(Board, Grid.Stations[oven]);
            var markers = InspectionMarkers(Board);
            int[] markerIds = markers.Select(marker => marker.GetInstanceID()).OrderBy(id => id).ToArray();
            Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("定义能力：烘烤").And.Contain("本关匹配工序")
                .And.Contain(Tables.Instance.DTGridItemConfig.Get(GridKitchenRules.Toast).Name).And.Contain("候选操作格"));
            Pointer(Button("收起详情"));
            Pointer(Button("隐藏位置标记")); yield return null;
            Assert.That(InspectionMarkers(Board), Is.Empty);
            Pointer(Button("显示位置标记")); yield return null;
            Assert.That(InspectionMarkers(Board).Select(marker => marker.GetInstanceID()).OrderBy(id => id), Is.EqualTo(markerIds));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("调整 烤箱"));
            yield return Command("旋转 烤箱"); Pointer(Button("结束调整")); yield return null;
            Assert.That(Grid.Stations[oven].Orientation, Is.EqualTo(GridOrientation.CounterClockwise90));
            AssertInspectionGeometry(Board, Grid.Stations[oven]);
            Assert.That(InspectionMarkers(Board).Select(marker => marker.GetInstanceID()).OrderBy(id => id), Is.EqualTo(markerIds));
            yield return RenovationUiMoveStation("烤箱", new GridCell(5, 7)); yield return null;
            AssertInspectionGeometry(Board, Grid.Stations[oven]);
            Assert.That(Grid.Stations[oven].Cell, Is.EqualTo(new GridCell(5, 7)));
            Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("位置：(5, 7)").And.Contain("逆时针90°"));
            yield return Command("暂停");
            command = Client.NextCommandId;
            Pointer(Button("收起详情")); InspectStationRow(Hud, Grid, oven);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            AssertInspectionGeometry(Board, Grid.Stations[oven]);
            yield return Command("恢复");
            Pointer(Button("关闭工位说明")); Pointer(Button("全部动作")); yield return null;
            Assert.That(InspectionMarkers(Board), Is.Empty);
            yield return Command("拿 面包台 ·", true); yield return Command("放 烤箱 ·", true); yield return Command("烘烤 烤箱 ·", true);
            yield return Wait(() => Grid.Items[Grid.Stations[oven].ItemId].DefinitionId == GridKitchenRules.Toast, "Original baking works at the moved and rotated oven");
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, oven); yield return null;
            var stale = CaptureButtonCallback(Button("隐藏位置标记"));
            Presentation.CloseGridHud(); yield return null;
            Assert.That(InspectionMarkers(Board), Is.Empty);
            Presentation.OpenGridHud(); yield return Wait(() => Presentation.Ready, "Inspector managed HUD reopen");
            command = Client.NextCommandId; stale();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(InspectionMarkers(Board), Is.Empty);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, oven); yield return null;
            var retiredMarkers = InspectionMarkers(Board, true);
            Runtime.StopProduct(); yield return null; yield return null;
            Assert.That(retiredMarkers.All(marker => marker == null), Is.True, "The original Board owner releases its decoration nodes on actual runtime exit");
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryStationInspectionActualKcpSteamCapabilitiesAndHostGeometryRemainReadOnly()
        {
            yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启咖啡餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Original coffee room opens");
            long steam = Grid.Stations.Values.Single(station => station.Name == "蒸汽机").Id;
            Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host owner releases the singleton UI for the remote");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP inspector ready");
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "工位说明")); InspectStationRow(remote.Hud, remote.Grid, steam);
                remote.Tick(); yield return null;
                var board = ((GridKitchenBoard)remote.Presentation.Board).View;
                AssertInspectionGeometry(board, remote.Grid.Stations[steam]);
                Pointer(ClaimUiButton(remote.Hud, "查看详情"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("定义能力：热奶、打泡").And.Contain("热奶 →").And.Contain("打泡 →"));
                var milk = Tables.Instance.DTGridAutoRecipeConfig.DataList.First(recipe => recipe.StationCapability == GridStationCapability.HeatMilk);
                Assert.That(remote.Hud.DetailsText.text, Does.Contain(Tables.Instance.DTGridItemConfig.Get(milk.ContainerDefinitionId).Name));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("调整 ")), Is.False);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                long version = Grid.LayoutVersion;
                var before = Grid.Stations[steam];
                var receiptTask = Client.CallProduct(Grid.CreateStationLayout(steam, before.Cell, GridOrientation.CounterClockwise90, version)).AsTask();
                yield return WaitCoopUi(remote, () => receiptTask.IsCompleted && remote.Grid.LayoutVersion > version && Grid.LastStatus == GridResultStatus.Executed, "Actual Host local layout reaches remote projection");
                using (var receipt = (StationLayoutResponse)receiptTask.GetAwaiter().GetResult())
                {
                    Assert.That(receipt.Error, Is.Zero);
                }
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("逆时针90°"));
                AssertInspectionGeometry(board, remote.Grid.Stations[steam]);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                var stale = CaptureButtonCallback(ClaimUiButton(remote.Hud, "隐藏位置标记"));
                remote.Presentation.CloseGridHud(); remote.Tick(); yield return null;
                Assert.That(InspectionMarkers(board), Is.Empty);
                remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote inspector pool reopen");
                stale();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
            }
        }

        [UnityTest, Category("EntryExperience"), Timeout(180000)]
        public IEnumerator EntryStationInspectionActualGrowthUnlockExpansionAndMenuNarrowingUpdateExplanation()
        {
            yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启经营成长餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary growth restaurant");
            long juicer = Grid.Stations.Values.Single(station => station.Name == "榨汁机").Id;
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, juicer); Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("定义能力：榨汁").And.Contain("本关没有匹配的生产工序"));
            Pointer(Button("关闭工位说明"));
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            RestaurantRuntime restaurant = match.Restaurant;
            ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
            ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            yield return ExpansionUiNaturalSettlement();
            yield return Command("装修 解锁现榨菜单"); yield return UnlockUiNext(true);
            Assert.That(Grid.AllowedDishIds, Does.Contain(3));
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, juicer); Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("本关匹配工序").And.Contain("榨汁 →"));
            var stale = CaptureButtonCallback(Button("隐藏位置标记"));
            Pointer(Button("关闭工位说明"));
            ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
            ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            yield return ExpansionUiNaturalSettlement();
            yield return Command("装修 扩建单客桌"); yield return UnlockUiNext(true);
            Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Tables.Count, Is.EqualTo(3));
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 4 }));
            long command = Client.NextCommandId; stale();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(InspectionMarkers(Board), Is.Empty);
            Pointer(Button("工位说明")); InspectStationRow(Hud, Grid, juicer); Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("定义能力：榨汁").And.Contain("本关没有匹配的生产工序"));
            var table = Grid.Stations.Values.Single(station => station.Cell == new GridCell(7, 3));
            InspectStationRow(Hud, Grid, table.Id); yield return null;
            AssertInspectionGeometry(Board, Grid.Stations[table.Id]);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Expansion and menu changes do not turn inspection into a command");
        }
    }
}
#endif
