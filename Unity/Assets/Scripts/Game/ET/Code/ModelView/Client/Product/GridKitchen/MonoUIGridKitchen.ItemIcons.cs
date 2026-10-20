using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public partial class MonoUIGridKitchen
    {
        private Sprite m_UnknownItemSprite;
        private Text m_UnknownItemLabel;

        private void ConfigureUnknownIcon()
        {
            m_UnknownItemSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(.5f, .5f), 1);
            var label = new GameObject("UnknownItemPlaceholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(m_HandIcon.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            m_UnknownItemLabel = label.GetComponent<Text>();
            m_UnknownItemLabel.font = font; m_UnknownItemLabel.text = "？"; m_UnknownItemLabel.fontSize = 28;
            m_UnknownItemLabel.color = new Color(.23f, .26f, .21f); m_UnknownItemLabel.alignment = TextAnchor.MiddleCenter;
            m_UnknownItemLabel.raycastTarget = false;
        }

        private void AddOrderIcon(Button button, int definition)
        {
            if (definition < 1 || definition > handArt.Length || handArt[definition - 1] == null)
            {
                return;
            }
            var obj = new GameObject("OrderDishIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(button.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(4, 0); rect.sizeDelta = new Vector2(34, 34);
            var icon = obj.GetComponent<Image>(); icon.sprite = handArt[definition - 1]; icon.preserveAspect = true; icon.raycastTarget = false;
            Text caption = button.GetComponentInChildren<Text>();
            caption.rectTransform.offsetMin = new Vector2(40, caption.rectTransform.offsetMin.y);
        }

        private void ReleaseUnknownIcon()
        {
            if (m_UnknownItemLabel != null)
            {
                Destroy(m_UnknownItemLabel.gameObject);
                m_UnknownItemLabel = null;
            }
            if (m_UnknownItemSprite != null)
            {
                Destroy(m_UnknownItemSprite);
                m_UnknownItemSprite = null;
            }
        }
    }
}
