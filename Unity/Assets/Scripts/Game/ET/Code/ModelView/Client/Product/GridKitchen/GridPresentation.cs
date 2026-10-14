using UnityEngine;
using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public sealed class GridPresentationComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<GridKitchenBoard> Board;
        public EntityRef<UIGridKitchen> Hud;
        public bool Ready;
        public string Failure;
        public long HudVersion;
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
        // 菜谱选择与首单说明只属于本轮视图，不保存任务进度或业务状态。
        public int RecipeDishId;
        public bool RecipeVisible, FirstOrderGuideVisible;
        public bool HudLoading;
        public long HudLoadEpoch;
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
