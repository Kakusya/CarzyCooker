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
