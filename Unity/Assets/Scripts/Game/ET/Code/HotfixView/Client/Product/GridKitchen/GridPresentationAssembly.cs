using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void AddAssemblyActions(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid, GridActorProjection actor)
        {
            if (!grid.Items.TryGetValue(actor.HandItemId, out var carrier))
            {
                return;
            }
            string operation = carrier.DefinitionId == GridKitchenRules.HotDrinkCup ? "混合" : "组装";
            var work = grid.Processes.Values.FirstOrDefault(value => value.InputItemId == carrier.Id && value.State != GridWorkState.Completed);
            EntityRef<GridPresentationComponent> original = self;
            if (work.Id != 0 && Tables.Instance.DTGridManualRecipeConfig.Get(work.RecipeId).Mode == GridManualMode.Assembly)
            {
                var intent = new GridIntent(0, actor.WorkId == work.Id ? GridAction.StopWork : GridAction.StartAssembly,
                    actor.Cell, carrier.Id, carrier.Id, carrier.Id, work.Id, carrier.Contents, recipeId: work.RecipeId);
                view.AddTarget(actor.WorkId == work.Id ? "停止" + operation : "继续" + operation, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendGridIntent(intent);
                });
                return;
            }
            foreach (var recipe in Tables.Instance.DTGridManualRecipeConfig.DataList)
            {
                if (recipe.Mode != GridManualMode.Assembly || recipe.ContainerDefinitionId != carrier.DefinitionId ||
                    !grid.AllowedDishIds.Any(id => Tables.Instance.DTDishConfig.Get(id).ContentDefinitionId == recipe.OutputDefinitionId))
                {
                    continue;
                }
                var intent = new GridIntent(0, GridAction.StartAssembly, actor.Cell, carrier.Id, carrier.Id, carrier.Id,
                    expectedContents: carrier.Contents, recipeId: recipe.Id);
                view.AddTarget(operation + " " + Tables.Instance.DTGridItemConfig.Get(recipe.OutputDefinitionId).Name, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendGridIntent(intent);
                });
            }
        }
    }
}
