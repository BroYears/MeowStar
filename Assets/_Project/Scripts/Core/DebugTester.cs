using UnityEngine;
using Nyangsta.Data;
using Nyangsta.Customer;
using Nyangsta.Economy;
using Nyangsta.Hunting;
using Nyangsta.Progression;

namespace Nyangsta.Core
{
    /// <summary>
    /// Dev-only on-screen controls so the core loop is playable without UI yet.
    /// Add to any GameObject in the Bootstrap scene (or it's added by ProjectSetup).
    /// Remove before release.
    /// </summary>
    public class DebugTester : MonoBehaviour
    {
        [SerializeField] private RegionType huntRegion = RegionType.Forest;
        [SerializeField] private int hitsPerTap = 1;

        private void OnGUI()
        {
            const int w = 300, h = 32;
            int x = 10, y = 10;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, 860), "MeowStar / 냥스타 디버그");

            y += 25;
            GUI.Label(new Rect(x, y, w, 20),
                $"Gold: {EconomyManager.Instance?.Gold:N0}  Gem: {EconomyManager.Instance?.Gems}");

            y += 24;
            DrawProgressionStatus(x, ref y, w, h);

            y += 24;
            var customers = CustomerManager.Instance;
            if (customers != null)
            {
                GUI.Label(new Rect(x, y, w, 20),
                    $"손님 {customers.Active.Count}/{customers.SeatCount}  조리대 {customers.CookStationCount}");
                y += 20;
                GUI.Label(new Rect(x, y, w, 20),
                    $"조리 {customers.CurrentCookSeconds:F1}s  방문 {customers.CurrentSpawnInterval:F1}s  매출 x{customers.RevenueMultiplier:F2}");
            }

            y += 24;
            var idle = IdleIncomeManager.Instance;
            if (idle != null)
                GUI.Label(new Rect(x, y, w, 20),
                    $"GPS {idle.GoldPerSecond:F2}  오프라인 상한 {idle.OfflineCapHours:F0}h");

            y += 24;
            var hunt = HuntingManager.Instance;
            if (hunt != null)
                GUI.Label(new Rect(x, y, w, 20),
                    hunt.IsHunting ? $"HUNT {hunt.TimeRemaining:F1}s  combo {hunt.Combo}" : "대기 중");

            y += 26;
            if (GUI.Button(new Rect(x, y, w, h), "현재 지역 사냥 시작"))
                hunt?.StartCurrentRegionHunt();

            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), $"선택 지역 사냥 ({huntRegion})"))
                hunt?.StartHunt(huntRegion);

            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), $"사냥 탭 (+{hitsPerTap})"))
                for (int i = 0; i < hitsPerTap; i++) hunt?.RegisterHit();

            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), "사냥 종료(정산)"))
                hunt?.FinishHunt();

            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), "골드 +100"))
                EconomyManager.Instance?.AddGold(100);

            y += h + 4;
            if (GUI.Button(new Rect(x, y, w, h), "재료 전부 +5"))
                GrantSampleIngredients();

            y += h + 10;
            GUI.Label(new Rect(x, y, w, 20), "업그레이드 구매");
            y += 22;
            DrawUpgradeButtons(x, ref y, w, h);

            y += h + 8;
            DrawTravelButtons(x, ref y, w, h);

            y += h + 8;
            DrawStaffButtons(x, ref y, w, h);
        }

        private void DrawProgressionStatus(int x, ref int y, int w, int h)
        {
            var progression = ProgressionManager.Instance;
            if (progression == null)
            {
                GUI.Label(new Rect(x, y, w, 20), "진행 매니저 없음");
                return;
            }

            string regionName = progression.GetRegionName(progression.CurrentRegion);
            GUI.Label(new Rect(x, y, w, 20),
                $"세계수 Lv.{progression.WorldTreeLevel}  정수 {progression.WorldTreeEssence}/{progression.NextWorldTreeCost}");
            y += 20;
            GUI.Label(new Rect(x, y, w, 20),
                $"현재 지역: {regionName}  직원 {progression.RecruitedStaffCount}");

            y += 24;
            GUI.enabled = progression.NextWorldTreeCost > 0 &&
                          progression.WorldTreeEssence >= progression.NextWorldTreeCost;
            if (GUI.Button(new Rect(x, y, w, h), "세계수 성장"))
                progression.TryGrowWorldTree();
            GUI.enabled = true;
            y += h + 4;
        }

        private void GrantSampleIngredients()
        {
            string[] ids = { "ING_001", "ING_002", "ING_003", "ING_004", "ING_005", "ING_006" };
            foreach (var id in ids) EconomyManager.Instance?.AddIngredient(id, 5);
        }

        private void DrawUpgradeButtons(int x, ref int y, int w, int h)
        {
            var economy = EconomyManager.Instance;
            var upgrades = economy != null ? economy.Upgrades : null;
            if (upgrades == null) return;

            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                int level = economy.GetUpgradeLevel(upgrade.id);
                double cost = economy.GetUpgradeCost(upgrade);
                string label = $"{upgrade.displayName} Lv.{level}  {cost:N0}G";
                if (GUI.Button(new Rect(x, y, w, h), label))
                    economy.TryPurchaseUpgrade(upgrade);
                y += h + 4;
            }
        }

        private void DrawTravelButtons(int x, ref int y, int w, int h)
        {
            var progression = ProgressionManager.Instance;
            if (progression == null) return;

            GUI.Label(new Rect(x, y, w, 20), "지역 이동");
            y += 22;
            foreach (var region in progression.Regions)
            {
                bool unlocked = progression.IsRegionUnlocked(region.region);
                bool current = progression.CurrentRegion == region.region;
                GUI.enabled = unlocked && !current;
                string label = current
                    ? $"{region.displayName} (현재)"
                    : unlocked
                        ? $"{region.displayName} 이동"
                        : $"{region.displayName} 잠김 - 세계수 Lv.{region.requiredWorldTreeLevel}";
                if (GUI.Button(new Rect(x, y, w, h), label))
                    progression.TryTravelTo(region.region);
                GUI.enabled = true;
                y += h + 4;
            }
        }

        private void DrawStaffButtons(int x, ref int y, int w, int h)
        {
            var progression = ProgressionManager.Instance;
            if (progression == null) return;

            GUI.Label(new Rect(x, y, w, 20), "종업원 영입");
            y += 22;
            foreach (var staff in progression.Staff)
            {
                bool recruited = progression.IsStaffRecruited(staff.id);
                GUI.enabled = !recruited && progression.CanRecruitStaff(staff);
                string label = recruited
                    ? $"{staff.displayName} 영입됨"
                    : $"{staff.displayName} {staff.goldCost:N0}G/{staff.essenceCost}정수";
                if (GUI.Button(new Rect(x, y, w, h), label))
                    progression.TryRecruitStaff(staff.id);
                GUI.enabled = true;
                y += h + 4;
            }
        }
    }
}
