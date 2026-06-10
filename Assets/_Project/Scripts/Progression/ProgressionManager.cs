using System;
using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Save;

namespace Nyangsta.Progression
{
    [Serializable]
    public class RegionRule
    {
        public RegionType region;
        public string displayName;
        public int requiredWorldTreeLevel;
        public int essencePerHuntBonus;
    }

    [Serializable]
    public class StaffDefinition
    {
        public string id;
        public string displayName;
        public RegionType homeRegion;
        public int requiredWorldTreeLevel;
        public int goldCost;
        public int essenceCost;
        public StaffEffectType effectType;
        public float effectAmount;
        public string description;
    }

    /// <summary>
    /// MeowStar progression layer: world-tree growth unlocks travel regions,
    /// and region staff recruitment feeds bonuses back into the tycoon loop.
    /// </summary>
    public class ProgressionManager : Singleton<ProgressionManager>
    {
        private static readonly RegionRule[] RegionRules =
        {
            new RegionRule { region = RegionType.Forest, displayName = "요정의 숲", requiredWorldTreeLevel = 1, essencePerHuntBonus = 0 },
            new RegionRule { region = RegionType.River, displayName = "달빛 강가", requiredWorldTreeLevel = 2, essencePerHuntBonus = 1 },
            new RegionRule { region = RegionType.Cave, displayName = "꿀빛 동굴", requiredWorldTreeLevel = 3, essencePerHuntBonus = 2 },
            new RegionRule { region = RegionType.Snow, displayName = "별눈 설산", requiredWorldTreeLevel = 4, essencePerHuntBonus = 3 },
        };

        private static readonly StaffDefinition[] StaffDefinitions =
        {
            new StaffDefinition
            {
                id = "STAF_001", displayName = "다람쥐 주방 보조", homeRegion = RegionType.Forest,
                requiredWorldTreeLevel = 1, goldCost = 250, essenceCost = 8,
                effectType = StaffEffectType.CookSpeed, effectAmount = 0.25f,
                description = "조리 속도 보너스"
            },
            new StaffDefinition
            {
                id = "STAF_002", displayName = "비버 홀 매니저", homeRegion = RegionType.River,
                requiredWorldTreeLevel = 2, goldCost = 750, essenceCost = 16,
                effectType = StaffEffectType.ServiceSlots, effectAmount = 1f,
                description = "동시 서빙 수 증가"
            },
            new StaffDefinition
            {
                id = "STAF_003", displayName = "박쥐 정찰꾼", homeRegion = RegionType.Cave,
                requiredWorldTreeLevel = 3, goldCost = 1400, essenceCost = 28,
                effectType = StaffEffectType.IngredientBonus, effectAmount = 1f,
                description = "사냥 획득량 증가"
            },
            new StaffDefinition
            {
                id = "STAF_004", displayName = "펭귄 회계사", homeRegion = RegionType.Snow,
                requiredWorldTreeLevel = 4, goldCost = 2600, essenceCost = 44,
                effectType = StaffEffectType.IdleGoldPerSecond, effectAmount = 1.2f,
                description = "방치 수익 증가"
            },
        };

        public IReadOnlyList<RegionRule> Regions => RegionRules;
        public IReadOnlyList<StaffDefinition> Staff => StaffDefinitions;

        public int WorldTreeLevel => SaveManager.Instance != null ? SaveManager.Instance.Data.worldTreeLevel : 1;
        public int WorldTreeEssence => SaveManager.Instance != null ? SaveManager.Instance.Data.worldTreeEssence : 0;
        public RegionType CurrentRegion => ParseRegion(SaveManager.Instance != null ? SaveManager.Instance.Data.currentRegion : null);
        public int RecruitedStaffCount => SaveManager.Instance != null ? SaveManager.Instance.Data.recruitedStaff.Count : 0;

        public int NextWorldTreeCost
        {
            get
            {
                if (WorldTreeLevel >= MaxWorldTreeLevel) return 0;
                return Mathf.RoundToInt(12f * Mathf.Pow(1.65f, WorldTreeLevel - 1));
            }
        }

        public int MaxWorldTreeLevel => RegionRules.Length;

        protected override void OnAwake()
        {
            NormalizeSave();
            RefreshUnlockedRegions();
            GameEvents.RaiseCurrentRegionChanged(CurrentRegion);
            GameEvents.RaiseWorldTreeChanged(WorldTreeLevel, WorldTreeEssence);
        }

        public string GetRegionName(RegionType region)
        {
            var rule = GetRegionRule(region);
            return rule != null ? rule.displayName : region.ToString();
        }

        public RegionRule GetRegionRule(RegionType region)
        {
            foreach (var rule in RegionRules)
                if (rule.region == region) return rule;
            return null;
        }

