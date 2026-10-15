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
        private IEnumerator ContentUiPump(IEnumerator operation, CoopUiRemote remote)
        {
            while (operation.MoveNext())
            {
                if (operation.Current is IEnumerator nested)
                {
                    yield return ContentUiPump(nested, remote);
                }
                else
                {
                    remote?.Tick();
                    yield return operation.Current;
                }
            }
        }

        private void ContentUiReadRecipe(string name)
        {
            long command = Client.NextCommandId;
            int items = Grid.Items.Count;
            Pointer(Button("菜谱 " + name));
            Assert.That(Presentation.RecipeVisible, Is.True);
            Assert.That(Hud.GetComponentsInChildren<Text>(false).Any(text => text.text.Contains(name)), Is.True);
            Pointer(Button("关闭菜谱"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items.Count, Is.EqualTo(items), "Reading a newly available recipe creates no ingredients");
        }

        private IEnumerator ContentUiJourney(bool cooperative)
        {
            SoloSaveRepository repository = null;
            if (cooperative)
            {
                yield return StartProgressHostRoom();
                Assert.That(EntryButton("开启大关 1 · 家常开张"), Is.Not.Null);
                Pointer(EntryButton("开启大关 2 · 进阶小馆"));
            }
            else
            {
                yield return StartModeLauncher();
                repository = CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Actual content chapter selector");
                Assert.That(SelectionButton("新开大关 1 · 家常开张"), Is.Not.Null);
                Pointer(SelectionButton("新开大关 2 · 进阶小馆"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Selected second content chapter opens");
            var chapter = Tables.Instance.DTRestaurantChapterConfig.Get(2);
            Assert.That(Grid.LevelConfigId, Is.EqualTo(cooperative ? chapter.CooperativeFirstLevelId : chapter.SoloFirstLevelId));
            Assert.That(Grid.AllowedDishIds, Is.EquivalentTo(Tables.Instance.DTLevelConfig.Get(Grid.LevelConfigId).AllowedDishIds
                .Concat(Tables.Instance.DTLevelConfig.Get(Grid.LevelConfigId).AllowedDrinkIds)));
            Assert.That(Grid.AllowedDishIds.Contains(6) && Grid.AllowedDishIds.Contains(9), Is.True);
            Assert.That(Hud.PhaseText.text, Does.Contain("进阶小馆").And.Contain("第 1/3 关"));
            if (!cooperative)
            {
                long command = Client.NextCommandId;
                Pointer(Button("查看首单步骤"));
                Assert.That(Presentation.RecipeDishId, Is.EqualTo(3), "First guide follows this chapter's configured first order");
                Pointer(Button("关闭菜谱"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
            }
            ContentUiReadRecipe("鲜榨橙汁");
            ContentUiReadRecipe("芝士蔬菜沙拉");
            yield return Command("拿 橙子台 ·", true);
            yield return Command("放 切配台 ·", true);
            yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.OrangeChunks), "Actual chapter orange preparation");
            yield return Command("拿 切配台 ·", true);
            yield return Command("添加 果汁壶台 ·", true);
            yield return Command("拿 果汁壶台 ·", true);
            yield return Command("放 榨汁机 ·", true);
            yield return Command("榨汁 榨汁机 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(process => process.State == GridAutoState.Ready && process.LockContainer), "Actual chapter juice completes");
            yield return Command("采购 橙子", true);
            yield return Wait(() => Grid.SupplyShipments.Values.Any(shipment => shipment.SourceId == 4 && shipment.Stage == ShipmentStage.Ready), "Finite chapter supply actually arrives");
            yield return Command("接收 橙子", true);
            yield return Command("拆一份 橙子", true);
            long replenished = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("放 橙子台 ·", true);
            Assert.That(Grid.SupplySources[4].RemainingPackages, Is.EqualTo(4));
            Assert.That(Grid.Stations.Values.Single(station => station.Name == "橙子台").ItemId, Is.EqualTo(replenished));
            yield return Command("拿 杯台 ·", true);
            long cup = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("盛一份 榨汁机 ·", true);
            yield return Command("放 贴单台 ·", true);
            string world = Grid.WorldId;
            yield return UnlockUiStartOperating();
            CoopUiRemote remote = null;
            if (cooperative)
            {
                long movement = Client.NextCommandId + 1;
                Presentation.ClickGridCell(new GridCell(2, 10));
                yield return Wait(() => Grid.LastCommandId == movement && Grid.LastStatus == GridResultStatus.Executed, "Host parks through ordinary map input");
                Presentation.CloseGridHud();
                remote = new CoopUiRemote(Runtime.ListenEndpoint);
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "KCP teammate observes selected chapter");
            }
            try
            {
                var view = remote?.Grid ?? Grid;
                yield return ContentUiPump(Wait(() => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 3), "Configured second chapter first drink order", 30), remote);
                var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 3);
                string table = view.Stations[order.TableStationId].Name;
                yield return AssemblyUiCommand(remote, "贴单 → " + table);
                yield return AssemblyUiCommand(remote, "拿 贴单台 ·", true);
                yield return AssemblyUiCommand(remote, "前往 " + table);
                yield return AssemblyUiCommand(remote, "交付 鲜榨橙汁 → " + table);
                Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(view.LevelScore, Is.EqualTo(10));
                Assert.That(view.Items.ContainsKey(cup), Is.False);
                yield return ContentUiPump(Wait(() => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Served customer leaves naturally", 15), remote);
                yield return AssemblyUiCommand(remote, "清桌 " + table, true);
                if (remote != null)
                {
                    remote.Presentation.CloseGridHud();
                    Presentation.OpenGridHud();
                    yield return WaitCoopUi(remote, () => Presentation?.Ready == true, "Host resumes ordinary chapter controls");
                }
                for (int stage = 1; stage <= 3; ++stage)
                {
                    Assert.That(Grid.LevelConfigId, Is.EqualTo((cooperative ? chapter.CooperativeFirstLevelId : chapter.SoloFirstLevelId) + stage - 1));
                    Assert.That(Hud.PhaseText.text, Does.Contain("进阶小馆").And.Contain($"第 {stage}/3 关"));
                    if (stage > 1)
                    {
                        ContentUiReadRecipe(stage == 2 ? "番茄蛋花汤" : "盘装吐司");
                        yield return ContentUiPump(UnlockUiStartOperating(), remote);
                    }
                    yield return ContentUiPump(Wait(() => Grid.LevelPhase == LevelPhase.Completed &&
                        (stage == 3 || Grid.RenovationLevelId == Grid.LevelId && Grid.RenovationChoiceVersion > 0), "Actual second chapter stage naturally closes " + stage, 90), remote);
                    Assert.That(Grid.WorldId, Is.EqualTo(world));
                    if (stage < 3)
                    {
                        if (Grid.RenovationCandidates.Contains(8) && Grid.SelectedRenovationId == 0)
                        {
                            yield return ContentUiPump(Command("装修 扩建单客桌"), remote);
                        }
                        string next = cooperative ? "下一关" : "保存并继续";
                        yield return ContentUiPump(Wait(() => Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == next), "Current continuation control appears after renovation selection"), remote);
                        var retired = CaptureButtonCallback(Button(cooperative ? "下一关" : "保存并继续"));
                        yield return ContentUiPump(UnlockUiNext(cooperative), remote);
                        long command = Client.NextCommandId;
                        retired();
                        Assert.That(Client.NextCommandId, Is.EqualTo(command));
                        Assert.That(Grid.Items.ContainsKey(replenished), Is.True);
                        Assert.That(Grid.SupplySources[4].RemainingPackages, Is.EqualTo(4));
                    }
                }
                Assert.That(Grid.RenovationCandidates, Is.Empty);
                Assert.That(Hud.SettlementText.text, Does.Contain("大关完成"));
                string finishCaption = cooperative ? "完成大关并返回房间" : "保存完成并返回选档";
                yield return ContentUiPump(Wait(() => Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == finishCaption), "Actual terminal control becomes visible"), remote);
                var oldClient = Client;
                var finish = CaptureButtonCallback(Button(cooperative ? "完成大关并返回房间" : "保存完成并返回选档"));
                Pointer(Button(cooperative ? "完成大关并返回房间" : "保存完成并返回选档"));
                if (cooperative)
                {
                    yield return WaitCoopUi(remote, () => Presentation == null && !remote.Grid.Confirmed && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Completed second chapter returns same room");
                    Assert.That(Client.Roster.Count, Is.EqualTo(2));
                    Pointer(EntryButton("开启大关 1 · 家常开张"));
                    yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed, "Same cooperative room independently opens first chapter");
                }
                else
                {
                    yield return Wait(() => Client == null && Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Any(header => header.RecordKind == SoloSaveRecordKind.ChapterCompleted), "Actual second chapter completed record returns to selector", 30);
                    var record = SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                    Assert.That(record.GetComponentInChildren<Text>().text, Does.Contain("进阶小馆").And.Contain("3/3").And.Contain("已完成"));
                    Pointer(record);
                    yield return Wait(() => Selection.ViewedResult != null && !Selection.LoadingSelection, "Completed chapter record is read without creating a world");
                    Assert.That((Scene)Runtime.HostScene, Is.Null);
                    Assert.That(Selection.ViewedResult.ChapterId, Is.EqualTo(12));
                    Assert.That(SelectionHud.SettlementText.text, Does.Contain("进阶小馆"));
                    Pointer(SelectionButton("新开大关 1 · 家常开张"));
                    yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Completed record permits an independent first chapter");
                    Assert.That(repository.Browse().Length, Is.EqualTo(3));
                }
                Assert.That(Grid.LevelConfigId, Is.EqualTo(cooperative ? 2101 : 1101));
                Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
                Assert.That(Grid.Width, Is.EqualTo(7));
                Assert.That(Grid.Items.ContainsKey(replenished), Is.False);
                Assert.That(Grid.SupplySources[4].RemainingPackages, Is.EqualTo(5));
                Assert.That(Grid.Tables.Count, Is.EqualTo(2));
                long previousCommand = oldClient.NextCommandId;
                finish();
                Assert.That(oldClient.NextCommandId, Is.EqualTo(previousCommand));
            }
            finally
            {
                remote?.Dispose();
            }
        }

        [UnityTest, Category("RestaurantContentChapterUi"), Timeout(420000)]
        public IEnumerator RestaurantContentChapterUiSoloActualSecondChapterOrderSupplyTerminalRecordAndIndependentReplay() => ContentUiJourney(false);

        [UnityTest, Category("RestaurantContentChapterUi"), Timeout(420000)]
        public IEnumerator RestaurantContentChapterUiCooperativeActualSecondChapterRemoteDeliveryTerminalAndSameRoomReplay() => ContentUiJourney(true);
    }
}
#endif
