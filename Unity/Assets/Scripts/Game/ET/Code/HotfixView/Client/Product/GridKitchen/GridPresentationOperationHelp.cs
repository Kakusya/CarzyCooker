using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ClearOperationHelp(this GridPresentationComponent self)
        {
            ++self.HelpSerial;
            self.HelpTopic = GridHelpTopic.Map;
            if (self.ActionGroup == GridActionGroup.Help)
            {
                self.ActionGroup = self.HelpPreviousGroup == GridActionGroup.Help ? GridActionGroup.All : self.HelpPreviousGroup;
            }
            if (self.OverviewPage == GridOverviewPage.Help)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
        }

        private static void OpenOperationHelp(this GridPresentationComponent self)
        {
            self.ClearActionAdvice();
            self.HelpPreviousGroup = self.ActionGroup;
            self.ResetItemLocator(); self.ClearOrderNavigation(); self.ResetSupplyOverview(); self.ResetFrontWorkboard();
            self.ClearStationInspection(); self.ClearRenovationPreview();
            self.RecipeVisible = self.DetailsVisible = false;
            self.CompanionChat = CompanionChatTopic.None;
            self.OverviewPage = GridOverviewPage.Help;
            self.ActionGroup = GridActionGroup.Help;
            ++self.HelpSerial;
            self.RefreshGridHud();
        }

        private static void RefreshOperationHelp(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Help)
            {
                if (self.ActionGroup == GridActionGroup.Help)
                {
                    self.ActionGroup = self.HelpPreviousGroup;
                }
                return;
            }
            if (self.HelpDisplayedSerial == self.HelpSerial && self.HelpDisplayedPhase == grid.LevelPhase && self.HelpDisplayedPaused == grid.LevelPaused)
            {
                return;
            }
            ProductRuntimeComponent runtime = ((ClientSessionComponent)grid.Parent).Runtime;
            RuntimeInstanceRole role = runtime.Config.Role;
            string phase = grid.LevelPhase switch { LevelPhase.Preparing => "准备", LevelPhase.Operating => "营业", LevelPhase.Draining => "收尾", _ => "结算" };
            string text = self.HelpTopic switch
            {
                GridHelpTopic.Map => "轻点工位会选择并执行当前互动。\n轻点可走空地会移动。\n上下拖动查看前厅，释放不追加点击。\n布局调整中点格会直接提交移位。\n本机设置可调字号和拖动速度，拖动仍不点击。",
                GridHelpTopic.Carry => "普通手槽和台面一格一物。\n拿取和放置使用对应动作。\n满手时先找合法空台面放下。\n容器搬运会连同内容一起移动。",
                GridHelpTopic.Containers => "容器和内部内容是不同实物。\n取出固体、倾倒液体使用各自动作。\n熟食按份盛装，不等同整锅搬走。\n加工锁定会限制搬运或改料。",
                GridHelpTopic.Work => "手工加工需到位并持续作业。\n停手保留进度，合法队友可接续。\n自动加工完成不等于已交付。\n收到请求不等于加工已经完成。",
                GridHelpTopic.Phase => "当前：" + phase + (grid.LevelPaused ? " · 已暂停\n阅读不会自动恢复。" : "\n阅读不会自动暂停，经营按原状态推进。") +
                    (grid.LevelPhase == LevelPhase.Preparing ? "\n开店条件见准备总览；备料不是门槛。" :
                        grid.LevelPhase == LevelPhase.Draining ? "\n停止新客，继续已有前厅工作。" :
                        grid.LevelPhase == LevelPhase.Completed ? "\n按结算入口处理装修、保存和去向。" : "\n查看有效订单，并按实际材料制作。"),
                _ => role switch
                {
                    RuntimeInstanceRole.Solo => "当前：单机。\n可选择加成、开店、暂停和换关。\n退出不会补存；保存走原结算入口。",
                    RuntimeInstanceRole.Host => "当前：合作房主。\n房主管开店、暂停、装修和换关。\n世界控制权不等于来源记录所有权。",
                    _ => "当前：合作队员。\n可做菜、前厅和合法补货。\n开店、暂停、装修和换关由房主控制。"
                }
            };
            view.SetInformation("操作帮助 · " + phase + (grid.LevelPaused ? " · 暂停" : ""), text, true, self.OrdersVisible);
            self.HelpDisplayedSerial = self.HelpSerial;
            self.HelpDisplayedPhase = grid.LevelPhase;
            self.HelpDisplayedPaused = grid.LevelPaused;
        }

        private static void AddOperationHelp(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            EntityRef<UIGridKitchen> hudRef = self.Hud;
            long epoch = self.HudLoadEpoch, serial = self.HelpSerial;
            string world = grid.WorldId;
            long worldGeneration = grid.WorldGeneration, level = grid.LevelId, generation = grid.LevelGeneration, binding = grid.BindingGeneration;
            bool shown = self.OverviewPage == GridOverviewPage.Help;
            GridPresentationComponent Current()
            {
                GridPresentationComponent active = original;
                UIGridKitchen hud = hudRef;
                var current = ((Scene)active?.IScene)?.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                return active?.Ready == true && hud?.Available == true && (UIGridKitchen)active.Hud == hud && active.HudLoadEpoch == epoch &&
                    active.HelpSerial == serial && (active.OverviewPage == GridOverviewPage.Help) == shown &&
                    current?.Confirmed == true && current.WorldId == world && current.WorldGeneration == worldGeneration &&
                    current.LevelId == level && current.LevelGeneration == generation && current.BindingGeneration == binding ? active : null;
            }
            view.AddControl(shown ? "关闭操作帮助" : "操作帮助", () =>
            {
                var active = Current();
                if (active == null) { return; }
                if (!shown) { active.OpenOperationHelp(); }
                else { active.ClearOperationHelp(); active.RefreshGridHud(); }
            });
            if (!shown) { return; }
            AddTopic("帮助：地图与输入", GridHelpTopic.Map);
            AddTopic("帮助：拿取与放置", GridHelpTopic.Carry);
            AddTopic("帮助：容器与内容", GridHelpTopic.Containers);
            AddTopic("帮助：加工与接续", GridHelpTopic.Work);
            AddTopic("帮助：当前阶段", GridHelpTopic.Phase);
            AddTopic("帮助：角色权限", GridHelpTopic.Roles);
            AddLink("帮助：前往菜谱", GridActionGroup.Recipes);
            AddLink("帮助：前往补货", GridActionGroup.Supply);
            AddLink("帮助：前往前厅", GridActionGroup.FrontService);

            void AddTopic(string caption, GridHelpTopic topic) => view.AddTarget(caption, () =>
            {
                var active = Current();
                if (active == null) { return; }
                active.HelpTopic = topic; ++active.HelpSerial; active.RefreshGridHud();
            }, group: GridActionGroup.Help);
            void AddLink(string caption, GridActionGroup group) => view.AddTarget(caption, () =>
            {
                var active = Current();
                if (active == null) { return; }
                active.ClearOperationHelp(); active.ActionGroup = group; active.RefreshGridHud();
            }, group: GridActionGroup.Help);
        }
    }
}
