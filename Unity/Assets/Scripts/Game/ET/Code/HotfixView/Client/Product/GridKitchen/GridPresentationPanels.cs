using System;
using System.Linq;
namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class GridPresentationSystem
    {
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
            var active=grid.Orders.Values.Where(order=>order.State==OrderState.Waiting).OrderBy(order=>order.Id).ToArray();
            view.OrdersText.text=active.Length==0?"暂无待交付订单":string.Join("\n",active.Select(order=>
            {
                string table=grid.Stations.TryGetValue(order.TableStationId,out GridStationProjection station)?station.Name:"餐桌";
                int customer=grid.Customers.TryGetValue(order.CustomerId,out GridCustomerProjection guest)?guest.ArrivalSequence:0;
                return $"顾客 {customer} · {table} · {Tables.Instance.DTDishConfig.Get(order.DishId).Name} · {Math.Max(0,order.DeadlineTick-grid.Tick)*grid.TickMilliseconds/1000f:0.0} 秒";
            }));
            view.BuffText.text=grid.SelectedBuffId!=0?"本关加成："+Tables.Instance.DTBuffConfig.Get(grid.SelectedBuffId).Name
                :grid.BuffCandidates.Count==0?"本关没有加成":self.BuffCandidatesVisible?"选择一个本关加成":"候选已隐藏，可再次查看";
            view.SettlementText.text=$"本关完成\n{grid.LevelScore} 分 · {grid.LevelStars} 星\n点击「下一关」继续";
        }
    }
}
