using UnityEngine;
using System.Collections.Generic;

namespace ET.Client
{
    public enum CompanionChatTopic { None = 0, Preparation = 1, Cooperation = 2, Closing = 3 }
    public enum GridOverviewPage { None = 0, Preparation = 1, Items = 2, Supply = 3, FrontWork = 4, Stations = 5, Renovation = 6, Settlement = 7, Help = 8, ActionAdvice = 9 }
    public enum ActionAdviceKind { Unknown = 0, Hand = 1, Locked = 2, Container = 3, Path = 4, Permission = 5, Supply = 6, Target = 7, Save = 8, Paused = 9 }
    public enum GridHelpTopic { Map = 0, Carry = 1, Containers = 2, Work = 3, Phase = 4, Roles = 5 }

    public enum GridActionGroup
    {
        All = 0,
        CurrentTarget = 1,
        Kitchen = 2,
        Recipes = 3,
        Supply = 4,
        FrontService = 5,
        Layout = 6,
        Companions = 7,
        Items = 8,
        Help = 9,
        ActionAdvice = 10
    }

    [ComponentOf(typeof(Scene))]
    public sealed class GridPresentationComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<GridKitchenBoard> Board;
        public EntityRef<UIGridKitchen> Hud;
        public bool Ready;
        public bool RoomControlPending;
        public string Failure;
        public long HudVersion;
        // Root合作待办的显示版本，查询和持久事实仍归原owner。
        public long DisplayedClaimInboxVersion;
        // 已展示字段，不含消息或业务实体。动作结构与时间／进度分别失效；重开首次完整显示。
        public bool HudTargetsValid, HudInformationValid;
        public readonly List<(long Number,string Text)> HudActionFields=new(512);
        public readonly List<(long Number,string Text)> HudInformationFields=new(128);
        // 本轮恢复页的派生显示上下文；凭证只比较可用性，绝不存入显示键。
        public bool RecoveryDisplayed, RecoveryCredentialAvailable;
        public ProductStage RecoveryStage;
        public long RecoveryAttempt, RecoveryHudEpoch;
        public string RecoveryFeedback;
        public EntityRef<ProductRuntimeComponent> RecoveryRuntime;
        public EntityRef<ClientSessionComponent> RecoveryClient;
        public EntityRef<UIGridKitchen> RecoveryHud;
        public bool PointerHeld, PointerDragged, PointerConsumed;
        public Vector2 PointerStart, PointerLast;
        public GridCell? Highlight;
        public long HighlightCommand;
        // 短寿命界面选择；位置和朝向仍从已提交投影读取。
        public long LayoutStationId, LayoutLevelId, LayoutLevelGeneration;
        public bool BuffCandidatesVisible;
        // 仅保存界面选择；内容与作业进度仍从当前关的独立投影读取。
        public long SelectedStationId, SelectedLevelId, SelectedLevelGeneration;
        public bool DetailsVisible, OrdersVisible;
        // 仅筛选原动作按钮；不保存或重新裁决业务资格。
        public GridActionGroup ActionGroup;
        // 只读总览的短寿命页面；所属关卡／世界换轮失效，不保存准备或完成状态。
        public GridOverviewPage OverviewPage;
        // 本轮只读帮助分类及返回分组；不持有教学进度或输入/业务权威。
        public GridHelpTopic HelpTopic;
        public GridActionGroup HelpPreviousGroup;
        public long HelpSerial;
        // 帮助正文只随页面轮次、阶段和暂停变化；不随每次厨房Tick重建。
        public long HelpDisplayedSerial;
        public LevelPhase HelpDisplayedPhase;
        public bool HelpDisplayedPaused;
        // 最近一次本HUD原请求的目标元数据；不保存请求/响应或操作历史。
        public long AdviceSubmittedCommand, AdviceSubmittedStation, AdviceSubmittedItem, AdviceSubmittedLayout;
        // 只读处理页绑定当前反馈和查看目标，任一变化都撤下旧建议。
        public long ActionAdviceSerial, ActionAdviceDisplayedSerial, ActionAdviceCommand, ActionAdviceStation, ActionAdviceItem, ActionAdviceHand, ActionAdviceLayout;
        public GridResultStatus ActionAdviceStatus;
        public SoloSaveState ActionAdviceSaveState;
        public LevelPhase ActionAdvicePhase;
        public bool ActionAdvicePaused;
        // 仅捕获当前查看的两个实物值；内容或锁定改变后不能继续展示旧建议。
        public GridItemProjection ActionAdviceTargetValue, ActionAdviceHandValue;
        public bool ActionAdviceTargetLocked, ActionAdviceHandLocked;
        public string ActionAdviceReason, ActionAdviceSaveResult, ActionAdviceFeedback;
        public GridActionGroup ActionAdvicePreviousGroup;
        // 本轮物品查询与精确选择；位置、宿主、加工状态每次从当前投影解析。
        public string ItemLocatorQuery, ItemLocatorInput, ItemLocatorWorld;
        public long ItemLocatorSelectedId, ItemLocatorGeneration;
        public GridItemKind? ItemLocatorKind;
        public bool ItemLocatorDetailsVisible;
        // 供应页仅保存查看目标；余额、货物与包装仍归原投影。
        public int SupplyOverviewSourceId;
        public long SupplyOverviewShipmentId;
        // 前厅看板仅持当前查看的工作ID，不持有认领或完成权威。
        public long FrontWorkboardSelectedId;
        // 工位说明只保存显示选择；几何每次取原已提交投影。
        public bool StationInspectionGeometryVisible, StationInspectionDetailsVisible;
        // 装修预览只持显示目标及预期版本；SelectedRenovationId仍只由原权威提交。
        public int RenovationPreviewId, RenovationPreviewExpectedLevel;
        public long RenovationPreviewStationId, RenovationPreviewVersion, RenovationPreviewLevelId, RenovationPreviewGeneration;
        // 菜谱选择与首单说明只属于本轮视图，不保存任务进度或业务状态。
        public int RecipeDishId;
        public bool RecipeVisible, FirstOrderGuideVisible;
        // 本轮阅读目标，不是订单认领或饮品绑定；完整菜谱／缺口共用原详情区。
        public long RecipeOrderId;
        public long RecipeItemId;
        public bool OrderRecipeDetailsVisible;
        public string OrderNavigationFeedback;
        public bool HudLoading;
        public long HudLoadEpoch;
        // 当前视图的只读选择与已显示次数，不持有业务成长权威。
        public CompanionChatTopic CompanionChat;
        public string CompanionWorld, CompanionFeedback;
        public long CompanionWorldGeneration, CompanionLevel, CompanionLevelGeneration, CompanionRole, CompanionFeedbackRevision;
        public int CompanionDisplayedCount;
    }

    [ComponentOf(typeof(GFEntityComponent))]
    public sealed class GridKitchenBoard : UGFEntity<MonoGridKitchenBoard>, IAwake, IUGFEntityOnHide
    {
    }
    [ComponentOf(typeof(UIComponent))]
    public sealed class UIGridKitchen : UGFUIForm<MonoUIGridKitchen>, IAwake, IUGFUIFormOnClose
    {
    }
}
