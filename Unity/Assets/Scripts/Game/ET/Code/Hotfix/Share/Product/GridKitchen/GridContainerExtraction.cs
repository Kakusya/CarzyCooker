#if !DOTNET
using System.Linq;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        private static bool IsDrinkAction(GridAction action) =>
            action is GridAction.StartBrewTea or GridAction.StartBoilPearls or GridAction.StartMixTea or GridAction.PortionTeaBase or GridAction.StartGrind or GridAction.StartExtract or GridAction.HeatMilk or GridAction.FoamMilk or GridAction.PortionDrinkBase or GridAction.StartJuice or GridAction.PortionJuice or GridAction.BindDrink or GridAction.UnbindDrink;

        private static bool RequiresGridStation(GridAction action) =>
            action is GridAction.TakeCounter or GridAction.PlaceCounter or GridAction.AddToContainer or GridAction.StartStationWork or
                GridAction.PourContents or GridAction.StartAutoWork or GridAction.PortionSoup or GridAction.StartBake or GridAction.StartFry or GridAction.StartSteam or GridAction.StartMix or GridAction.PortionBatter or GridAction.Unmold or GridAction.TakeContainerContent ||
            IsDrinkAction(action) || IsCleanupAction(action) || IsSupplyInteraction(action);

        private static string CheckContainerExtraction(this GridKitchenComponent self, GridActor actor, GridStation station,
            GridIntent intent, out GridItem container, out GridItem content)
        {
            container = self.Item(station?.ItemId ?? 0);
            content = self.Item(intent.ExpectedItemId);
            if (actor.HandItemId != 0)
            {
                return "HandFull";
            }
            if (actor.WorkId != 0)
            {
                return "WorkBusy";
            }
            if (station == null || container == null || container.Id != intent.ExpectedTargetItemId ||
                container.Location != GridItemLocation.Counter || container.HolderId != station.Id ||
                content == null || content.Location != GridItemLocation.Container || content.HolderId != container.Id ||
                !container.Contents.Contains(content.Id))
            {
                return "ContentsChanged";
            }
            if (intent.ExpectedLayoutVersion != self.LayoutVersion || !container.Contents.SequenceEqual(intent.ExpectedContentIds))
            {
                return "ContentsChanged";
            }
            if (container.DefinitionId == GridKitchenRules.BakingMold)
            {
                return "RequiresUnmold";
            }
            if (container.Hygiene != ItemHygiene.Clean || content.Hygiene != ItemHygiene.Clean)
            {
                return "DirtyUtensil";
            }
            foreach (DRSupplyConfig supply in Tables.Instance.DTSupplyConfig.DataList)
            {
                if (supply.PackageDefinitionId == container.DefinitionId)
                {
                    return "RequiresSupplyPackageAction";
                }
            }
            if (Tables.Instance.DTGridItemConfig.Get(content.DefinitionId).Form != GridMaterialForm.Solid)
            {
                return "RequiresSolidContent";
            }
            if (container.AutoProcessId != 0 || container.BakeProcessId != 0 || content.AutoProcessId != 0 || content.BakeProcessId != 0)
            {
                return "ProcessLocked";
            }
            foreach (var processRef in self.Processes.Values)
            {
                GridManualProcess process = processRef;
                if (process != null && process.State != GridWorkState.Completed &&
                    (process.InputItemId == content.Id || process.TargetItemId == container.Id ||
                     process.InputItemId == container.Id || process.TargetItemId == content.Id))
                {
                    return "ProcessLocked";
                }
            }
            return null;
        }

        private static string TakeContainerContent(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            string error = self.CheckContainerExtraction(actor, station, intent, out GridItem container, out GridItem content);
            if (error != null)
            {
                return error;
            }
            container.Contents.Remove(content.Id);
            content.Location = GridItemLocation.Hand;
            content.HolderId = actor.MemberId;
            content.Cell = actor.Cell;
            actor.HandItemId = content.Id;
            return null;
        }
    }
}
#endif
