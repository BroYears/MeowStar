using UnityEditor;
using UnityEngine;

namespace Nyangsta.EditorTools
{
    /// <summary>
    /// Auto-configures PNGs under Art/Sprites as 2D sprites (single mode) with
    /// sensible defaults, so dropped-in art is usable without manual inspector work.
    /// </summary>
    public class SpriteImportSettings : AssetPostprocessor
    {
        private const string SpriteFolder = "Assets/_Project/Art/Sprites";
        private const string BgFolder = "Assets/_Project/Art/Backgrounds";
        private const string ConceptFolder = "Assets/_Project/Art/Concepts";
        // Arcade sprites live in Resources and are loaded as Texture2D at runtime
        // (ArcadeSprites builds Sprites with per-use pivots), so they only need clean
        // texture settings — not the Sprite type.
        private const string ArcadeResFolder = "Assets/_Project/Resources/Arcade";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ArcadeResFolder))
            {
                var tex = (TextureImporter)assetImporter;
                tex.textureType = TextureImporterType.Default;
                tex.filterMode = FilterMode.Bilinear;
                tex.alphaIsTransparency = true;
                tex.mipmapEnabled = false;
                tex.wrapMode = TextureWrapMode.Repeat; // tile_* sprites repeat as floors
                return;
            }

            if (!assetPath.StartsWith(SpriteFolder) &&
                !assetPath.StartsWith(BgFolder) &&
                !assetPath.StartsWith(ConceptFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;   // illustrations are ~600-900px tall
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
