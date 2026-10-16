using System;
using System.Linq;
using UnityEngine;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static bool HasActionAdvice(GridClientComponent grid) =>
            grid.LastStatus is GridResultStatus.Rejected or GridResultStatus.Cancelled && !string.IsNullOrEmpty(grid.LastReason) ||
            grid.SaveState == SoloSaveState.Failed;

        private static ActionAdviceKind AdviceKind(GridClientComponent grid)
        {
            if (grid.LastStatus is not (GridResultStatus.Rejected or GridResultStatus.Cancelled) || string.IsNullOrEmpty(grid.LastReason))
            {
                return grid.SaveState == SoloSaveState.Failed ? ActionAdviceKind.Save : ActionAdviceKind.Unknown;
            }
            return grid.LastReason switch
            {
                "HandFull" or "WorkerBusy" => ActionAdviceKind.Hand,
                "ProcessLocked" or "WorkBusy" or "StationBusy" or "NotReady" => ActionAdviceKind.Locked,
                "ContainerFull" or "IncompatibleMaterial" or "NestedContainer" or "RequiresEmptyBowl" or "RequiresEggLiquidBowl" or
                    "RequiresHeldEggBowl" or "RequiresCleanAssemblyCarrier" or "AssemblyIngredientsMismatch" or "NoRecipe" or
                    "RequiresJuicer" or "RequiresMixer" or "RequiresGrinder" or "RequiresCoffeeMachine" or "RequiresSteamMachine" => ActionAdviceKind.Container,
                "Unreachable" or "BlockedCell" or "CounterNotAdjacent" or "PickupRequiresSameCell" or "RequiresTablePosition" => ActionAdviceKind.Path,
                nameof(ProductionPermissionDecision.MenuRestricted) or nameof(ProductionPermissionDecision.EquipmentLocked) or "DrinkMenuRequired" or "HostControlOnly" => ActionAdviceKind.Permission,
                "ReceivingFull" or "SupplyExhausted" or "ShipmentNotReady" or "AlreadyReceived" or "ShipmentChanged" => ActionAdviceKind.Supply,
                "TargetChanged" or "ItemChanged" or "ProcessChanged" or "OrderChanged" or "OrderEnded" or "BindingChanged" or "LayoutChanged" or "WrongLevel" => ActionAdviceKind.Target,
                "Paused" => ActionAdviceKind.Paused,
                "SaveFailed" => grid.SaveState == SoloSaveState.Failed ? ActionAdviceKind.Save : ActionAdviceKind.Unknown,
                _ => ActionAdviceKind.Unknown
            };
        }

        private static string AdviceExplanation(ActionAdviceKind kind) => kind switch
        {
            ActionAdviceKind.Hand => "手槽或工作占用条件不满足。\n先查看手持物和正在做的工作。\n自行选择合法空台面放下或停手。",
            ActionAdviceKind.Locked => "加工、工位占用或完成条件不满足。\n查看实际作业进度和锁定。\n停手只用于原来允许停手的工作。",
            ActionAdviceKind.Container => "载具、内容或配方条件不匹配。\n查看当前物品和对应菜谱。\n先核对空位、卫生和材料，再自行操作。",
            ActionAdviceKind.Path => "当前位置或通路不满足操作条件。\n查看工位占地及合法操作位置。\n不会替你移动角色或改布局。",
            ActionAdviceKind.Permission => "菜单、设备或角色权限不满足。\n查看本关菜谱和当前工位能力。\n不会通过查看解锁或取得房主控制。",
            ActionAdviceKind.Supply => "供应、货物或收货条件不满足。\n查看真实配额、货物和收货台。\n腾空或等待后，自行选择原接收动作。",
            ActionAdviceKind.Target => "原目标、订单或布局已经变化。\n重新查看当前物品和目标。\n不会将旧操作自动套到新目标。",
            ActionAdviceKind.Save => "成功检查点尚未保存。\n查看原结算中的保存状态和失败原因。\n原记录保留；需要你明确选择重试。",
            ActionAdviceKind.Paused => "世界当前已暂停。\n可查看帮助；阅读不会自动恢复。\n由有权限的玩家明确恢复后再操作。",
            _ => "暂未识别该原因。\n保留原始诊断信息，可主动复制。\n不会猜测解决办法或自动重试。"
        };

        private static void RememberAdviceRequest(this GridPresentationComponent self, GridClientComponent grid, long command, long target)
        {
            self.AdviceSubmittedCommand = command;
            self.AdviceSubmittedStation = grid.Stations.ContainsKey(target) ? target : 0;
            self.AdviceSubmittedItem = grid.Stations.TryGetValue(target, out var station) ? station.ItemId : 0;
            self.AdviceSubmittedLayout = grid.LayoutVersion;
        }

        private static void ClearActionAdvice(this GridPresentationComponent self)
        {
            ++self.ActionAdviceSerial;
            self.ActionAdviceCommand = 0;
            self.ActionAdviceReason = self.ActionAdviceSaveResult = self.ActionAdviceFeedback = null;
            self.ActionAdviceTargetValue = self.ActionAdviceHandValue = default;
            if (self.OverviewPage == GridOverviewPage.ActionAdvice) { self.OverviewPage = GridOverviewPage.None; }
            if (self.ActionGroup == GridActionGroup.ActionAdvice) { self.ActionGroup = self.ActionAdvicePreviousGroup; }
        }

        private static void ResetActionAdvice(this GridPresentationComponent self)
        {
            self.ClearActionAdvice();
            self.AdviceSubmittedCommand = self.AdviceSubmittedStation = self.AdviceSubmittedItem = self.AdviceSubmittedLayout = 0;
        }

        private static bool AdviceItemLocked(GridClientComponent grid, long id) => id != 0 &&
            (grid.AutoProcesses.Values.Any(value => value.PotItemId == id && value.State == GridAutoState.Cooking && value.LockContainer) ||
             grid.BakeProcesses.Values.Any(value => (value.InputItemId == id || value.CarrierItemId == id) && value.State == GridBakeState.Baking) ||
             grid.Processes.Values.Any(value => (value.InputItemId == id || value.TargetItemId == id) && value.State == GridWorkState.Working));

        private static bool AdviceItemMatches(GridClientComponent grid, long id, GridItemProjection expected, bool locked)
        {
            if (id == 0) { return expected.Id == 0; }
            return grid.Items.TryGetValue(id, out var current) && current.Id == expected.Id && current.Location == expected.Location &&
                current.HolderId == expected.HolderId && current.Hygiene == expected.Hygiene && current.BoundOrderId == expected.BoundOrderId &&
                current.AutoProcessId == expected.AutoProcessId && current.BakeProcessId == expected.BakeProcessId &&
                current.Contents.SequenceEqual(expected.Contents) && AdviceItemLocked(grid, id) == locked;
        }

        private static bool AdviceContextMatches(this GridPresentationComponent self, GridClientComponent grid) =>
            HasActionAdvice(grid) && self.ActionAdviceCommand == grid.LastCommandId && self.ActionAdviceStatus == grid.LastStatus &&
            self.ActionAdviceReason == grid.LastReason && self.ActionAdviceSaveState == grid.SaveState && self.ActionAdviceSaveResult == grid.SaveResultId &&
            self.ActionAdvicePhase == grid.LevelPhase && self.ActionAdvicePaused == grid.LevelPaused &&
            self.ActionAdviceStation == self.SelectedStationId && self.ActionAdviceLayout == grid.LayoutVersion &&
            self.ActionAdviceItem == (grid.Stations.TryGetValue(self.SelectedStationId, out var station) ? station.ItemId : 0) &&
            self.ActionAdviceHand == (grid.Actors.TryGetValue(grid.MemberId, out var actor) ? actor.HandItemId : 0) &&
            AdviceItemMatches(grid, self.ActionAdviceItem, self.ActionAdviceTargetValue, self.ActionAdviceTargetLocked) &&
            AdviceItemMatches(grid, self.ActionAdviceHand, self.ActionAdviceHandValue, self.ActionAdviceHandLocked);

        private static void ObserveActionAdvice(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.ActionAdviceCommand != 0 && (self.OverviewPage != GridOverviewPage.ActionAdvice || !self.AdviceContextMatches(grid)))
            {
                self.ClearActionAdvice();
            }
        }

        private static void OpenActionAdvice(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (!HasActionAdvice(grid)) { return; }
            self.ClearOperationHelp();
            self.ActionAdvicePreviousGroup = self.ActionGroup;
            self.ResetItemLocator(); self.ClearOrderNavigation(); self.ResetSupplyOverview(); self.ResetFrontWorkboard();
            self.ClearStationInspection(); self.ClearRenovationPreview();
            self.RecipeVisible = self.DetailsVisible = false; self.CompanionChat = CompanionChatTopic.None;
            self.OverviewPage = GridOverviewPage.ActionAdvice; self.ActionGroup = GridActionGroup.ActionAdvice;
            self.ActionAdviceCommand = grid.LastCommandId; self.ActionAdviceStatus = grid.LastStatus; self.ActionAdviceReason = grid.LastReason;
            self.ActionAdviceSaveState = grid.SaveState; self.ActionAdviceSaveResult = grid.SaveResultId;
            self.ActionAdvicePhase = grid.LevelPhase; self.ActionAdvicePaused = grid.LevelPaused;
            self.ActionAdviceStation = self.SelectedStationId; self.ActionAdviceLayout = grid.LayoutVersion;
            self.ActionAdviceItem = grid.Stations.TryGetValue(self.SelectedStationId, out var station) ? station.ItemId : 0;
            self.ActionAdviceHand = grid.Actors.TryGetValue(grid.MemberId, out var actor) ? actor.HandItemId : 0;
            self.ActionAdviceTargetValue = grid.Items.TryGetValue(self.ActionAdviceItem, out var targetValue) ? targetValue : default;
            self.ActionAdviceHandValue = grid.Items.TryGetValue(self.ActionAdviceHand, out var handValue) ? handValue : default;
            self.ActionAdviceTargetLocked = AdviceItemLocked(grid, self.ActionAdviceItem);
            self.ActionAdviceHandLocked = AdviceItemLocked(grid, self.ActionAdviceHand);
            self.ActionAdviceFeedback = null; ++self.ActionAdviceSerial;
            self.RefreshGridHud();
        }

        private static string ActionDiagnostic(GridClientComponent grid) =>
            "Match=" + grid.MatchId + "\nWorld=" + grid.WorldId + "/" + grid.WorldGeneration +
            "\nLevel=" + grid.LevelId + "/" + grid.LevelGeneration + "\nBinding=" + grid.BindingGeneration +
            "\nCommand=" + grid.LastCommandId + "/" + grid.LastCommandKind + "\nStatus=" + grid.LastStatus +
            "\nReason=" + grid.LastReason + "\nSaveState=" + grid.SaveState + "\nSaveResult=" + grid.SaveResultId;

        private static void RefreshActionAdvice(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.ActionAdvice)
            {
                if (self.ActionGroup == GridActionGroup.ActionAdvice) { self.ActionGroup = self.ActionAdvicePreviousGroup; }
                return;
            }
            if (self.ActionAdviceDisplayedSerial == self.ActionAdviceSerial) { return; }
            ActionAdviceKind kind = AdviceKind(grid);
            string raw = kind == ActionAdviceKind.Unknown ? "\n原始原因：" + grid.LastReason : "";
            view.SetInformation(kind == ActionAdviceKind.Save ? "保存未成功 · 处理说明" : "操作未完成 · 处理说明",
                AdviceExplanation(kind) + raw + "\n查看不会重新执行原动作。" + (self.ActionAdviceFeedback == null ? "" : "\n" + self.ActionAdviceFeedback), true, self.OrdersVisible);
            self.ActionAdviceDisplayedSerial = self.ActionAdviceSerial;
        }

        private static void AddActionAdvice(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (!HasActionAdvice(grid)) { return; }
            EntityRef<GridPresentationComponent> original = self;
            EntityRef<UIGridKitchen> hudRef = self.Hud;
            long epoch = self.HudLoadEpoch, serial = self.ActionAdviceSerial, command = grid.LastCommandId;
            string reason = grid.LastReason, world = grid.WorldId;
            long worldGeneration = grid.WorldGeneration, level = grid.LevelId, generation = grid.LevelGeneration, binding = grid.BindingGeneration;
            bool shown = self.OverviewPage == GridOverviewPage.ActionAdvice;
            GridPresentationComponent Current()
            {
                GridPresentationComponent active = original; UIGridKitchen hud = hudRef;
                var current = ((Scene)active?.IScene)?.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                return active?.Ready == true && hud?.Available == true && (UIGridKitchen)active.Hud == hud && active.HudLoadEpoch == epoch &&
                    active.ActionAdviceSerial == serial && current?.Confirmed == true && current.WorldId == world && current.WorldGeneration == worldGeneration &&
                    current.LevelId == level && current.LevelGeneration == generation && current.BindingGeneration == binding &&
                    current.LastCommandId == command && current.LastReason == reason && HasActionAdvice(current) &&
                    (!shown || active.AdviceContextMatches(current)) ? active : null;
            }
            view.AddControl(shown ? "关闭处理说明" : "查看处理说明", () =>
            {
                var active = Current(); if (active == null) { return; }
                if (shown) { active.ClearActionAdvice(); active.RefreshGridHud(); }
                else { active.OpenActionAdvice(grid); }
            });
            if (!shown) { return; }
            ActionAdviceKind kind = AdviceKind(grid);
            Link("复制诊断信息", active =>
            {
                GUIUtility.systemCopyBuffer = ActionDiagnostic(grid);
                active.ActionAdviceFeedback = "诊断信息已复制"; ++active.ActionAdviceSerial; active.RefreshGridHud();
            });
            if (kind == ActionAdviceKind.Unknown) { return; }
            if (kind == ActionAdviceKind.Save)
            {
                if (grid.LevelPhase == LevelPhase.Completed)
                {
                    Link("查看保存状态", active => Navigate(active, GridOverviewPage.Settlement, GridActionGroup.Layout));
                }
                return;
            }
            if (kind == ActionAdviceKind.Supply)
            {
                Link("查看供应与收货", active => Navigate(active, GridOverviewPage.Supply, GridActionGroup.Supply));
                return;
            }
            if (kind == ActionAdviceKind.Paused)
            {
                Link("查看暂停与权限帮助", active =>
                {
                    active.ClearActionAdvice(); active.OpenOperationHelp(); active.HelpTopic = GridHelpTopic.Phase;
                    ++active.HelpSerial; active.RefreshGridHud();
                });
                return;
            }
            Link("查看当前物品", active => { active.ClearActionAdvice(); active.OpenItemLocator(grid); });
            if (kind is ActionAdviceKind.Container or ActionAdviceKind.Permission)
            {
                Link("查看本关菜谱", active => Navigate(active, GridOverviewPage.None, GridActionGroup.Recipes));
            }
            if (kind != ActionAdviceKind.Target && self.AdviceSubmittedCommand == command && self.AdviceSubmittedStation == self.SelectedStationId &&
                self.AdviceSubmittedLayout == grid.LayoutVersion && grid.Stations.TryGetValue(self.SelectedStationId, out var station) &&
                station.ItemId == self.AdviceSubmittedItem)
            {
                Link("查看本次请求目标", active =>
                {
                    active.ClearActionAdvice(); active.OverviewPage = GridOverviewPage.Stations;
                    active.StationInspectionDetailsVisible = active.StationInspectionGeometryVisible = true;
                    active.ActionGroup = GridActionGroup.CurrentTarget; active.RefreshGridHud();
                });
            }
            void Link(string caption, Action<GridPresentationComponent> action) => view.AddTarget(caption, () =>
            {
                var active = Current(); if (active != null) { action(active); }
            }, group: GridActionGroup.ActionAdvice);
            void Navigate(GridPresentationComponent active, GridOverviewPage page, GridActionGroup group)
            {
                active.ClearActionAdvice(); active.OverviewPage = page; active.ActionGroup = group; active.RefreshGridHud();
            }
        }
    }
}
