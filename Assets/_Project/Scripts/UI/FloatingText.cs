using UnityEngine;
using UnityEngine.UI;

namespace Nyangsta.UI
{
    /// <summary>
    /// Spawns short-lived "+12 G" style numbers that pop, rise and fade. Positions
    /// are given in screen space and mapped into the overlay FX layer. The layer is
    /// supplied once by <see cref="GameUIController"/>.
    /// </summary>
    public static class FloatingText
    {
        private static RectTransform _layer;

        public static void SetLayer(RectTransform layer) => _layer = layer;

        public static void Spawn(Vector2 screenPos, string text, Color color, int fontSize = 40, float rise = 110f)
        {
            if (_layer == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _layer, screenPos, null, out Vector2 local);

            var t = UIFactory.Text("Float", _layer, text, fontSize, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = t.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.anchoredPosition = local + new Vector2(Random.Range(-12f, 12f), 0f);
            rt.sizeDelta = new Vector2(260, 60);

            UITween.PopIn(rt, 0.18f);
            Vector2 target = rt.anchoredPosition + new Vector2(Random.Range(-10f, 10f), rise);
            UITween.MoveAnchored(rt, target, 0.95f);
            UITween.FadeGraphic(t, 0f, 0.95f, () => { if (t != null) Object.Destroy(t.gameObject); });
        }

        /// <summary>Convenience for gold gains formatted with a coin glyph.</summary>
        public static void Gold(Vector2 screenPos, double amount)
            => Spawn(screenPos, $"+{amount:N0}", UITheme.Gold, 42);
    }

    /// <summary>
    /// Transient top-of-screen notifications ("새 지역 해금!", "직원 영입!"). Stacks
    /// downward, slides in, holds, then fades out.
    /// </summary>
    public static class Toast
    {
        private static RectTransform _layer;
        private static int _active;

        public static void SetLayer(RectTransform layer) => _layer = layer;

        public static void Show(string message, Color? accent = null)
        {
            if (_layer == null) return;
            Color color = accent ?? UITheme.Leaf;

            var holder = UIFactory.Panel("Toast", _layer, UITheme.Panel, out _, shadow: true, outline: true);
            UIFactory.SetAnchors(holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            UIFactory.Size(holder, 0, 78);

            var stripe = UIFactory.Image("Accent", holder, UITheme.Pill, color, Image.Type.Sliced);
            UIFactory.SetAnchors(stripe.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f));
            stripe.rectTransform.sizeDelta = new Vector2(12, -16);
            stripe.rectTransform.anchoredPosition = new Vector2(14, 0);

            var label = UIFactory.Text("Msg", holder, message, 28, UITheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(label.rectTransform, 28, 18, 6, 6);

            // Width based on message length, clamped.
            float width = Mathf.Clamp(message.Length * 22f + 90f, 360f, 720f);
            UIFactory.Size(holder, width, 78);

            int slot = _active++;
            float targetY = -40f - slot * 88f;
            holder.anchoredPosition = new Vector2(0, 60f);
            UITween.MoveAnchored(holder, new Vector2(0, targetY), 0.32f);
            UITween.PopIn(holder, 0.32f);

            var cg = holder.gameObject.AddComponent<CanvasGroup>();
            UITween.Run(HideAfter(holder, cg));
        }

        private static System.Collections.IEnumerator HideAfter(RectTransform holder, CanvasGroup cg)
        {
            yield return new WaitForSecondsRealtime(2.1f);
            UITween.MoveAnchored(holder, holder.anchoredPosition + new Vector2(0, 50f), 0.4f);
            UITween.Fade(cg, 0f, 0.4f, () =>
            {
                if (holder != null) Object.Destroy(holder.gameObject);
                _active = Mathf.Max(0, _active - 1);
            });
        }
    }
}
