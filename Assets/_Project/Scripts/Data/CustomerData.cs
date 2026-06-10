using UnityEngine;

namespace Nyangsta.Data
{
    [CreateAssetMenu(menuName = "Nyangsta/Customer")]
    public class CustomerData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public MenuData preferredMenu;
        public RegionType homeRegion = RegionType.Forest;
        public float payMultiplier = 1f;
        public float patienceSeconds = 15f;
        public int unlockStage = 1;
    }
}
