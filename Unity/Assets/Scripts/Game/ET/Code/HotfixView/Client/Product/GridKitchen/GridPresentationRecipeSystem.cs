using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static bool HasSoloRecipeGuide(GridClientComponent grid)
        {
            ProductRuntimeComponent runtime=((ClientSessionComponent)grid.Parent).Runtime;
            return runtime.Config.Role==RuntimeInstanceRole.Solo;
        }

        private static int GuideDish(this GridPresentationComponent self,GridClientComponent grid)
        {
            if(grid.Stations.TryGetValue(self.SelectedStationId,out GridStationProjection station) && grid.Items.TryGetValue(station.ItemId,out GridItemProjection item))
            {
                bool HasDefinition(int stageId)
                {
                    var stage=Tables.Instance.DTDishStageConfig.Get(stageId);
                    return stage.OutputDefinitionId==item.DefinitionId || stage.DependencyIds.Any(HasDefinition);
                }
                int selected=grid.AllowedDishIds.FirstOrDefault(id=>HasDefinition(Tables.Instance.DTDishConfig.Get(id).FinalStageId));
                if(selected!=0)return selected;
            }
            var order=grid.Orders.Values.Where(value=>value.State==OrderState.Waiting).OrderBy(value=>value.Id).FirstOrDefault();
            if (order.Id != 0)
            {
                return order.DishId;
            }
            var flow = Tables.Instance.DTCustomerFlowConfig.DataList.FirstOrDefault(row => row.LevelConfigId == grid.LevelConfigId);
            if (flow != null)
            {
                foreach (int id in flow.OrderConfigIds)
                {
                    int dish = Tables.Instance.DTOrderConfig.Get(id).DishId;
                    if (grid.AllowedDishIds.Contains(dish))
                    {
                        return dish;
                    }
                }
            }
            return grid.AllowedDishIds.First();
        }

        private static void RefreshRecipeInformation(this GridPresentationComponent self,MonoUIGridKitchen view,GridClientComponent grid)
        {
            if(self.RecipeDishId!=0 && !grid.AllowedDishIds.Contains(self.RecipeDishId))
            {
                self.RecipeDishId=0;self.RecipeVisible=false;
            }
            if(self.FirstOrderGuideVisible && HasSoloRecipeGuide(grid))
            {
                int dish=self.RecipeDishId!=0?self.RecipeDishId:self.GuideDish(grid);
                GridRecipeStep step = grid.LevelConfigId == 1101 ? self.FirstSessionStep(grid) : GridRecipeGuide.CurrentStep(grid, dish);
                view.HintText.text = "首单：" + step.Description;
            }
            if(self.RecipeVisible)
            {
                int dish=self.RecipeDishId;
                view.SetInformation(self.SelectedTargetDescription(grid),GridRecipeGuide.Describe(dish,grid.TickMilliseconds)+"\n\n当前提示：\n"+GridRecipeGuide.CurrentNeed(grid,dish),true,self.OrdersVisible);
            }
        }

        private static void AddRecipeTargets(this GridPresentationComponent self,MonoUIGridKitchen view,GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original=self;
            self.AddOrderNavigation(view, grid);
            foreach(int dish in grid.AllowedDishIds)
            {
                view.AddTarget("菜谱 "+Tables.Instance.DTDishConfig.Get(dish).Name,()=>
                {
                    GridPresentationComponent current=original;if(current==null)return;
                    current.ResetItemLocator();current.ClearOrderNavigation();current.OverviewPage=GridOverviewPage.None;current.CompanionChat=CompanionChatTopic.None;current.RecipeDishId=dish;current.RecipeVisible=true;current.RefreshGridHud();
                });
            }
            if(self.RecipeVisible)
            {
                view.AddTarget("关闭菜谱",()=>
                {
                    GridPresentationComponent current=original;if(current==null)return;
                    current.RecipeVisible=false;current.ClearOrderNavigation();current.RefreshGridHud();
                });
            }
            if(HasSoloRecipeGuide(grid))
            {
                bool shown=self.FirstOrderGuideVisible;
                if(shown)
                {
                    int dish=self.RecipeDishId!=0?self.RecipeDishId:self.GuideDish(grid);
                    view.AddTarget("查看首单步骤",()=>
                    {
                        GridPresentationComponent current=original;if(current==null)return;
                        current.ResetItemLocator();current.ClearOrderNavigation();current.OverviewPage=GridOverviewPage.None;current.CompanionChat=CompanionChatTopic.None;current.RecipeDishId=dish;current.RecipeVisible=true;current.RefreshGridHud();
                    },self.SelectedStationId==0);
                }
                view.AddTarget(shown?"跳过首单引导":"重看首单引导",()=>
                {
                    GridPresentationComponent current=original;if(current==null)return;
                    current.FirstOrderGuideVisible=!shown;
                    if (shown)
                    {
                        current.FirstGuideLocatedItem = current.FirstGuideLocatedStation = 0;
                    }
                    current.RefreshGridHud();
                });
                if (shown && grid.LevelConfigId == 1101)
                {
                    self.AddFirstSessionLocation(view, grid);
                }
            }
        }
    }
}
