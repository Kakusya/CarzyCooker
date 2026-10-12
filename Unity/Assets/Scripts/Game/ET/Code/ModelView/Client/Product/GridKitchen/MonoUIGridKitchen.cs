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
        public void SetInputGuard(Func<bool> guard)=>inputGuard=guard;
        public void ConfigureHud()
        {
            if(font==null)font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 34);
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText})text.font=font;
            HintText.text = "点击地面行走 · 拖动查看餐厅 · 点击台面拿放";
        }
        public Rect MapViewport()
        {
            Canvas root=GetComponent<Canvas>().rootCanvas;Camera camera=root.renderMode==RenderMode.ScreenSpaceOverlay?null:root.worldCamera;
            var corners=new Vector3[4];CameraDragAreaRectTransform.GetWorldCorners(corners);
            Vector2 lower=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),upper=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
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
        public void AddTarget(string label, Action action)
            =>AddButton(TargetsRectTransform,label,action);
        public void AddControl(string label,Action action)=>AddButton(ControlPanelRectTransform,label,action);
        public void AddOrderAction(string label,Action action)=>AddButton(OrderActionsRectTransform,label,action);
        public void AddBuffAction(string label,Action action)=>AddButton(BuffActionsRectTransform,label,action);
        private void AddButton(RectTransform parent,string label,Action action)
        {
            Button button=Instantiate(TargetTemplateButton,parent);button.gameObject.SetActive(true);
            Text caption=button.GetComponentInChildren<Text>();caption.font=font;caption.fontSize=24;caption.resizeTextForBestFit=true;caption.resizeTextMinSize=16;caption.resizeTextMaxSize=24;caption.text=label;
            Func<bool> guard=inputGuard;
            button.onClick.AddListener(()=>{if(guard==null || guard())action();});buttons.Add(button);
            GridLayoutGroup layout=parent.GetComponent<GridLayoutGroup>();
            if(layout!=null)layout.cellSize=new Vector2(Mathf.Max(40,(parent.rect.width-layout.padding.horizontal-layout.spacing.x*(layout.constraintCount-1))/layout.constraintCount),layout.cellSize.y);
        }
        public void ReleaseHud()
        {
            ClearTargets();inputGuard=null; if(font!=null)Destroy(font);font=null;
        }
    }
}
