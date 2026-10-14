using CodeBind;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [MonoBehaviourBinding]
    public partial class MonoUIGridKitchen : AETMonoUGFUIForm
    {
        private readonly List<Button> buttons = new();
        private Font font;
        private Func<bool> inputGuard;
        // 本轮 HUD 的视口工作区；每次重新计算角点，布局变化无需另建事件来源。
        private readonly Vector3[] viewportCorners = new Vector3[4];
        private Canvas hudCanvas;
        public void SetInputGuard(Func<bool> guard)=>inputGuard=guard;
        public void ConfigureHud()
        {
            hudCanvas=GetComponent<Canvas>();
            CameraDragAreaRectTransform.gameObject.SetActive(true);
            if(font==null)font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 34);
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText,TargetText,DetailsText})text.font=font;
            DetailsPanelRectTransform.gameObject.SetActive(false);
            HintText.text = "点击地面行走 · 拖动查看餐厅 · 点击台面拿放";
        }
        public Rect MapViewport()
        {
            if(hudCanvas==null)
            {
                hudCanvas=GetComponent<Canvas>();
            }
            Canvas root=hudCanvas.rootCanvas;Camera camera=root.renderMode==RenderMode.ScreenSpaceOverlay?null:root.worldCamera;
            CameraDragAreaRectTransform.GetWorldCorners(viewportCorners);
            Vector2 lower=RectTransformUtility.WorldToScreenPoint(camera,viewportCorners[0]),upper=RectTransformUtility.WorldToScreenPoint(camera,viewportCorners[2]);
            return new Rect(lower.x/Screen.width,lower.y/Screen.height,(upper.x-lower.x)/Screen.width,(upper.y-lower.y)/Screen.height);
        }
        public void ShowPanels(bool preparing,bool completed)
        {
            BuffPanelRectTransform.gameObject.SetActive(preparing);OrderPanelRectTransform.gameObject.SetActive(!preparing && !completed);
            SettlementPanelRectTransform.gameObject.SetActive(completed);
        }
        public void ClearTargets()
        {
            foreach(Button button in buttons){button.onClick.RemoveAllListeners();button.gameObject.SetActive(false);Destroy(button.gameObject);}buttons.Clear();
        }
        public void SetInformation(string target, string details, bool showDetails, bool showOrders)
        {
            TargetText.text=target;
            DetailsText.text=details;
            DetailsPanelRectTransform.gameObject.SetActive(showDetails);
            if (!BuffPanelRectTransform.gameObject.activeSelf && !SettlementPanelRectTransform.gameObject.activeSelf)
            {
                OrderPanelRectTransform.gameObject.SetActive(showOrders);
            }
        }
        public void AddTarget(string label, Action action, bool primary=false)
            =>AddButton(primary?PrimaryActionsRectTransform:TargetsRectTransform,label,action,primary);
        public void AddControl(string label,Action action)=>AddButton(ControlPanelRectTransform,label,action);
        public void AddOrderAction(string label,Action action)=>AddButton(OrderActionsRectTransform,label,action);
        public void AddBuffAction(string label,Action action)=>AddButton(BuffActionsRectTransform,label,action);
        private void AddButton(RectTransform parent,string label,Action action,bool primary=false)
        {
            Button button=Instantiate(TargetTemplateButton,parent);button.gameObject.SetActive(true);
            Text caption=button.GetComponentInChildren<Text>();caption.font=font;caption.fontSize=24;caption.resizeTextForBestFit=true;caption.resizeTextMinSize=16;caption.resizeTextMaxSize=24;caption.text=label;
            if(primary)
            {
                ColorBlock colors=button.colors;colors.normalColor=new Color(.58f,.85f,.76f);colors.highlightedColor=new Color(.72f,.96f,.86f);button.colors=colors;
            }
            Func<bool> guard=inputGuard;
            button.onClick.AddListener(()=>{if(guard==null || guard())action();});buttons.Add(button);
            GridLayoutGroup layout=parent.GetComponent<GridLayoutGroup>();
            if(layout!=null)layout.cellSize=new Vector2(Mathf.Max(40,(parent.rect.width-layout.padding.horizontal-layout.spacing.x*(layout.constraintCount-1))/layout.constraintCount),layout.cellSize.y);
        }
        public void ReleaseHud()
        {
            ClearTargets();inputGuard=null;hudCanvas=null; if(font!=null)Destroy(font);font=null;
        }
    }
}
