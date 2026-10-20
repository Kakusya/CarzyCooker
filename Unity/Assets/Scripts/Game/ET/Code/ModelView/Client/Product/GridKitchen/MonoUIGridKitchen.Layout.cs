using System;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
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
        private bool m_Preparing;
        private bool m_Completed;
        private float m_ActionPosition = 1;
        private float m_ControlPosition = 1;
        private bool m_RestoreScroll;
        private bool m_RestoreDetails;
        private float m_DetailsPosition = 1;
        private (bool Entry, bool Preparing, bool Completed, bool Details, float Scale, float Width, bool Context, bool More) m_LayoutKey;
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
            m_LayoutValid = false;
            ApplyLayout();
        }

        public void ShowEntryLayout()
        {
            m_EntryLayout = true;
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
            var key = (m_EntryLayout, m_Preparing, m_Completed, details, m_FontScale, ((RectTransform)transform).rect.width, m_ContextSummaryReady, m_MoreActionsVisible);
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
            LayoutButtons(ControlPanelRectTransform, m_EntryLayout ? 2 : 3);
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
            layout.cellSize = new Vector2(Mathf.Max(40, width), 76 * m_FontScale);
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
            LayoutButtons(ControlPanelRectTransform, m_EntryLayout ? 2 : 3);
            LayoutButtons(TargetsRectTransform, 2);
            LayoutButtons(m_Navigation, 3);
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
    }
}
