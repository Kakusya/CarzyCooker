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
        private IEnumerator ThemedUiSteam()
        {
            ContentUiReadRecipe("牛肉蒸饺");
            yield return Command("拿 洋葱台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == 65), "Themed onion actually chopped");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 拌馅盆台 ·", true);
            yield return Command("拿 牛肉末台 ·", true); yield return Command("添加 拌馅盆台 ·", true);
            yield return Command("拿 拌馅盆台 ·", true); yield return Command("组装 牛肉洋葱馅");
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            yield return Wait(() => Grid.Items[bowl].Contents.Count == 1 && Grid.Items[Grid.Items[bowl].Contents[0]].DefinitionId == 68, "Themed filling actually mixed");
            yield return Command("放 拌馅盆台 ·", true); yield return Command("拿 饺子皮台 ·", true); yield return Command("添加 拌馅盆台 ·", true);
            yield return Command("拿 拌馅盆台 ·", true); yield return Command("组装 未熟牛肉蒸饺");
            yield return Wait(() => Grid.Items[bowl].Contents.Count == 1 && Grid.Items[Grid.Items[bowl].Contents[0]].DefinitionId == 69, "Themed dumpling actually wrapped");
            yield return Command("放 拌馅盆台 ·", true); Pointer(Button("查看详情"));
            yield return Command("取出 未熟牛肉蒸饺 · 拌馅盆台");
            yield return Command("放 蒸锅 ·", true); yield return Command("蒸制 蒸锅 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == 70), "Themed dumpling actually steamed");
            yield return Command("拿 蒸锅 ·", true); yield return Command("添加 蒸食盘台 ·", true);
        }

        private IEnumerator ThemedUiServe(CoopUiRemote remote, int dish, string name, string source, bool drink)
        {
            var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
            yield return HotUiWait(remote, () => view.Orders.Values.Any(value => value.DishId == dish && value.State == OrderState.Waiting), "Actual themed order for " + name, 40);
            var order = view.Orders.Values.First(value => value.DishId == dish && value.State == OrderState.Waiting);
            string table = view.Stations[order.TableStationId].Name;
            yield return HotUiCommand(remote, "拿 " + source + " ·", true);
            long serving = view.Actors[client.MemberId].HandItemId;
            if (drink)
            {
                yield return HotUiCommand(remote, "放 茶饮贴单台 ·", true); yield return HotUiCommand(remote, "贴单 → " + table);
                Assert.That(view.Items[serving].BoundOrderId, Is.EqualTo(order.Id));
                yield return HotUiCommand(remote, "拿 茶饮贴单台 ·", true);
            }
            yield return HotUiCommand(remote, "前往 " + table); yield return HotUiCommand(remote, "交付 " + name + " → " + table);
            Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
            if (drink) { Assert.That(view.Items.ContainsKey(serving), Is.False); }
            yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Actual themed customer leaves", 20);
            long clear = view.FrontWorks.Values.Single(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available).Id;
            yield return HotUiCommand(remote, "清桌 " + table, true);
            yield return HotUiWait(remote, () => view.FrontWorks[clear].State == FrontWorkState.Completed, "Actual themed table cleared", 20);
            if (!drink)
            {
                yield return HotUiWait(remote, () => view.Items[serving].Location == GridItemLocation.CleanPool, "Same steam plate actually washed", 40);
            }
        }

        private IEnumerator ThemedUiJourney(bool cooperative)
        {
            SoloSaveRepository repository = null;
            if (cooperative)
            {
                yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启大关 3 · 蒸茶小馆"));
            }
            else
            {
                yield return StartModeLauncher(); repository = CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Original themed chapter selector");
                yield return StartPreviewAndConfirm(SelectionButton("新开大关 3 · 蒸茶小馆"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Original themed kitchen opens");
            string world = Grid.WorldId; int first = cooperative ? 2801 : 1801;
            long[] initialStations = Grid.Stations.Keys.OrderBy(id => id).ToArray();
            yield return ThemedUiSteam(); ContentUiReadRecipe("热红茶");
            yield return Command("直接拿泡茶桶"); yield return Command("接水 → 取水台热水档"); yield return Command("放 泡茶桶 ·", true);
            yield return Command("拿 红茶叶台 ·", true); yield return Command("添加 泡茶桶 ·", true); yield return Command("泡茶 泡茶桶 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.BrewTea && batch.State == GridAutoState.Ready), "Theme real tea ready");
            long teaBarrel = Grid.Stations.Values.Single(station => station.Name == "泡茶桶").ItemId;
            yield return Command("拿 热茶杯台 ·", true); yield return Command("取一份 泡茶桶 ·", true); yield return Command("放 热茶杯台 ·", true);
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Real KCP teammate receives actual shared themed food");
            }
            try
            {
                var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
                UnityEngine.Events.UnityAction staleContinue = null;
                for (int stage = 1; stage <= 3; ++stage)
                {
                    Assert.That(view.LevelConfigId, Is.EqualTo(first + stage - 1));
                    Assert.That(view.WorldId, Is.EqualTo(world));
                    Assert.That(view.Stations.Keys.Intersect(initialStations).Count(), Is.EqualTo(initialStations.Length), "Initial stations are never installed twice");
                    if (stage == 2)
                    {
                        ContentUiReadRecipeForRemote(remote, "冰鲜奶茶");
                        Assert.That(view.AutoProcesses.Values.Single(batch => batch.RequiredCapability == GridStationCapability.BrewTea).RemainingPortions, Is.EqualTo(2));
                        yield return HotUiCommand(remote, "直接拿调饮壶"); yield return HotUiCommand(remote, "取一份 泡茶桶 ·", true); yield return HotUiCommand(remote, "放 调饮台 ·", true);
                        yield return HotUiCommand(remote, "拿 牛奶台 ·", true); yield return HotUiCommand(remote, "添加 调饮台 ·", true);
                        yield return HotUiCommand(remote, "拿 糖浆台 ·", true); yield return HotUiCommand(remote, "添加 调饮台 ·", true); yield return HotUiCommand(remote, "调饮 调饮台 ·", true);
                        yield return HotUiWait(remote, () => view.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.MixTea && batch.State == GridAutoState.Ready), "Themed milk base ready");
                        yield return HotUiCommand(remote, "拿 冷饮杯台 ·", true); long cup = view.Actors[client.MemberId].HandItemId;
                        yield return HotUiCommand(remote, "取一份 调饮台 ·", true); yield return HotUiCommand(remote, "放 冷饮杯台 ·", true);
                        yield return HotUiCommand(remote, "拿 冰块台 ·", true); yield return HotUiCommand(remote, "添加 冷饮杯台 ·", true); yield return HotUiCommand(remote, "拿 冷饮杯台 ·", true);
                        yield return HotUiCommand(remote, "混合 冰鲜奶茶");
                        yield return HotUiWait(remote, () => view.Items[cup].Contents.Count == 1 && view.Items[view.Items[cup].Contents[0]].DefinitionId == 88, "Themed iced tea mixed");
                        yield return HotUiCommand(remote, "放 冷饮杯台 ·", true);
                    }
                    if (stage == 3)
                    {
                        ContentUiReadRecipeForRemote(remote, "珍珠奶茶");
                        Assert.That(view.AutoProcesses.Values.Single(batch => batch.RequiredCapability == GridStationCapability.MixTea).RemainingPortions, Is.EqualTo(1));
                        yield return HotUiCommand(remote, "拿 生珍珠台 ·", true); yield return HotUiCommand(remote, "放 煮珍珠台 ·", true); yield return HotUiCommand(remote, "煮珍珠 煮珍珠台 ·", true);
                        yield return HotUiWait(remote, () => view.Items.Values.Any(item => item.DefinitionId == 80), "Themed pearls cooked");
                        yield return HotUiCommand(remote, "拿 煮珍珠台 ·", true); yield return HotUiCommand(remote, "添加 小料罐台 ·", true);
                        yield return HotUiBuyRaw(remote, 24, "透明冷饮杯"); long cup = view.Actors[client.MemberId].HandItemId;
                        yield return HotUiCommand(remote, "放 红茶叶台 ·", true); yield return HotUiCommand(remote, "拿 交接台 ·", true); yield return HotUiCommand(remote, "放 牛奶台 ·", true);
                        yield return HotUiBuyRaw(remote, 22, "冰块"); yield return HotUiCommand(remote, "放 糖浆台 ·", true);
                        if (!(remote?.Presentation ?? Presentation).DetailsVisible) { Pointer(ClaimUiButton(remote?.Hud ?? Hud, "查看详情")); }
                        yield return HotUiCommand(remote, "取出 熟珍珠 · 小料罐台"); yield return HotUiCommand(remote, "添加 红茶叶台 ·", true);
                        yield return HotUiCommand(remote, "拿 红茶叶台 ·", true); yield return HotUiCommand(remote, "取一份 调饮台 ·", true); yield return HotUiCommand(remote, "放 红茶叶台 ·", true);
                        yield return HotUiCommand(remote, "拿 糖浆台 ·", true); yield return HotUiCommand(remote, "添加 红茶叶台 ·", true); yield return HotUiCommand(remote, "拿 红茶叶台 ·", true);
                        yield return HotUiCommand(remote, "混合 珍珠奶茶");
                        yield return HotUiWait(remote, () => view.Items[cup].Contents.Count == 1 && view.Items[view.Items[cup].Contents[0]].DefinitionId == 89, "Themed pearl milk tea mixed");
                        yield return HotUiCommand(remote, "放 红茶叶台 ·", true);
                    }
                    yield return HotUiSwitch(remote, true); yield return ContentUiPump(UnlockUiStartOperating(), remote); yield return HotUiSwitch(remote, false);
                    if (stage == 1)
                    {
                        yield return ThemedUiServe(remote, 12, "牛肉蒸饺", "蒸食盘台", false);
                        yield return ThemedUiServe(remote, 13, "热红茶", "热茶杯台", true);
                        Assert.That(view.LevelScore, Is.EqualTo(20));
                    }
                    else
                    {
                        yield return ThemedUiServe(remote, stage == 2 ? 14 : 15, stage == 2 ? "冰鲜奶茶" : "珍珠奶茶", stage == 2 ? "冷饮杯台" : "红茶叶台", true);
                        Assert.That(view.LevelScore, Is.EqualTo(10));
                    }
                    yield return HotUiWait(remote, () => view.LevelPhase == LevelPhase.Completed && (stage == 3 || view.RenovationChoiceVersion > 0), "Themed front actually drains at stage " + stage, 90);
                    yield return HotUiSwitch(remote, true);
                    Assert.That(Hud.PhaseText.text, Does.Contain("蒸茶小馆").And.Contain($"第 {stage}/3 关"));
                    if (stage < 3)
                    {
                        if (stage == 1) { yield return ContentUiPump(Command("装修 扩建单客桌"), remote); }
                        yield return HotUiWait(remote, () => Hud.GetComponentsInChildren<Button>(false).Any(button =>
                            button.GetComponentInChildren<Text>().text == (cooperative ? "下一关" : "保存并继续")), "Current themed continuation appears after actual renovation publication", 20);
                        staleContinue = CaptureButtonCallback(Button(cooperative ? "下一关" : "保存并继续"));
                        yield return ContentUiPump(UnlockUiNext(cooperative), remote);
                        long command = Client.NextCommandId; staleContinue(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
                        Assert.That(Grid.Items.ContainsKey(teaBarrel), Is.True); Assert.That(Grid.Width, Is.EqualTo(9));
                        Assert.That(Grid.Stations.Count, Is.EqualTo(initialStations.Length + 1));
                        Assert.That(Grid.Items.Values.All(item => item.BoundOrderId == 0), Is.True);
                        if (!cooperative) { Assert.That(repository.Browse().Length, Is.EqualTo(stage)); }
                        yield return HotUiSwitch(remote, false);
                    }
                }
                Assert.That(Grid.RenovationCandidates, Is.Empty); Assert.That(Grid.SupplySources[22].RemainingPackages, Is.EqualTo(4));
                Assert.That(Grid.SupplySources[24].RemainingPackages, Is.EqualTo(4));
                var oldClient = Client; var finish = CaptureButtonCallback(Button(cooperative ? "完成大关并返回房间" : "保存完成并返回选档"));
                Pointer(Button(cooperative ? "完成大关并返回房间" : "保存完成并返回选档"));
                if (cooperative)
                {
                    yield return WaitCoopUi(remote, () => Presentation == null && !remote.Grid.Confirmed && ((UIGridKitchen)Entry.Hud)?.Available == true, "Themed terminal returns same room");
                    Assert.That(Client.Roster.Count, Is.EqualTo(2));
                    yield return StartPreviewAndConfirm(EntryButton("开启大关 3 · 蒸茶小馆"));
                    yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed, "Same room independently replays themed chapter");
                }
                else
                {
                    yield return Wait(() => Client == null && Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Any(header => header.RecordKind == SoloSaveRecordKind.ChapterCompleted), "Themed completed record returned to selector", 30);
                    var record = SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                    Assert.That(record.GetComponentInChildren<Text>().text, Does.Contain("蒸茶小馆").And.Contain("3/3"));
                    Pointer(record); yield return Wait(() => Selection.ViewedResult != null && !Selection.LoadingSelection, "Themed completion is read only");
                    Assert.That((Scene)Runtime.HostScene, Is.Null); Assert.That(Selection.ViewedResult.NextLevelId, Is.Zero);
                    Pointer(SelectionButton("返回记录列表"));
                    yield return StartPreviewAndConfirm(SelectionButton("新开大关 3 · 蒸茶小馆"));
                    yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Independent themed replay starts");
                    Assert.That(repository.Browse().Length, Is.EqualTo(3));
                }
                Assert.That(Grid.LevelConfigId, Is.EqualTo(first)); Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
                Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Stations.Count, Is.EqualTo(initialStations.Length));
                Assert.That(Grid.Items.ContainsKey(teaBarrel), Is.False); Assert.That(Grid.AutoProcesses, Is.Empty);
                long before = oldClient.NextCommandId; staleContinue(); finish(); finish(); Assert.That(oldClient.NextCommandId, Is.EqualTo(before));
                Assert.That(Grid.LevelConfigId, Is.EqualTo(first));
            }
            finally { remote?.Dispose(); }
        }

        private void ContentUiReadRecipeForRemote(CoopUiRemote remote, string name)
        {
            var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
            long command = client.NextCommandId; int items = view.Items.Count;
            Pointer(ClaimUiButton(remote?.Hud ?? Hud, "全部动作"));
            Pointer(ClaimUiButton(remote?.Hud ?? Hud, "菜谱 " + name));
            Pointer(ClaimUiButton(remote?.Hud ?? Hud, "关闭菜谱"));
            Assert.That(client.NextCommandId, Is.EqualTo(command)); Assert.That(view.Items.Count, Is.EqualTo(items));
        }

        [UnityTest, Category("ThemedChapterUi"), Timeout(600000)]
        public IEnumerator ThemedUiSoloOriginalLauncherFourDishesThreeStagesActualFilesTerminalAndIndependentReplay() => ThemedUiJourney(false);

        [UnityTest, Category("ThemedChapterUi"), Timeout(600000)]
        public IEnumerator ThemedUiCooperativeHostCooksKcpFourDishesThreeStagesTerminalSameRoomAndIndependentReplay() => ThemedUiJourney(true);
    }
}
#endif
