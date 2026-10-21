using System;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public enum ProductEntryLayout { Page, Home, ChapterSelection }

    public enum ProductHudLayout
    {
        Entry,
        Restaurant,
        Details
    }

    public partial class MonoUIGridKitchen
    {
        [SerializeField] private RectTransform m_Navigation;
        [SerializeField] private ScrollRect m_ControlScroll;
        [SerializeField] private ScrollRect m_EntryScroll;
        private ScrollRect m_ActionScroll;
        private ScrollRect m_DetailsScroll;
        private RectTransform m_Header;
        private RectTransform m_Footer;
        private bool m_EntryLayout;
        private bool m_DevelopmentTest;
        private ProductEntryLayout m_EntryPage;
        private bool m_Preparing;
        private bool m_Completed;
        private float m_ActionPosition = 1;
        private float m_ControlPosition = 1;
        private bool m_RestoreScroll;
        private bool m_RestoreDetails;
        private float m_DetailsPosition = 1;
        private (bool Entry, ProductEntryLayout Page, bool Preparing, bool Completed, bool Details, float Scale, float Width, bool Context, bool More) m_LayoutKey;
        private bool m_LayoutValid;

        public ProductHudLayout LayoutMode => m_EntryLayout ? ProductHudLayout.Entry :
            DetailsPanelRectTransform.gameObject.activeSelf ? ProductHudLayout.Details : ProductHudLayout.Restaurant;

        private void ConfigureLayout()
        {
            if (m_Navigation == null || m_ControlScroll == null || m_EntryScroll == null)
            {
                throw new InvalidOperationException("Restaurant HUD layout bindings are missing.");
            }
            m_ActionScroll = TargetsRectTransform.parent.GetComponent<ScrollRect>();
            m_DetailsScroll = DetailsPanelRectTransform.GetComponent<ScrollRect>();
            m_Header = (RectTransform)PhaseText.transform.parent;
            m_Footer = (RectTransform)HandText.transform.parent;
            m_ActionPosition = m_ControlPosition = 1;
            m_ControlScroll.verticalNormalizedPosition = m_ActionScroll.verticalNormalizedPosition = 1;
            m_EntryScroll.verticalNormalizedPosition = m_DetailsScroll.verticalNormalizedPosition = 1;
            OrderActionsRectTransform.gameObject.SetActive(false);
            var orders = OrderPanelRectTransform.GetComponent<ScrollRect>();
            if (orders == null || orders.content != OrdersText.rectTransform)
            {
                throw new InvalidOperationException("Restaurant order-summary scroll binding is missing.");
            }
            orders.verticalNormalizedPosition = 1;
            m_EntryLayout = false;
            m_DevelopmentTest = false;
            m_LayoutValid = false;
            ApplyLayout();
        }

        public void ShowEntryLayout(ProductEntryLayout page = ProductEntryLayout.Page)
        {
            m_EntryLayout = true;
            m_EntryPage = page;
            BuffPanelRectTransform.gameObject.SetActive(false);
            OrderPanelRectTransform.gameObject.SetActive(false);
            SettlementPanelRectTransform.gameObject.SetActive(true);
            CameraDragAreaRectTransform.gameObject.SetActive(false);
            ApplyLayout();
        }

        private static void Region(RectTransform rect, float bottom, float top, float left = 0, float right = 1)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = new Vector2(12, 4);
            rect.offsetMax = new Vector2(-12, -4);
        }

        private void ApplyLayout()
        {
            if (m_Header == null)
            {
                return;
            }
            bool details = DetailsPanelRectTransform.gameObject.activeSelf;
            var key = (m_EntryLayout, m_EntryPage, m_Preparing, m_Completed, details, m_FontScale, ((RectTransform)transform).rect.width, m_ContextSummaryReady, m_MoreActionsVisible);
            if (m_LayoutValid && m_LayoutKey == key)
            {
                return;
            }
            m_LayoutValid = true;
            m_LayoutKey = key;
            Region(m_Header, .85f, 1);
            Region(PhaseText.rectTransform, .60f, 1, 0, .65f);
            Region(ClockText.rectTransform, .60f, 1, .65f, 1);
            Region(ScoreText.rectTransform, .28f, .60f, 0, .65f);
            Region(StatusText.rectTransform, .28f, .60f, .65f, 1);
            Region(HintText.rectTransform, 0, .28f);
            Region(m_Navigation, 0, .075f);
            Region((RectTransform)m_ControlScroll.transform, .075f, m_EntryLayout ? .55f : .15f);
            Region(m_Footer, m_EntryLayout ? .075f : .15f, m_EntryLayout ? .30f : .35f);
            // Entry actions and record rows each have their own scroll viewport.
            if (m_EntryLayout)
            {
                Region((RectTransform)m_ControlScroll.transform, .31f, .59f);
                Region(SettlementPanelRectTransform, .60f, .85f);
                Region((RectTransform)m_ActionScroll.transform, 0, 1);
                if (m_EntryPage == ProductEntryLayout.Home)
                {
                    Region(SettlementPanelRectTransform, .64f, .84f, .06f, .94f);
                    Region((RectTransform)m_ControlScroll.transform, .32f, .63f, .10f, .90f);
                    Region(m_Footer, .14f, .30f, .06f, .94f);
                }
                else if (m_EntryPage == ProductEntryLayout.ChapterSelection && ((RectTransform)transform).rect.width > 900 &&
                    ((RectTransform)transform).rect.width > ((RectTransform)transform).rect.height)
                {
                    Region((RectTransform)m_ControlScroll.transform, .10f, .84f, .02f, .42f);
                    Region(SettlementPanelRectTransform, .53f, .84f, .44f, .98f);
                    Region(m_Footer, .10f, .52f, .44f, .98f);
                }
            }
            else
            {
                Region((RectTransform)m_ActionScroll.transform, 0, .43f);
                Region(SettlementPanelRectTransform, .39f, .84f, .04f, .96f);
            }
            float mapBottom = .35f;
            if (!m_EntryLayout && m_ContextSummaryReady)
            {
                mapBottom = m_MoreActionsVisible ? .48f : .32f;
                Region(m_Footer, .15f, mapBottom);
                Region((RectTransform)m_ActionScroll.transform, 0, .46f);
                Region(PrimaryActionsRectTransform, m_MoreActionsVisible ? .48f : .02f, .68f);
                Region(TargetText.rectTransform, .68f, .86f);
                Region(HandText.rectTransform, .86f, 1);
            }
            m_ActionScroll.gameObject.SetActive(m_EntryLayout || !m_ContextSummaryReady || m_MoreActionsVisible);
            HandText.gameObject.SetActive(!m_EntryLayout);
            TargetText.gameObject.SetActive(!m_EntryLayout);
            PrimaryActionsRectTransform.gameObject.SetActive(!m_EntryLayout);
            WorkText.gameObject.SetActive(false);
            Region(CameraDragAreaRectTransform, mapBottom, m_Preparing ? .70f : .80f);
            Region(BuffPanelRectTransform, .70f, .85f);
            Region(OrderPanelRectTransform, .80f, .85f);
            Region(DetailsPanelRectTransform, .35f, .85f, .02f, .98f);
            // A full opaque detail surface consumes pointer/scroll input before the map.
            DetailsPanelRectTransform.SetAsLastSibling();
            m_Navigation.SetAsLastSibling();
            LayoutButtons(ControlPanelRectTransform, EntryColumns());
            LayoutButtons(TargetsRectTransform, 2);
            LayoutButtons(m_Navigation, 3);
            LayoutButtons(PrimaryActionsRectTransform, m_ContextSummaryReady ? 3 : 2);
            LayoutButtons(BuffActionsRectTransform, 2);
            LayoutButtons(OrderActionsRectTransform, 2);
            m_Navigation.gameObject.SetActive(true);
            if (m_EntryLayout || details || m_Completed)
            {
                m_RestoreScroll = true;
            }
        }

        private void LayoutButtons(RectTransform parent, int columns)
        {
            GridLayoutGroup layout = parent.GetComponent<GridLayoutGroup>();
            if (layout == null)
            {
                return;
            }
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            float width = (parent.rect.width - layout.padding.horizontal - layout.spacing.x * (columns - 1)) / columns;
            float height = 76 * m_FontScale;
            if (m_EntryLayout && m_EntryPage == ProductEntryLayout.Home && parent == ControlPanelRectTransform)
            {
                float available = ((RectTransform)m_ControlScroll.transform).rect.height - layout.padding.vertical - layout.spacing.y * 2;
                height = Mathf.Min(132 * m_FontScale, Mathf.Max(60, available / 3));
            }
            layout.cellSize = new Vector2(Mathf.Max(40, width), height);
        }

        private void RememberListPosition()
        {
            if (m_ActionScroll == null)
            {
                return;
            }
            m_ActionPosition = m_ActionScroll.verticalNormalizedPosition;
            m_ControlPosition = m_ControlScroll.verticalNormalizedPosition;
            m_RestoreScroll = true;
        }

        private void LateUpdate()
        {
            if (!m_RestoreScroll || m_ActionScroll == null)
            {
                return;
            }
            m_RestoreScroll = false;
            Canvas.ForceUpdateCanvases();
            LayoutButtons(ControlPanelRectTransform, EntryColumns());
            LayoutButtons(TargetsRectTransform, 2);
            LayoutButtons(m_Navigation, 3);
            m_Footer.gameObject.SetActive(!m_EntryLayout || buttons.Exists(button => button != null && button.transform.parent == TargetsRectTransform));
            m_Navigation.gameObject.SetActive(!m_EntryLayout || buttons.Exists(button => button != null && button.transform.parent == m_Navigation));
            ScoreText.gameObject.SetActive(!m_EntryLayout || !string.IsNullOrEmpty(ScoreText.text));
            Canvas.ForceUpdateCanvases();
            m_ActionScroll.verticalNormalizedPosition = Mathf.Clamp01(m_ActionPosition);
            m_ControlScroll.verticalNormalizedPosition = Mathf.Clamp01(m_ControlPosition);
            if (m_RestoreDetails)
            {
                m_RestoreDetails = false;
                m_DetailsScroll.StopMovement();
                m_DetailsScroll.verticalNormalizedPosition = m_DetailsPosition;
            }
        }

        private void RememberViewportPosition()
        {
            if (m_DetailsScroll == null)
            {
                return;
            }
            m_DetailsPosition = Mathf.Clamp01(m_DetailsScroll.verticalNormalizedPosition);
            m_RestoreDetails = true;
            RememberListPosition();
        }

        private RectTransform ControlParent(string label)
        {
            if (label.StartsWith("返回", StringComparison.Ordinal) || label == "收起详情" ||
                label == "取消本机设置" || label == "保存本机设置" || label == "确认开始" || label == "取消开局")
            {
                return m_Navigation;
            }
            return ControlPanelRectTransform;
        }

        private int EntryColumns() => !m_EntryLayout ? 3 : m_EntryPage == ProductEntryLayout.Home ? 1 : 2;

        public void SetDevelopmentTest(bool enabled)
        {
            m_DevelopmentTest = enabled;
            if (enabled && !ScoreText.text.StartsWith("测试 · ", StringComparison.Ordinal))
            {
                SetScore(ScoreText.text);
            }
        }

        public void SetScore(string score) => ScoreText.text = (m_DevelopmentTest ? "测试 · " : "") + score;
    }
}
