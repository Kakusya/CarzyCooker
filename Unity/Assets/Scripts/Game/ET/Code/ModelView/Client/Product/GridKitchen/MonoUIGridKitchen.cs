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
        public void ConfigureHud()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 34);
            StatusText.font = font; HandText.font = font; HintText.font = font;
            HintText.text = "点击地面行走 · 拖动查看餐厅 · 点击台面拿放";
        }
        public void ClearTargets()
        {
            foreach(Button button in buttons){button.onClick.RemoveAllListeners();Destroy(button.gameObject);}buttons.Clear();
        }
        public void AddTarget(string label, Action action)
        {
            Button button=Instantiate(TargetTemplateButton,TargetsRectTransform);button.gameObject.SetActive(true);
            Text caption=button.GetComponentInChildren<Text>();caption.font=font;caption.fontSize=30;caption.text=label;
            button.onClick.AddListener(()=>action());buttons.Add(button);
        }
        public void ReleaseHud()
        {
            ClearTargets(); if(font!=null)Destroy(font);font=null;
        }
    }
}
