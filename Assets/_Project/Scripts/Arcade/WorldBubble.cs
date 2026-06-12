using UnityEngine;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// World-space speech bubble that floats above zones/actors and faces the camera,
    /// replacing the graybox TextMesh labels. Holds an optional icon, title text,
    /// value text and a small patience ring. The root counters the parent's (possibly
    /// squashed) scale so the bubble never shears, and a "Body" child billboards like
    /// SpriteBillboard (camera pitch + depth-based sortingOrder, kept in sync here so
    /// the icon/text always draw on top of the bubble background).
    /// </summary>
    public class WorldBubble : MonoBehaviour
    {
        private const int ORDER_BG = 0;
        private const int ORDER_RING = 1;
        private const int ORDER_ICON = 2;
        private const int ORDER_TEXT = 3;
        private const float GAUGE_SHOW_RATIO = 0.7f;
        private const float GAUGE_WORLD_SIZE = 0.3f;

        private static readonly Color GAUGE_GREEN = new(0.35f, 0.80f, 0.30f);
        private static readonly Color GAUGE_YELLOW = new(0.98f, 0.78f, 0.20f);
        private static readonly Color GAUGE_RED = new(0.90f, 0.30f, 0.24f);

        // Serialized so runtime "prefab" clones (Instantiate of an inactive template)
        // keep their internal references, same trick as ArcadeCustomer/MoneyPile.
        [SerializeField] private Transform body;
        [SerializeField] private SpriteRenderer bg;
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh value;
        [SerializeField] private int orderBias = 80;

        private Transform _cam;
        private Renderer _titleRenderer;
        private Renderer _valueRenderer;

        /// <summary>
        /// Builds a bubble under <paramref name="parent"/>. <paramref name="worldOffset"/>
        /// is expressed in world units; it is converted into the parent's local space so
        /// squashed zone cylinders can still host bubbles at sane positions.
        /// </summary>
        public static WorldBubble Create(Transform parent, Vector3 worldOffset, float width = 1f, float height = 0.85f)
        {
            var root = new GameObject("Bubble");
            root.transform.SetParent(parent, false);

            Vector3 ls = parent != null ? parent.lossyScale : Vector3.one;
            float ix = 1f / Mathf.Max(0.0001f, ls.x);
            float iy = 1f / Mathf.Max(0.0001f, ls.y);
            float iz = 1f / Mathf.Max(0.0001f, ls.z);
            root.transform.localScale = new Vector3(ix, iy, iz);
            root.transform.localPosition = new Vector3(worldOffset.x * ix, worldOffset.y * iy, worldOffset.z * iz);

            var bubble = root.AddComponent<WorldBubble>();
            bubble.BuildBody(width, height);
            return bubble;
        }

        private void Awake()
        {
            // Re-cache renderers on Instantiate'd clones (private caches don't serialize).
            if (title != null) _titleRenderer = title.GetComponent<MeshRenderer>();
            if (value != null) _valueRenderer = value.GetComponent<MeshRenderer>();
        }

        private void BuildBody(float width, float height)
        {
            var bodyGo = new GameObject("Body");
            body = bodyGo.transform;
            body.SetParent(transform, false);

            var bgGo = new GameObject("Img_Bg");
            bgGo.transform.SetParent(body, false);
            bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = UITheme.Bubble;
            bg.color = Color.white;
            Vector2 s = bg.sprite.bounds.size;
            bgGo.transform.localScale = new Vector3(
                width / Mathf.Max(0.0001f, s.x),
                height / Mathf.Max(0.0001f, s.y), 1f);
        }

        /// <summary>Center icon (item/coin). Pass null to hide.</summary>
        public SpriteRenderer SetIcon(Sprite sprite, float worldHeight, Vector2 offset)
        {
            if (icon == null)
            {
                var go = new GameObject("Img_Icon");
                go.transform.SetParent(body, false);
                icon = go.AddComponent<SpriteRenderer>();
            }
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            if (sprite != null)
            {
                float h = sprite.bounds.size.y;
                float sc = h > 0f ? worldHeight / h : 1f;
                icon.transform.localScale = new Vector3(sc, sc, 1f);
            }
            icon.transform.localPosition = new Vector3(offset.x, offset.y, -0.02f);
            return icon;
        }

        public void SetIconVisible(bool visible)
        {
            if (icon != null) icon.gameObject.SetActive(visible && icon.sprite != null);
        }

        /// <summary>Main (top/center) text line. Empty text hides it.</summary>
        public TextMesh SetTitle(string text, int fontSize, Vector2 offset, Color? color = null)
        {
            if (title == null)
            {
                title = MakeText("Txt_Title");
                _titleRenderer = title.GetComponent<MeshRenderer>();
            }
            Apply(title, text, fontSize, offset, color ?? UITheme.Ink);
            return title;
        }

        /// <summary>Secondary text line (count/cost). Empty text hides it.</summary>
        public TextMesh SetValue(string text, int fontSize, Vector2 offset, Color? color = null)
        {
            if (value == null)
            {
                value = MakeText("Txt_Value");
                _valueRenderer = value.GetComponent<MeshRenderer>();
            }
            Apply(value, text, fontSize, offset, color ?? UITheme.Ink);
            return value;
        }

        /// <summary>Update only the value string, keeping the existing layout.</summary>
        public void SetValue(string text)
        {
            if (value == null)
            {
                SetValue(text, 32, new Vector2(0f, -0.1f));
                return;
            }
            value.text = text;
            value.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Patience gauge: hidden while remainRatio is above 70%; below that a small
        /// ring at the bubble's top-right shifts green→yellow→red and shrinks.
        /// </summary>
        public void SetGauge(float remainRatio)
        {
            bool show = remainRatio > 0f && remainRatio < GAUGE_SHOW_RATIO;
            if (ring == null)
            {
                if (!show) return;
                var go = new GameObject("Img_Ring");
                go.transform.SetParent(body, false);
                go.transform.localPosition = new Vector3(0.36f, 0.34f, -0.01f);
                ring = go.AddComponent<SpriteRenderer>();
                ring.sprite = UITheme.Ring;
            }
            ring.gameObject.SetActive(show);
            if (!show) return;

            float t = Mathf.Clamp01(remainRatio / GAUGE_SHOW_RATIO); // 1 = just shown, 0 = out of patience
            ring.color = t > 0.5f
                ? Color.Lerp(GAUGE_YELLOW, GAUGE_GREEN, (t - 0.5f) * 2f)
                : Color.Lerp(GAUGE_RED, GAUGE_YELLOW, t * 2f);
            float sc = GAUGE_WORLD_SIZE / Mathf.Max(0.0001f, ring.sprite.bounds.size.x);
            sc *= Mathf.Lerp(0.6f, 1f, t);
            ring.transform.localScale = new Vector3(sc, sc, 1f);
        }

        private TextMesh MakeText(string goName)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(body, false);
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.045f;
            // Korean-capable dynamic font (TextMesh default font lacks hangul glyphs).
            var font = UITheme.Font;
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().material = font.material;
            }
            return tm;
        }

        private static void Apply(TextMesh tm, string text, int fontSize, Vector2 offset, Color color)
        {
            tm.text = text;
            tm.fontSize = fontSize;
            tm.color = color;
            tm.transform.localPosition = new Vector3(offset.x, offset.y, -0.03f);
            tm.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null && body != null)
                // Match the camera pitch so the bubble reads as standing upright,
                // same convention as SpriteBillboard.
                body.rotation = Quaternion.Euler(_cam.eulerAngles.x, 0f, 0f);

            if (bg == null) return;
            // Depth-based sorting identical to SpriteBillboard, with fixed offsets so
            // contents always draw over the bubble background.
            float depth = -transform.position.z * 100f - transform.position.y * 10f;
            int baseOrder = orderBias + Mathf.RoundToInt(depth);
            bg.sortingOrder = baseOrder + ORDER_BG;
            if (ring != null) ring.sortingOrder = baseOrder + ORDER_RING;
            if (icon != null) icon.sortingOrder = baseOrder + ORDER_ICON;
            if (_titleRenderer != null) _titleRenderer.sortingOrder = baseOrder + ORDER_TEXT;
            if (_valueRenderer != null) _valueRenderer.sortingOrder = baseOrder + ORDER_TEXT;
        }
    }
}
