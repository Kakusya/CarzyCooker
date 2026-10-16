using System;
using System.Linq;
namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class GridPresentationSystem
    {
        private static string BuffEffectDescription(DRBuffConfig definition)
        {
            string target = definition.EffectKind switch
            {
                StationWorkKind.Batch => "制作 " + Tables.Instance.DTGridItemConfig.Get(Tables.Instance.DTGridAutoRecipeConfig.Get(definition.BatchRecipeId).OutputDefinitionId).Name,
                StationWorkKind.Chop => "切配 " + Tables.Instance.DTGridItemConfig.Get(Tables.Instance.DTGridManualRecipeConfig.Get(definition.ManualRecipeId).InputDefinitionId).Name,
                StationWorkKind.Bake => ((GridStationCapability)Tables.Instance.DTGridBakeRecipeConfig.Get(definition.BakeRecipeId).StationCapability switch
                    {
                        GridStationCapability.Fry => "煎制 ",
                        GridStationCapability.Grind => "研磨 ",
                        _ => "烘焙 "
                    }) + Tables.Instance.DTGridItemConfig.Get(Tables.Instance.DTGridBakeRecipeConfig.Get(definition.BakeRecipeId).InputDefinitionId).Name,
                _ => "未知效果"
            };
            return target + "耗时 " + definition.DurationNumerator + "/" + definition.DurationDenominator;
        }
        private static void RefreshRestaurantPanels(this GridPresentationComponent self,MonoUIGridKitchen view,GridClientComponent grid)
        {
            view.ShowPanels(grid.LevelPhase==LevelPhase.Preparing,grid.LevelPhase==LevelPhase.Completed);
            view.PhaseText.text=grid.LevelPhase switch{LevelPhase.Preparing=>"准备",LevelPhase.Operating=>"营业",LevelPhase.Draining=>"收尾",LevelPhase.Completed=>"结算",_=>"厨房"};
            if(grid.LevelPaused)view.PhaseText.text+=" · 暂停";
            view.ClockText.text=grid.LevelPhase is LevelPhase.Preparing or LevelPhase.Operating
                ?$"剩余 {Math.Max(0,grid.LevelOperatingTicks-grid.LevelElapsedTicks)*grid.TickMilliseconds/1000f:0.0} 秒"
                :grid.LevelPhase==LevelPhase.Draining?"等待前厅完成":"本关已结束";
            view.ScoreText.text=$"{grid.LevelScore} 分 · {grid.LevelStars} 星";
            string partner=string.Join(" · ",grid.Companions.Values.Select(role=>
            {
                string job="空闲";
                if(role.WorkId!=0 && grid.FrontWorks.TryGetValue(role.WorkId,out GridFrontWorkProjection work))
                    job=(work.Kind==FrontWorkKind.Ask?"询问":work.Kind==FrontWorkKind.Clear?"清桌":"洗碗")+$" {work.Progress:P0}";
                return role.Name+"："+job+$" · 完成 {role.CompletedCount} 项";
            }));
            view.WorkText.text=partner;
            if (!string.IsNullOrEmpty(self.CompanionFeedback))
            {
                view.WorkText.text += "\n" + self.CompanionFeedback;
            }
            KitchenWorkOutcomeValue kitchen = grid.LastKitchenOutcome;
            if (kitchen.WorkId != 0 && (grid.Processes.ContainsKey(kitchen.WorkId) || grid.AutoProcesses.ContainsKey(kitchen.WorkId) || grid.BakeProcesses.ContainsKey(kitchen.WorkId)))
            {
                string title = kitchen.Kind == KitchenProcessKind.Soup ? "煮汤" : kitchen.Kind == KitchenProcessKind.Bake ? "烤制" : "手工加工";
                string stage = kitchen.Stage switch { KitchenWorkStage.Started => "已开始", KitchenWorkStage.Resumed => "已接续", KitchenWorkStage.Paused => "已停手", KitchenWorkStage.Finished => "已完成", _ => "已取消" };
                view.WorkText.text += " · " + title + stage;
            }
            FrontWorkOutcomeValue front = grid.LastFrontWorkOutcome;
            if (front.WorkId != 0 && grid.FrontWorks.ContainsKey(front.WorkId))
            {
                string title = front.Kind == FrontWorkKind.Ask ? "询问" : front.Kind == FrontWorkKind.Clear ? "清桌" : "洗碗";
                string stage = front.Stage switch { FrontWorkStage.Claimed => "已认领", FrontWorkStage.Started => "已开始", FrontWorkStage.Released => "已停手", FrontWorkStage.Finished => "已完成", _ => "已取消" };
                view.WorkText.text += " · " + title + stage;
            }
            var active=grid.Orders.Values.Where(order=>order.State==OrderState.Waiting).OrderBy(order=>order.Id).ToArray();
            view.OrdersText.text=active.Length==0?"暂无待交付订单":string.Join("\n",active.Select(order=>
            {
                string table=grid.Stations.TryGetValue(order.TableStationId,out GridStationProjection station)?station.Name:"餐桌";
                int customer=grid.Customers.TryGetValue(order.CustomerId,out GridCustomerProjection guest)?guest.ArrivalSequence:0;
                return $"顾客 {customer} · {table} · {Tables.Instance.DTDishConfig.Get(order.DishId).Name} · {Math.Max(0,order.DeadlineTick-grid.Tick)*grid.TickMilliseconds/1000f:0.0} 秒";
            }));
            view.BuffText.text=grid.SelectedBuffId!=0?"本关加成："+Tables.Instance.DTBuffConfig.Get(grid.SelectedBuffId).Name+" · "+BuffEffectDescription(Tables.Instance.DTBuffConfig.Get(grid.SelectedBuffId))
                :grid.BuffDrawVersion==0?"本关加成准备中":grid.BuffCandidates.Count==0?"本关没有加成":self.BuffCandidatesVisible?"选择一个本关加成":"候选已隐藏，可再次查看";
            var definition = Tables.Instance.DTLevelConfig.GetOrDefault(grid.LevelConfigId);
            if (definition?.ChapterId > 0)
            {
                string name = RestaurantChapters.Find(definition)?.Name;
                view.PhaseText.text += (name == null ? "" : " · " + name) + $" · 第 {definition.ChapterStage}/{RestaurantConfig.ChapterLevelCount(definition)} 关";
            }
            string next = definition?.IsChapterTerminal == true
                ? IsCooperativeRestaurant(grid) ? grid.CanControlRestaurant ? "内存合作完成，确认后返回房间" : "内存合作完成，等待 Host 返回房间"
                    : grid.SaveEnabled ? "保存完成记录后返回选档" : "内存试玩完成，返回选档"
                : "点击继续进入下一关";
            view.SettlementText.text=$"{(definition?.IsChapterTerminal == true ? "大关完成" : "本关完成")}\n成功 {grid.ServedOrders} 单 · 未满足 {grid.UnfulfilledOrders} 单\n{grid.LevelScore} 分 · {grid.LevelStars} 星\n{next}";
        }

        private static bool IsCooperativeRestaurant(GridClientComponent grid)
        {
            ProductRuntimeComponent runtime = grid.Root().GetComponent<ProductRuntimeComponent>();
            return runtime != null && runtime.Config.Role != Game.RuntimeInstanceRole.Solo;
        }
    }
}
