using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string InspectionCapabilityName(GridStationCapability capability) => capability switch
        {
            GridStationCapability.Chop => "切配", GridStationCapability.Cook => "煮制", GridStationCapability.Bake => "烘烤",
            GridStationCapability.Serve => "交付", GridStationCapability.Wash => "洗碗", GridStationCapability.DirtyPool => "脏餐具池",
            GridStationCapability.CleanPool => "干净餐具池", GridStationCapability.FillWater => "接水", GridStationCapability.DrainLiquid => "排液",
            GridStationCapability.Trash => "投垃圾", GridStationCapability.Juice => "榨汁", GridStationCapability.LabelDrink => "饮品贴单",
            GridStationCapability.Fry => "煎制", GridStationCapability.Mixing => "搅拌", GridStationCapability.Grind => "研磨",
            GridStationCapability.Extract => "萃取", GridStationCapability.HeatMilk => "热奶", GridStationCapability.FoamMilk => "打泡", GridStationCapability.Steam => "蒸制",
            GridStationCapability.BrewTea => "泡茶", GridStationCapability.BoilPearls => "煮珍珠", GridStationCapability.MixTea => "调饮",
            _ => capability.ToString()
        };

        private static string InspectionOrientation(GridOrientation orientation) => orientation switch
        {
            GridOrientation.Unrotated => "未旋转", GridOrientation.CounterClockwise90 => "逆时针90°",
            GridOrientation.HalfTurn => "180°", GridOrientation.CounterClockwise270 => "逆时针270°", _ => "朝向已变化"
        };

        private static string StationCapabilityDetails(GridClientComponent grid, GridStationProjection station)
        {
            var names = new List<string>();
            // 现有生成能力都是单bit；仅在详情失效刷新时派生名称，不在Hotfix保存静态状态。
            for (GridStationCapability capability = GridStationCapability.Chop; capability <= GridStationCapability.FoamMilk;
                capability = (GridStationCapability)((int)capability << 1))
            {
                if ((station.Capabilities & capability) != 0)
                {
                    names.Add(InspectionCapabilityName(capability));
                }
            }
            string capabilities = string.Join("、", names);
            string text = "\n定义能力：" + (capabilities.Length == 0 ? "普通存放，按原槽位规则" : capabilities);
            text += "\n朝向：" + InspectionOrientation(station.Orientation) + " · 外层槽容量 " + station.ItemSlotCapacity;
            text += "\n占地：" + string.Join("、", station.Footprint);
            text += "\n候选操作格：" + string.Join("、", station.Approaches);
            text += "\n蓝色为占地，绿色为操作格；实时占用和路径仍由原动作验证。";
            if (!grid.PermissionReady)
            {
                return text + "\n本关生产许可准备中，请稍候";
            }
            var recipes = new List<string>();
            foreach (int stageId in grid.ProductionPermit.Stages.OrderBy(value => value))
            {
                DRDishStageConfig stage = Tables.Instance.DTDishStageConfig.Get(stageId);
                GridStationCapability required = GridStationCapability.None;
                int container = 0;
                switch (stage.Kind)
                {
                    case DishStageKind.Manual:
                        if (Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId).Mode == GridManualMode.Chop)
                        {
                            required = GridStationCapability.Chop;
                        }
                        break;
                    case DishStageKind.Batch:
                        var batch = Tables.Instance.DTGridAutoRecipeConfig.Get(stage.RecipeId);
                        required = batch.StationCapability; container = batch.ContainerDefinitionId;
                        break;
                    case DishStageKind.Boil:
                case DishStageKind.Steam:
                case DishStageKind.Grind:
                    case DishStageKind.Bake:
                    case DishStageKind.Fry:
                    case DishStageKind.BakeInCarrier:
                        var bake = Tables.Instance.DTGridBakeRecipeConfig.Get(stage.RecipeId);
                        required = bake.StationCapability; container = bake.CarrierDefinitionId;
                        break;
                }
                if (required == GridStationCapability.None || (station.Capabilities & required) != required)
                {
                    continue;
                }
                string vessel = container == 0 ? "原料直接放工位单槽" : "需要干净" + Tables.Instance.DTGridItemConfig.Get(container).Name;
                recipes.Add(InspectionCapabilityName(required) + " → " + Tables.Instance.DTGridItemConfig.Get(stage.OutputDefinitionId).Name + " · " + vessel);
            }
            if ((station.Capabilities & GridStationCapability.LabelDrink) != 0 && grid.AllowedDishIds.Any(id => Tables.Instance.DTDishConfig.Get(id).RequiresOrderBinding))
            {
                recipes.Add("贴单：已完成饮品放在本台，选择当前有效订单");
            }
            text += recipes.Count == 0 ? "\n本关没有匹配的生产工序；存放/清理仍按原规则" : "\n本关匹配工序：\n" + string.Join("\n", recipes);
            return text + "\n能做某工序不等于立即可执行，仍须材料、卫生、空位及当前原动作完整校验。";
        }

        private static void ClearStationInspection(this GridPresentationComponent self)
        {
            self.StationInspectionGeometryVisible = self.StationInspectionDetailsVisible = false;
            GridKitchenBoard board = self.Board;
            board?.View?.SetStationInspection(null);
        }

        private static void UpdateStationInspection(this GridPresentationComponent self, GridClientComponent grid)
        {
            GridStationProjection? selected = self.OverviewPage == GridOverviewPage.Stations && self.StationInspectionGeometryVisible &&
                self.SelectedLevelId == grid.LevelId && self.SelectedLevelGeneration == grid.LevelGeneration &&
                grid.Stations.TryGetValue(self.SelectedStationId, out var station) ? station : null;
            GridKitchenBoard board = self.Board;
            board?.View?.SetStationInspection(selected);
        }

        private static void RefreshStationInspection(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Stations)
            {
                return;
            }
            string target = grid.Stations.TryGetValue(self.SelectedStationId, out var station)
                ? "工位说明：" + station.Name + " · 蓝色占地/绿色操作格" : "工位说明：从下方条目选择，仅查看不发命令";
            view.SetInformation(target, self.SelectedTargetDetails(grid), self.StationInspectionDetailsVisible, self.OrdersVisible);
        }

        private static void AddStationInspection(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.Stations;
            view.AddTarget(shown ? "关闭工位说明" : "工位说明", () =>
            {
                GridPresentationComponent current = original;
                if (current == null)
                {
                    return;
                }
                current.CloseItemLocator(); current.ClearOrderNavigation(); current.ClearStationInspection();
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.Stations;
                current.RecipeVisible = current.DetailsVisible = false; current.CompanionChat = CompanionChatTopic.None;
                current.StationInspectionDetailsVisible = true; current.StationInspectionGeometryVisible = !shown;
                current.ActionGroup = GridActionGroup.Layout;
                current.RefreshGridHud();
            });
            if (!shown)
            {
                return;
            }
            bool markers = self.StationInspectionGeometryVisible;
            view.AddTarget(markers ? "隐藏位置标记" : "显示位置标记", () =>
            {
                GridPresentationComponent current = original;
                if (current != null)
                {
                    current.StationInspectionGeometryVisible = !markers;
                    current.UpdateStationInspection(grid);
                    current.RefreshGridHud();
                }
            });
            int index = 0;
            foreach (var station in grid.Stations.Values.OrderBy(value => value.Id))
            {
                view.AddTarget($"工位 {++index} · {station.Name}", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.SelectedStationId = station.Id; current.SelectedLevelId = grid.LevelId; current.SelectedLevelGeneration = grid.LevelGeneration;
                        current.StationInspectionDetailsVisible = false;
                        current.UpdateStationInspection(grid);
                        current.RefreshGridHud();
                    }
                });
            }
        }
    }
}
