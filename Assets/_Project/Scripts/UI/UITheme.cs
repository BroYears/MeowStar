using UnityEngine;

namespace Nyangsta.UI
{
    /// <summary>
    /// Central look-and-feel for the whole game UI. Provides a warm storybook
    /// palette, a Korean-capable dynamic font, and procedurally generated sprites
    /// (rounded panels, circles, rings, soft glows) so the entire interface can be
    /// built from code with zero imported UI assets. Everything is cached.
    /// </summary>
    public static class UITheme
    {
        // ---------------------------------------------------------------- palette
        public static readonly Color Cream      = new Color(0.99f, 0.96f, 0.89f);
        public static readonly Color Parchment  = new Color(0.96f, 0.90f, 0.78f);
        public static readonly Color Wood       = new Color(0.42f, 0.27f, 0.16f);
        public static readonly Color WoodDark   = new Color(0.30f, 0.19f, 0.11f);
        public static readonly Color Ink        = new Color(0.24f, 0.16f, 0.10f);
        public static readonly Color InkSoft    = new Color(0.40f, 0.31f, 0.23f);
        public static readonly Color Leaf       = new Color(0.40f, 0.66f, 0.36f);
        public static readonly Color LeafDark   = new Color(0.26f, 0.49f, 0.27f);
        public static readonly Color Gold       = new Color(1.00f, 0.80f, 0.27f);
        public static readonly Color GoldDeep   = new Color(0.93f, 0.62f, 0.16f);
        public static readonly Color Gem        = new Color(0.39f, 0.74f, 0.96f);
        public static readonly Color Essence    = new Color(0.62f, 0.85f, 0.48f);
        public static readonly Color Danger     = new Color(0.90f, 0.40f, 0.36f);
        public static readonly Color Panel      = new Color(1.00f, 0.985f, 0.94f);
        public static readonly Color PanelAlt   = new Color(0.98f, 0.94f, 0.85f);
        public static readonly Color PanelEdge  = new Color(0.80f, 0.66f, 0.47f);
        public static readonly Color Shade      = new Color(0.09f, 0.06f, 0.04f, 0.55f);
        public static readonly Color Muted      = new Color(0.58f, 0.49f, 0.40f);
        public static readonly Color Disabled   = new Color(0.74f, 0.70f, 0.64f);
        public static readonly Color White      = Color.white;

        /// <summary>Accent colour per upgrade category, used for cards and icons.</summary>
        public static Color CategoryColor(Data.UpgradeCategory category) => category switch
        {
            Data.UpgradeCategory.Hunting    => new Color(0.55f, 0.74f, 0.40f),
            Data.UpgradeCategory.Cooking    => new Color(0.95f, 0.62f, 0.35f),
            Data.UpgradeCategory.Restaurant => new Color(0.86f, 0.49f, 0.55f),
            Data.UpgradeCategory.Idle       => new Color(0.45f, 0.66f, 0.90f),
            _ => Muted,
        };

        /// <summary>Soft ambient tint per region, for backgrounds and the hunt stage.</summary>
        public static Color RegionColor(Data.RegionType region) => region switch
        {
            Data.RegionType.River => new Color(0.52f, 0.78f, 0.92f),
            Data.RegionType.Cave  => new Color(0.74f, 0.56f, 0.34f),
            Data.RegionType.Snow  => new Color(0.78f, 0.90f, 0.98f),
            _ => new Color(0.55f, 0.80f, 0.52f),
        };

        // ------------------------------------------------------------------- font
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = BuildFont();
                return _font;
            }
        }

        private static Font BuildFont()
        {
            // Names cover macOS, Windows and Linux Korean fonts, with Latin fallback.
            string[] names =
            {
                "Apple SD Gothic Neo", "AppleGothic", "Noto Sans CJK KR",
                "Noto Sans KR", "Malgun Gothic", "맑은 고딕", "NanumGothic",
                "Arial Unicode MS", "Arial"
            };
            Font f = null;
            try { f = Font.CreateDynamicFontFromOSFont(names, 48); } catch { /* ignore */ }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        // ---------------------------------------------------------------- sprites
        private static Sprite _square, _rounded, _roundedSoft, _circle, _ring, _glow, _bubble;

        /// <summary>Flat 1x1 white sprite (tint with Image.color).</summary>
        public static Sprite Square => _square != null ? _square : (_square = MakeSolid());

        /// <summary>Rounded rectangle with 9-slice border for crisp panels at any size.</summary>
        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = MakeRounded(64, 18));

        /// <summary>Softer rounded rectangle (larger radius) for pills and chips.</summary>
        public static Sprite Pill => _roundedSoft != null ? _roundedSoft : (_roundedSoft = MakeRounded(64, 30));

        public static Sprite Circle => _circle != null ? _circle : (_circle = MakeCircle(128));
        public static Sprite Ring => _ring != null ? _ring : (_ring = MakeRing(128, 0.16f));
        public static Sprite Glow => _glow != null ? _glow : (_glow = MakeGlow(128));
        public static Sprite Bubble => _bubble != null ? _bubble : (_bubble = MakeBubble(96));

        private static Sprite MakeSolid()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color[4];
            for (int i = 0; i < 4; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundedDistance(x, y, size, radius);   // <=0 inside
                float a = Mathf.Clamp01(0.5f - d);               // 1px anti-aliased edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, border);
        }

        // Distance in pixels outside the rounded rect (0 inside, grows outside) — AA helper.
        private static float RoundedDistance(int x, int y, int size, int radius)
        {
            float hx = size * 0.5f, hy = size * 0.5f;
            float dx = Mathf.Abs(x + 0.5f - hx) - (hx - radius);
            float dy = Mathf.Abs(y + 0.5f - hy) - (hy - radius);
            dx = Mathf.Max(dx, 0f);
            dy = Mathf.Max(dy, 0f);
            float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            return dist; // <=0 inside
        }

        private static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = (size - 1) * 0.5f, r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRing(int size, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = (size - 1) * 0.5f, r = size * 0.5f - 1f;
            float inner = r * (1f - thickness * 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float outA = Mathf.Clamp01(r - d);
                float inA = Mathf.Clamp01(d - inner);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Min(outA, inA)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = (size - 1) * 0.5f, r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                       // soft falloff
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Speech bubble: rounded body + small tail at bottom-left.
        private static Sprite MakeBubble(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            int radius = size / 4;
            int tailTop = size / 5;             // body sits above this
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = 0f;
                // Body
                float bodyD = RoundedDistanceRect(x, y, 2, tailTop, size - 2, size - 2, radius);
                a = Mathf.Max(a, Mathf.Clamp01(-bodyD));
                // Tail triangle pointing down-left
                if (y < tailTop)
                {
                    float tx = x - size * 0.30f;
                    float prog = (float)y / tailTop;     // 1 at top, 0 at bottom tip
                    float halfWidth = Mathf.Lerp(1.5f, size * 0.12f, prog);
                    if (Mathf.Abs(tx) < halfWidth) a = 1f;
                }
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Distance to a rounded rect defined by [x0,y0]-[x1,y1] (negative inside).
        private static float RoundedDistanceRect(int px, int py, int x0, int y0, int x1, int y1, int radius)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            float dx = Mathf.Abs(px + 0.5f - cx) - (hx - radius);
            float dy = Mathf.Abs(py + 0.5f - cy) - (hy - radius);
            dx = Mathf.Max(dx, 0f); dy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }
    }
}
