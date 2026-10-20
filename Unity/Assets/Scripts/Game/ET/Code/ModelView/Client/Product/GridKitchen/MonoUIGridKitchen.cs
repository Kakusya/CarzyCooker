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
        private readonly List<(Button Button, GridActionGroup Group, bool Primary, string Label, string Summary, GridContextPriority Priority)> actionButtons = new();
        private readonly List<(Button Button, GridActionGroup Group)> actionNavigationButtons = new();
        private GridActionGroup targetGroup;
        private Font font;
        private Func<bool> inputGuard;
        // 本轮 HUD 的视口工作区；每次重新计算角点，布局变化无需另建事件来源。
        private readonly Vector3[] viewportCorners = new Vector3[4];
        private Canvas hudCanvas;
        [SerializeField] private Texture2D m_ArtAtlas;
        [SerializeField] private Texture2D m_BistroAtlas;
        [SerializeField] private Image m_HandIcon;
        private Sprite[] handArt;
        private InputField m_EntryAddress;
        private Rect m_AppliedSafeArea;
        private Vector2 m_AppliedScreenSize;
        private bool m_HasSafeArea;
        private float m_FontScale = 1;
        private bool m_HasFontSize;
        private ProductFontSize m_FontSize;
        private readonly Dictionary<Text, int> m_OriginalTextSizes = new();
        private readonly Dictionary<GridLayoutGroup, float> m_OriginalCellHeights = new();
        private readonly Dictionary<RectTransform, (Vector2 Min, Vector2 Max)> m_OriginalHeaderAnchors = new();
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
                text.fontSize = Mathf.RoundToInt(22 * m_FontScale);
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
            m_HasFontSize = false;
            hudCanvas=GetComponent<Canvas>();
            m_HasSafeArea = false;
            ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            CameraDragAreaRectTransform.gameObject.SetActive(true);
            if(m_ArtAtlas==null)
            {
                throw new InvalidOperationException("Restaurant HUD art atlas is missing.");
            }
            if(font==null)font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 34);
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText,TargetText,DetailsText})text.font=font;
            foreach(Text text in new[]{StatusText,HandText,HintText,PhaseText,ClockText,ScoreText,WorkText,OrdersText,BuffText,SettlementText,TargetText,DetailsText})text.color=new Color(.15f,.26f,.23f);
            handArt=new Sprite[MonoGridKitchenBoard.ItemArtDefinitionCount];
            for(int i=0;i<handArt.Length;++i)
            {
                handArt[i]=MonoGridKitchenBoard.CreateHudItemArtSlice(m_ArtAtlas,m_BistroAtlas,i+1);
            }
            ConfigureUnknownIcon();
            SetHandItemIcon(0);
            DetailsPanelRectTransform.gameObject.SetActive(false);
            ConfigureLayout();
            HintText.text = "点击地面行走 · 拖动查看餐厅 · 点击台面拿放";
        }

        public void ApplyLocalFontSize(ProductFontSize size)
        {
            if (m_HasFontSize && m_FontSize == size)
            {
                return;
            }
            m_HasFontSize = true;
            m_FontSize = size;
            m_FontScale = size switch { ProductFontSize.Larger => 1.15f, ProductFontSize.Largest => 1.3f, _ => 1f };
            foreach (var text in new[] { PhaseText, ClockText, ScoreText })
            {
                RectTransform rect = text.rectTransform;
                if (!m_OriginalHeaderAnchors.ContainsKey(rect))
                {
                    m_OriginalHeaderAnchors.Add(rect, (rect.anchorMin, rect.anchorMax));
                }
                var original = m_OriginalHeaderAnchors[rect];
                rect.anchorMin = original.Min;
                rect.anchorMax = original.Max;
            }
            if (size != ProductFontSize.Standard)
            {
                // 放大后为完整“参考/未结算”留宽，不用缩字抵消字号选择。
                PhaseText.rectTransform.anchorMax = new Vector2(.35f, PhaseText.rectTransform.anchorMax.y);
                ClockText.rectTransform.anchorMin = new Vector2(.35f, ClockText.rectTransform.anchorMin.y);
                ClockText.rectTransform.anchorMax = new Vector2(.6f, ClockText.rectTransform.anchorMax.y);
                ScoreText.rectTransform.anchorMin = new Vector2(.6f, ScoreText.rectTransform.anchorMin.y);
            }
            foreach (Text text in new[] { StatusText, HandText, HintText, PhaseText, ClockText, ScoreText, WorkText, OrdersText, BuffText, SettlementText, TargetText, DetailsText })
            {
                if (!m_OriginalTextSizes.TryGetValue(text, out int original))
                {
                    original = text.fontSize;
                    m_OriginalTextSizes.Add(text, original);
                }
                text.fontSize = Mathf.RoundToInt(original * m_FontScale);
                text.resizeTextForBestFit = false;
            }
            foreach (var layout in GetComponentsInChildren<GridLayoutGroup>(true))
            {
                if (!m_OriginalCellHeights.TryGetValue(layout, out float original))
                {
                    original = layout.cellSize.y;
                    m_OriginalCellHeights.Add(layout, original);
                }
                layout.cellSize = new Vector2(layout.cellSize.x, original * m_FontScale);
            }
            foreach (Button button in buttons)
            {
                ApplyButtonFont(button.GetComponentInChildren<Text>());
            }
            if (m_EntryAddress != null)
            {
                m_EntryAddress.textComponent.fontSize = Mathf.RoundToInt(22 * m_FontScale);
            }
            ApplyLayout();
        }

        private void ApplyButtonFont(Text caption)
        {
            caption.fontSize = Mathf.RoundToInt(24 * m_FontScale);
            caption.resizeTextForBestFit = m_FontSize == ProductFontSize.Standard;
            caption.resizeTextMinSize = Mathf.RoundToInt(16 * m_FontScale);
            caption.resizeTextMaxSize = Mathf.RoundToInt(24 * m_FontScale);
        }

        private void Update()
        {
            if (hudCanvas != null)
            {
                ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            }
        }

        public void ApplySafeArea(Rect area, Vector2 screenSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || area.width <= 0 || area.height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(area));
            }
            if (m_HasSafeArea && area == m_AppliedSafeArea && screenSize == m_AppliedScreenSize)
            {
                return;
            }
            RememberViewportPosition();
            m_HasSafeArea = true;
            m_AppliedSafeArea = area;
            m_AppliedScreenSize = screenSize;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(Mathf.Clamp01(area.xMin / screenSize.x), Mathf.Clamp01(area.yMin / screenSize.y));
            rect.anchorMax = new Vector2(Mathf.Clamp01(area.xMax / screenSize.x), Mathf.Clamp01(area.yMax / screenSize.y));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            m_EntryLayout = false;
            CameraDragAreaRectTransform.gameObject.SetActive(true);
            m_Preparing = preparing;
            m_Completed = completed;
            BuffPanelRectTransform.gameObject.SetActive(preparing);OrderPanelRectTransform.gameObject.SetActive(!preparing && !completed);
            SettlementPanelRectTransform.gameObject.SetActive(completed);
            ApplyLayout();
        }
        public void ClearTargets()
        {
            RememberListPosition();
            if (m_EntryAddress != null)
            {
                m_EntryAddress.gameObject.SetActive(false);
            }
            foreach(Button button in buttons){button.onClick.RemoveAllListeners();button.gameObject.SetActive(false);Destroy(button.gameObject);}buttons.Clear();
            actionButtons.Clear();
            m_ContextSummaryReady = false;
            m_SummaryButtons.Clear();
            actionNavigationButtons.Clear();
            targetGroup = GridActionGroup.Kitchen;
        }
        public void SetInformation(string target, string details, bool showDetails, bool showOrders)
        {
            TargetText.text=target;
            DetailsText.text=details;
            DetailsPanelRectTransform.gameObject.SetActive(showDetails);
            ApplyLayout();
            if (!BuffPanelRectTransform.gameObject.activeSelf && !SettlementPanelRectTransform.gameObject.activeSelf)
            {
                OrderPanelRectTransform.gameObject.SetActive(showOrders);
            }
        }
        public void BeginActionGroup(GridActionGroup group) => targetGroup = group;

        public void AddTarget(string label, Action action, bool primary=false, GridActionGroup? group=null,
            string summaryLabel = null, GridContextPriority priority = GridContextPriority.Related)
        {
            Button button = AddButton(primary ? PrimaryActionsRectTransform : TargetsRectTransform, label, action, primary);
            actionButtons.Add((button, group ?? targetGroup, primary, label, summaryLabel ?? label, priority));
        }

        public void AddActionNavigation(string label, GridActionGroup group, Action action)
        {
            Button button = AddButton(TargetsRectTransform, label, action);
            button.transform.SetSiblingIndex(actionNavigationButtons.Count);
            actionNavigationButtons.Add((button, group));
        }

        public void ApplyActionGroup(GridActionGroup group)
        {
            m_CurrentActionGroup = group;
            foreach (var entry in actionButtons)
            {
                bool summary = m_ContextSummaryReady && m_SummaryButtons.Contains(entry.Button);
                entry.Button.gameObject.SetActive(summary || (!m_ContextSummaryReady || m_MoreActionsVisible) &&
                    (entry.Primary || group == GridActionGroup.All || entry.Group == group));
            }
            foreach (var entry in actionNavigationButtons)
            {
                entry.Button.gameObject.SetActive(!m_ContextSummaryReady || m_MoreActionsVisible);
                ColorBlock colors = entry.Button.colors;
                colors.normalColor = entry.Group == group ? new Color(.58f, .85f, .76f) : TargetTemplateButton.colors.normalColor;
                entry.Button.colors = colors;
            }
        }
        public void AddControl(string label,Action action)=>AddButton(ControlParent(label),label,action);
        public void AddOrderAction(string label, Action action, bool primary = false, int dishDefinition = 0)
        {
            Button button = AddButton(OrderActionsRectTransform, label, action);
            AddOrderIcon(button, dishDefinition);
            actionButtons.Add((button, GridActionGroup.FrontService, primary, label, label,
                primary ? GridContextPriority.SelectedOrder : GridContextPriority.Related));
        }
        public void AddBuffAction(string label,Action action)=>AddButton(BuffActionsRectTransform,label,action);
        public void SetHandItemIcon(int definition)
        {
            if(m_HandIcon==null)
            {
                throw new InvalidOperationException("Restaurant hand icon binding is missing.");
            }
            bool known = definition >= 1 && definition <= handArt.Length && handArt[definition - 1] != null;
            m_HandIcon.enabled = definition != 0;
            m_HandIcon.sprite = known ? handArt[definition - 1] : definition == 0 ? null : m_UnknownItemSprite;
            m_HandIcon.color = known ? Color.white : new Color(.82f, .77f, .66f);
            m_UnknownItemLabel.gameObject.SetActive(definition != 0 && !known);
        }
        private Button AddButton(RectTransform parent,string label,Action action,bool primary=false)
        {
            Button button=Instantiate(TargetTemplateButton,parent);button.gameObject.SetActive(true);
            button.gameObject.name = label;
            Text caption=button.GetComponentInChildren<Text>();caption.font=font;ApplyButtonFont(caption);caption.text=label;
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
            m_MoreActionsVisible = false;
            if(m_HandIcon!=null)m_HandIcon.sprite=null;
            ReleaseUnknownIcon();
            if(handArt!=null)
            {
                foreach(Sprite sprite in handArt)Destroy(sprite);
                handArt=null;
            }
        }
    }
}
