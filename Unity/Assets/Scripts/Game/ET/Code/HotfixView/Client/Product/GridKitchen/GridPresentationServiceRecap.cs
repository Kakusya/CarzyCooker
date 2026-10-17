namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetServiceRecap(this GridPresentationComponent self)
        {
            self.RecapText.Clear();
            self.RecapDishIds.Clear();
            if (self.OverviewPage == GridOverviewPage.Recap)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
        }

        private static void RefreshServiceRecap(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Recap)
            {
                return;
            }
            if (grid.LevelPhase != LevelPhase.Completed)
            {
                self.ResetServiceRecap();
                return;
            }
            var text = self.RecapText;
            text.Clear();
            self.RecapDishIds.Clear();
            foreach (var order in grid.Orders.Values)
            {
                if (!self.RecapDishIds.Contains(order.DishId))
                {
                    self.RecapDishIds.Add(order.DishId);
                }
            }
            self.RecapDishIds.Sort();
            int served = 0, expired = 0, score = 0, waiting = 0;
            text.Append("本关结果：").Append(grid.LevelScore).Append(" 分 · ").Append(grid.LevelStars).Append(" 星\n");
            foreach (int dishId in self.RecapDishIds)
            {
                int dishServed = 0, dishExpired = 0, dishScore = 0;
                foreach (var order in grid.Orders.Values)
                {
                    if (order.DishId != dishId)
                    {
                        continue;
                    }
                    switch (order.State)
                    {
                        case OrderState.Served:
                            ++dishServed;
                            dishScore += order.Score;
                            break;
                        case OrderState.Expired:
                            ++dishExpired;
                            break;
                        default:
                            ++waiting;
                            break;
                    }
                }
                served += dishServed;
                expired += dishExpired;
                score += dishScore;
                text.Append(Tables.Instance.DTDishConfig.Get(dishId).Name).Append("：已交付 ").Append(dishServed)
                    .Append(" 单 · 超时 ").Append(dishExpired).Append(" 单 · 贡献 ").Append(dishScore).Append(" 分\n");
            }
            if (self.RecapDishIds.Count == 0)
            {
                text.Append("本关没有已生成订单\n");
            }
            text.Append("订单合计：已交付 ").Append(served).Append(" 单 · 超时 ").Append(expired)
                .Append(" 单 · 贡献 ").Append(score).Append(" 分\n");
            if (waiting != 0 || served != grid.ServedOrders || expired != grid.UnfulfilledOrders || score != grid.LevelScore)
            {
                text.Append("结果尚未完整：订单明细与本关结果未对齐，请等待原状态更新。\n");
            }
            text.Append("超时贡献零分，不倒扣；未询问和跳过来客不算订单。\n只读本关，不保存历史、不自动装修或继续。");
            view.SetInformation("本关复盘 · 只读", text.ToString(), true, false);
        }

        private static void AddServiceRecap(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Completed)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.Recap;
            view.AddControl(shown ? "关闭本关复盘" : "本关复盘", () =>
            {
                GridPresentationComponent current = original;
                if (current == null || !current.Ready)
                {
                    return;
                }
                current.ClearOrderNavigation();
                current.CloseItemLocator();
                current.ServiceReturnVisible = false;
                current.RecipeVisible = false;
                current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.Recap;
                current.RefreshGridHud();
            });
            if (shown)
            {
                view.AddControl("复盘：返回结算", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null && current.Ready)
                    {
                        current.OverviewPage = GridOverviewPage.Settlement;
                        current.RefreshGridHud();
                    }
                });
            }
        }
    }
}
