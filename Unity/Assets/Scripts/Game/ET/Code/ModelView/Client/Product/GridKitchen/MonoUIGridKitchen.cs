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
        private readonly List<(Button Button, GridActionGroup Group, bool Primary)> actionButtons = new();
        private readonly List<(Button Button, GridActionGroup Group)> actionNavigationButtons = new();
        private GridActionGroup targetGroup;
        private Font font;
        private Func<bool> inputGuard;
        // 本轮 HUD 的视口工作区；每次重新计算角点，布局变化无需另建事件来源。
        private readonly Vector3[] viewportCorners = new Vector3[4];
        private Canvas hudCanvas;
        [SerializeField] private Texture2D m_ArtAtlas;
        [SerializeField] private Image m_HandIcon;
        private Sprite[] handArt;
        private InputField m_EntryAddress;
        public string EntryAddress => m_EntryAddress == null ? "" : m_EntryAddress.text;
        public bool EntryAddressVisible => m_EntryAddress != null && m_EntryAddress.gameObject.activeSelf;

        // 仅由已确认的公开监听地址调用；不接受Session凭证或身份文件。
        public void CopyPublicEndpoint(string endpoint) => GUIUtility.systemCopyBuffer = endpoint;
        public string ReadClipboardAddress() => GUIUtility.systemCopyBuffer;

        public void ShowEntryAddress(string address, int characterLimit = 64)
        {
            if (m_EntryAddress == null)
            {
                var field = new GameObject("JoinAddress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
                field.transform.SetParent(ControlPanelRectTransform, false);
                field.GetComponent<Image>().color = new Color(.99f, .96f, .86f);
                var caption = new GameObject("Address", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                caption.transform.SetParent(field.transform, false);
                RectTransform rect = caption.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(12, 4);
                rect.offsetMax = new Vector2(-12, -4);
                Text text = caption.GetComponent<Text>();
                text.font = font;
                text.fontSize = 22;
                text.color = new Color(.15f, .26f, .23f);
                text.alignment = TextAnchor.MiddleLeft;
                text.supportRichText = false;
                m_EntryAddress = field.GetComponent<InputField>();
                m_EntryAddress.textComponent = text;
                m_EntryAddress.characterLimit = 64;
                m_EntryAddress.lineType = InputField.LineType.SingleLine;
            }
            m_EntryAddress.characterLimit = characterLimit;
            m_EntryAddress.text = address;
            m_EntryAddress.gameObject.SetActive(true);
        }
        public void SetInputGuard(Func<bool> guard)=>inputGuard=guard;
        public void ConfigureHud()
        {
            hudCanvas=GetComponent<Canvas>();
            CameraDragAreaRectTransform.gameObject.SetActive(true);
            if(m_ArtAtlas==null)
            {
                throw new InvalidOperationException("Restaurant HUD art atlas is missing.");
            }
            if(font==null)font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 34);
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText,TargetText,DetailsText})text.font=font;
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText,TargetText,DetailsText})text.color=new Color(.15f,.26f,.23f);
            handArt=new Sprite[21];
            for(int i=0;i<handArt.Length;++i)
            {
                handArt[i]=MonoGridKitchenBoard.CreateItemArtSlice(m_ArtAtlas,i+1);
            }
            SetHandItemIcon(0);
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
            if (m_EntryAddress != null)
            {
                m_EntryAddress.gameObject.SetActive(false);
            }
            foreach(Button button in buttons){button.onClick.RemoveAllListeners();button.gameObject.SetActive(false);Destroy(button.gameObject);}buttons.Clear();
            actionButtons.Clear();
            actionNavigationButtons.Clear();
            targetGroup = GridActionGroup.Kitchen;
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
        public void BeginActionGroup(GridActionGroup group) => targetGroup = group;

        public void AddTarget(string label, Action action, bool primary=false, GridActionGroup? group=null)
        {
            Button button = AddButton(primary ? PrimaryActionsRectTransform : TargetsRectTransform, label, action, primary);
            actionButtons.Add((button, group ?? targetGroup, primary));
        }

        public void AddActionNavigation(string label, GridActionGroup group, Action action)
        {
            Button button = AddButton(TargetsRectTransform, label, action);
            button.transform.SetSiblingIndex(actionNavigationButtons.Count);
            actionNavigationButtons.Add((button, group));
        }

        public void ApplyActionGroup(GridActionGroup group)
        {
            foreach (var entry in actionButtons)
            {
                entry.Button.gameObject.SetActive(entry.Primary || group == GridActionGroup.All || entry.Group == group);
            }
            foreach (var entry in actionNavigationButtons)
            {
                ColorBlock colors = entry.Button.colors;
                colors.normalColor = entry.Group == group ? new Color(.58f, .85f, .76f) : TargetTemplateButton.colors.normalColor;
                entry.Button.colors = colors;
            }
        }
        public void AddControl(string label,Action action)=>AddButton(ControlPanelRectTransform,label,action);
        public void AddOrderAction(string label,Action action)=>AddButton(OrderActionsRectTransform,label,action);
        public void AddBuffAction(string label,Action action)=>AddButton(BuffActionsRectTransform,label,action);
        public void SetHandItemIcon(int definition)
        {
            if(m_HandIcon==null)
            {
                throw new InvalidOperationException("Restaurant hand icon binding is missing.");
            }
            m_HandIcon.enabled=definition>=1 && definition<=21;
            m_HandIcon.sprite=m_HandIcon.enabled?handArt[definition-1]:null;
        }
        private Button AddButton(RectTransform parent,string label,Action action,bool primary=false)
        {
            Button button=Instantiate(TargetTemplateButton,parent);button.gameObject.SetActive(true);
            Text caption=button.GetComponentInChildren<Text>();caption.font=font;caption.fontSize=24;caption.resizeTextForBestFit=true;caption.resizeTextMinSize=16;caption.resizeTextMaxSize=24;caption.text=label;
            caption.color=new Color(.99f,.96f,.86f);
            if(primary)
            {
                ColorBlock colors=button.colors;colors.normalColor=new Color(.58f,.85f,.76f);colors.highlightedColor=new Color(.72f,.96f,.86f);button.colors=colors;
            }
            Func<bool> guard=inputGuard;
            button.onClick.AddListener(() =>
            {
                if (button != null && button.gameObject.activeInHierarchy && (guard == null || guard()))
                {
                    action();
                }
            });
            buttons.Add(button);
            GridLayoutGroup layout=parent.GetComponent<GridLayoutGroup>();
            if(layout!=null)layout.cellSize=new Vector2(Mathf.Max(40,(parent.rect.width-layout.padding.horizontal-layout.spacing.x*(layout.constraintCount-1))/layout.constraintCount),layout.cellSize.y);
            return button;
        }
        public void ReleaseHud()
        {
            if (m_EntryAddress != null)
            {
                m_EntryAddress.gameObject.SetActive(false);
                Destroy(m_EntryAddress.gameObject);
                m_EntryAddress = null;
            }
            ClearTargets();inputGuard=null;hudCanvas=null; if(font!=null)Destroy(font);font=null;
            if(m_HandIcon!=null)m_HandIcon.sprite=null;
            if(handArt!=null)
            {
                foreach(Sprite sprite in handArt)Destroy(sprite);
                handArt=null;
            }
        }
    }
}
