using System;
using UnityEngine;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 세계수 레벨 달성 시 지급되는 영구 패시브 버프 효과 (GDD 2 / 4.5 연동).
    /// </summary>
    [Serializable]
    public struct WorldTreeBuff
    {
        [Tooltip("전역 골드 생산량(GPS) 증가 배율 (예: 0.1이면 +10%)")]
        public float gpsBonusMultiplier;

        [Tooltip("오프라인 수익 누적 상한 시간 증가량 (단위: 시간)")]
        public float offlineLimitHoursAdd;

        [Tooltip("사냥 미니게임 제한 시간 증가량 (단위: 초)")]
        public float huntingTimeSecondsAdd;
    }

    /// <summary>
    /// 세계수 정적 설정(ScriptableObject) — 레벨업 비용 테이블 + 전체 지역 목록. (GDD 3.3 / 3.5)
    /// 기획자가 인스펙터에서 밸런스를 조정한다. 런타임에는 읽기 전용으로만 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldTreeConfig", menuName = "Nyastar/World Tree/Config")]
    public class WorldTreeConfig : ScriptableObject
    {
        /// <summary>단일 재화 비용 항목.</summary>
        [Serializable]
        public struct GrowthCost
        {
            public ResourceType type;
            [Min(0)] public int amount;
        }

        /// <summary>한 단계(현재 레벨 → 다음 레벨) 성장 비용 및 버프 혜택.</summary>
        [Serializable]
        public class LevelStep
        {
            [Tooltip("이 단계를 올리는 데 필요한 비용(여러 재화 동시 가능)")]
            public GrowthCost[] cost;

            [Tooltip("이 단계를 완료했을 때(다음 레벨 도달 시) 해금되는 패시브 버프")]
            public WorldTreeBuff buff;
        }

        [Header("레벨업 비용 테이블")]
        [Tooltip("index 0 = Lv1→Lv2 비용. 배열 길이 + 1 이 최대 레벨이 된다.")]
        public LevelStep[] levelSteps;

        [Header("지역")]
        [Tooltip("게임에 존재하는 모든 지역 데이터")]
        public RegionData[] regions;

        /// <summary>도달 가능한 최대 세계수 레벨.</summary>
        public int MaxLevel => (levelSteps?.Length ?? 0) + 1;

        /// <summary>현재 레벨에서 다음 레벨로 가는 비용. 최대 레벨이면 빈 배열.</summary>
        public GrowthCost[] GetCostToNext(int currentLevel)
        {
            int idx = currentLevel - 1; // Lv1 → index 0
            if (levelSteps == null || idx < 0 || idx >= levelSteps.Length)
                return Array.Empty<GrowthCost>();
            return levelSteps[idx].cost ?? Array.Empty<GrowthCost>();
        }

        /// <summary>regionId 로 지역 데이터 조회. 없으면 null.</summary>
        public RegionData GetRegion(string regionId)
        {
            if (regions == null || string.IsNullOrEmpty(regionId)) return null;
            foreach (var r in regions)
                if (r != null && r.regionId == regionId) return r;
            return null;
        }

        /// <summary>특정 레벨까지 누적된 모든 버프 효과의 합을 계산합니다.</summary>
        public WorldTreeBuff GetCumulativeBuff(int currentLevel)
        {
            var total = new WorldTreeBuff();
            if (levelSteps == null) return total;

            // Lv1은 기본 상태이고, Lv1 -> Lv2로 성장하면서 levelSteps[0].buff가 누적됨
            int limit = Mathf.Min(currentLevel - 1, levelSteps.Length);
            for (int i = 0; i < limit; i++)
            {
                var b = levelSteps[i].buff;
                total.gpsBonusMultiplier += b.gpsBonusMultiplier;
                total.offlineLimitHoursAdd += b.offlineLimitHoursAdd;
                total.huntingTimeSecondsAdd += b.huntingTimeSecondsAdd;
            }
            return total;
        }
    }
}
