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
        private Button LocatorButton(long id)
        {
            var visible = Grid.Items.Values.Where(item =>
                    (Presentation.ItemLocatorKind == null || Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Kind == Presentation.ItemLocatorKind) &&
                    (string.IsNullOrEmpty(Presentation.ItemLocatorQuery) || item.Name.Contains(Presentation.ItemLocatorQuery)))
                .OrderBy(item => item.Id).ToArray();
            int index = System.Array.FindIndex(visible, item => item.Id == id) + 1;
            Assert.That(index, Is.GreaterThan(0), "Expected physical item is in this query");
            return Button("物品 " + index + " ·", true);
        }

        private void QueryItems(string query)
        {
            Hud.ShowEntryAddress(query, 80);
            Pointer(Button("查找物品"));
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryItemLocatorFollowsContainerHandAndActorWithoutRebuildingButtonsOrSendingActions()
        {
            yield return StartLauncher();
            yield return Command("拿 食材台 ·", true);
            long egg = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("添加 碗台 ·", true);
            long bowl = Grid.Items[egg].HolderId;
            long command = Client.NextCommandId;
            Pointer(Button("物品查找"));
            Assert.That(Hud.DetailsText.text, Does.Contain("当前结果 " + Grid.Items.Count + " 个物品实例"));
            QueryItems("鸡蛋");
            Pointer(Button("只看材料"));
            Pointer(LocatorButton(egg));
            Assert.That(Presentation.ItemLocatorSelectedId, Is.EqualTo(egg));
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf, Is.False, "Selecting a result exposes the map highlight");
            Pointer(Button("查看详情"));
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf, Is.True);
            Assert.That(Hud.DetailsText.text, Does.Contain("碗内").And.Contain("台面"));
            Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.EqualTo(Grid.Stations[Grid.Items[bowl].HolderId].Cell));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("只看容器"));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("物品 ")), Is.False);
            Pointer(Button("关闭物品查找"));
            yield return Command("拿 碗台 ·", true);
            Pointer(Button("物品查找"));
            QueryItems("鸡蛋");
            Pointer(LocatorButton(egg));
            Assert.That(Hud.DetailsText.text, Does.Contain("玩家 " + Client.MemberId + " 手持"));
            var before = Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false).Select(button => button.GetInstanceID()).OrderBy(id => id).ToArray();
            GridCell destination = new(1, 16);
            command = Client.NextCommandId + 1;
            Presentation.ClickGridCell(destination);
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Original movement carries the container");
            Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.EqualTo(destination));
            Assert.That(Hud.DetailsText.text, Does.Contain("(1,16)"));
            Assert.That(Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false).Select(button => button.GetInstanceID()).OrderBy(id => id), Is.EqualTo(before),
                "Pure actor position changes update information, not the action list");
            yield return Command("暂停");
            long tick = Grid.Tick;
            command = Client.NextCommandId;
            Pointer(LocatorButton(egg));
            yield return null; yield return null;
            Assert.That(Grid.Tick, Is.EqualTo(tick));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            var retired = CaptureButtonCallback(LocatorButton(egg));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Locator managed pool reopen");
            retired();
            Assert.That(Presentation.ItemLocatorSelectedId, Is.Zero);
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl));
            Assert.That(Grid.Items[egg].HolderId, Is.EqualTo(bowl));
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryItemLocatorShowsActualSavedPoolsAndGroundWithoutPickingBeforeExplicitAction()
        {
            yield return StartLauncher(false, false);
            var runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                var payload = SelectionFixture();
                var header = SelectionHeader(profile);
                var items = payload.Items.ToList();
                int key = items.Max(item => item.Key);
                foreach (var kind in new[] { GridStationKind.DirtyPool, GridStationKind.CleanPool })
                {
                    var station = payload.Stations.Single(value => Tables.Instance.DTStationConfig.Get(value.DefinitionId).Kind == kind);
                    var item = new SoloSavedItem
                    {
                        Key = ++key, DefinitionId = GridKitchenRules.Bowl, HolderKey = station.Key, X = station.X, Y = station.Y,
                        Location = (int)(kind == GridStationKind.DirtyPool ? GridItemLocation.DirtyPool : GridItemLocation.CleanPool),
                        Hygiene = (int)(kind == GridStationKind.DirtyPool ? ItemHygiene.Dirty : ItemHygiene.Clean)
                    };
                    station.PoolItemKeys = station.PoolItemKeys.Concat(new[] { item.Key }).ToArray(); items.Add(item);
                }
                var level = Tables.Instance.DTLevelConfig.Get(header.NextLevelId);
                items.Add(new SoloSavedItem { Key = ++key, DefinitionId = GridKitchenRules.Bowl, Location = (int)GridItemLocation.Ground, X = level.SpawnX, Y = level.SpawnY });
                payload.Items = items.ToArray(); repository.Commit(header, payload);
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 1, "Actual valid pool and ground file");
                yield return InspectAndContinueSave(header.SlotId);
                yield return Wait(() => Presentation?.Ready == true, "Actual restore installs physical pool members");
                long command = Client.NextCommandId;
                Pointer(Button("物品查找"));
                QueryItems("碗");
                foreach (var location in new[] { GridItemLocation.DirtyPool, GridItemLocation.CleanPool, GridItemLocation.Ground })
                {
                    var item = Grid.Items.Values.Single(value => value.Location == location);
                    Pointer(LocatorButton(item.Id));
                    string label = location == GridItemLocation.DirtyPool ? "脏餐具池" : location == GridItemLocation.CleanPool ? "净餐具池" : "地面";
                    Assert.That(Hud.DetailsText.text, Does.Contain(label));
                    Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.Not.Null);
                }
                var drop = Grid.Items.Values.Single(item => item.Location == GridItemLocation.Ground);
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Zero);
                Pointer(Button("关闭物品查找"));
                yield return Command("拾 ", true);
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(drop.Id), "Only the explicit original pickup takes the physical item");
            }
            finally
            {
                runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryItemLocatorTracksRealJuicerLockAndLiquidConsumptionWithoutSelectingAnotherItem()
        {
            yield return StartLauncher(menuVariant: ProductMenuVariant.Drink);
            yield return Command("拿 橙子台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.OrangeChunks), "Actual orange preparation");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 果汁壶台 ·", true);
            long orange = Grid.Items.Values.Single(item => item.DefinitionId == GridKitchenRules.OrangeChunks).Id;
            yield return Command("拿 果汁壶台 ·", true); yield return Command("放 榨汁机 ·", true); yield return Command("榨汁 榨汁机 ·", true);
            Pointer(Button("物品查找")); QueryItems(Grid.Items[orange].Name); Pointer(LocatorButton(orange));
            Assert.That(Hud.DetailsText.text, Does.Contain("加工锁定"));
            Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.EqualTo(Grid.Stations.Values.Single(station => station.Kind == GridStationKind.Juicer).Cell));
            yield return Wait(() => Grid.AutoProcesses.Values.Any(process => process.State == GridAutoState.Ready), "Real juicer finishes");
            Assert.That(Hud.DetailsText.text, Does.Not.Contain("加工锁定"));
            Pointer(Button("关闭物品查找"));
            yield return Command("拿 杯台 ·", true); yield return Command("盛一份 榨汁机 ·", true);
            long cup = Grid.Actors[Client.MemberId].HandItemId;
            long liquid = Grid.Items[cup].Contents.Single();
            Pointer(Button("物品查找")); Pointer(LocatorButton(liquid));
            Assert.That(Presentation.ItemLocatorSelectedId, Is.EqualTo(liquid));
            var sink = Grid.Stations.Values.Single(station => station.Kind == GridStationKind.Wash);
            long command = Client.NextCommandId + 1;
            // 保持查找页打开，用原命令刺激真实消费，验证迟后结果不会沿用旧位置。
            Presentation.SendGridIntent(Grid.CleanupIntent(sink.Id, GridAction.DrainLiquid));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Original sink request actually consumes the liquid");
            Assert.That(Grid.Items.ContainsKey(liquid), Is.False);
            Assert.That(Hud.DetailsText.text, Does.Contain("已消失"));
            Assert.That(Hud.TargetText.text, Does.Contain("已消失"));
            Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.Null);
            Assert.That(Presentation.ItemLocatorSelectedId, Is.EqualTo(liquid), "A missing selection never silently becomes another item");
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryItemLocatorActualKcpTeammateContainerFallsOnDisconnectAndHostQueryFollowsIt()
        {
            yield return StartCoopHostLauncher();
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases display");
            var remote = new CoopUiRemote(Runtime.ListenEndpoint);
            ownedCoopUiRemotes.Add(remote);
            try
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP teammate ready");
                yield return AssemblyUiCommand(remote, "拿 食材台 ·", true);
                long egg = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                yield return AssemblyUiCommand(remote, "添加 碗台 ·", true);
                yield return AssemblyUiCommand(remote, "拿 碗台 ·", true);
                long bowl = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                int member = remote.Client.MemberId;
                remote.Presentation.CloseGridHud();
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Teammate releases UI");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready && Grid.Items[egg].HolderId == bowl, "Host receives actual carried container contents");
                long command = Client.NextCommandId;
                GridCell ownCell = Grid.Actors[Client.MemberId].Cell;
                Pointer(Button("物品查找")); QueryItems("鸡蛋"); Pointer(LocatorButton(egg));
                Assert.That(Hud.DetailsText.text, Does.Contain("玩家 " + member + " 手持"));
                Assert.That(Grid.Actors[Client.MemberId].Cell, Is.EqualTo(ownCell));
                remote.Dispose();
                yield return Wait(() => Grid.Items[bowl].Location == GridItemLocation.Ground, "Actual disconnected teammate drops the same container", 30);
                Assert.That(Grid.Items[egg].HolderId, Is.EqualTo(bowl));
                Assert.That(Hud.DetailsText.text, Does.Contain("地面").And.Not.Contain("玩家 " + member + " 手持"));
                Assert.That(Presentation.ItemLocatorHighlight(Grid), Is.EqualTo(Grid.Items[bowl].Cell));
                Assert.That(Grid.Items.Values.Count(item => item.Id == egg), Is.EqualTo(1));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
            }
            finally
            {
                remote.Dispose();
            }
        }
    }
}
#endif
