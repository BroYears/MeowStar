using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Progression;
using Nyangsta.Save;

namespace Nyangsta.Economy
{
    /// <summary>
    /// Owns currency balances, ingredient inventory, upgrade levels, and the
    /// exponential cost curve. Reads/writes through SaveManager.Data.
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        [SerializeField] private GameDatabase database;

        // Runtime fast-lookup maps rebuilt from SaveData lists.
        private readonly Dictionary<string, int> _ingredients = new();
        private readonly Dictionary<string, int> _upgradeLevels = new();

        public double Gold => Save.SaveManager.Instance.Data.gold;
        public int Gems => Save.SaveManager.Instance.Data.gems;
        public IReadOnlyList<UpgradeData> Upgrades => database != null ? database.upgrades : null;

        [Header("First-run grant (so the loop is visible immediately)")]
        [SerializeField] private int starterIngredientEach = 10;

        protected override void OnAwake()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[EconomyManager] SaveManager must initialise first. " +
                               "Re-run Nyangsta > Setup, or set SaveManager's Script Execution Order before EconomyManager.");
                return;
            }
            if (database != null) database.BuildLookup();
            RebuildRuntimeMaps();
            GrantStarterIfFresh();
            RefreshRestaurantProgression();
        }

        /// <summary>On a brand-new save, seed a few ingredients so customers can be served.</summary>
        private void GrantStarterIfFresh()
        {
            var data = SaveManager.Instance.Data;
            if (data.ingredients.Count > 0 || database == null) return;
            foreach (var ing in database.ingredients)
                if (ing != null) AddIngredient(ing.id, starterIngredientEach);
        }

        private void RebuildRuntimeMaps()
        {
            var data = SaveManager.Instance.Data;
            _ingredients.Clear();
            foreach (var s in data.ingredients) _ingredients[s.id] = s.amount;
            _upgradeLevels.Clear();
            foreach (var u in data.upgradeLevels) _upgradeLevels[u.id] = u.level;
        }

        // ---- Gold ----
        public void AddGold(double amount)
        {
            if (amount == 0) return;
            var data = SaveManager.Instance.Data;
            data.gold = System.Math.Max(0, data.gold + amount);
            GameEvents.RaiseGoldChanged(data.gold);
        }

        public bool TrySpendGold(double amount)
        {
            var data = SaveManager.Instance.Data;
            if (data.gold < amount) return false;
            data.gold -= amount;
            GameEvents.RaiseGoldChanged(data.gold);
            return true;
        }

        // ---- Gems ----
        public void AddGems(int amount)
        {
            var data = SaveManager.Instance.Data;
            data.gems = Mathf.Max(0, data.gems + amount);
            GameEvents.RaiseGemsChanged(data.gems);
        }

        public bool TrySpendGems(int amount)
        {
            var data = SaveManager.Instance.Data;
            if (data.gems < amount) return false;
            data.gems -= amount;
            GameEvents.RaiseGemsChanged(data.gems);
            return true;
        }

        // ---- Ingredients ----
        public int GetIngredient(string id) => _ingredients.TryGetValue(id, out var v) ? v : 0;

        public void AddIngredient(string id, int amount)
        {
            var v = GetIngredient(id) + amount;
            if (v < 0) v = 0;
            _ingredients[id] = v;
            SyncIngredient(id, v);
            GameEvents.RaiseIngredientChanged(id, v);
        }

        public bool TryConsumeIngredient(string id, int amount)
        {
            if (GetIngredient(id) < amount) return false;
            AddIngredient(id, -amount);
            return true;
        }

        /// <summary>Returns true if all menu requirements are in inventory.</summary>
        public bool CanCook(MenuData menu)
        {
            if (menu == null || !IsMenuUnlocked(menu)) return false;
            foreach (var req in menu.requirements)
                if (req.ingredient == null || GetIngredient(req.ingredient.id) < req.amount)
                    return false;
            return true;
        }

        public bool IsMenuUnlocked(MenuData menu)
        {
            if (menu == null || SaveManager.Instance == null) return false;

            var unlock = menu.unlock;
            var save = SaveManager.Instance.Data;
            if (unlock.restaurantLevel > 0 && save.restaurantLevel < unlock.restaurantLevel)
                return false;

            if (unlock.requiresRegion)
            {
                var progression = ProgressionManager.Instance;
                if (progression != null) return progression.IsRegionUnlocked(unlock.requiredRegion);
                return save.unlockedRegions.Contains(unlock.requiredRegion.ToString());
            }

            return true;
        }

        /// <summary>Consumes the menu's ingredients if available; returns success.</summary>
        public bool TryCook(MenuData menu)
        {
            if (!CanCook(menu)) return false;
            foreach (var req in menu.requirements)
                TryConsumeIngredient(req.ingredient.id, req.amount);
            return true;
        }

        // ---- Upgrades ----
        public int GetUpgradeLevel(string id) => _upgradeLevels.TryGetValue(id, out var v) ? v : 0;

        public float GetUpgradeEffect(string id)
        {
            int level = GetUpgradeLevel(id);
            if (level <= 0) return 0f;

            var upgrade = database != null ? database.GetUpgrade(id) : null;
            return upgrade != null ? upgrade.TotalEffect(level) : level;
        }

        public double GetUpgradeCost(UpgradeData upgrade)
            => upgrade.CostForLevel(GetUpgradeLevel(upgrade.id));

        public bool TryPurchaseUpgrade(string id)
        {
            var upgrade = database != null ? database.GetUpgrade(id) : null;
            return upgrade != null && TryPurchaseUpgrade(upgrade);
        }

        public bool TryPurchaseUpgrade(UpgradeData upgrade)
        {
            int level = GetUpgradeLevel(upgrade.id);
            if (upgrade.maxLevel > 0 && level >= upgrade.maxLevel) return false;

            double cost = upgrade.CostForLevel(level);
            if (!TrySpendGold(cost)) return false;

            int newLevel = level + 1;
            _upgradeLevels[upgrade.id] = newLevel;
            SyncUpgrade(upgrade.id, newLevel);
            RefreshRestaurantProgression();
            GameEvents.RaiseUpgradePurchased(upgrade.id, newLevel);
            return true;
        }

        private void RefreshRestaurantProgression()
        {
            var game = GameManager.Instance;
            if (game == null) return;

            int investment =
                GetUpgradeLevel("UPG_SEATS") +
                GetUpgradeLevel("UPG_REPUTATION") +
                GetUpgradeLevel("UPG_COOK_STATION") +
                GetUpgradeLevel("UPG_STAFF");

            game.SetRestaurantLevel(1 + Mathf.Clamp(investment / 3, 0, 3));
            ProgressionManager.Instance?.RefreshUnlockedRegions();
        }

        // ---- SaveData sync helpers (keep lists in step with runtime maps) ----
        private void SyncIngredient(string id, int amount)
        {
            var list = SaveManager.Instance.Data.ingredients;
            var entry = list.Find(s => s.id == id);
            if (entry == null) list.Add(new IngredientStack { id = id, amount = amount });
            else entry.amount = amount;
        }

        private void SyncUpgrade(string id, int level)
        {
            var list = SaveManager.Instance.Data.upgradeLevels;
            var entry = list.Find(u => u.id == id);
            if (entry == null) list.Add(new UpgradeLevel { id = id, level = level });
            else entry.level = level;
        }
    }
}
