#if !DOTNET
using System.Linq;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        private static KitchenProcessKind AutoWorkKind(GridStationCapability capability) => capability switch
        {
            GridStationCapability.Mixing => KitchenProcessKind.Mix,
            GridStationCapability.Extract or GridStationCapability.HeatMilk or GridStationCapability.FoamMilk => KitchenProcessKind.Beverage,
            GridStationCapability.Juice => KitchenProcessKind.Juice,
            _ => KitchenProcessKind.Soup
        };

        private static string Unmold(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem mold = self.Item(actor.HandItemId);
            GridItem plate = self.Item(station.ItemId);
            if (mold == null || mold.Id != intent.ExpectedItemId || mold.DefinitionId != GridKitchenRules.BakingMold ||
                mold.Location != GridItemLocation.Hand || mold.HolderId != actor.MemberId || plate == null ||
                plate.Id != intent.ExpectedTargetItemId || plate.Location != GridItemLocation.Counter || plate.HolderId != station.Id)
            {
                return "TargetChanged";
            }
            if (self.ItemWorking(mold.Id) || self.ItemWorking(plate.Id))
            {
                return "ProcessLocked";
            }
            if (mold.Hygiene != ItemHygiene.Clean || plate.Hygiene != ItemHygiene.Clean)
            {
                return "DirtyUtensil";
            }
            if (!mold.Contents.SequenceEqual(intent.ExpectedContentIds) || mold.Contents.Count != 1)
            {
                return "ContentsChanged";
            }
            GridItem cake = self.Item(mold.Contents[0]);
            if (cake == null || cake.DefinitionId != GridKitchenRules.PlainCake || cake.Location != GridItemLocation.Container || cake.HolderId != mold.Id)
            {
                return "CakeNotReady";
            }
            if (plate.DefinitionId != GridKitchenRules.DessertPlate)
            {
                return "IncompatibleContainer";
            }
            if (plate.Contents.Count != 0)
            {
                return "ContainerFull";
            }
            string permission = self.CheckProductionInput(plate.DefinitionId, cake.DefinitionId);
            if (permission != null)
            {
                return permission;
            }
            self.PauseWork(actor);
            mold.Contents.Clear();
            plate.Contents.Add(cake.Id);
            cake.Location = GridItemLocation.Container;
            cake.HolderId = plate.Id;
            cake.Cell = plate.Cell;
            return null;
        }
    }
}
#endif
