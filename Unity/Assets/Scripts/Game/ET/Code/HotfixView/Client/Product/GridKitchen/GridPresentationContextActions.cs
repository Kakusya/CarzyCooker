using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string ContextActionReason(GridClientComponent grid, GridStationProjection station, GridIntent intent)
        {
            if (station.Kind is GridStationKind.Table or GridStationKind.Wash or GridStationKind.DirtyPool or GridStationKind.CleanPool)
            {
                return null;
            }
            if (grid.LevelPaused)
            {
                return "已暂停，恢复后再操作";
            }
            if (grid.LevelPhase == LevelPhase.Completed)
            {
                return "本关已结束，可查看结算";
            }
            if (!grid.Actors.TryGetValue(grid.MemberId, out var actor))
            {
                return "正在等待本人角色";
            }
            grid.Items.TryGetValue(actor.HandItemId, out var hand);
            grid.Items.TryGetValue(station.ItemId, out var target);
            if (intent.Action is GridAction.AddToContainer or GridAction.PourContents && target.AutoProcessId != 0)
            {
                return "此批次已开始，不能追加材料";
            }
            if (intent.Action != GridAction.StopWork && AdviceItemLocked(grid, target.Id))
            {
                return "物品正在加工，等待完成或原执行者停手";
            }
            if (intent.Action == GridAction.TakeCounter && target.Id == 0)
            {
                return "台面为空，请选择有物品的目标";
            }
            if (intent.Action == GridAction.AddToContainer)
            {
                if (hand.DefinitionId == GridKitchenRules.Plate && target.DefinitionId == GridKitchenRules.Toast)
                {
                    return "先放下盘，再拿吐司加入盘中";
                }
                if (target.Form != GridMaterialForm.Container)
                {
                    return "目标不是容器；先放下手中物或选择空台面";
                }
                if (hand.Form == GridMaterialForm.Container)
                {
                    return "不能直接套装容器，请查看原配方步骤";
                }
                var definition = Tables.Instance.DTGridItemConfig.Get(target.DefinitionId);
                if (target.Contents.Count >= definition.ContainerCapacity)
                {
                    return "容器已满，请先盛装或清理";
                }
                if (!definition.AcceptedDefinitionIds.Contains(hand.DefinitionId))
                {
                    return "此容器不接受手中材料，请查看条件";
                }
            }
            if (intent.Action == GridAction.PortionSoup &&
                (!grid.AutoProcesses.TryGetValue(target.AutoProcessId, out var batch) || batch.State != GridAutoState.Ready || batch.RemainingPortions <= 0))
            {
                return "汤尚未完成，完成后才能盛装";
            }
            if (intent.Action == GridAction.StartAutoWork && !Tables.Instance.DTGridAutoRecipeConfig.DataList.Any(recipe =>
                recipe.ContainerDefinitionId == target.DefinitionId && (station.Capabilities & recipe.StationCapability) == recipe.StationCapability &&
                target.Contents.Count == recipe.InputCounts.Sum() && recipe.InputDefinitionIds.Select((definition, index) =>
                    target.Contents.Count(key => grid.Items.TryGetValue(key, out var content) && content.DefinitionId == definition) == recipe.InputCounts[index]).All(value => value)))
            {
                return "配料尚未齐备，请按菜谱补入原锅";
            }
            return null;
        }

        private static string ContextActionCaption(GridClientComponent grid, GridStationProjection station, GridIntent intent, string originalAction)
        {
            grid.Items.TryGetValue(station.ItemId, out var target);
            grid.Items.TryGetValue(grid.Actors[grid.MemberId].HandItemId, out var hand);
            return intent.Action switch
            {
                GridAction.TakeCounter => "拿起「" + target.Name + "」",
                GridAction.PlaceCounter => "把「" + hand.Name + "」放到「" + station.Name + "」",
                GridAction.AddToContainer => "把「" + hand.Name + "」加入「" + target.Name + "」",
                GridAction.PourContents => "把蛋液倒入「" + target.Name + "」",
                GridAction.PortionSoup => "从锅盛一份汤到「" + hand.Name + "」",
                GridAction.StartStationWork => originalAction + "「" + target.Name + "」",
                _ => originalAction + "「" + (target.Id == 0 ? station.Name : target.Name) + "」"
            };
        }

        private static bool ContextServeMatches(GridClientComponent grid, GridOrderProjection order, long handId)
        {
            if (!grid.Items.TryGetValue(handId, out var hand) || hand.Hygiene != ItemHygiene.Clean || hand.Contents.Count != 1)
            {
                return false;
            }
            var dish = Tables.Instance.DTDishConfig.Get(order.DishId);
            return hand.DefinitionId == dish.ContainerDefinitionId && grid.Items.TryGetValue(hand.Contents[0], out var content) &&
                content.DefinitionId == dish.ContentDefinitionId && (!dish.RequiresOrderBinding || hand.BoundOrderId == order.Id);
        }
    }
}
