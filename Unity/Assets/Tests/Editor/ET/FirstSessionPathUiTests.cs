#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using Game;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("FirstSessionPath"), Category("EntryFlowClarity")]
        public IEnumerator FirstSessionPathAllChaptersTrialsCancelAndRetiredPreviewRemainReadOnly()
        {
            yield return StartLauncher(chooseGame: false);
            var repository = CreateSaveUiRepository(Runtime, out string directory, out _);
            try
            {
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true, "Selector reopens against the owned test repository");
                Assert.That(Selection.Page, Is.EqualTo(SoloEntryPage.Home));
                Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "内存试玩"), Is.False);
                Pointer(SelectionButton("选择其他章节"));
                Assert.That(Selection.Page, Is.EqualTo(SoloEntryPage.Chapters));
                foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList)
                {
                    var retired = CaptureButtonCallback(SelectionButton("确认开始"));
                    Pointer(SelectionButton($"新开大关 {chapter.Id} · {chapter.Name}"));
                    Assert.That(Selection.PreviewChapterId, Is.EqualTo(chapter.Id));
                    Assert.That(SelectionHud.SettlementText.text, Does.Contain(chapter.Name));
                    retired();
                    Assert.That((Scene)Runtime.HostScene, Is.Null);
                    Assert.That(repository.Browse(), Is.Empty);
                }
                Pointer(SelectionButton("菜单试玩"));
                foreach (string caption in new[] { "新开餐厅", "内存试玩", "新开沙拉餐厅", "新开经营成长餐厅", "新开热餐餐厅", "新开甜品餐厅", "新开咖啡餐厅", "新开蒸食餐厅", "新开茶饮餐厅" })
                {
                    Pointer(SelectionButton(caption));
                    Assert.That(Selection.PreviewChapterId, Is.Zero);
                    Assert.That(SelectionHud.SettlementText.text, Does.Contain("独立菜单试玩"));
                    Pointer(SelectionButton("返回开始方式"));
                    Assert.That((Scene)Runtime.HostScene, Is.Null);
                    Assert.That(repository.Browse(), Is.Empty);
                }
                Pointer(SelectionButton("返回单机首页"));
                yield return ReachAndClick(SelectionButton("开始第一章 · 家常开张"));
                Assert.That(Selection.PreviewChapterId, Is.EqualTo(1));
                Assert.That(Selection.PreviewMenuVariant, Is.EqualTo(ProductMenuVariant.Growth));
                yield return ReachAndClick(SelectionButton("确认开始"));
                yield return Wait(() => Presentation?.Ready == true, "Only the explicitly confirmed first chapter starts");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(1101));
                Assert.That(Presentation.FirstOrderGuideVisible, Is.True);
                Assert.That(repository.Browse(), Is.Empty, "Starting alone never writes a success record");
                yield return ReadableChooseBuff();
                yield return ReachAndClick(Button("查看首单目标位置"));
                Assert.That(Presentation.FirstGuideLocatedItem, Is.Not.Zero);
                yield return ReadableCommand("拿 备料台 ·", true);
                yield return ReadableCommand("放 切配台 ·", true);
                Assert.That(Presentation.FirstGuideLocatedItem, Is.Zero, "The old material location is withdrawn when the actual next step changes");
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveUiFiles(directory);
            }
        }

        private IEnumerator ReadableCommand(string caption, bool prefix = false)
        {
            long id = Client.NextCommandId + 1;
            yield return ReachAndClick(Button(caption, prefix));
            if (caption.StartsWith("装修 "))
            {
                Assert.That(Client.NextCommandId, Is.EqualTo(id - 1));
                yield return ReachAndClick(Button("确认此装修"));
            }
            Assert.That(Client.NextCommandId, Is.EqualTo(id));
            yield return Wait(() => Grid.LastCommandId == id && Grid.LastStatus == GridResultStatus.Executed, "Real hit executes: " + caption);
        }

        [UnityTest, Category("FirstSessionPath"), Category("FirstChapterJourney"), Timeout(900000)]
        public IEnumerator FirstSessionPathActualThreeLevelsSoupToastSaladAndNaturalClosing()
        {
            yield return StartLauncher(chooseGame: false);
            var repository = CreateSaveUiRepository(Runtime, out string directory, out _);
            var sound = Game.GameEntry.Sound.GetSoundGroup(ProductAudioSettings.Group);
            bool oldMute = sound.Mute; float oldVolume = sound.Volume;
            try
            {
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true, "Selector reopens against the owned test repository");
                yield return ReachAndClick(SelectionButton("开始第一章 · 家常开张"));
                yield return ReachAndClick(SelectionButton("确认开始"));
                yield return Wait(() => Presentation?.Ready == true && Grid.BuffDrawVersion > 0, "First chapter ready");
                sound.Mute = false; sound.Volume = .6f;
                Assert.That(Presentation.FirstSessionStep(Grid).Kind, Is.EqualTo(GridRecipeStepKind.BuffChoice));
                yield return ReadableCommand("暂停");
                long pausedTick = Grid.Tick;
                Assert.That(Presentation.FirstSessionStep(Grid).Description, Does.Contain("已暂停"));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(Grid.Tick, Is.EqualTo(pausedTick));
                yield return ReadableCommand("恢复");
                long command = Client.NextCommandId;
                yield return ReachAndClick(Button("跳过首单引导"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(Presentation.FirstOrderGuideVisible, Is.False);
                yield return ReachAndClick(Button("重看首单引导"));
                string buff = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
                yield return ReadableCommand(buff);
                GridRecipeStep initial = GridRecipeGuide.CurrentStep(Grid, 1);
                Assert.That(initial.ItemId, Is.Not.Zero);
                command = Client.NextCommandId;
                yield return ReachAndClick(Button("查看首单目标位置"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Locate only changes presentation");
                var oldLocation = CaptureButtonCallback(Button("查看首单目标位置"));
                yield return ReadableCommand("拿 备料台 ·", true);
                oldLocation();
                yield return ReadableCommand("放 切配台 ·", true);
                yield return ReadableCommand("切配 切配台 ·", true);
                yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.TomatoChunks), "Chopping really finishes");
                Assert.That(GridRecipeGuide.CurrentStep(Grid, 1).StageId, Is.Not.EqualTo(initial.StageId), "Existing prepared tomato is not requested again");
                yield return ReadableCommand("拿 切配台 ·", true);
                yield return ReadableCommand("添加 灶台 ·", true);
                yield return ReadableCommand("拿 食材台 ·", true);
                yield return ReadableCommand("添加 碗台 ·", true);
                yield return ReadableCommand("拿 碗台 ·", true);
                yield return ReadableCommand("开始打蛋");
                yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.EggLiquid), "Egg work really finishes");
                yield return ReadableCommand("倒蛋液 灶台 ·", true);
                yield return ReadableCommand("放 碗台 ·", true);
                yield return ReadableCommand("煮汤 灶台 ·", true);
                Assert.That(GridRecipeGuide.CurrentStep(Grid, 1).Kind, Is.EqualTo(GridRecipeStepKind.Waiting));
                yield return Wait(() => Grid.AutoProcesses.Values.Any(work => work.State == GridAutoState.Ready), "Soup really finishes");
                yield return ReadableCommand("拿 碗台 ·", true);
                yield return ReadableCommand("盛一份 灶台 ·", true);
                yield return ReadableCommand("放 碗台 ·", true);
                Assert.That(GridRecipeGuide.CurrentNeed(Grid, 1), Does.Contain("已备好，无需重做"));
                var beforeReopen = CaptureButtonCallback(Button("查看首单目标位置"));
                command = Client.NextCommandId;
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                yield return Wait(() => Presentation.Ready, "First-chapter guide survives an actual managed HUD reopen");
                Assert.That(Presentation.FirstOrderGuideVisible, Is.True);
                beforeReopen();
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                yield return ReadableCommand("开始营业");
                yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 1), "A real guest creates the first soup order", 35);
                yield return ReadableCommand("拿 碗台 ·", true);
                var order = Grid.Orders[Presentation.FirstGuideOrderId];
                string table = Grid.Stations[order.TableStationId].Name;
                yield return ReachAndClick(Button("查看首单目标位置"));
                long expired = order.Id;
                var expiredLocation = CaptureButtonCallback(Button("查看首单目标位置"));
                command = Client.NextCommandId;
                yield return Wait(() => Grid.Orders[expired].State == OrderState.Expired && Presentation.FirstGuideNeedsOrderChoice, "The selected first order really expires under its normal sixty-second deadline", 75);
                Assert.That(Presentation.FirstSessionStep(Grid).Kind, Is.EqualTo(GridRecipeStepKind.Unavailable));
                Assert.That(Presentation.FirstGuideLocatedItem, Is.Zero);
                Assert.That(Presentation.FirstGuideLocatedStation, Is.Zero);
                expiredLocation();
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                order = Grid.Orders.Values.Where(value => value.State == OrderState.Waiting && value.DishId == 1).OrderBy(value => value.Id).First();
                table = Grid.Stations[order.TableStationId].Name;
                yield return ReachAndClick(Button("选择首单订单 · " + table));
                Assert.That(Presentation.FirstGuideOrderId, Is.EqualTo(order.Id));
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Choosing another current order is explicit read-only navigation");
                yield return ReachAndClick(Button("查看首单目标位置"));
                yield return ReadableCommand("前往 " + table);
                yield return ReadableCommand("交付 番茄蛋花汤 → " + table);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Serve] == 1, "Actual first soup delivery emits exactly one success cue");
                Assert.That(Grid.ServedOrders, Is.EqualTo(1));
                Assert.That(Presentation.FirstSessionStep(Grid).Kind, Is.EqualTo(GridRecipeStepKind.FirstOrderComplete));
                yield return ReadableClearServedTable();
                // Stand clear of the sole table approach so ordinary companions can finish cleanup.
                GridCell destination = new(3, 6);
                var point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(destination));
                Assert.That(Board.InputScreenRect.Contains(point), Is.True);
                long move = Client.NextCommandId + 1;
                Presentation.ProcessGridPointer(point, true, true, false);
                Presentation.ProcessGridPointer(point, false, false, true);
                Assert.That(Client.NextCommandId, Is.EqualTo(move));
                yield return Wait(() => Grid.LastCommandId == move && Grid.LastStatus == GridResultStatus.Executed, "Ordinary map movement leaves the table approach clear");
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Unaccelerated natural service and draining", 180);
                Assert.That(Presentation.FirstSessionStep(Grid).Kind, Is.EqualTo(GridRecipeStepKind.Completed));
                Assert.That(Hud.SettlementText.text, Does.Contain("成功 1 单"));
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.LevelComplete] == 1, "Natural completion emits exactly one settlement cue");
                Assert.That(Grid.LevelStars, Is.EqualTo(1), "One real soup delivery reaches the original ten-point first-star threshold");
                yield return ReadableChapterNext();
                Assert.That(Grid.LevelConfigId, Is.EqualTo(1102), "The original save operation continues the chosen chapter");
                yield return ReadableChooseBuff();
                yield return ReadableCommand("拿 面包台 ·", true);
                yield return ReadableCommand("放 烤箱 ·", true);
                yield return ReadableCommand("烘烤 烤箱 ·", true);
                yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.Toast), "Actual second-stage toast finishes");
                yield return ReadableCommand("拿 烤箱 ·", true);
                yield return ReadableCommand("添加 盘台 ·", true);
                yield return ReadableCommand("拿 盘台 ·", true);
                yield return ReadableCommand("放 交接台 ·", true);
                yield return ReadableServePrepared(2, "交接台");
                yield return ReadableClearServedTable();
                yield return ReadableMoveAside();
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Stage two naturally drains at its real timing", 210);
                Assert.That(Grid.ServedOrders, Is.EqualTo(1));
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.LevelComplete] == 2, "Second natural completion emits one new cue");
                yield return ReadableChapterNext();
                Assert.That(Grid.LevelConfigId, Is.EqualTo(1103));
                yield return ReadableChooseBuff();
                yield return ReadableCommand("取回 盘", true);
                yield return ReadableCommand("放 盘台 ·", true);
                int supply = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Tomato).Id;
                int balance = Grid.SupplySources[supply].RemainingPackages;
                yield return ReadableCommand("采购 番茄", true);
                yield return Wait(() => Grid.SupplyShipments.Values.Any(value => value.SourceId == supply && value.Stage == ShipmentStage.Ready), "Finite tomato shipment arrives through ordinary time");
                yield return ReadableCommand("接收 番茄", true);
                yield return ReadableCommand("拆一份 番茄", true);
                Assert.That(Grid.SupplySources[supply].RemainingPackages, Is.EqualTo(balance - 1));
                yield return ReadableChopIntoPlate(null, GridKitchenRules.TomatoChunks);
                yield return ReadableChopIntoPlate("生菜台", 23);
                yield return ReadableChopIntoPlate("黄瓜台", 25);
                yield return ReadableCommand("拿 盘台 ·", true);
                yield return ReadableCommand("组装 田园沙拉");
                int salad = Tables.Instance.DTDishConfig.Get(4).ContentDefinitionId;
                yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == salad), "Salad is assembled from the three actual chopped ingredients");
                yield return ReadableCommand("放 盘台 ·", true);
                yield return ReadableServePrepared(4, "盘台");
                yield return ReadableClearServedTable();
                yield return ReadableMoveAside();
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Stage three naturally drains at its real timing", 240);
                Assert.That(Grid.ServedOrders, Is.EqualTo(1));
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.LevelComplete] == 3 && Feedback.Requested[(int)ProductFeedbackKind.Serve] == 3, "Three actual deliveries and settlements each emit their own cue without history replay");
                Assert.That(Grid.RenovationCandidates, Is.Empty);
                Assert.That(Tables.Instance.DTLevelConfig.Get(Grid.LevelConfigId).NextLevelConfigId, Is.Zero);
                Assert.That(Hud.SettlementText.text, Does.Contain("大关完成"));
                var finish = CaptureButtonCallback(Button("保存完成并返回选档"));
                yield return ReachAndClick(Button("保存完成并返回选档"));
                yield return Wait(() => Client == null && Selection?.Headers.Any(value => value.RecordKind == SoloSaveRecordKind.ChapterCompleted) == true, "Terminal completion returns without creating a fourth stage");
                finish();
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                if (Selection.Page == SoloEntryPage.Home)
                {
                    yield return ReachAndClick(SelectionButton("继续记录"));
                }
                var record = SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                yield return ReachAndClick(record);
                yield return Wait(() => Selection.ViewedResult != null, "Completed chapter is inspected read-only");
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                Assert.That(Selection.ViewedResult.CompletedLevelId, Is.EqualTo(1103));
            }
            finally
            {
                sound.Mute = oldMute; sound.Volume = oldVolume;
                repository.Dispose();
                RemoveOwnSaveUiFiles(directory);
            }
        }

        private IEnumerator ReadableChooseBuff()
        {
            yield return Wait(() => Grid.BuffDrawVersion > 0 && Hud.BuffActionsRectTransform.GetComponentsInChildren<Button>(false).Length > 0, "Current actual buff choices");
            string buff = Hud.BuffActionsRectTransform.GetComponentsInChildren<Button>(false).First().GetComponentInChildren<Text>().text;
            yield return ReadableCommand(buff);
        }

        private IEnumerator ReadableChapterNext()
        {
            yield return Wait(() => Grid.RenovationChoiceVersion > 0 && Grid.RenovationLevelId == Grid.LevelId, "Current renovation result");
            if (Grid.RenovationCandidates.Contains(8) && Grid.SelectedRenovationId == 0)
            {
                yield return ReadableCommand("装修 扩建单客桌");
            }
            else if (Grid.RenovationCandidates.Count > 0 && Grid.SelectedRenovationId == 0 && !Grid.RenovationSelectionFinalized)
            {
                int candidate = Grid.RenovationCandidates.First();
                yield return ReadableCommand("装修 " + Tables.Instance.DTRenovationConfig.Get(candidate).Name, true);
            }
            yield return Wait(() => Hud.GetComponentsInChildren<Button>(false).Any(button => button.name == "保存并继续"), "Original save-and-continue control");
            long level = Grid.LevelId;
            yield return ReachAndClick(Button("保存并继续"));
            yield return Wait(() => Grid.LevelId != level && Grid.LevelPhase == LevelPhase.Preparing && Grid.BuffDrawVersion > 0, "Actual saved next stage");
        }

        private IEnumerator ReadableMoveAside()
        {
            GridCell cell = new(3, 6);
            Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(cell));
            Assert.That(Board.InputScreenRect.Contains(point), Is.True);
            long command = Client.NextCommandId + 1;
            Presentation.ProcessGridPointer(point, true, true, false);
            Presentation.ProcessGridPointer(point, false, false, true);
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Normal map input leaves the cleared table approach free");
        }

        private IEnumerator ReadableClearServedTable()
        {
            long table = Grid.Orders.Values.Single(order => order.State == OrderState.Served).TableStationId;
            yield return Wait(() => Grid.FrontWorks.Values.Any(work => work.StationId == table && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Served guest leaves and creates the player's clear-table work", 15);
            long id = Grid.FrontWorks.Values.Single(work => work.StationId == table && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available).Id;
            yield return ReadableCommand("清桌 " + Grid.Stations[table].Name);
            yield return Wait(() => Grid.FrontWorks[id].State == FrontWorkState.Completed && Grid.Actors[Client.MemberId].FrontWorkId == 0, "The player actually finishes clearing the table");
        }

        private IEnumerator ReadableServePrepared(int dish, string counter)
        {
            yield return ReadableCommand("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.DishId == dish && order.State == OrderState.Waiting), "Actual guest orders dish " + dish, 75);
            var order = Grid.Orders.Values.Where(value => value.DishId == dish && value.State == OrderState.Waiting).OrderBy(value => value.Id).First();
            string table = Grid.Stations[order.TableStationId].Name;
            Button destination = Button("前往 " + table);
            var orderIcon = destination.GetComponentsInChildren<Image>(true).Single(image => image.name == "OrderDishIcon");
            var sprites = (Sprite[])typeof(MonoUIGridKitchen).GetField("handArt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Hud);
            Assert.That(orderIcon.sprite, Is.SameAs(sprites[Tables.Instance.DTDishConfig.Get(dish).ContentDefinitionId - 1]), "Order icon describes its own dish while the actor's hand is empty");
            yield return ReadableCommand("拿 " + counter + " ·", true);
            yield return ReadableCommand("前往 " + table);
            yield return ReadableCommand("交付 " + Tables.Instance.DTDishConfig.Get(dish).Name + " → " + table);
            Assert.That(Grid.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
        }

        private IEnumerator ReadableChopIntoPlate(string source, int output)
        {
            if (source != null)
            {
                yield return ReadableCommand("拿 " + source + " ·", true);
            }
            yield return ReadableCommand("放 切配台 ·", true);
            yield return ReadableCommand("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == output && item.Location == GridItemLocation.Counter), "Actual ingredient chopped: " + output);
            yield return ReadableCommand("拿 切配台 ·", true);
            yield return ReadableCommand("添加 盘台 ·", true);
        }
    }
}
#endif
