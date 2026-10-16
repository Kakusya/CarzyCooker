namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void AddActionNavigation(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage == GridOverviewPage.Help)
            {
                view.ApplyActionGroup(GridActionGroup.Help);
                return;
            }
            if (self.OverviewPage == GridOverviewPage.ActionAdvice)
            {
                view.ApplyActionGroup(GridActionGroup.ActionAdvice);
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            EntityRef<UIGridKitchen> originalHud = self.Hud;
            long epoch = self.HudLoadEpoch;
            string world = grid.WorldId;
            long generation = grid.WorldGeneration, level = grid.LevelId, levelGeneration = grid.LevelGeneration;
            long binding = grid.BindingGeneration;

            Add("全部动作", GridActionGroup.All);
            Add("当前目标动作", GridActionGroup.CurrentTarget);
            Add("厨房动作", GridActionGroup.Kitchen);
            Add("菜谱入口", GridActionGroup.Recipes);
            Add("补货动作", GridActionGroup.Supply);
            Add("前厅动作", GridActionGroup.FrontService);
            Add("布局与装修", GridActionGroup.Layout);
            Add("伙伴互动", GridActionGroup.Companions);
            Add("物品查找", GridActionGroup.Items);
            view.ApplyActionGroup(self.ActionGroup);

            void Add(string caption, GridActionGroup group)
            {
                view.AddActionNavigation(caption, group, () =>
                {
                    GridPresentationComponent current = original;
                    UIGridKitchen hud = originalHud;
                    if (current == null || !current.Ready || hud?.Available != true ||
                        (UIGridKitchen)current.Hud != hud || current.HudLoadEpoch != epoch)
                    {
                        return;
                    }
                    GridClientComponent latest = ((Scene)current.IScene).GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                    if (latest == null || !latest.Confirmed || latest.WorldId != world || latest.WorldGeneration != generation ||
                        latest.LevelId != level || latest.LevelGeneration != levelGeneration || latest.BindingGeneration != binding)
                    {
                        return;
                    }
                    if (group == GridActionGroup.Items)
                    {
                        current.OpenItemLocator(latest);
                        return;
                    }
                    if (current.OverviewPage == GridOverviewPage.Items)
                    {
                        current.CloseItemLocator();
                        current.ActionGroup = group;
                        current.RefreshGridHud();
                        return;
                    }
                    current.ActionGroup = group;
                    hud.View.ApplyActionGroup(group);
                });
            }
        }
    }
}
