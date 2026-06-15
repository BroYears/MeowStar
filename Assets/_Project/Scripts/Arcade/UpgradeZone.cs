using System;
using UnityEngine;
using Nyangsta.Economy;
using Nyangsta.Save;

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
        private bool _partialDirty;
        private float _lastPartialSaveTime = -10f;

        private const float PartialSaveInterval = 0.75f;

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
            RestorePartialPayment();
            _applyLevel?.Invoke(_level);
            UpdateLabel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_partialDirty) StorePartialPayment(true);
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
            StorePartialPayment(false);
            Nyangsta.Audio.Sfx.CoinTick();
            if (_paidTowardNext >= NextCost) LevelUp();
            UpdateLabel();
        }

        private void LevelUp()
        {
            _level++;
            _paidTowardNext = 0;
            ClearPartialPayment();
            _applyLevel?.Invoke(_level);
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Medium();

            if (_upgrades != null)
            {
                _upgrades.SetLevel(_zoneId, _level);
                Save.SaveManager.Instance?.Save();
            }
        }

        private void RestorePartialPayment()
        {
            if (string.IsNullOrWhiteSpace(_zoneId) || IsMaxed)
            {
                _paidTowardNext = 0;
                ClearPartialPayment();
                return;
            }

            var progress = FindPartialPayment(_zoneId);
            if (progress == null || progress.level != _level)
            {
                _paidTowardNext = 0;
                if (progress != null) ClearPartialPayment();
                return;
            }

            _paidTowardNext = System.Math.Min(NextCost, System.Math.Max(0, progress.paid));
            if (_paidTowardNext >= NextCost) LevelUpRestoredPayment();
        }

        private void LevelUpRestoredPayment()
        {
            _level++;
            _paidTowardNext = 0;
            ClearPartialPayment();

            if (_upgrades != null)
            {
                _upgrades.SetLevel(_zoneId, _level);
                Save.SaveManager.Instance?.Save();
            }
        }

        private void StorePartialPayment(bool flush)
        {
            if (string.IsNullOrWhiteSpace(_zoneId) || IsMaxed) return;

            var save = Save.SaveManager.Instance;
            var data = save?.Data;
            if (data == null) return;
            if (data.arcadeUpgradePaymentProgress == null) data.arcadeUpgradePaymentProgress = new System.Collections.Generic.List<ArcadeUpgradePaymentProgress>();

            var progress = FindPartialPayment(_zoneId);
            if (progress == null)
            {
                progress = new ArcadeUpgradePaymentProgress { id = _zoneId };
                data.arcadeUpgradePaymentProgress.Add(progress);
            }

            progress.level = _level;
            progress.paid = System.Math.Min(NextCost, System.Math.Max(0, _paidTowardNext));
            _partialDirty = true;

            if (flush || Time.unscaledTime - _lastPartialSaveTime >= PartialSaveInterval)
            {
                save.Save();
                _lastPartialSaveTime = Time.unscaledTime;
                _partialDirty = false;
            }
        }

        private void ClearPartialPayment()
        {
            if (string.IsNullOrWhiteSpace(_zoneId)) return;

            var data = Save.SaveManager.Instance?.Data;
            var list = data?.arcadeUpgradePaymentProgress;
            if (list == null) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null || list[i].id == _zoneId) list.RemoveAt(i);
            }

            _partialDirty = false;
        }

        private ArcadeUpgradePaymentProgress FindPartialPayment(string zoneId)
        {
            var list = Save.SaveManager.Instance?.Data?.arcadeUpgradePaymentProgress;
            if (list == null) return null;
            foreach (var progress in list)
                if (progress != null && progress.id == zoneId) return progress;
            return null;
        }

        private void OnDestroy()
        {
            if (_partialDirty) StorePartialPayment(true);
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
