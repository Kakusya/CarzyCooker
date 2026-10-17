using System;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetServiceOverview(this GridPresentationComponent self)
        {
            self.ServiceSection = ServiceOverviewSection.Demand;
            self.ServiceDishId = 0;
            self.ServiceReturnVisible = false;
            self.ServicePreviousGroup = GridActionGroup.All;
            self.ServiceText.Clear();
            self.ServiceDishIds.Clear();
        }

        public static int ServiceReferenceStars(GridClientComponent grid)
        {
            var definition = Tables.Instance.DTLevelConfig.GetOrDefault(grid.LevelConfigId);
            if (definition == null)
            {
                return 0;
            }
            int stars = 0;
            foreach (int threshold in definition.StarThresholds)
            {
                if (grid.LevelScore >= threshold)
                {
                    ++stars;
                }
            }
            return stars;
        }

        public static int ServiceDemandCount(GridClientComponent grid, int dishId)
        {
            int count = 0;
            foreach (var order in grid.Orders.Values)
            {
                if (order.State == OrderState.Waiting && order.DishId == dishId)
                {
                    ++count;
                }
            }
            return count;
        }

        public static void OpenServiceOverview(this GridPresentationComponent self, bool returning = false)
        {
            if (!returning)
            {
                self.ServicePreviousGroup = self.ActionGroup;
                self.ServiceSection = ServiceOverviewSection.Demand;
                self.ServiceDishId = 0;
            }
            self.CloseItemLocator();
            self.ClearOrderNavigation();
            self.RecipeVisible = false;
            self.DetailsVisible = false;
            self.CompanionChat = CompanionChatTopic.None;
            self.OverviewPage = GridOverviewPage.Service;
            self.ActionGroup = GridActionGroup.FrontService;
            self.RefreshGridHud();
        }

        private static void RefreshServiceOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Service)
            {
                return;
            }
            var text = self.ServiceText;
            text.Clear();
            text.Append(grid.LevelPaused ? "已暂停：时间与订单期限冻结\n" : grid.LevelPhase switch
            {
                LevelPhase.Preparing => "尚未开店：不预估顾客订单\n",
                LevelPhase.Operating => "营业中：只读当前已提交事实\n",
                LevelPhase.Draining => "停止新来客：已有顾客与必要前厅自然收尾\n",
                LevelPhase.Completed => "本关已结束：最终结果见原结算\n",
                _ => "当前营业事实\n"
            });
            switch (self.ServiceSection)
            {
                case ServiceOverviewSection.Demand:
                    text.Append("待交付需求：已贴单但未交付仍算订单\n成品和加工不抵扣；未询问不预估开单\n");
                    int total = 0;
                    foreach (var order in grid.Orders.Values)
                    {
                        if (order.State == OrderState.Waiting)
                        {
                            ++total;
                        }
                    }
                    text.Append("当前待交付 ").Append(total).Append(" 单\n");
                    if (total == 0)
                    {
                        text.Append("暂无待交付订单");
                    }
                    foreach (int dishId in self.ServiceDishIds)
                    {
                        int count = 0;
                        long earliest = long.MaxValue;
                        foreach (var order in grid.Orders.Values)
                        {
                            if (order.State != OrderState.Waiting || order.DishId != dishId)
                            {
                                continue;
                            }
                            ++count;
                            earliest = Math.Min(earliest, order.DeadlineTick);
                        }
                        if (count == 0)
                        {
                            continue;
                        }
                        text.Append(Tables.Instance.DTDishConfig.Get(dishId).Name).Append("：待交付 ").Append(count)
                            .Append(" 单 · 最早剩余 ").Append((Math.Max(0, earliest - grid.Tick) * grid.TickMilliseconds / 1000f).ToString("0.0"))
                            .Append(" 秒\n");
                    }
                    break;
                case ServiceOverviewSection.Flow:
                    var flow = Tables.Instance.DTCustomerFlowConfig.DataList.SingleOrDefault(row => row.LevelConfigId == grid.LevelConfigId);
                    if (flow == null)
                    {
                        text.Append("当前客流配置不可用，请查看配置错误。");
                        break;
                    }
                    int entering = 0, queued = 0;
                    foreach (long id in grid.WaitingCustomers)
                    {
                        if (!grid.Customers.TryGetValue(id, out var customer))
                        {
                            continue;
                        }
                        if (customer.Phase == CustomerPhase.Entering)
                        {
                            ++entering;
                        }
                        else if (customer.Phase == CustomerPhase.Queued)
                        {
                            ++queued;
                        }
                    }
                    text.Append("队列占用 ").Append(grid.WaitingCustomers.Count).Append('/').Append(flow.QueueCapacity)
                        .Append(" · 进场 ").Append(entering).Append(" · 已等位 ").Append(queued)
                        .Append("\n进场即占队列；入座后退出队列\n");
                    int free = 0, obstructed = 0, reserved = 0, occupied = 0, dirty = 0;
                    foreach (var table in grid.Tables.Values)
                    {
                        switch (table.State)
                        {
                            case FrontTableState.Free:
                                if (grid.Stations.TryGetValue(table.Id, out var station) && station.ItemId != 0)
                                {
                                    ++obstructed;
                                }
                                else
                                {
                                    ++free;
                                }
                                break;
                            case FrontTableState.Reserved: ++reserved; break;
                            case FrontTableState.Occupied: ++occupied; break;
                            case FrontTableState.Dirty: ++dirty; break;
                        }
                    }
                    text.Append("空闲桌 ").Append(free).Append(" · 空桌被物占用 ").Append(obstructed)
                        .Append("\n已预留 ").Append(reserved).Append(" · 占用 ").Append(occupied).Append(" · 待清 ").Append(dirty)
                        .Append("\n被物占用的空桌需腾空，空桌数不保证可立即入座")
                        .Append("\n本关满队列跳过来客 ").Append(grid.SkippedArrivals)
                        .Append(" 次：不是超时订单，不扣分\n收尾不要求排空后厨；工作详情见原前厅看板");
                    break;
                case ServiceOverviewSection.Score:
                    var level = Tables.Instance.DTLevelConfig.GetOrDefault(grid.LevelConfigId);
                    if (level == null)
                    {
                        text.Append("当前评分配置不可用，请查看配置错误。");
                        break;
                    }
                    text.Append("当前已提交 ").Append(grid.LevelScore).Append(" 分\n已交付 ").Append(grid.ServedOrders)
                        .Append(" 单 · 未满足 ").Append(grid.UnfulfilledOrders).Append(" 单\n");
                    int reference = ServiceReferenceStars(grid);
                    text.Append(grid.LevelPhase == LevelPhase.Completed ? "最终结算 " : "未结算 · 当前分数参考 ")
                        .Append(grid.LevelPhase == LevelPhase.Completed ? grid.LevelStars : reference).Append(" 星\n");
                    for (int index = 0; index < level.StarThresholds.Count; ++index)
                    {
                        text.Append(index + 1).Append(" 星门槛 ").Append(level.StarThresholds[index]).Append(" 分\n");
                    }
                    text.Append(reference == level.StarThresholds.Count ? "已达到最高星门槛" :
                        "距下一门槛 " + (level.StarThresholds[reference] - grid.LevelScore) + " 分");
                    text.Append("\n等待订单不预加分；超时不倒扣\n参考不发奖励、不提前结束；零星仍正常成功");
                    break;
            }
            view.SetInformation(self.ServiceSection switch
            {
                ServiceOverviewSection.Demand => "营业总览 · 需求",
                ServiceOverviewSection.Flow => "营业总览 · 客流",
                _ => "营业总览 · 得分"
            }, text.ToString(), true, self.OrdersVisible);
        }

        private static void AddServiceOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.Service;
            view.AddControl(shown ? "关闭营业总览" : "营业总览", () =>
            {
                GridPresentationComponent current = original;
                if (current == null)
                {
                    return;
                }
                if (shown)
                {
                    current.OverviewPage = GridOverviewPage.None;
                    current.ActionGroup = current.ServicePreviousGroup;
                    current.ResetServiceOverview();
                    current.RefreshGridHud();
                }
                else
                {
                    current.OpenServiceOverview();
                }
            });
            if (!shown)
            {
                if (self.ServiceReturnVisible)
                {
                    view.AddTarget("返回营业总览", () =>
                    {
                        GridPresentationComponent current = original;
                        current?.OpenServiceOverview(true);
                    });
                }
                return;
            }
            self.ServiceDishIds.Clear();
            foreach (var order in grid.Orders.Values)
            {
                if (order.State == OrderState.Waiting && !self.ServiceDishIds.Contains(order.DishId))
                {
                    self.ServiceDishIds.Add(order.DishId);
                }
            }
            self.ServiceDishIds.Sort();
            // 信息刷新先于动作重建，打开首帧也应使用本次收集的菜品集合。
            self.RefreshServiceOverview(view, grid);
            foreach (ServiceOverviewSection section in Enum.GetValues(typeof(ServiceOverviewSection)))
            {
                string caption = section switch
                {
                    ServiceOverviewSection.Demand => "总览：需求",
                    ServiceOverviewSection.Flow => "总览：客流",
                    _ => "总览：得分"
                };
                view.AddTarget(caption, () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.ServiceSection = section;
                        current.RefreshGridHud();
                    }
                });
            }
            if (self.ServiceSection == ServiceOverviewSection.Flow)
            {
                view.AddTarget("总览：前厅工作", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.OverviewPage = GridOverviewPage.FrontWork;
                        current.ServiceReturnVisible = true;
                        current.RefreshGridHud();
                    }
                });
            }
            if (self.ServiceSection == ServiceOverviewSection.Score && grid.LevelPhase == LevelPhase.Completed)
            {
                view.AddTarget("总览：原结算", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.OverviewPage = GridOverviewPage.Settlement;
                        current.ServiceReturnVisible = true;
                        current.RefreshGridHud();
                    }
                });
            }
            if (self.ServiceSection == ServiceOverviewSection.Demand)
            {
                foreach (int dishId in self.ServiceDishIds)
                {
                    view.AddTarget("需求 · " + Tables.Instance.DTDishConfig.Get(dishId).Name, () =>
                    {
                        GridPresentationComponent current = original;
                        if (current != null)
                        {
                            current.ServiceDishId = dishId;
                            current.RefreshGridHud();
                        }
                    });
                }
                foreach (var order in grid.Orders.Values)
                {
                    if (order.State != OrderState.Waiting || order.DishId != self.ServiceDishId)
                    {
                        continue;
                    }
                    int sequence = grid.Customers.TryGetValue(order.CustomerId, out var customer) ? customer.ArrivalSequence : 0;
                    string table = grid.Stations.TryGetValue(order.TableStationId, out var station) ? station.Name : "餐桌";
                    view.AddTarget($"总览订单 · 顾客 {sequence} · {table}", () =>
                    {
                        GridPresentationComponent current = original;
                        if (current != null && current.SelectOrderForReading(grid, order.Id, order.CustomerId, order.DishId))
                        {
                            current.ServiceReturnVisible = true;
                            current.RefreshGridHud();
                        }
                    });
                }
            }
        }
    }
}
