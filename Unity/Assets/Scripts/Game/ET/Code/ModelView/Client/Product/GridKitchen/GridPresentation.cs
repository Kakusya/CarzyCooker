using UnityEngine;

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
        public string HudTargetsKey;
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
