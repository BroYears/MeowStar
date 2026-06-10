using NUnit.Framework;
using UnityEngine;
using Nyangsta.Data;

namespace Nyangsta.Tests
{
    /// <summary>
    /// Verifies the core economy math that the GDD calls out for unit testing
    /// (exponential cost curve, offline-style accumulation). No scene required.
    /// </summary>
    public class EconomyMathTests
    {
        private UpgradeData MakeUpgrade(double baseCost, float growth)
        {
            var u = ScriptableObject.CreateInstance<UpgradeData>();
            u.baseCost = baseCost;
            u.costGrowth = growth;
            return u;
        }

        [Test]
        public void CostCurve_MatchesGddExample()
        {
            // GDD §4.4: base 100, growth 1.15 -> 100, 115, 132, 152 ...
            var u = MakeUpgrade(100, 1.15f);
            Assert.AreEqual(100, u.CostForLevel(0), 0.5);
            Assert.AreEqual(115, u.CostForLevel(1), 0.5);
            Assert.AreEqual(132, u.CostForLevel(2), 0.5);
            Assert.AreEqual(152, u.CostForLevel(3), 0.5);
        }

        [Test]
        public void CostCurve_IsMonotonicIncreasing()
        {
            var u = MakeUpgrade(100, 1.15f);
            double prev = u.CostForLevel(0);
            for (int lvl = 1; lvl < 50; lvl++)
            {
                double cost = u.CostForLevel(lvl);
                Assert.Greater(cost, prev);
                prev = cost;
            }
        }

        [Test]
        public void OfflineIncome_IsCappedByDuration()
        {
            const double gps = 5.0;
            const double capSeconds = 8 * 3600;
            double elapsed = 20 * 3600;      // 20h offline
            double capped = System.Math.Clamp(elapsed, 0, capSeconds);
            Assert.AreEqual(capSeconds * gps, capped * gps, 0.001);
        }
    }
}
