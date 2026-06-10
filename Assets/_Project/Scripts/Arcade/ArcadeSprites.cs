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
    }
}
