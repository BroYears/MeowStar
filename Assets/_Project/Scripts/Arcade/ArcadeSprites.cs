using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Runtime sprite loader for the 2.5D arcade. PNGs live in Resources/Arcade and
    /// import as plain textures (the sprite postprocessor only covers Art/Sprites), so
    /// we build Sprites from Texture2D and cache them — same approach as HuntMiniGame.
    ///
    /// Two pivots: actors/props use a BOTTOM-center pivot so they "stand" on the ground
    /// and sort cleanly by Y; tiles and items use a CENTER pivot.
    /// </summary>
    public static class ArcadeSprites
    {
        // 256 matches the project's spritePixelsPerUnit (item/character art ~600-900px).
        private const float PPU = 256f;
        // Floor tiles use a lower PPU so each 128px tile repeats every ~1.7 world units
        // instead of every 0.5 — a calmer, less busy ground.
        private const float TILE_PPU = 75f;

        private static readonly Dictionary<string, Sprite> _center = new();
        private static readonly Dictionary<string, Sprite> _bottom = new();
        private static readonly Dictionary<string, Sprite> _tile = new();

        /// <summary>Center-pivoted sprite (items, money, pads).</summary>
        public static Sprite Get(string key) => Load(key, _center, new Vector2(0.5f, 0.5f), PPU);

        /// <summary>Bottom-center-pivoted sprite (characters, facilities, props).</summary>
        public static Sprite GetGrounded(string key) => Load(key, _bottom, new Vector2(0.5f, 0.04f), PPU);

        /// <summary>Center-pivoted tile sprite at a coarser PPU, FullRect for Tiled draw mode.</summary>
        public static Sprite GetTile(string key) => Load(key, _tile, new Vector2(0.5f, 0.5f), TILE_PPU);

        public static bool Has(string key) => Resources.Load<Texture2D>($"Arcade/{key}") != null;

        private static Sprite Load(string key, Dictionary<string, Sprite> cache, Vector2 pivot, float ppu)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;

            var tex = Resources.Load<Texture2D>($"Arcade/{key}");
            if (tex == null)
                tex = CreateProceduralTexture(key);

            Sprite sp = null;
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                // FullRect (not the default Tight mesh) so SpriteDrawMode.Tiled works for
                // the floor tiles; harmless for non-tiled sprites.
                sp = Sprite.Create(
                    tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu,
                    0, SpriteMeshType.FullRect);
                sp.name = $"Arcade_{key}";
            }
            cache[key] = sp;
            return sp;
        }

        private static Texture2D CreateProceduralTexture(string key)
        {
            return key switch
            {
                "item_wood" => MakeWoodTexture(),
                "item_mushroom" => MakeMushroomTexture(),
                "item_mushroomskewer" => MakeMushroomSkewerTexture(),
                "mushroom_pot" => MakeMushroomPotTexture(),
                _ => null,
            };
        }

        private static Texture2D NewTexture()
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static Texture2D MakeWoodTexture()
        {
            var tex = NewTexture();
            DrawEllipse(tex, 64, 66, 42, 22, new Color(0.45f, 0.28f, 0.14f, 1f));
            DrawRect(tex, 28, 48, 100, 83, new Color(0.56f, 0.34f, 0.17f, 1f));
            DrawEllipse(tex, 30, 66, 13, 18, new Color(0.35f, 0.22f, 0.12f, 1f));
            DrawEllipse(tex, 98, 66, 13, 18, new Color(0.72f, 0.50f, 0.26f, 1f));
            DrawEllipse(tex, 98, 66, 7, 10, new Color(0.45f, 0.28f, 0.14f, 1f), outlineOnly: true);
            DrawRect(tex, 42, 56, 86, 59, new Color(0.72f, 0.48f, 0.24f, 0.75f));
            DrawRect(tex, 38, 72, 90, 75, new Color(0.34f, 0.20f, 0.10f, 0.55f));
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMushroomTexture()
        {
            var tex = NewTexture();
            DrawEllipse(tex, 64, 56, 38, 26, new Color(0.82f, 0.28f, 0.38f, 1f));
            DrawEllipse(tex, 64, 72, 18, 34, new Color(0.95f, 0.84f, 0.62f, 1f));
            DrawEllipse(tex, 49, 49, 7, 5, new Color(1f, 0.92f, 0.82f, 1f));
            DrawEllipse(tex, 66, 42, 8, 6, new Color(1f, 0.92f, 0.82f, 1f));
            DrawEllipse(tex, 82, 54, 6, 5, new Color(1f, 0.92f, 0.82f, 1f));
            DrawEllipse(tex, 64, 55, 40, 28, new Color(0.36f, 0.12f, 0.18f, 0.22f), outlineOnly: true);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMushroomSkewerTexture()
        {
            var tex = NewTexture();
            DrawRect(tex, 60, 20, 68, 108, new Color(0.55f, 0.34f, 0.18f, 1f));
            DrawEllipse(tex, 54, 44, 20, 17, new Color(0.86f, 0.36f, 0.40f, 1f));
            DrawEllipse(tex, 72, 62, 20, 17, new Color(0.95f, 0.76f, 0.42f, 1f));
            DrawEllipse(tex, 54, 82, 20, 17, new Color(0.86f, 0.36f, 0.40f, 1f));
            DrawEllipse(tex, 72, 100, 20, 17, new Color(0.95f, 0.76f, 0.42f, 1f));
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMushroomPotTexture()
        {
            var tex = NewTexture();
            DrawEllipse(tex, 64, 77, 42, 18, new Color(0.22f, 0.24f, 0.27f, 1f));
            DrawRect(tex, 30, 52, 98, 82, new Color(0.25f, 0.27f, 0.31f, 1f));
            DrawEllipse(tex, 64, 52, 34, 13, new Color(0.95f, 0.78f, 0.48f, 1f));
            DrawEllipse(tex, 52, 43, 7, 11, new Color(0.9f, 0.9f, 0.86f, 0.55f));
            DrawEllipse(tex, 70, 36, 8, 13, new Color(0.9f, 0.9f, 0.86f, 0.45f));
            DrawEllipse(tex, 86, 45, 6, 10, new Color(0.9f, 0.9f, 0.86f, 0.4f));
            tex.Apply();
            return tex;
        }

        private static void DrawRect(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(tex.height - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(tex.width - 1, x1); x++)
                tex.SetPixel(x, y, color);
        }

        private static void DrawEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color color, bool outlineOnly = false)
        {
            float inner = outlineOnly ? 0.82f : 0f;
            for (int y = Mathf.Max(0, cy - ry); y <= Mathf.Min(tex.height - 1, cy + ry); y++)
            for (int x = Mathf.Max(0, cx - rx); x <= Mathf.Min(tex.width - 1, cx + rx); x++)
            {
                float nx = (x - cx) / Mathf.Max(1f, (float)rx);
                float ny = (y - cy) / Mathf.Max(1f, (float)ry);
                float d = nx * nx + ny * ny;
                if (d <= 1f && d >= inner)
                    tex.SetPixel(x, y, color);
            }
        }
    }
}
