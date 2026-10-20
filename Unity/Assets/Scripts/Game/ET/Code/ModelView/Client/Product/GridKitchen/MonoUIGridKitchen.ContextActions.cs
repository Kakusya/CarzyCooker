using System.Collections.Generic;
using UnityEngine.UI;

namespace ET.Client
{
    public partial class MonoUIGridKitchen
    {
        private bool m_ContextSummaryReady;
        private bool m_MoreActionsVisible;
        private GridActionGroup m_CurrentActionGroup;
        private readonly List<Button> m_SummaryButtons = new(3);
        public int ContextActionCount => m_SummaryButtons.Count;
        public bool MoreActionsVisible => m_MoreActionsVisible;

        public void CompleteContextActions()
        {
            if (m_EntryLayout)
            {
                return;
            }
            m_ContextSummaryReady = true;
            for (GridContextPriority priority = GridContextPriority.SelectedOrder; priority <= GridContextPriority.Navigation; ++priority)
            {
                foreach (var entry in actionButtons)
                {
                    if (entry.Primary && entry.Priority == priority && m_SummaryButtons.Count < 3)
                    {
                        m_SummaryButtons.Add(entry.Button);
                    }
                }
            }
            foreach (var entry in actionButtons)
            {
                bool summary = m_SummaryButtons.Contains(entry.Button);
                entry.Button.transform.SetParent(summary ? PrimaryActionsRectTransform : TargetsRectTransform, false);
                entry.Button.GetComponentInChildren<Text>().text = summary ? entry.Summary : entry.Label;
                var colors = entry.Button.colors;
                colors.normalColor = summary && m_SummaryButtons[0] == entry.Button
                    ? new UnityEngine.Color(.25f, .55f, .43f) : TargetTemplateButton.colors.normalColor;
                entry.Button.colors = colors;
            }
            AddButton(m_Navigation, m_MoreActionsVisible ? "收起更多动作" : "更多动作", () =>
            {
                m_MoreActionsVisible = !m_MoreActionsVisible;
                foreach (Button button in buttons)
                {
                    if (button != null && (button.name == "更多动作" || button.name == "收起更多动作"))
                    {
                        button.GetComponentInChildren<Text>().text = m_MoreActionsVisible ? "收起更多动作" : "更多动作";
                    }
                }
                ApplyActionGroup(m_CurrentActionGroup);
                ApplyLayout();
            });
            ApplyActionGroup(m_CurrentActionGroup);
            ApplyLayout();
        }
    }
}
