using System;
using UnityEngine;

namespace Nyangsta.Data
{
    [Serializable]
    public struct IngredientRequirement
    {
        public IngredientData ingredient;
        public int amount;
    }

    [Serializable]
    public struct UnlockCondition
    {
        public int restaurantLevel;   // 0 = no requirement
        public RegionType requiredRegion;
        public bool requiresRegion;
    }

    [CreateAssetMenu(menuName = "Nyangsta/Menu")]
    public class MenuData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public IngredientRequirement[] requirements;
        public float cookTime;
        public int sellPrice;
        public UnlockCondition unlock;
    }
}
