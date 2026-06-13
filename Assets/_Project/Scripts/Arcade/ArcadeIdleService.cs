using UnityEngine;
using Nyangsta.Save;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Arcade-side offline income. Hired staff keep the restaurant earning while the
    /// player is away: on launch this computes capped offline gold from the saved hire
    /// count and shows the settlement popup (claim, or 2x via rewarded ad).
    ///
    /// Online play is intentionally NOT accrued here — hired <see cref="StaffAgent"/>s
    /// already drop physical money piles the player collects, so a passive per-second
    /// trickle on top would double-pay. The legacy <see cref="Economy.IdleIncomeManager"/>
    /// stays disabled in arcade mode; this only reuses its <c>ClaimOffline</c> grant path
    /// (invoked from <see cref="OfflinePopup"/>) and <see cref="SaveManager.GetOfflineSeconds"/>.
    /// </summary>
    public class ArcadeIdleService : MonoBehaviour
    {
        // Tuning: gold/sec earned by each hired staff line while the player is away.
        // Deliberately conservative so active play (physical money piles) stays the
        // primary income; balance against unlock costs during playtest.
        [SerializeField] private double goldPerSecondPerStaff = 0.6;
        [SerializeField] private double offlineCapHours = 8;

        private RectTransform _modalLayer;
        private double _pendingGold;
        private bool _presented;

        public void Configure(RectTransform modalLayer)
        {
            _modalLayer = modalLayer;
        }

        private void Start()
        {
            double gps = HiredStaffCount() * goldPerSecondPerStaff;
            if (gps <= 0) return;   // no staff hired yet → no passive income, no popup

            double capSeconds = offlineCapHours * 3600.0;
            double seconds = SaveManager.Instance != null
                ? SaveManager.Instance.GetOfflineSeconds(capSeconds)
                : 0;
            _pendingGold = seconds * gps;
        }

        private void Update()
        {
            // Present once, after the HUD canvas + EventSystem are live so the popup
            // buttons receive input. Cheap one-shot guard; disables itself afterwards.
            if (_presented || _pendingGold <= 0 || _modalLayer == null) return;
            _presented = true;
            OfflinePopup.Present(_modalLayer, _pendingGold);
            enabled = false;
        }

        // Count save-backed hire completions (zone ids are "HireZone_<cooked>"; build
        // zones use other prefixes). Reflects every staff hired in past sessions.
        private static int HiredStaffCount()
        {
            var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            if (data?.arcadeCompletedZones == null) return 0;

            int count = 0;
            foreach (var id in data.arcadeCompletedZones)
                if (!string.IsNullOrEmpty(id) && id.StartsWith("HireZone_")) count++;
            return count;
        }
    }
}
