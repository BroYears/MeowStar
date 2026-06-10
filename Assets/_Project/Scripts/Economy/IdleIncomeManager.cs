using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Progression;
using Nyangsta.Save;

namespace Nyangsta.Economy
{
    /// <summary>
    /// Accrues passive Gold Per Second while playing and computes offline income
    /// on launch (capped). Staff, reputation, seats, and offline-cap upgrades
    /// feed the values so the tycoon loop has visible compounding.
    /// </summary>
    public class IdleIncomeManager : Singleton<IdleIncomeManager>
    {
        [SerializeField] private double goldPerSecond = 0;     // manual baseline for tuning/events
        [SerializeField] private double staffGoldPerSecond = 0.8;
        [SerializeField] private double reputationGoldPerSecondBonus = 0.12;
        [SerializeField] private double seatGoldPerSecondBonus = 0.04;
        [SerializeField] private double offlineCapHours = 8;
        [SerializeField] private double offlineCapHoursPerLevel = 1;

        private double _accumulator;

        public double GoldPerSecond
        {
            get => CalculateGoldPerSecond();
            set => goldPerSecond = Mathf.Max(0f, (float)value);
        }

        public double OfflineCapHours =>
            offlineCapHours + UpgradeEffect("UPG_OFFLINE_CAP") * offlineCapHoursPerLevel;

        private void Start()
        {
            double offlineSeconds = SaveManager.Instance.GetOfflineSeconds(OfflineCapHours * 3600.0);
            double offlineGold = offlineSeconds * GoldPerSecond;
            if (offlineGold > 0)
            {
                // Don't auto-grant — let UI show a settlement popup (optionally 2x via ad).
                GameEvents.RaiseOfflineIncomeReady(offlineGold);
            }
        }

        private void Update()
        {
            double currentGoldPerSecond = GoldPerSecond;
            if (currentGoldPerSecond <= 0) return;

            _accumulator += currentGoldPerSecond * Time.deltaTime;
            if (_accumulator >= 1.0)
            {
                double whole = System.Math.Floor(_accumulator);
                _accumulator -= whole;
                EconomyManager.Instance.AddGold(whole);
            }
        }

        /// <summary>Called by UI after the player accepts offline settlement.</summary>
        public void ClaimOffline(double amount, bool doubled)
        {
            EconomyManager.Instance.AddGold(doubled ? amount * 2 : amount);
        }

        private double CalculateGoldPerSecond()
        {
            double staff = UpgradeEffect("UPG_STAFF");
            double recruitedStaff = StaffEffect(StaffEffectType.IdleGoldPerSecond);
            if (staff <= 0 && recruitedStaff <= 0) return goldPerSecond;

            double reputationMultiplier = 1 + UpgradeEffect("UPG_REPUTATION") * reputationGoldPerSecondBonus;
            double seatMultiplier = 1 + UpgradeEffect("UPG_SEATS") * seatGoldPerSecondBonus;
            return goldPerSecond + (staff * staffGoldPerSecond + recruitedStaff) * reputationMultiplier * seatMultiplier;
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
    }
}
