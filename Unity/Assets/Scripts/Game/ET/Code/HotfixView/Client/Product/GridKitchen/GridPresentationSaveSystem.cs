namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void RefreshSaveStatus(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (!grid.SaveEnabled || grid.LevelPhase != LevelPhase.Completed) return;
            view.SettlementText.text += grid.SaveState switch
            {
                SoloSaveState.Writing => "\n正在保存检查点…",
                SoloSaveState.Failed => "\n保存失败，本次结算保留，请重试",
                SoloSaveState.Committed => "\n检查点已保存，可重试继续下一关",
                _ => "\n只新建检查点，不覆盖已有记录"
            };
        }
    }
}
