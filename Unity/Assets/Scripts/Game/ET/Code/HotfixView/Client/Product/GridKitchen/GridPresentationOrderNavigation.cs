using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ClearOrderNavigation(this GridPresentationComponent self)
        {
            self.RecipeOrderId = 0;
            self.RecipeItemId = 0;
            self.OrderRecipeDetailsVisible = false;
            self.OrderNavigationFeedback = null;
            self.ServiceReturnVisible = false;
        }

        public static bool SelectOrderForReading(this GridPresentationComponent self, GridClientComponent grid,
            long orderId, long customerId, int dishId)
        {
            if (!grid.Confirmed || !grid.Orders.TryGetValue(orderId, out var order) || order.State != OrderState.Waiting ||
                order.CustomerId != customerId || order.DishId != dishId || !grid.AllowedDishIds.Contains(dishId))
            {
                return false;
            }
            self.ResetItemLocator();
            self.OverviewPage = GridOverviewPage.None;
            self.CompanionChat = CompanionChatTopic.None;
            self.DetailsVisible = false;
            self.RecipeDishId = dishId;
            self.RecipeOrderId = orderId;
            self.RecipeItemId = 0;
            self.FirstOrderGuideVisible = false;
            self.OrderRecipeDetailsVisible = false;
            self.OrderNavigationFeedback = null;
            self.ServiceReturnVisible = false;
            self.RecipeVisible = true;
            return true;
        }

        private static void ObserveOrderNavigation(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.RecipeOrderId != 0 && (!grid.Orders.TryGetValue(self.RecipeOrderId, out var order) ||
                order.State != OrderState.Waiting || !grid.AllowedDishIds.Contains(order.DishId)))
            {
                self.ClearOrderNavigation();
                self.RecipeVisible = false;
                self.OrderNavigationFeedback = "所选订单已结束，请重新选择；不会自动改为其他顾客。";
            }
        }

        private static void RefreshOrderNavigation(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OrderNavigationFeedback != null)
            {
                view.SetInformation("订单导航", self.OrderNavigationFeedback, true, self.OrdersVisible);
            }
            else if (self.RecipeOrderId != 0 && self.RecipeItemId != 0)
            {
                if (grid.Items.TryGetValue(self.RecipeItemId, out var item))
                {
                    view.SetInformation("订单相关物品", grid.ItemDescription(item.Id) + "\n" + OrderItemLocation(grid, item) +
                        "\n这里只查看，不移动或取用物品。", true, self.OrdersVisible);
                }
                else
                {
                    view.SetInformation("订单相关物品", "物品已变化或消失，请返回订单制作。", true, self.OrdersVisible);
                }
            }
            else if (self.RecipeVisible && self.RecipeOrderId != 0 && grid.Orders.TryGetValue(self.RecipeOrderId, out var order))
            {
                int sequence = grid.Customers.TryGetValue(order.CustomerId, out var customer) ? customer.ArrivalSequence : 0;
                string table = grid.Stations.TryGetValue(order.TableStationId, out var station) ? station.Name : "餐桌";
                string title = $"查看订单 · 顾客 {sequence} · {table}";
                string need = GridRecipeGuide.CurrentNeed(grid, order.DishId, order.Id);
                string details = self.OrderRecipeDetailsVisible ? GridRecipeGuide.Describe(order.DishId, grid.TickMilliseconds)
                    : Tables.Instance.DTDishConfig.Get(order.DishId).Name + "\n" + need + "\n只读目标，不认领、不贴单、不交付。";
                string intentions = string.Join("、", grid.OrderIntents.Where(value => value.OrderId == order.Id)
                    .Select(value => "成员 " + value.MemberId));
                details += "\n制作意向：" + (intentions.Length == 0 ? "暂无" : intentions) + "\n仅作提示，可多人；所有人仍可正常制作和交付。";
                view.SetInformation(title, details, true, self.OrdersVisible);
            }
        }

        private static void AddOrderNavigation(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            foreach (var order in grid.Orders.Values.Where(value => value.State == OrderState.Waiting).OrderBy(value => value.Id))
            {
                if (!grid.Stations.TryGetValue(order.TableStationId, out var table))
                {
                    continue;
                }
                view.AddTarget("制作 · " + table.Name + " · " + Tables.Instance.DTDishConfig.Get(order.DishId).Name, () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null || !current.SelectOrderForReading(grid, order.Id, order.CustomerId, order.DishId))
                    {
                        return;
                    }
                    current.RefreshGridHud();
                });
            }
            if (self.RecipeOrderId != 0)
            {
                if (!grid.LevelPaused && grid.LevelPhase is LevelPhase.Operating or LevelPhase.Draining &&
                    grid.Orders.TryGetValue(self.RecipeOrderId, out var selectedOrder) && selectedOrder.State == OrderState.Waiting)
                {
                    long level = grid.LevelId, generation = grid.LevelGeneration;
                    bool mine = grid.OrderIntents.Any(value => value.MemberId == grid.MemberId && value.OrderId == selectedOrder.Id);
                    view.AddTarget(mine ? "撤销我的制作意向" : "我准备制作此单（不独占）", () =>
                    {
                        GridPresentationComponent current = original;
                        current?.SendOrderIntent(mine ? OrderIntentAction.Clear : OrderIntentAction.Set,
                            selectedOrder.Id, selectedOrder.CustomerId, level, generation);
                    });
                }
                bool full = self.OrderRecipeDetailsVisible;
                view.AddTarget(full ? "订单：制作缺口" : "订单：完整菜谱", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.RecipeVisible = true;
                        current.RecipeItemId = 0;
                        current.DetailsVisible = false;
                        current.OrderRecipeDetailsVisible = !full;
                        current.RefreshGridHud();
                    }
                });
                self.AddOrderStationLinks(view, grid);
                if (!self.RecipeVisible)
                {
                    view.AddTarget("返回订单制作", () =>
                    {
                        GridPresentationComponent current = original;
                        if (current != null)
                        {
                            current.RecipeVisible = true;
                            current.RecipeItemId = 0;
                            current.DetailsVisible = false;
                            current.RefreshGridHud();
                        }
                    });
                }
            }
            if (self.RecipeOrderId != 0 || self.OrderNavigationFeedback != null)
            {
                view.AddTarget("关闭订单导航", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.ClearOrderNavigation();
                        current.RecipeVisible = current.DetailsVisible = false;
                        current.RefreshGridHud();
                    }
                });
            }
        }

        private static void AddOrderStationLinks(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (!grid.Orders.TryGetValue(self.RecipeOrderId, out var order))
            {
                return;
            }
            var definitions = new HashSet<int>();
            var visited = new HashSet<int>();
            GridStationCapability capabilities = GridStationCapability.None;
            void Visit(int id)
            {
                if (!visited.Add(id))
                {
                    return;
                }
                var stage = Tables.Instance.DTDishStageConfig.Get(id);
                definitions.Add(stage.OutputDefinitionId);
                definitions.Add(stage.ContainerDefinitionId);
                foreach (int dependency in stage.DependencyIds)
                {
                    Visit(dependency);
                }
                if (stage.Kind == DishStageKind.Batch)
                {
                    capabilities |= Tables.Instance.DTGridAutoRecipeConfig.Get(stage.RecipeId).StationCapability;
                }
                else if (stage.Kind is DishStageKind.Bake or DishStageKind.Boil or DishStageKind.Steam or DishStageKind.Grind or DishStageKind.Fry or DishStageKind.BakeInCarrier)
                {
                    capabilities |= Tables.Instance.DTGridBakeRecipeConfig.Get(stage.RecipeId).StationCapability;
                }
                else if (stage.Kind == DishStageKind.Manual && Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId).Mode == GridManualMode.Chop)
                {
                    capabilities |= GridStationCapability.Chop;
                }
            }
            Visit(Tables.Instance.DTDishConfig.Get(order.DishId).FinalStageId);
            EntityRef<GridPresentationComponent> original = self;
            foreach (var item in grid.Items.Values.Where(value => definitions.Contains(value.DefinitionId)))
            {
                view.AddTarget("订单：物品 " + item.Name + " · " + OrderItemLocation(grid, item), () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null && grid.Items.ContainsKey(item.Id))
                    {
                        current.RecipeItemId = item.Id;
                        current.RecipeVisible = current.DetailsVisible = false;
                        current.RefreshGridHud();
                    }
                });
            }
            foreach (var station in grid.Stations.Values)
            {
                bool hasItem = grid.Items.Values.Any(item => definitions.Contains(item.DefinitionId) &&
                    (item.HolderId == station.Id && item.Location is GridItemLocation.Counter or GridItemLocation.CleanPool or GridItemLocation.DirtyPool ||
                        item.Location == GridItemLocation.Container && item.HolderId == station.ItemId));
                if (!hasItem && (station.Capabilities & capabilities) == 0)
                {
                    continue;
                }
                view.AddTarget("订单：查看 " + station.Name, () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null || !grid.Stations.ContainsKey(station.Id))
                    {
                        return;
                    }
                    current.SelectedStationId = station.Id;
                    current.RecipeItemId = 0;
                    current.SelectedLevelId = grid.LevelId;
                    current.SelectedLevelGeneration = grid.LevelGeneration;
                    current.RecipeVisible = false;
                    current.DetailsVisible = true;
                    current.RefreshGridHud();
                });
            }
        }

        private static string OrderItemLocation(GridClientComponent grid, GridItemProjection item)
        {
            if (item.Location == GridItemLocation.Hand)
            {
                return "玩家 " + item.HolderId + " 手持";
            }
            if (item.Location == GridItemLocation.Container)
            {
                return grid.Items.TryGetValue(item.HolderId, out var holder) ? holder.Name + " 内" : "容器已变化";
            }
            if (item.Location == GridItemLocation.Ground)
            {
                return $"地面 ({item.Cell.X},{item.Cell.Y})";
            }
            return grid.Stations.TryGetValue(item.HolderId, out var station) ? station.Name : item.Location.ToString();
        }
    }
}
