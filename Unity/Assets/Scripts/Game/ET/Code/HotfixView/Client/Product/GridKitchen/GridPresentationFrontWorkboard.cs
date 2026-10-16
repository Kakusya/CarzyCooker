using System;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetFrontWorkboard(this GridPresentationComponent self) => self.FrontWorkboardSelectedId = 0;

        private static string FrontWorkboardKind(FrontWorkKind kind) => kind switch
        {
            FrontWorkKind.Ask => "询问",
            FrontWorkKind.Clear => "清桌",
            FrontWorkKind.Wash => "洗碗",
            _ => "工作已变化"
        };

        private static string FrontWorkboardState(FrontWorkState state) => state switch
        {
            FrontWorkState.Available => "可认领",
            FrontWorkState.Approaching => "前往中",
            FrontWorkState.Working => "执行中",
            FrontWorkState.Completed => "已完成",
            FrontWorkState.Cancelled => "已取消",
            _ => "状态已变化"
        };

        private static string FrontWorkboardWorker(GridClientComponent grid, GridFrontWorkProjection work)
        {
            if (work.WorkerKind == FrontWorkerKind.Player)
            {
                return grid.Actors.ContainsKey((int)work.WorkerId) ? "玩家 " + work.WorkerId : "原玩家已离场";
            }
            if (work.WorkerKind == FrontWorkerKind.Companion)
            {
                return grid.Companions.TryGetValue(work.WorkerId, out var companion) ? companion.Name : "原伙伴已离场";
            }
            return "无执行者";
        }

        private static void RefreshFrontWorkboard(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.FrontWork)
            {
                return;
            }
            int customers = grid.Customers.Values.Count(value => value.Phase != CustomerPhase.Gone);
            int active = grid.FrontWorks.Values.Count(value => value.State is FrontWorkState.Available or FrontWorkState.Approaching or FrontWorkState.Working);
            string text = $"当前顾客 {customers} · 必要工作 {active}";
            text += $"\n占用桌位 {grid.Tables.Values.Count(value => value.State != FrontTableState.Free)} · 待清桌 {grid.Tables.Values.Count(value => value.State == FrontTableState.Dirty)}";
            foreach (FrontWorkKind kind in new[] { FrontWorkKind.Ask, FrontWorkKind.Clear, FrontWorkKind.Wash })
            {
                int available = 0, approaching = 0, working = 0;
                foreach (var work in grid.FrontWorks.Values)
                {
                    if (work.Kind != kind)
                    {
                        continue;
                    }
                    if (work.State == FrontWorkState.Available) { ++available; }
                    else if (work.State == FrontWorkState.Approaching) { ++approaching; }
                    else if (work.State == FrontWorkState.Working) { ++working; }
                }
                text += $"\n{FrontWorkboardKind(kind)}：可认领 {available} · 前往 {approaching} · 执行 {working}";
            }
            text += grid.LevelPaused ? "\n已暂停：顾客与工作进度冻结" : grid.LevelPhase switch
            {
                LevelPhase.Preparing => "\n准备中：开店后按真实客流产生工作",
                LevelPhase.Draining => "\n正在收尾：已有顾客与必要工作自然完成；后厨不必排空",
                LevelPhase.Completed => "\n前厅已收口，本关已结算",
                _ => "\n查看不认领；使用原询问/清桌/洗碗按钮接手"
            };
            if (self.FrontWorkboardSelectedId != 0)
            {
                if (grid.FrontWorks.TryGetValue(self.FrontWorkboardSelectedId, out var work))
                {
                    string station = grid.Stations.TryGetValue(work.StationId, out var target) ? target.Name : "原目标已离开";
                    text += "\n\n所选：" + FrontWorkboardKind(work.Kind) + " · " + station + " · " + FrontWorkboardState(work.State);
                    text += "\n执行者：" + FrontWorkboardWorker(grid, work);
                    if (work.CustomerId != 0)
                    {
                        text += grid.Customers.TryGetValue(work.CustomerId, out var customer) && customer.Phase != CustomerPhase.Gone
                            ? "\n关联顾客：" + customer.ArrivalSequence : "\n原顾客已离开";
                    }
                    if (work.ItemId != 0)
                    {
                        text += "\n关联餐具：" + (grid.Items.TryGetValue(work.ItemId, out var item) ? item.Name : "原实物已离开");
                    }
                    text += $"\n实际进度：{Math.Clamp(work.Progress, 0, 1):P0}";
                    if (work.FrozenTicks > 0)
                    {
                        text += $" · 本次工作 {work.FrozenTicks * grid.TickMilliseconds / 1000f:0.0} 秒";
                    }
                }
                else
                {
                    text += "\n所选工作已离开当前关，请重新选择；不会改为同桌的新工作";
                }
            }
            view.SetInformation("前厅工作与收尾", text, true, self.OrdersVisible);
        }

        private static void AddFrontWorkboard(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.FrontWork;
            view.AddTarget(shown ? "关闭前厅看板" : "前厅工作看板", () =>
            {
                GridPresentationComponent current = original;
                if (current == null)
                {
                    return;
                }
                current.CloseItemLocator(); current.ClearOrderNavigation(); current.ResetSupplyOverview(); current.ResetFrontWorkboard();
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.FrontWork;
                current.RecipeVisible = current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.ActionGroup = GridActionGroup.FrontService;
                current.RefreshGridHud();
            });
            if (!shown)
            {
                return;
            }
            int index = 0;
            foreach (var work in grid.FrontWorks.Values.OrderBy(value => value.Kind).ThenBy(value => value.Id))
            {
                view.AddTarget($"工作 {++index} · {FrontWorkboardKind(work.Kind)} · {FrontWorkboardState(work.State)}", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.FrontWorkboardSelectedId = work.Id;
                        current.RefreshGridHud();
                    }
                });
            }
        }
    }
}
