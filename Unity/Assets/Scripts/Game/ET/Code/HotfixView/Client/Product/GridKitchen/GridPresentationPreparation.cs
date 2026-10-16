using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void RefreshPreparationOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Preparation)
            {
                return;
            }
            string condition = grid.BuffDrawVersion == 0 ? "加成准备中，请稍候"
                : grid.RequireBuffChoice && grid.SelectedBuffId == 0 ? "必须先选择本关加成"
                : grid.SelectedBuffId == 0 ? "加成可选，已满足开店条件" : "加成已选，已满足开店条件";
            string control = !grid.CanControlRestaurant ? "等待房主决定开店"
                : grid.LevelPaused ? "当前暂停，请先恢复" : "房主／单机使用原“开始营业”按钮";
            string menu = string.Join("、", grid.AllowedDishIds.Select(id => Tables.Instance.DTDishConfig.Get(id).Name));
            view.SetInformation("开店准备总览",
                "本关菜单：" + menu + "\n开店条件：" + condition + "\n" + control +
                $"\n工位 {grid.Stations.Count} · 物品 {grid.Items.Count} · 可生产菜品 {grid.ProductionPermit.DishIds.Count}" +
                $"\n未接收采购 {grid.SupplyShipments.Values.Count(shipment => shipment.Stage != ShipmentStage.Received)}" +
                "\n备料、补货和布局是可选准备。\n没有预制成品也可开店，不是额外门槛。", true, self.OrdersVisible);
        }

        private static void AddPreparationOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Preparing)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.Preparation;
            view.AddControl(shown ? "关闭准备总览" : "开店准备总览", () =>
            {
                GridPresentationComponent current = original;
                if (current == null)
                {
                    return;
                }
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.Preparation;
                current.ResetItemLocator();
                current.ClearOrderNavigation();
                current.RecipeVisible = current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.RefreshGridHud();
            });
            if (!shown)
            {
                return;
            }
            Add("准备：查看菜谱", GridActionGroup.Recipes);
            Add("准备：查看补货", GridActionGroup.Supply);
            Add("准备：查看布局", GridActionGroup.Layout);

            void Add(string caption, GridActionGroup group)
            {
                view.AddControl(caption, () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null)
                    {
                        return;
                    }
                    current.OverviewPage = GridOverviewPage.None;
                    current.ActionGroup = group;
                    current.RefreshGridHud();
                });
            }
        }
    }
}
