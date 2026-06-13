using System;
using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Stand here with gold to upgrade a facility. Like <see cref="BuildZone"/> it drains
    /// gold while the player waits, but it is REPEATABLE: each completed payment raises the
    /// level (up to a cap), applies the effect via a callback, persists it, and the next
    /// level costs more. Decoupled from what it upgrades — the bootstrap passes an
    /// <see cref="Action{T}"/> that maps a level onto the target (e.g. CookStation speed).
    /// </summary>
    public class UpgradeZone : InteractionZone
    {
        [SerializeField] private double baseCost = 40;
        [SerializeField] private double costGrowth = 1.6;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private double drainPerTick = 6;
        [SerializeField] private float tickInterval = 0.08f;
        [SerializeField] private WorldBubble costBubble;
        [SerializeField] private string displayName = "업그레이드";

        private int _level;
        private double _paidTowardNext;
        private float _timer;
        private string _zoneId;
        private ArcadeUpgradeService _upgrades;
        private Action<int> _applyLevel;

        public int Level => _level;
        public bool IsMaxed => _level >= maxLevel;

        /// <summary>Gold required to go from the current level to the next.</summary>
        public double NextCost => CostForLevel(_level);

        public void Configure(
            string name,
            double cost,
            double growth,
            int max,
            double tick,
            WorldBubble bubble,
            Action<int> applyLevel)
        {
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            baseCost = System.Math.Max(1, cost);
            costGrowth = System.Math.Max(1.01, growth);
            maxLevel = Mathf.Max(1, max);
            drainPerTick = System.Math.Max(1, tick);
            costBubble = bubble;
            _applyLevel = applyLevel;
            UpdateLabel();
        }

        /// <summary>Hook into the save-backed upgrade tracker.</summary>
        public void BindProgress(ArcadeUpgradeService upgrades, string zoneId)
        {
            _upgrades = upgrades;
            _zoneId = zoneId;
        }

        /// <summary>Re-apply the saved level (no cost), e.g. on load.</summary>
        public void RestoreLevel()
        {
            _level = _upgrades != null ? _upgrades.GetLevel(_zoneId) : 0;
            _applyLevel?.Invoke(_level);
            UpdateLabel();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (IsMaxed || !IsPlayer(agent)) return;

            _timer += dt;
            if (_timer < tickInterval) return;
            _timer = 0f;

            double remaining = NextCost - _paidTowardNext;
            double tick = System.Math.Min(drainPerTick, remaining);
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpendGold(tick)) return;

            _paidTowardNext += tick;
            Nyangsta.Audio.Sfx.CoinTick();
            if (_paidTowardNext >= NextCost) LevelUp();
            UpdateLabel();
        }

        private void LevelUp()
        {
            _level++;
            _paidTowardNext = 0;
            _applyLevel?.Invoke(_level);
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Medium();

            if (_upgrades != null)
            {
                _upgrades.SetLevel(_zoneId, _level);
                Save.SaveManager.Instance?.Save();
            }
        }

        private double CostForLevel(int level) =>
            System.Math.Round(baseCost * System.Math.Pow(costGrowth, level));

        private void UpdateLabel()
        {
            if (costBubble == null) return;
            if (IsMaxed)
            {
                costBubble.SetTitle($"{displayName} MAX", 26, new Vector2(0f, 0.24f), Nyangsta.UI.UITheme.LeafDark);
                costBubble.SetIconVisible(false);
                costBubble.SetValue("");
                return;
            }
            costBubble.SetTitle($"{displayName} Lv.{_level}", 26, new Vector2(0f, 0.24f));
            costBubble.SetIconVisible(true);
            costBubble.SetValue($"{NextCost - _paidTowardNext:N0}");
        }

        private static bool IsPlayer(StackHolder agent)
        {
            return agent != null && agent.GetComponentInParent<ArcadePlayerController>() != null;
        }
    }
}
