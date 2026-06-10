using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nyangsta.UI
{
    /// <summary>Lightweight handle returned by <see cref="UIFactory.Button"/>.</summary>
    public class ButtonRef
    {
        public GameObject go;
        public RectTransform rect;
        public Button button;
        public Image bg;
        public Text label;
        private Color _baseColor;

        public void Bind(Color baseColor) => _baseColor = baseColor;

        public void SetInteractable(bool v)
        {
            button.interactable = v;
            if (bg != null) bg.color = v ? _baseColor : UITheme.Disabled;
            if (label != null) label.color = v ? label.color : new Color(0.95f, 0.93f, 0.9f);
        }

        public void SetText(string s) { if (label != null) label.text = s; }
        public void SetColor(Color c) { _baseColor = c; if (bg != null) bg.color = c; }
    }

    /// <summary>
    /// Code-first uGUI builder. Every widget the game needs (panels, buttons,
    /// labels, pills, scroll lists, progress bars) is created here so screens can
    /// be assembled without prefabs or the Unity editor.
    /// </summary>
    public static class UIFactory
    {
        // ---------------------------------------------------------- base objects
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color,
            UnityEngine.UI.Image.Type type = UnityEngine.UI.Image.Type.Simple)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            if (type == UnityEngine.UI.Image.Type.Sliced) img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        /// <summary>
        /// Rounded, 9-sliced panel with optional drop shadow + outline. Returns the
        /// holder to position/size; <paramref name="body"/> is the fill image (recolour it).
        /// </summary>
        public static RectTransform Panel(string name, Transform parent, Color color, out Image body,
            bool shadow = true, bool outline = false)
        {
            var holder = Rect(name, parent);
            if (shadow)
            {
                var sh = Image("Shadow", holder, UITheme.Rounded, new Color(0f, 0f, 0f, 0.18f),
                    UnityEngine.UI.Image.Type.Sliced);
                Stretch(sh.rectTransform, 0, 0, 0, 0);
                sh.rectTransform.anchoredPosition = new Vector2(0, -6);
            }
            if (outline)
            {
                var ol = Image("Edge", holder, UITheme.Rounded, UITheme.PanelEdge,
                    UnityEngine.UI.Image.Type.Sliced);
                Stretch(ol.rectTransform, -3, -3, -3, -3);
            }
            body = Image("Body", holder, UITheme.Rounded, color, UnityEngine.UI.Image.Type.Sliced);
            Stretch(body.rectTransform, 0, 0, 0, 0);
            return holder;
        }

        /// <summary>Convenience overload when the body image isn't needed.</summary>
        public static RectTransform Panel(string name, Transform parent, Color color,
            bool shadow = true, bool outline = false)
            => Panel(name, parent, color, out _, shadow, outline);

        public static Text Text(string name, Transform parent, string value, int fontSize,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UITheme.Font;
            t.text = value;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static ButtonRef Button(string name, Transform parent, string label, Color bg,
            Action onClick, int fontSize = 30, Color? textColor = null)
        {
            var holder = Rect(name, parent);

            var shadow = Image("Shadow", holder, UITheme.Rounded, new Color(0, 0, 0, 0.18f),
                UnityEngine.UI.Image.Type.Sliced);
            Stretch(shadow.rectTransform, 0, 0, 0, 0);
            shadow.rectTransform.anchoredPosition = new Vector2(0, -5);

            var img = Image("BG", holder, UITheme.Rounded, bg, UnityEngine.UI.Image.Type.Sliced);
            Stretch(img.rectTransform, 0, 0, 0, 0);

            var btn = holder.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var txt = Text("Label", img.transform, label, fontSize, textColor ?? Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(txt.rectTransform, 10, 10, 4, 4);

            var fx = holder.gameObject.AddComponent<ButtonFx>();
            fx.Init(holder);

            var refObj = new ButtonRef { go = holder.gameObject, rect = holder, button = btn, bg = img, label = txt };
            refObj.Bind(bg);
            return refObj;
        }

        /// <summary>Small icon + value chip used in the HUD (gold, gems, essence).</summary>
        public static Text Pill(string name, Transform parent, Color iconColor, string iconGlyph,
            out Image icon, out RectTransform holder)
        {
            holder = Rect(name, parent);
            var bg = Image("BG", holder, UITheme.Pill, new Color(0f, 0f, 0f, 0.28f),
                UnityEngine.UI.Image.Type.Sliced);
            Stretch(bg.rectTransform, 0, 0, 0, 0);

            icon = Image("Icon", holder, UITheme.Circle, iconColor);
            SetAnchors(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            icon.rectTransform.anchoredPosition = new Vector2(20, 0);
            icon.rectTransform.sizeDelta = new Vector2(26, 26);
            var glyph = Text("Glyph", icon.transform, iconGlyph, 18, UITheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(glyph.rectTransform, 0, 0, 0, 0);

            var label = Text("Value", holder, "0", 26, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            SetAnchors(label.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0.5f));
            Stretch(label.rectTransform, 38, 16, 0, 0);
            return label;
        }

        /// <summary>Scrollable vertical list. Returns the content transform (add cards to it).</summary>
        public static RectTransform ScrollList(string name, Transform parent, out ScrollRect scroll,
            float spacing = 14f, RectOffset padding = null)
        {
            var viewport = Rect(name, parent);
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.001f);   // near-invisible, needed for masking
            var mask = viewport.gameObject.AddComponent<RectMask2D>();

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            var content = Rect("Content", viewport);
            SetAnchors(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            content.sizeDelta = new Vector2(0, 0);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(8, 8, 8, 24);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewport;
            return content;
        }

        /// <summary>Horizontal progress/fill bar. Returns the fill image (set .fillAmount).</summary>
        public static Image ProgressBar(string name, Transform parent, Color track, Color fill, out RectTransform holder)
        {
            holder = Rect(name, parent);
            var bg = Image("Track", holder, UITheme.Pill, track, UnityEngine.UI.Image.Type.Sliced);
            Stretch(bg.rectTransform, 0, 0, 0, 0);
            var fillImg = Image("Fill", holder, UITheme.Pill, fill, UnityEngine.UI.Image.Type.Filled);
            Stretch(fillImg.rectTransform, 3, 3, 3, 3);
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;
            return fillImg;
        }

        // ------------------------------------------------------------- anchoring
        public static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        /// <summary>Stretch to fill parent with per-side insets (left,right,top,bottom).</summary>
        public static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Size(RectTransform rt, float w, float h) => rt.sizeDelta = new Vector2(w, h);

        public static void AnchorTopStretch(RectTransform rt, float height, float top = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, -top);
        }

        public static void AnchorBottomStretch(RectTransform rt, float height, float bottom = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, bottom);
        }

        public static void FillParent(RectTransform rt) => Stretch(rt, 0, 0, 0, 0);

        /// <summary>Adds a LayoutElement with a fixed preferred height (for list cards).</summary>
        public static LayoutElement PreferredHeight(GameObject go, float h)
        {
            if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            return le;
        }
    }

    /// <summary>Idle-game number formatting (1.2K, 3.4M, 5.6B ...).</summary>
    public static class Num
    {
        private static readonly string[] Suffix = { "", "K", "M", "B", "T", "aa", "bb", "cc" };

        public static string Short(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "0";
            bool neg = v < 0;
            v = System.Math.Abs(v);
            if (v < 1000) return (neg ? "-" : "") + ((long)v).ToString("N0");
            int tier = 0;
            while (v >= 1000 && tier < Suffix.Length - 1) { v /= 1000.0; tier++; }
            string num = v < 10 ? v.ToString("0.00") : v < 100 ? v.ToString("0.0") : v.ToString("0");
            return (neg ? "-" : "") + num + Suffix[tier];
        }
    }

    /// <summary>
    /// Press feedback for code-built buttons: a quick squash on pointer-down,
    /// pop-back on release, plus a click sound. Pure presentation.
    /// </summary>
    public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private RectTransform _target;
        private Vector3 _baseScale;

        public void Init(RectTransform target)
        {
            _target = target;
            _baseScale = target.localScale;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_target != null) _target.localScale = _baseScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_target != null) UITween.PunchScale(_target, _baseScale, 1.04f, 0.12f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            var btn = GetComponent<Button>();
            if (btn == null || btn.interactable) Audio.Sfx.Tap();
        }
    }
}
