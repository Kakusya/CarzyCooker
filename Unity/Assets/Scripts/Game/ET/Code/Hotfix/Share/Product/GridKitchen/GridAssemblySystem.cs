#if !DOTNET
using System.Linq;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        // Tick内直接核对最多四份实物，不创建临时集合或第二份库存。
        private static bool AssemblyContentsMatch(this GridKitchenComponent self, GridItem carrier, DRGridManualRecipeConfig recipe)
        {
            int total = 0;
            for (int index = 0; index < recipe.InputDefinitionIds.Count; ++index)
            {
                int count = 0;
                foreach (long id in carrier.Contents)
                {
                    GridItem item = self.Item(id);
                    if (item == null || item.Location != GridItemLocation.Container || item.HolderId != carrier.Id || item.Hygiene != ItemHygiene.Clean)
                    {
                        return false;
                    }
                    if (item.DefinitionId == recipe.InputDefinitionIds[index])
                    {
                        ++count;
                    }
                }
                if (count != recipe.InputCounts[index])
                {
                    return false;
                }
                total += count;
            }
            return total == carrier.Contents.Count && (recipe.LastInputDefinitionId == 0 ||
                self.Item(carrier.Contents[carrier.Contents.Count - 1]).DefinitionId == recipe.LastInputDefinitionId);
        }

        private static string StartAssembly(this GridKitchenComponent self, GridActor actor, GridIntent intent)
        {
            GridItem carrier = self.Item(actor.HandItemId);
            var recipe = Tables.Instance.DTGridManualRecipeConfig.GetOrDefault(intent.RecipeId);
            if (recipe == null || recipe.Mode != GridManualMode.Assembly)
            {
                return "NoRecipe";
            }
            if (carrier == null || carrier.Id != intent.ExpectedTargetItemId || carrier.Id != intent.ExpectedItemId ||
                carrier.Location != GridItemLocation.Hand || carrier.HolderId != actor.MemberId)
            {
                return "TargetChanged";
            }
            if (carrier.DefinitionId != recipe.ContainerDefinitionId || carrier.Hygiene != ItemHygiene.Clean)
            {
                return "RequiresCleanAssemblyCarrier";
            }
            if (!carrier.Contents.SequenceEqual(intent.ExpectedContentIds) || !self.AssemblyContentsMatch(carrier, recipe))
            {
                return "AssemblyIngredientsMismatch";
            }
            GridManualProcess process = self.InputProcess(carrier.Id);
            if ((process?.Id ?? 0) != intent.ExpectedProcessId || process != null && process.RecipeId != recipe.Id)
            {
                return "ProcessChanged";
            }
            if (actor.WorkId != 0 || self.ItemWorking(carrier.Id))
            {
                return "WorkBusy";
            }
            bool continuing = process != null;
            if (!continuing)
            {
                string permission = self.CheckNewRecipePermission(self.ProductLevel()?.ProductionPermit.ManualRecipes.Contains(recipe.Id) ?? true);
                if (permission != null)
                {
                    return permission;
                }
            }
            if (process == null)
            {
                process = self.AddChild<GridManualProcess>();
                process.RecipeId = recipe.Id; process.InputItemId = process.TargetItemId = carrier.Id;
                process.RequiredTicks = recipe.RequiredTicks; process.ProgressTicks = 0; process.OutputItemId = 0;
                self.Processes.Add(process.Id, process);
            }
            process.StationId = 0; process.StartedTick = self.Tick;
            process.WorkerMemberId = actor.MemberId; process.WorkerGeneration = actor.Generation;
            process.State = GridWorkState.Working; actor.WorkId = process.Id;
            process.CompletionQueued = process.CompletionReceived = false;
            self.PublishKitchenStage(process, KitchenProcessKind.Manual, continuing ? KitchenWorkStage.Resumed : KitchenWorkStage.Started, ++process.TransitionSerial);
            return null;
        }

        private static void CompleteAssembly(this GridKitchenComponent self, GridManualProcess process, GridActor actor,
            GridItem carrier, DRGridManualRecipeConfig recipe)
        {
            // 本Tick已完整核对载具及精确内容；新成品身份替换内部投入，外层盘不变。
            GridItem output = self.CreateGridItem(recipe.OutputDefinitionId, GridItemLocation.Container, carrier.Id, carrier.Cell);
            foreach (long id in carrier.Contents)
            {
                GridItem input = self.Item(id);
                self.Items.Remove(id); input.Dispose();
            }
            carrier.Contents.Clear(); carrier.Contents.Add(output.Id);
            process.OutputItemId = output.Id; process.State = GridWorkState.Completed;
            process.WorkerMemberId = 0; process.WorkerGeneration = 0; actor.WorkId = 0;
            process.CompletionQueued = process.CompletionReceived = false;
            self.PublishKitchenStage(process, KitchenProcessKind.Manual, KitchenWorkStage.Finished, ++process.TransitionSerial, output.Id);
        }
    }
}
#endif
