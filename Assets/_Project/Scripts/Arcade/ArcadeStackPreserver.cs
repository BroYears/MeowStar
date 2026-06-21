using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Preserves the player's stacked item list across scene transitions.
    /// This prevents item loss when walking between outdoor forest and indoor restaurant.
    /// </summary>
    public static class ArcadeStackPreserver
    {
        private static readonly List<ArcadeItemType> SavedStack = new();

        /// <summary>
        /// Saves current item types from the player's StackHolder before loading a new scene.
        /// </summary>
        public static void SaveStack(StackHolder holder)
        {
            SavedStack.Clear();
            if (holder == null) return;

            // Extract item types from the stack items
            var items = holder.GetItems();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        SavedStack.Add(item.type);
                    }
                }
            }
            Debug.Log($"[ArcadeStackPreserver] Saved {SavedStack.Count} items.");
        }

        /// <summary>
        /// Restores the saved stack items to the player's StackHolder in the newly loaded scene.
        /// </summary>
        public static void RestoreStack(StackHolder holder, ArcadeStackItem fishPrefab, ArcadeStackItem berryPrefab, ArcadeStackItem mushroomPrefab, ArcadeStackItem grilledFishPrefab, ArcadeStackItem juicePrefab, ArcadeStackItem soupPrefab)
        {
            if (holder == null || SavedStack.Count == 0) return;

            Debug.Log($"[ArcadeStackPreserver] Restoring {SavedStack.Count} items.");
            
            // Clear current stack to avoid duplicates
            holder.ClearStack();

            foreach (var type in SavedStack)
            {
                ArcadeStackItem prefab = null;
                switch (type)
                {
                    case ArcadeItemType.Fish: prefab = fishPrefab; break;
                    case ArcadeItemType.Berry: prefab = berryPrefab; break;
                    case ArcadeItemType.Mushroom: prefab = mushroomPrefab; break;
                    case ArcadeItemType.GrilledFish: prefab = grilledFishPrefab; break;
                    case ArcadeItemType.BerryJuice: prefab = juicePrefab; break;
                    case ArcadeItemType.MushroomSkewer: prefab = soupPrefab; break;
                }

                if (prefab != null)
                {
                    var clone = Object.Instantiate(prefab);
                    clone.gameObject.SetActive(true);
                    holder.Push(clone);
                }
            }
            SavedStack.Clear();
        }

        public static bool HasSavedStack()
        {
            return SavedStack.Count > 0;
        }
    }
}
