using System;
using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void RenderDevelopmentTests(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            RuntimeInstanceConfig.RequireDevelopmentAccess(self.Root().GetComponent<ProductRuntimeComponent>().Config.Role);
            var entries = DevelopmentTestCatalog.Entries();
            if (self.SelectedTestLevelId == 0 && self.Failure == null && entries.Count > 0)
            {
                self.SelectedTestLevelId = entries[0].LevelId;
            }
            DevelopmentTestLevel selected = self.SelectedTestLevelId > 0 ? entries.SingleOrDefault(value => value.LevelId == self.SelectedTestLevelId) : null;
            view.PhaseText.text = "开发测试";
            view.ClockText.text = "开始 / 测试关卡";
            view.ScoreText.text = "不计正式进度";
            view.StatusText.text = self.Failure ?? selected?.Failure ?? "选择场景，从准备阶段开始";
            view.HintText.text = "临时餐厅 · 单人无监听 · 退出不保存";
            view.SettlementText.text = selected == null ? "上次选择已失效，请重新选择测试条目。" :
                selected.Name + "\n" + (selected.Group == DevelopmentTestGroup.Chapter ? "直接检查指定小关的操作与关卡流程。" : "单独检查此菜单的制作、物资与服务。") +
                "\n从配置基线初始化工位、有限原料与生产许可。\n不继承正式库存、装修或分数，不生成正式记录。";
            EntityRef<ProductEntryPresentation> original = self;
            foreach (DevelopmentTestLevel entry in entries)
            {
                int id = entry.LevelId;
                string failure = entry.Failure;
                string prefix = entry.Group == DevelopmentTestGroup.Chapter ? "章节 · " : "功能 · ";
                view.AddTarget((id == self.SelectedTestLevelId ? "已选 · " : prefix) + entry.Name + (entry.Failure == null ? "" : " · 不可用"), () =>
                {
                    ProductEntryPresentation current = original;
                    if (current != null && !current.Busy)
                    {
                        current.SelectedTestLevelId = id;
                        current.Failure = failure;
                        current.RenderProductEntry();
                    }
                });
            }
            if (selected != null && selected.Failure == null && !self.Busy)
            {
                int id = selected.LevelId;
                var level = DevelopmentTestCatalog.Resolve(id);
                view.SettlementText.text += "\n本关菜单：" + string.Join("、", level.AllowedDishIds.Concat(level.AllowedDrinkIds).Select(dish => Tables.Instance.DTDishConfig.Get(dish).Name));
                view.AddControl("进入测试", () =>
                {
                    ProductEntryPresentation current = original;
                    if (current == null || current.Busy || current.SelectedTestLevelId != id)
                    {
                        return;
                    }
                    try
                    {
                        DevelopmentTestCatalog.Resolve(id);
                        var config = RuntimeInstanceConfig.Create(Guid.NewGuid().ToString("N"), "test-run", "solo",
                            testDestination: RuntimeTestDestination.Run, testLevelId: id);
                        current.Busy = ProductBootstrapBridge.SwitchInitialization(current.Root().GetComponent<ProductRuntimeComponent>().Config, config);
                    }
                    catch (InvalidOperationException error)
                    {
                        current.Failure = error.Message;
                        current.RenderProductEntry();
                    }
                });
            }
            self.AddEntryNavigation(view, "返回开始页", ProductEntryPage.Home);
        }
    }
}
