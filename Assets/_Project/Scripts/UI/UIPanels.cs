using UnityEngine;
using UnityEngine.UI;

namespace Nyangsta.UI
{
    /// <summary>Shared building blocks for the full-screen panels (title bars, section labels).</summary>
    public static class UIPanels
    {
        /// <summary>A centred title just under the HUD with a small accent underline.</summary>
        public static Text Header(RectTransform parent, string title, float topSafe)
        {
            var holder = UIFactory.Rect("Header", parent);
            holder.anchorMin = new Vector2(0, 1);
            holder.anchorMax = new Vector2(1, 1);
            holder.pivot = new Vector2(0.5f, 1);
            holder.sizeDelta = new Vector2(0, 84);
            holder.anchoredPosition = new Vector2(0, -(topSafe - 6));

            var text = UIFactory.Text("Title", holder, title, 40, UITheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(text.rectTransform, 0, 0, 0, 16);

            var underline = UIFactory.Image("Underline", holder, UITheme.Pill, UITheme.GoldDeep, Image.Type.Sliced);
            UIFactory.SetAnchors(underline.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(underline.rectTransform, 120, 8);
            underline.rectTransform.anchoredPosition = new Vector2(0, 10);
            return text;
        }

        /// <summary>A category/section divider used inside a scroll list.</summary>
        public static void SectionLabel(Transform content, string label, Color accent)
        {
            var row = UIFactory.Rect("Section_" + label, content);
            UIFactory.PreferredHeight(row.gameObject, 52);

            var dot = UIFactory.Image("Dot", row, UITheme.Circle, accent);
            UIFactory.SetAnchors(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            UIFactory.Size(dot.rectTransform, 22, 22);
            dot.rectTransform.anchoredPosition = new Vector2(22, -2);

            var text = UIFactory.Text("Label", row, label, 28, UITheme.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Stretch(text.rectTransform, 46, 12, 0, 0);
        }
    }
}
