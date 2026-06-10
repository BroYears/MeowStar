using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Progression;

namespace Nyangsta.Hunting
{
    /// <summary>
    /// Drives the short tap mini-game: spawns ingredient targets for a region,
    /// tracks combo, and grants drops to the inventory on finish.
    /// View/visuals are handled by a separate UI/scene layer; this is logic only.
    /// </summary>
    public class HuntingManager : Singleton<HuntingManager>
    {
        [Header("Tuning (placeholder; tune per GDD §4.1)")]
        [SerializeField] private float huntDuration = 15f;
        [SerializeField] private float comboWindow = 1.2f;     // seconds to keep combo alive
        [SerializeField] private float comboBonusPerStep = 0.1f;
        [SerializeField] private float staminaSecondsPerLevel = 1.5f;
        [SerializeField] private int baseEssencePerHunt = 2;
        [SerializeField] private float essencePerIngredient = 0.25f;

        [Header("Content")]
        [SerializeField] private GameDatabase database;

        public bool IsHunting { get; private set; }
        public float TimeRemaining { get; private set; }
        public int Combo { get; private set; }

        /// <summary>Region of the active/most-recent hunt (read by the view layer).</summary>
        public RegionType ActiveRegion => _region;

        /// <summary>Ingredients tallied in the current/most-recent session (id -> count).</summary>
        public IReadOnlyDictionary<string, int> SessionDrops => _sessionDrops;

        private RegionType _region;
        private float _lastTapTime;
        private readonly Dictionary<string, int> _sessionDrops = new();

        private void Update()
        {
            if (!IsHunting) return;
            TimeRemaining -= Time.deltaTime;

            if (Time.time - _lastTapTime > comboWindow) Combo = 0;
            if (TimeRemaining <= 0f) FinishHunt();
        }

        public void StartHunt(RegionType region)
        {
            if (IsHunting) return;
            var progression = ProgressionManager.Instance;
            if (progression != null && !progression.IsRegionUnlocked(region))
                region = progression.CurrentRegion;

            _region = region;
            IsHunting = true;
            TimeRemaining = huntDuration +
                            UpgradeEffect("UPG_STAMINA") * staminaSecondsPerLevel +
                            StaffEffect(StaffEffectType.HuntDuration);
            Combo = 0;
            _sessionDrops.Clear();
            GameEvents.RaiseHuntStarted();
        }

        public void StartCurrentRegionHunt()
        {
            var progression = ProgressionManager.Instance;
            StartHunt(progression != null ? progression.CurrentRegion : RegionType.Forest);
        }

        /// <summary>
        /// Call when the player successfully taps a spawned target.
        /// Rolls a region-appropriate ingredient weighted by spawnRate, scaled by combo.
        /// </summary>
        public void RegisterHit()
        {
            if (!IsHunting) return;

            if (Time.time - _lastTapTime <= comboWindow) Combo++;
            else Combo = 1;
            _lastTapTime = Time.time;

            var ingredient = RollIngredient(_region);
            if (ingredient == null) return;

            int baseQty = 1 + Mathf.FloorToInt(Combo * comboBonusPerStep);
            int qty = Mathf.Max(1,
                baseQty +
                Mathf.FloorToInt(UpgradeEffect("UPG_HUNT_TOOL")) +
                Mathf.FloorToInt(StaffEffect(StaffEffectType.IngredientBonus)));
            _sessionDrops.TryGetValue(ingredient.id, out int cur);
            _sessionDrops[ingredient.id] = cur + qty;
        }

        public void FinishHunt()
        {
            if (!IsHunting) return;
            IsHunting = false;

            int totalGained = 0;
            foreach (var kv in _sessionDrops)
            {
                EconomyManager.Instance.AddIngredient(kv.Key, kv.Value);
                totalGained += kv.Value;
            }
            GrantWorldTreeEssence(totalGained);
            GameEvents.RaiseHuntFinished(totalGained);
        }

        private IngredientData RollIngredient(RegionType region)
        {
            if (database == null) return null;

            float totalWeight = 0f;
            foreach (var ing in database.ingredients)
                if (ing != null && ing.region == region) totalWeight += ing.spawnRate;
            if (totalWeight <= 0f) return null;

            float roll = Random.value * totalWeight;
            foreach (var ing in database.ingredients)
            {
                if (ing == null || ing.region != region) continue;
                roll -= ing.spawnRate;
                if (roll <= 0f) return ing;
            }
            return null;
        }

        private float UpgradeEffect(string id)
        {
            var economy = EconomyManager.Instance;
            return economy != null ? economy.GetUpgradeEffect(id) : 0f;
        }

        private float StaffEffect(StaffEffectType effectType)
        {
            var progression = ProgressionManager.Instance;
            return progression != null ? progression.GetStaffEffect(effectType) : 0f;
        }

        private void GrantWorldTreeEssence(int totalGained)
        {
            var progression = ProgressionManager.Instance;
            if (progression == null || totalGained <= 0) return;

            var rule = progression.GetRegionRule(_region);
            int regionBonus = rule != null ? rule.essencePerHuntBonus : 0;
            int essence = baseEssencePerHunt + regionBonus + Mathf.FloorToInt(totalGained * essencePerIngredient);
            progression.AddWorldTreeEssence(Mathf.Max(1, essence));
        }
    }
}
