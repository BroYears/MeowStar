using UnityEngine;

namespace Nyangsta.Data
{
    [CreateAssetMenu(menuName = "Nyangsta/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public UpgradeCategory category;
        public double baseCost = 100;
        public float costGrowth = 1.15f;   // exponential cost curve
        public float effectPerLevel = 1f;
        public int maxLevel = 0;            // 0 = unlimited

        /// <summary>Cost to go from `currentLevel` to `currentLevel + 1`.</summary>
        public double CostForLevel(int currentLevel)
        {
            return baseCost * System.Math.Pow(costGrowth, currentLevel);
        }

        public float TotalEffect(int level) => effectPerLevel * level;
    }
}
