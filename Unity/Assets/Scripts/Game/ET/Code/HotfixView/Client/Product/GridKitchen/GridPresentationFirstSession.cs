using System.Linq;
using UnityEngine;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ObserveFirstSession(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (!self.FirstOrderGuideVisible || grid.LevelConfigId != 1101 || !HasSoloRecipeGuide(grid))
            {
                return;
            }
            if (self.FirstGuideOrderId == 0 && !self.FirstGuideNeedsOrderChoice)
            {
                self.FirstGuideOrderId = grid.Orders.Values.Where(order => order.State == OrderState.Waiting && order.DishId == 1).OrderBy(order => order.Id).FirstOrDefault().Id;
            }
            if (self.FirstGuideOrderId != 0 && (!grid.Orders.TryGetValue(self.FirstGuideOrderId, out var selected) || selected.State == OrderState.Expired))
            {
                self.FirstGuideNeedsOrderChoice = true;
                self.FirstGuideLocatedItem = self.FirstGuideLocatedStation = 0;
            }
            if (self.FirstGuideLocatedItem != 0 || self.FirstGuideLocatedStation != 0)
            {
                GridRecipeStep step = self.FirstSessionStep(grid);
                if (step.ItemId != self.FirstGuideLocatedItem || step.StationId != self.FirstGuideLocatedStation)
                {
                    self.FirstGuideLocatedItem = self.FirstGuideLocatedStation = 0;
                }
            }
        }

        public static GridRecipeStep FirstSessionStep(this GridPresentationComponent self, GridClientComponent grid)
        {
            string pause = grid.LevelPaused ? "已暂停，只查看；恢复后再操作。\n" : "";
            GridRecipeStep Step(GridRecipeStepKind kind, string description, long station = 0) => new(kind, pause + description, 0, 0, 0, station);
            if (grid.LevelPhase == LevelPhase.Completed)
            {
                return Step(GridRecipeStepKind.Completed, grid.ServedOrders > 0 ? "本关完成，查看结算并选择原下一步。" : "本关已结算，零星也可以继续；可在下一次练习首单。");
            }
            if (grid.ServedOrders > 0)
            {
                return Step(GridRecipeStepKind.FirstOrderComplete, grid.LevelPhase == LevelPhase.Draining
                    ? "首单已交付，正在收尾。请清理脏餐桌；清桌由你完成，小麦会继续洗碗。"
                    : "首单已真实交付！顾客离席后请清桌，小麦负责问单和洗碗；继续经营。");
            }
            if (grid.LevelPhase == LevelPhase.Preparing && grid.RequireBuffChoice && grid.SelectedBuffId == 0)
            {
                return Step(GridRecipeStepKind.BuffChoice, "先选择一项本关加成。可以备料，也可以随后直接开始营业。");
            }
            if (self.FirstGuideNeedsOrderChoice)
            {
                return Step(GridRecipeStepKind.Unavailable, "原汤单已结束；在首单步骤中明确选择一张当前汤单，已有备料保留。");
            }
            if (grid.LevelPhase is LevelPhase.Operating or LevelPhase.Draining && self.FirstGuideOrderId == 0)
            {
                var ask = grid.FrontWorks.Values.Where(work => work.Kind == FrontWorkKind.Ask && work.State is not (FrontWorkState.Completed or FrontWorkState.Cancelled)).OrderBy(work => work.Id).FirstOrDefault();
                return Step(GridRecipeStepKind.Reception, ask.Id == 0 ? "等待顾客入座；备好的汤可以先暂存。" : "顾客已入座：查看餐桌并询问；伙伴正在接待时可等待真实订单。", ask.StationId);
            }
            GridRecipeStep result = GridRecipeGuide.CurrentStep(grid, 1, self.FirstGuideOrderId);
            return grid.LevelPhase == LevelPhase.Preparing
                ? new GridRecipeStep(result.Kind, "备汤可选；准备好后点「开始营业」。\n" + result.Description, result.StageId, result.MissingDefinitionId, result.ItemId, result.StationId)
                : result;
        }

        private static GridCell? FirstSessionHighlight(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (!self.FirstOrderGuideVisible || grid.LevelConfigId != 1101 || self.FirstGuideNeedsOrderChoice)
            {
                return null;
            }
            if (self.FirstGuideLocatedItem != 0 && LocatorOuter(grid, self.FirstGuideLocatedItem, out var outer))
            {
                return outer.Location == GridItemLocation.Hand
                    ? grid.Actors.TryGetValue((int)outer.HolderId, out var actor) ? actor.Cell : null
                    : outer.Location == GridItemLocation.Ground ? outer.Cell
                    : grid.Stations.TryGetValue(outer.HolderId, out var holder) ? holder.Cell : null;
            }
            return grid.Stations.TryGetValue(self.FirstGuideLocatedStation, out var station) ? station.Cell : null;
        }

        private static void AddFirstSessionLocation(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            GridRecipeStep step = self.FirstSessionStep(grid);
            if (step.ItemId != 0 || step.StationId != 0)
            {
                long item = step.ItemId, station = step.StationId;
                view.AddTarget("查看首单目标位置", () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null)
                    {
                        return;
                    }
                    GridRecipeStep latest = current.FirstSessionStep(grid);
                    if (latest.ItemId != item || latest.StationId != station)
                    {
                        current.FirstGuideLocatedItem = current.FirstGuideLocatedStation = 0;
                        current.RefreshGridHud();
                        return;
                    }
                    current.FirstGuideLocatedItem = item;
                    current.FirstGuideLocatedStation = station;
                    GridCell? cell = current.FirstSessionHighlight(grid);
                    GridKitchenBoard board = current.Board;
                    if (cell.HasValue && board?.View != null)
                    {
                        Vector2 point = board.View.BoardCamera.WorldToScreenPoint(board.View.CellToWorld(cell.Value));
                        board.View.Pan(board.View.InputScreenRect.center.y - point.y);
                    }
                });
            }
            if (self.FirstGuideNeedsOrderChoice)
            {
                foreach (var order in grid.Orders.Values.Where(value => value.State == OrderState.Waiting && value.DishId == 1).OrderBy(value => value.Id))
                {
                    long id = order.Id;
                    view.AddTarget("选择首单订单 · " + grid.Stations[order.TableStationId].Name, () =>
                    {
                        GridPresentationComponent current = original;
                        if (current != null && grid.Orders.TryGetValue(id, out var latest) && latest.State == OrderState.Waiting)
                        {
                            current.FirstGuideOrderId = id;
                            current.FirstGuideNeedsOrderChoice = false;
                            current.RefreshGridHud();
                        }
                    });
                }
            }
        }
    }
}
