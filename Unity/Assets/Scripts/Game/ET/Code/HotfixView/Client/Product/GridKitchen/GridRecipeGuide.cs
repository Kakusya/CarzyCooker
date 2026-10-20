using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    // 只读解释现有配方图与投影；没有动作发送器、计时器或完成标志。
    [FriendOf(typeof(GridClientComponent))]
    public static partial class GridRecipeGuide
    {
        public static string Describe(int dishId, int tickMilliseconds)
        {
            DRDishConfig dish=Tables.Instance.DTDishConfig.Get(dishId);
            var lines=new List<string> { dish.Name, "步骤按实际配方读取，材料需从现有库存或有限供应取得。" };
            var visited=new HashSet<int>();
            void Visit(int id)
            {
                if(!visited.Add(id))return;
                DRDishStageConfig stage=Tables.Instance.DTDishStageConfig.Get(id);
                foreach(int dependency in stage.DependencyIds)Visit(dependency);
                lines.Add(Step(stage,tickMilliseconds));
            }
            Visit(dish.FinalStageId);
            if (dish.RequiresOrderBinding) lines.Add("贴单：把完成饮品放到贴单台，选择当前对应订单贴单。加工中锁壶，完成后可搬运逐杯盛装；一次性杯交付即消耗。");
            lines.Add("交付：开店后询问顾客取得订单，拿起干净成品，走到对应餐桌旁再交付。普通台面放物不计分。");
            return string.Join("\n",lines);
        }

        private static string ItemName(int definition)=>Tables.Instance.DTGridItemConfig.Get(definition).Name;

        private static string Equipment(GridStationCapability capability)=>string.Join("／",Tables.Instance.DTStationConfig.DataList
            .Where(row=>(row.Capabilities&capability)==capability).Select(row=>row.Name));

        private static string Step(DRDishStageConfig stage,int tickMilliseconds)
        {
            string output=ItemName(stage.OutputDefinitionId);
            switch((DishStageKind)stage.Kind)
            {
                case DishStageKind.Raw:
                    if (stage.OutputDefinitionId == GridKitchenRules.HotWater)
                    {
                        return "接热水：拿空泡茶桶，到取水台热水档显式接水；放回泡茶设备，再加入一份红茶叶。热水是实际投入。";
                    }
                    return "备料："+output;
                case DishStageKind.Manual:
                    var manual=Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId);
                    string action=manual.Mode==GridManualMode.Chop?$"在{Equipment(GridStationCapability.Chop)}切配{ItemName(manual.InputDefinitionId)}"
                        :$"将{ItemName(manual.InputDefinitionId)}放进{ItemName(stage.ContainerDefinitionId)}，拿起后打蛋";
                    return $"{action} → {output}（基础 {manual.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）";
                case DishStageKind.Batch:
                    var batch=Tables.Instance.DTGridAutoRecipeConfig.Get(stage.RecipeId);
                    string inputs=string.Join("、",batch.InputDefinitionIds.Select((id,index)=>$"{ItemName(id)} ×{batch.InputCounts[index]}"));
                    return $"把{inputs}放入{ItemName(batch.ContainerDefinitionId)}{(batch.LockContainer ? "；加工期间锁定载具" : "；蛋液从碗倒入")}。在{Equipment((GridStationCapability)batch.StationCapability)}显式{(batch.StationCapability == GridStationCapability.BrewTea ? "泡茶" : batch.StationCapability == GridStationCapability.MixTea ? "调饮" : batch.StationCapability == GridStationCapability.Extract ? "萃取" : batch.StationCapability == GridStationCapability.HeatMilk ? "热奶" : batch.StationCapability == GridStationCapability.FoamMilk ? "打泡" : batch.StationCapability == GridStationCapability.Mixing ? "搅拌" : batch.LockContainer ? "榨汁" : "煮制")} → {output}（基础 {batch.RequiredTicks*tickMilliseconds/1000f:0.0} 秒，{batch.OutputPortions} 份）";
                case DishStageKind.Boil:
                case DishStageKind.Steam:
                case DishStageKind.Grind:
                case DishStageKind.Bake:
                case DishStageKind.Fry:
                case DishStageKind.BakeInCarrier:
                    var bake=Tables.Instance.DTGridBakeRecipeConfig.Get(stage.RecipeId);
                    if ((DishStageKind)stage.Kind == DishStageKind.BakeInCarrier)
                    {
                        return $"把装有一份{ItemName(bake.InputDefinitionId)}的{ItemName(bake.CarrierDefinitionId)}放入{Equipment((GridStationCapability)bake.StationCapability)}，显式烘烤（基础 {bake.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）；完成只替换内部内容，原模具保留。";
                    }
                    return $"将{ItemName(bake.InputDefinitionId)}放入{Equipment((GridStationCapability)bake.StationCapability)}，显式{((DishStageKind)stage.Kind == DishStageKind.Boil ? "煮珍珠" : (DishStageKind)stage.Kind == DishStageKind.Steam ? "蒸制" : (DishStageKind)stage.Kind == DishStageKind.Grind ? "研磨" : (DishStageKind)stage.Kind == DishStageKind.Fry ? "煎制" : "烘烤")} → {output}（基础 {bake.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）";
                case DishStageKind.Unmold:
                    return $"脱模：手持装有烤熟{output}的原模具，到放有干净空{ItemName(stage.ContainerDefinitionId)}的台面显式脱模。模具保留；未熟或未脱模不能交付，满盘失败不转移。";
                case DishStageKind.Portion:
                    if (stage.ContainerDefinitionId == GridKitchenRules.ColdDrinkCup)
                    {
                        return "取份：拿起干净透明冷饮杯，从完成的调饮壶取一份奶茶基底；珍珠奶茶须先放一份熟珍珠，取份后再加冰。每次扣源一份。";
                    }
                    if (stage.ContainerDefinitionId == GridKitchenRules.HotDrinkCup)
                    {
                        return $"取份：手持干净热饮杯，从已完成基底取一份{output}；按配方继续向同一杯添加，每次扣源一份。";
                    }
                    return $"盛装：手持干净空{ItemName(stage.ContainerDefinitionId)}，从已完成批次盛一份{output}。";
                case DishStageKind.AddToContainer:
                    return $"装盘：将{output}添加到干净空{ItemName(stage.ContainerDefinitionId)}，再拿起成品。";
                case DishStageKind.Blend:
                case DishStageKind.Assembly:
                    var assembly = Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId);
                    string components = string.Join("、", assembly.InputDefinitionIds.Select((id, index) => $"{ItemName(id)} ×{assembly.InputCounts[index]}"));
                    if ((DishStageKind)stage.Kind == DishStageKind.Blend)
                    {
                        if (stage.ContainerDefinitionId == GridKitchenRules.ColdDrinkCup)
                        {
                            return $"调饮：在同一干净透明冷饮杯按顺序加入{components}，拿起并明确混合{output}（基础 {assembly.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）。熟珍珠先经煮熟、放入小料罐并从原详情取出；冰最后投入。完成后再贴有效订单。";
                        }
                        return $"调饮：将{components}逐份取入同一干净热饮杯，拿起并明确混合{output}（基础 {assembly.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）" +
                            (assembly.LastInputDefinitionId == 0 ? "。" : $"；{ItemName(assembly.LastInputDefinitionId)}必须等其他基底齐全后最后加入，拒绝不扣源份数。") + "成品之后再贴有效订单。";
                    }
                    string extraction = Tables.Instance.DTGridBakeRecipeConfig.DataList.Any(recipe => recipe.InputDefinitionId == stage.OutputDefinitionId && recipe.CarrierDefinitionId == 0)
                        ? $"完成后把{ItemName(stage.ContainerDefinitionId)}放回台面，在“查看详情”取出{output}，再放入对应加工设备；原器具保留。" : "";
                    return $"合流：将{components}放入同一干净{ItemName(stage.ContainerDefinitionId)}，拿起并明确选择组装{output}（基础 {assembly.RequiredTicks*tickMilliseconds/1000f:0.0} 秒）。未完成不是成品，可停止后交接原载具接续。" + extraction;
                default:
                    throw new System.InvalidOperationException("Unknown recipe guide stage");
            }
        }

        public static string CurrentNeed(GridClientComponent grid, int dishId, long selectedOrderId = 0)
            => CurrentStep(grid, dishId, selectedOrderId).Description;

        public static GridRecipeStep CurrentStep(GridClientComponent grid, int dishId, long selectedOrderId = 0)
        {
            DRDishConfig dish = Tables.Instance.DTDishConfig.Get(dishId);
            int nextStage = 0;
            long preparedItem = 0, destination = 0;
            GridRecipeStepKind stepKind = GridRecipeStepKind.Production;
            string Explain()
            {
                if (selectedOrderId != 0 && (!grid.Orders.TryGetValue(selectedOrderId, out var selected) ||
                    selected.State != OrderState.Waiting || selected.DishId != dishId))
                {
                    return "所选订单已结束，请重新选择，不会自动改为其他顾客。";
                }
                bool Available(GridItemProjection item) => GuideAvailable(grid, item);
                bool HasItem(int definition)=>grid.Items.Values.Any(item=>item.DefinitionId==definition && Available(item));
                bool EmptyContainer(int definition)=>grid.Items.Values.Any(item=>item.DefinitionId==definition && Available(item) && item.Contents.Count==0);
                bool ContainerWith(int container,int content)=>grid.Items.Values.Any(item=>item.DefinitionId==container && item.Hygiene==ItemHygiene.Clean &&
                    item.Contents.Any(id=>grid.Items.TryGetValue(id,out GridItemProjection value) && value.DefinitionId==content));
                GridItemProjection prepared=grid.Items.Values.Where(item=>item.DefinitionId==dish.ContainerDefinitionId && item.Hygiene==ItemHygiene.Clean && item.Contents.Count==1 &&
                    grid.Items.TryGetValue(item.Contents[0],out GridItemProjection content) && content.DefinitionId==dish.ContentDefinitionId &&
                    content.Location==GridItemLocation.Container && content.HolderId==item.Id)
                    .OrderByDescending(item=>item.Location==GridItemLocation.Hand && item.HolderId==grid.MemberId)
                    .ThenByDescending(item=>selectedOrderId!=0 && item.BoundOrderId==selectedOrderId)
                    .ThenBy(item=>item.Id).FirstOrDefault();
                string pause=grid.LevelPaused?"已暂停：只查看现状，恢复后再操作。\n":"";
                if(grid.LevelPhase==LevelPhase.Completed)return pause+"本关已结算。可以继续查看菜谱，旧订单不能再交付。";
                if(prepared.Id!=0)
                {
                    preparedItem = prepared.Id;
                    stepKind = GridRecipeStepKind.Delivery;
                    if(prepared.Location==GridItemLocation.Hand && prepared.HolderId!=grid.MemberId)
                        return pause+$"{dish.Name}已备好，由玩家{prepared.HolderId}持有；请交接，无需重做。";
                    GridOrderProjection order = default;
                    if (dish.RequiresOrderBinding && prepared.BoundOrderId != 0)
                    {
                        if (!grid.Orders.TryGetValue(prepared.BoundOrderId, out order) || order.State != OrderState.Waiting || order.DishId != dishId)
                        {
                            return pause+$"{dish.Name}已备好，但原订单已失效；先到贴单台解绑，再选择当前有效订单。无需重做。";
                        }
                    }
                    else
                    {
                        if (selectedOrderId != 0)
                        {
                            if (dish.RequiresOrderBinding && grid.Items.Values.Any(item => item.BoundOrderId == selectedOrderId))
                            {
                                return pause + "所选订单已有贴单饮品，请查看原杯；阅读不换绑，也无需重复制作。";
                            }
                            order = grid.Orders[selectedOrderId];
                        }
                        else
                        {
                            order=grid.Orders.Values.Where(value=>value.State==OrderState.Waiting && value.DishId==dishId &&
                            (!dish.RequiresOrderBinding || !grid.Items.Values.Any(item=>item.BoundOrderId==value.Id))).OrderBy(value=>value.Id).FirstOrDefault();
                        }
                    }
                    if(order.Id!=0 && grid.Stations.TryGetValue(order.TableStationId,out GridStationProjection table))
                    {
                        destination = order.TableStationId;
                        if (dish.RequiresOrderBinding && prepared.BoundOrderId != order.Id)
                            return pause+$"{dish.Name}已备好，无需重做。放到贴单台，绑定{table.Name}的当前订单，再拿起交付。";
                        string binding = selectedOrderId != 0 && prepared.BoundOrderId != 0 && prepared.BoundOrderId != selectedOrderId
                            ? "此杯已贴另一有效订单，仍按原贴单交付；阅读不换绑。\n" : "";
                        return pause+binding+$"{dish.Name}已备好，无需重做。拿起成品，前往{table.Name}并交付对应订单。";
                    }
                    return pause+$"{dish.Name}已备好，无需重做；可先暂存。"+(grid.LevelPhase==LevelPhase.Preparing?"选择加成并开店，再询问顾客取得有效订单。":"等待顾客询问与对应有效订单。");
                }
                string At(DRDishStageConfig stage, string text)
                {
                    nextStage = stage.Id;
                    return text;
                }
                string Need(int id)
                {
                    DRDishStageConfig stage=Tables.Instance.DTDishStageConfig.Get(id);
                    var kind=(DishStageKind)stage.Kind;
                    if(kind is DishStageKind.Raw or DishStageKind.Manual or DishStageKind.Boil or DishStageKind.Steam or DishStageKind.Grind or DishStageKind.Bake or DishStageKind.Fry or DishStageKind.BakeInCarrier && HasItem(stage.OutputDefinitionId))return null;
                    if(kind==DishStageKind.Manual)
                    {
                        var working=grid.Processes.Values.FirstOrDefault(value=>value.RecipeId==stage.RecipeId && value.State!=GridWorkState.Completed);
                        if(working.Id!=0)return At(stage, GridClientSystem.WorkDescription(working)+(working.State==GridWorkState.Paused?"；可继续原作业。":"；等待真实完成。"));
                        var recipe=Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId);
                        if(recipe.Mode==GridManualMode.Beat && ContainerWith(stage.ContainerDefinitionId,recipe.InputDefinitionId))
                            return At(stage, $"材料已在{ItemName(stage.ContainerDefinitionId)}中，拿起后开始打蛋，无需重新装料。");
                    }
                    if (kind is DishStageKind.Assembly or DishStageKind.Blend)
                    {
                        string carrierName = ItemName(stage.ContainerDefinitionId);
                        string operation = kind == DishStageKind.Blend ? "混合" : "组装";
                        var working = grid.Processes.Values.FirstOrDefault(value => value.RecipeId == stage.RecipeId && value.State != GridWorkState.Completed);
                        if (working.Id != 0)
                        {
                            return At(stage, GridClientSystem.WorkDescription(working) + (working.State == GridWorkState.Paused ? $"；拿起{carrierName}继续{operation}。" : "；等待真实完成。"));
                        }
                        var recipe = Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId);
                        var carrier = grid.Items.Values.FirstOrDefault(item => item.DefinitionId == recipe.ContainerDefinitionId && item.Hygiene == ItemHygiene.Clean &&
                            item.Contents.Any(key => grid.Items.TryGetValue(key, out var content) && recipe.InputDefinitionIds.Contains(content.DefinitionId)));
                        if (carrier.Id != 0)
                        {
                            var missing = recipe.InputDefinitionIds.Select((definition, index) =>
                                (definition, count: recipe.InputCounts[index] - carrier.Contents.Count(key => grid.Items[key].DefinitionId == definition)))
                                .Where(value => value.count > 0).Select(value => ItemName(value.definition) + " ×" + value.count).ToArray();
                            if (missing.Length == 0 && !GridAssemblyConfig.Matches(recipe, carrier.Contents.Select(key => grid.Items[key].DefinitionId).ToArray()))
                            {
                                return At(stage, carrierName + "有多料、错料或顺序不符；到对应设施明确清理，再核对配方。");
                            }
                            return At(stage, missing.Length == 0 ? $"材料齐备；拿起{carrierName}并明确选择{operation}" + ItemName(recipe.OutputDefinitionId) + "。" :
                                carrierName + "还缺：" + string.Join("、", missing) + "。补入同一载具，不必重复已有材料。");
                        }
                    }
                    if(kind==DishStageKind.Batch)
                    {
                        var batch=grid.AutoProcesses.Values.FirstOrDefault(value=>value.RecipeId==stage.RecipeId && (value.State==GridAutoState.Cooking || value.RemainingPortions>0));
                        if(batch.Id!=0)return At(stage, batch.State==GridAutoState.Ready?null:GridClientSystem.AutoWorkDescription(batch)+"；等待真实完成。");
                        var recipe = Tables.Instance.DTGridAutoRecipeConfig.Get(stage.RecipeId);
                        var carrier = grid.Items.Values.Where(item => item.DefinitionId == recipe.ContainerDefinitionId && Available(item) &&
                            item.Contents.Any(key => grid.Items.TryGetValue(key, out var content) && recipe.InputDefinitionIds.Contains(content.DefinitionId)))
                            .OrderBy(item => item.Id).FirstOrDefault();
                        if (carrier.Id != 0)
                        {
                            var missing = recipe.InputDefinitionIds.Select((definition, index) =>
                                (definition, count: recipe.InputCounts[index] - carrier.Contents.Count(key => grid.Items[key].DefinitionId == definition)))
                                .Where(value => value.count > 0).Select(value => ItemName(value.definition) + " ×" + value.count).ToArray();
                            if (missing.Length > 0)
                            {
                                return At(stage, ItemName(recipe.ContainerDefinitionId) + "还缺：" + string.Join("、", missing) + "。补入原载具，不必重复已有材料。");
                            }
                            bool exact = carrier.Contents.Count == recipe.InputCounts.Sum() && carrier.Contents.All(key =>
                                recipe.InputDefinitionIds.Contains(grid.Items[key].DefinitionId));
                            return At(stage, exact ? "材料齐备；" + Step(stage, grid.TickMilliseconds) : "原载具有多料或错料；请核对配方并到对应设施清理。");
                        }
                    }
                    if (kind == DishStageKind.Unmold && grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.BakingMold &&
                        item.Contents.Any(key => grid.Items.TryGetValue(key, out var content) && content.DefinitionId == stage.OutputDefinitionId)))
                    {
                        return At(stage, EmptyContainer(stage.ContainerDefinitionId) ? Step(stage, grid.TickMilliseconds) :
                            $"蛋糕已烤熟，保留原模具；需要干净空{ItemName(stage.ContainerDefinitionId)}后脱模，脏餐具先洗净。");
                    }
                    if(kind is DishStageKind.Boil or DishStageKind.Steam or DishStageKind.Grind or DishStageKind.Bake or DishStageKind.Fry or DishStageKind.BakeInCarrier)
                    {
                        var bake=grid.BakeProcesses.Values.FirstOrDefault(value=>value.RecipeId==stage.RecipeId && value.State==GridBakeState.Baking);
                        if(bake.Id!=0)return At(stage, GridClientSystem.BakeWorkDescription(bake)+"；等待真实完成。");
                    }
                    foreach(int dependency in stage.DependencyIds)
                    {
                        var source=Tables.Instance.DTDishStageConfig.Get(dependency);
                        if(source.OutputDefinitionId==stage.ContainerDefinitionId && HasItem(source.OutputDefinitionId))continue;
                        string missing=Need(dependency);if(missing!=null)return missing;
                    }
                    if(kind==DishStageKind.Raw)
                    {
                        string name=ItemName(stage.OutputDefinitionId);
                        return At(stage, Tables.Instance.DTSupplyConfig.DataList.Any(row=>row.MaterialDefinitionId==stage.OutputDefinitionId)
                            ?$"缺少{name}：查看有限供应，采购后等待收货并拆出一份；不会自动补料。"
                            :$"缺少干净{name}：检查台面／落物和餐具池，脏餐具需先清桌洗净。");
                    }
                    if(kind is DishStageKind.Portion or DishStageKind.AddToContainer && stage.ContainerDefinitionId != GridKitchenRules.HotDrinkCup && !EmptyContainer(stage.ContainerDefinitionId))
                        return At(stage, $"需要干净空{ItemName(stage.ContainerDefinitionId)}；可从台面或干净餐具池取回，脏餐具先洗净。");
                    return At(stage, Step(stage,grid.TickMilliseconds));
                }
                return pause+Need(dish.FinalStageId);
            }
            string description = Explain();
            return ResolveStep(grid, dish, selectedOrderId, nextStage, preparedItem, destination, stepKind, description);
        }
    }
}