        public StaffDefinition GetStaff(string id)
        {
            foreach (var staff in StaffDefinitions)
                if (staff.id == id) return staff;
            return null;
        }

        public bool IsRegionUnlocked(RegionType region)
        {
            var save = SaveManager.Instance;
            return save != null && save.Data.unlockedRegions.Contains(region.ToString());
        }

        public bool TryTravelTo(RegionType region)
        {
            if (!IsRegionUnlocked(region)) return false;

            var data = SaveManager.Instance.Data;
            string key = region.ToString();
            if (data.currentRegion == key) return true;

            data.currentRegion = key;
            GameEvents.RaiseCurrentRegionChanged(region);
            SaveManager.Instance.Save();
            return true;
        }

        public void AddWorldTreeEssence(int amount)
        {
            if (amount <= 0 || SaveManager.Instance == null) return;

            var data = SaveManager.Instance.Data;
            data.worldTreeEssence = Mathf.Max(0, data.worldTreeEssence + amount);
            GameEvents.RaiseWorldTreeChanged(data.worldTreeLevel, data.worldTreeEssence);
        }

        public bool TryGrowWorldTree()
        {
            if (SaveManager.Instance == null || WorldTreeLevel >= MaxWorldTreeLevel) return false;

            int cost = NextWorldTreeCost;
            var data = SaveManager.Instance.Data;
            if (data.worldTreeEssence < cost) return false;

            data.worldTreeEssence -= cost;
            data.worldTreeLevel = Mathf.Min(MaxWorldTreeLevel, data.worldTreeLevel + 1);
            RefreshUnlockedRegions();
            GameEvents.RaiseWorldTreeChanged(data.worldTreeLevel, data.worldTreeEssence);
            SaveManager.Instance.Save();
            return true;
        }

        public bool CanRecruitStaff(StaffDefinition staff)
        {
            if (staff == null || IsStaffRecruited(staff.id)) return false;
            if (WorldTreeLevel < staff.requiredWorldTreeLevel) return false;
            if (!IsRegionUnlocked(staff.homeRegion)) return false;
            if (WorldTreeEssence < staff.essenceCost) return false;

            var economy = EconomyManager.Instance;
            return economy == null || economy.Gold >= staff.goldCost;
        }

        public bool TryRecruitStaff(string id)
        {
            var staff = GetStaff(id);
            if (!CanRecruitStaff(staff)) return false;

            var economy = EconomyManager.Instance;
            if (economy != null && !economy.TrySpendGold(staff.goldCost)) return false;

            var data = SaveManager.Instance.Data;
            data.worldTreeEssence = Mathf.Max(0, data.worldTreeEssence - staff.essenceCost);
            data.recruitedStaff.Add(staff.id);
            GameEvents.RaiseStaffRecruited(staff.id);
            GameEvents.RaiseWorldTreeChanged(data.worldTreeLevel, data.worldTreeEssence);
            SaveManager.Instance.Save();
            return true;
        }

        public bool IsStaffRecruited(string id)
        {
            return SaveManager.Instance != null && SaveManager.Instance.Data.recruitedStaff.Contains(id);
        }

        public float GetStaffEffect(StaffEffectType effectType)
        {
            var save = SaveManager.Instance;
            if (save == null) return 0f;

            float total = 0f;
            foreach (string id in save.Data.recruitedStaff)
            {
                var staff = GetStaff(id);
                if (staff != null && staff.effectType == effectType)
                    total += staff.effectAmount;
            }
            return total;
        }

        public void RefreshUnlockedRegions()
        {
            if (SaveManager.Instance == null) return;

            var data = SaveManager.Instance.Data;
            foreach (var rule in RegionRules)
            {
                string key = rule.region.ToString();
                if (data.worldTreeLevel >= rule.requiredWorldTreeLevel && !data.unlockedRegions.Contains(key))
                {
                    data.unlockedRegions.Add(key);
                    GameEvents.RaiseRegionUnlocked(rule.region);
                }
            }

            if (!IsRegionUnlocked(CurrentRegion))
                data.currentRegion = RegionType.Forest.ToString();
        }

        private static RegionType ParseRegion(string raw)
        {
            return Enum.TryParse(raw, out RegionType parsed) ? parsed : RegionType.Forest;
        }

        private static void NormalizeSave()
        {
            var save = SaveManager.Instance;
            if (save == null) return;

            var data = save.Data;
            if (data.worldTreeLevel < 1) data.worldTreeLevel = 1;
            if (string.IsNullOrEmpty(data.currentRegion)) data.currentRegion = RegionType.Forest.ToString();
            if (data.unlockedRegions == null) data.unlockedRegions = new();
            if (data.recruitedStaff == null) data.recruitedStaff = new();
            if (!data.unlockedRegions.Contains(RegionType.Forest.ToString()))
                data.unlockedRegions.Add(RegionType.Forest.ToString());
        }
    }
}
