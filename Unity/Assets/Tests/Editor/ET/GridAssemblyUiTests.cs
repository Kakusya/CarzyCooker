#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
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
        private IEnumerator AssemblyUiCommand(CoopUiRemote remote, string caption, bool prefix = false)
        {
            var client = remote?.Client ?? Client;
            var view = remote?.Grid ?? Grid;
            var hud = remote?.Hud ?? Hud;
            long command = client.NextCommandId + 1;
            Pointer(ClaimUiButton(hud, caption, prefix));
            Assert.That(client.NextCommandId, Is.EqualTo(command), "One actual pointer submits one intent: " + caption);
            bool Done() => view.LastCommandId == command && view.LastStatus == GridResultStatus.Executed;
            if (remote == null)
            {
                yield return Wait(Done, "Actual Solo action: " + caption, 20);
            }
            else
            {
                yield return WaitCoopUi(remote, Done, "Actual KCP action: " + caption, 20);
            }
        }

        private IEnumerator AssemblyUiChop(CoopUiRemote remote, string source, int output)
        {
            yield return AssemblyUiCommand(remote, "拿 " + source + " ·", true);
            yield return AssemblyUiCommand(remote, "放 切配台 ·", true);
            yield return AssemblyUiCommand(remote, "切配 切配台 ·", true);
            var view = remote?.Grid ?? Grid;
            bool Finished() => view.Stations.Values.Any(station => station.Name == "切配台" &&
                view.Items.TryGetValue(station.ItemId, out var item) && item.DefinitionId == output);
            if (remote == null)
            {
                yield return Wait(Finished, "Real cutting completed for " + source, 15);
            }
            else
            {
                yield return WaitCoopUi(remote, Finished, "Real KCP cutting completed for " + source, 15);
            }
            yield return AssemblyUiCommand(remote, "拿 切配台 ·", true);
            yield return AssemblyUiCommand(remote, "添加 盘台 ·", true);
        }

        private IEnumerator AssemblyUiMakeAndServe(bool cooperative, bool cheese)
        {
            CoopUiRemote remote = null;
            if (cooperative)
            {
                yield return StartProgressHostRoom();
                yield return StartPreviewAndConfirm(EntryButton("开启沙拉餐厅"));
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary Host opens salad restaurant");
            }
            else
            {
                yield return StartModeLauncher();
                CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary Solo selector");
                yield return StartPreviewAndConfirm(SelectionButton("新开沙拉餐厅"));
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary Solo salad world");
            }
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 4, 5 }));
            yield return AssemblyUiChop(null, "备料台", 2);
            string dishName = cheese ? "芝士蔬菜沙拉" : "田园沙拉";
            if (!cooperative && !cheese)
            {
                yield return Command("拿 盘台 ·", true);
                long rejected = Client.NextCommandId + 1;
                Pointer(Button("组装 田园沙拉"));
                yield return Wait(() => Grid.LastCommandId == rejected && Grid.LastStatus == GridResultStatus.Rejected, "Actual incomplete plate is rejected without consuming contents");
                Assert.That(Hud.StatusText.text, Does.Contain("盘内材料或份数不匹配"));
                Assert.That(Grid.Items[Grid.Actors[Client.MemberId].HandItemId].Contents.Count, Is.EqualTo(1));
                yield return Command("放 盘台 ·", true);
            }
            Pointer(Button("菜谱 " + dishName));
            Assert.That(Hud.GetComponentsInChildren<Text>(false).Any(text => text.text.Contains("原盘还缺") && text.text.Contains("生菜片") && text.text.Contains("黄瓜片")), Is.True);
            Pointer(Button("关闭菜谱"));
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);
            yield return Command("开始营业");
            if (cooperative)
            {
                long movement = Client.NextCommandId + 1;
                Presentation.ClickGridCell(new GridCell(2, 10));
                yield return Wait(() => Grid.LastCommandId == movement && Grid.LastStatus == GridResultStatus.Executed, "Host moves aside through the ordinary map handler");
                Presentation.CloseGridHud();
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Actual independent KCP teammate opens current world");
            }
            try
            {
                yield return AssemblyUiChop(remote, "生菜台", 23);
                yield return AssemblyUiChop(remote, "黄瓜台", 25);
                var view = remote?.Grid ?? Grid;
                var client = remote?.Client ?? Client;
                MonoUIGridKitchen CurrentHud() => remote?.Hud ?? Hud;
                if (cheese)
                {
                    yield return AssemblyUiCommand(remote, "拿 芝士台 ·", true);
                    yield return AssemblyUiCommand(remote, "添加 盘台 ·", true);
                }
                yield return AssemblyUiCommand(remote, "拿 盘台 ·", true);
                long plate = view.Actors[client.MemberId].HandItemId;
                Assert.That(view.Items[plate].Contents.Count, Is.EqualTo(cheese ? 4 : 3));
                yield return AssemblyUiCommand(remote, "组装 " + dishName);
                yield return AssemblyUiCommand(remote, "停止组装");
                var retired = CaptureButtonCallback(ClaimUiButton(CurrentHud(), "继续组装"));
                int paused = view.Processes.Values.Single(work => work.TargetItemId == plate && work.State == GridWorkState.Paused).ProgressTicks;
                if (remote == null)
                {
                    Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                    yield return Wait(() => Presentation.Ready, "Original managed Solo HUD reopens");
                }
                else
                {
                    remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                    yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Original managed KCP HUD reopens");
                }
                long before = client.NextCommandId;
                retired(); Assert.That(client.NextCommandId, Is.EqualTo(before));
                Assert.That(view.Processes.Values.Single(work => work.TargetItemId == plate && work.State == GridWorkState.Paused).ProgressTicks, Is.EqualTo(paused));
                yield return AssemblyUiCommand(remote, "继续组装");
                int definition = cheese ? 28 : 27;
                bool Finished() => view.Items.TryGetValue(plate, out var carrier) && carrier.Contents.Count == 1 && view.Items[carrier.Contents[0]].DefinitionId == definition;
                if (remote == null)
                {
                    yield return Wait(Finished, "Actual completed " + dishName, 15);
                    yield return Wait(() => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == (cheese ? 5 : 4)), "Actual matching customer order", 60);
                }
                else
                {
                    yield return WaitCoopUi(remote, Finished, "Actual KCP completed " + dishName, 15);
                    yield return WaitCoopUi(remote, () => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == (cheese ? 5 : 4)), "Actual matching cooperative customer order", 60);
                }
                var order = view.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == (cheese ? 5 : 4));
                string table = view.Stations[order.TableStationId].Name;
                yield return AssemblyUiCommand(remote, "前往 " + table);
                yield return AssemblyUiCommand(remote, "交付 " + dishName + " → " + table);
                Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(view.LevelScore, Is.EqualTo(10));
                Assert.That(view.Actors[client.MemberId].HandItemId, Is.Zero);
                Assert.That(Grid.LevelScore, Is.EqualTo(view.LevelScore));
                Assert.That(view.Items[plate].DefinitionId, Is.EqualTo(GridKitchenRules.Plate), "Same real outer plate is retained for the customer");
            }
            finally { remote?.Dispose(); }
        }

        [UnityTest, Category("GridAssemblyUi")]
        public IEnumerator GridAssemblyUiSoloF01() => AssemblyUiMakeAndServe(false, false);

        [UnityTest, Category("GridAssemblyUi")]
        public IEnumerator GridAssemblyUiSoloF02() => AssemblyUiMakeAndServe(false, true);

        [UnityTest, Category("GridAssemblyUi")]
        public IEnumerator GridAssemblyUiCooperativeF01() => AssemblyUiMakeAndServe(true, false);

        [UnityTest, Category("GridAssemblyUi")]
        public IEnumerator GridAssemblyUiCooperativeF02() => AssemblyUiMakeAndServe(true, true);
    }
}
#endif
