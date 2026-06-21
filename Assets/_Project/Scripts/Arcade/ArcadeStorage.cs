using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Global static storage to share raw resources (Fish, Berry, Mushroom)
    /// between the Outdoor Forest scene and the Indoor Restaurant scene.
    /// </summary>
    public static class ArcadeStorage
    {
        private static readonly Dictionary<ArcadeItemType, int> Inventory = new()
        {
            { ArcadeItemType.Fish, 0 },
            { ArcadeItemType.Berry, 0 },
            { ArcadeItemType.Mushroom, 0 }
        };

        public static void AddResource(ArcadeItemType type, int count = 1)
        {
            if (Inventory.ContainsKey(type))
            {
                Inventory[type] += count;
                Debug.Log($"[ArcadeStorage] Added {count} of {type}. Total: {Inventory[type]}");
            }
        }

        public static bool RemoveResource(ArcadeItemType type, int count = 1)
        {
            if (Inventory.ContainsKey(type) && Inventory[type] >= count)
            {
                Inventory[type] -= count;
                Debug.Log($"[ArcadeStorage] Removed {count} of {type}. Remaining: {Inventory[type]}");
                return true;
            }
            return false;
        }

        public static int GetCount(ArcadeItemType type)
        {
            return Inventory.TryGetValue(type, out int count) ? count : 0;
        }

        public static void Clear()
        {
            Inventory[ArcadeItemType.Fish] = 0;
            Inventory[ArcadeItemType.Berry] = 0;
            Inventory[ArcadeItemType.Mushroom] = 0;
        }
    }
}
