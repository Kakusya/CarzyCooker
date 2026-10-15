using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string ContainerExtractionReason(string code, string fallback) => code switch
        {
            "RequiresSupplyPackageAction" => "请使用包装拆份取料",
            "RequiresSolidContent" => "只能取出尚未加工的固体，液体请倾倒",
            _ => fallback
        };

        private static void AddContainerExtractionTargets(this GridPresentationComponent self, MonoUIGridKitchen view,
            GridClientComponent grid, GridActorProjection own, GridStationProjection station)
        {
            if (!self.DetailsVisible || grid.LevelPaused || grid.LevelPhase == LevelPhase.Completed || own.HandItemId != 0 ||
                own.WorkId != 0 || !grid.Items.TryGetValue(station.ItemId, out GridItemProjection source) ||
                source.Form != GridMaterialForm.Container || source.Hygiene != ItemHygiene.Clean ||
                source.AutoProcessId != 0 || source.BakeProcessId != 0 ||
                Tables.Instance.DTSupplyConfig.DataList.Any(row => row.PackageDefinitionId == source.DefinitionId))
            {
                return;
            }
            foreach (long contentId in source.Contents)
            {
                if (!grid.Items.TryGetValue(contentId, out GridItemProjection content) || content.Form != GridMaterialForm.Solid ||
                    content.Hygiene != ItemHygiene.Clean || content.AutoProcessId != 0 || content.BakeProcessId != 0 ||
                    grid.Processes.Values.Any(work => work.State != GridWorkState.Completed &&
                        (work.InputItemId == content.Id || work.TargetItemId == source.Id ||
                         work.InputItemId == source.Id || work.TargetItemId == content.Id)))
                {
                    continue;
                }
                var intent = new GridIntent(0, GridAction.TakeContainerContent, station.Cell, station.Id, content.Id, source.Id,
                    expectedContents: source.Contents, expectedLayoutVersion: grid.LayoutVersion);
                EntityRef<GridPresentationComponent> original = self;
                view.AddTarget("取出 " + content.Name + " · " + station.Name, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendGridIntent(intent);
                }, self.IsSelectedTarget(station.Id));
            }
        }
    }
}
