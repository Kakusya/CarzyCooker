using System;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void AddDevelopmentTestControls(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            RuntimeInstanceConfig config = self.Root().GetComponent<ProductRuntimeComponent>().Config;
            long epoch = self.HudLoadEpoch;
            string world = grid.WorldId;
            long generation = grid.WorldGeneration;
            view.SetDevelopmentTest(true);
            view.AddControl("重开当前测试", () => Switch(RuntimeTestDestination.Run));
            view.AddControl("返回测试列表", () => Switch(RuntimeTestDestination.Catalog));
            view.AddControl("返回主菜单", () => Switch(RuntimeTestDestination.None));

            void Switch(RuntimeTestDestination destination)
            {
                GridPresentationComponent current = original;
                var active = ((Scene)current?.IScene)?.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                if (current?.Ready != true || current.HudLoadEpoch != epoch || active?.Confirmed != true ||
                    active.WorldId != world || active.WorldGeneration != generation || !ReferenceEquals(config, RuntimeInstanceContext.Config))
                {
                    return;
                }
                RuntimeInstanceConfig next = RuntimeInstanceConfig.Create(Guid.NewGuid().ToString("N"), "test-navigation", "solo",
                    showModeMenu: destination != RuntimeTestDestination.Run, testDestination: destination, testLevelId: config.TestLevelId);
                ProductBootstrapBridge.SwitchInitialization(config, next);
            }
        }
    }
}
