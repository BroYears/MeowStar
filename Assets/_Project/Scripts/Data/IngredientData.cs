using UnityEngine;

namespace Nyangsta.Data
{
    [CreateAssetMenu(menuName = "Nyangsta/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public Rarity rarity;
        public RegionType region;
        [Range(0f, 1f)] public float spawnRate;
    }
}
