using System;
using System.Collections.Generic;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 세계수 성장·지역 해금/이동의 순수 C# 로직(POCO). MonoBehaviour 비의존 → Unity 없이 단위 테스트 가능.
    ///
    /// <para>정적 설정(<see cref="WorldTreeConfig"/>), 동적 상태(<see cref="WorldTreeState"/>),
    /// 재화 지갑(<see cref="IResourceWallet"/>)을 생성자로 주입받아 GDD 3.3 의 규칙을 수행한다.
    /// Unity 씬 쪽 진입은 <see cref="WorldTreeManager"/> 가 이 서비스를 감싸서 제공한다.</para>
    /// </summary>
    public class WorldTreeService
    {
        private readonly WorldTreeConfig _config;
        private readonly IResourceWallet _wallet;

        public WorldTreeState State { get; }
        public WorldTreeConfig Config => _config;

        // ── 이벤트 (UI/연출 디커플링) ──────────────────────────────────────────
        public event Action<int> OnLevelUp;               // 새 레벨
        public event Action<RegionData> OnRegionUnlocked; // 해금된 지역
        public event Action<RegionData> OnRegionChanged;  // 이동 완료된 지역

        public WorldTreeService(WorldTreeConfig config, WorldTreeState state, IResourceWallet wallet)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _wallet = wallet ?? new SimpleResourceWallet();
            State = state ?? new WorldTreeState();

            // 로드된 레벨 기준 해금 동기화(이벤트 없이) + 현재 지역 기본 배치.
            RefreshUnlocks(silent: true);
            if (string.IsNullOrEmpty(State.currentRegionId))
            {
                var unlocked = GetUnlockedRegions();
                if (unlocked.Count > 0) State.currentRegionId = unlocked[0].regionId;
            }
        }

        public int Level => State.level;
        public bool IsMaxLevel => State.level >= _config.MaxLevel;
        public RegionData CurrentRegion => _config.GetRegion(State.currentRegionId);
        public WorldTreeBuff CumulativeBuff => _config.GetCumulativeBuff(State.level);

        // ── 성장 ───────────────────────────────────────────────────────────────

        public bool CanGrow(out string reason)
        {
            reason = null;
            if (IsMaxLevel) { reason = "이미 최대 레벨입니다."; return false; }

            foreach (var c in _config.GetCostToNext(State.level))
            {
                if (!_wallet.CanAfford(c.type, c.amount))
                {
                    reason = $"{c.type} 재화가 부족합니다. (보유 {_wallet.GetAmount(c.type)}, 필요 {c.amount})";
                    return false;
                }
            }
            return true;
        }

        /// <summary>한 단계 성장 — 비용 차감 → 레벨업 → 해금 검사 → 이벤트 발행. 실패 시 false(상태 불변).</summary>
        public bool Grow()
        {
            if (!CanGrow(out _)) return false;

            foreach (var c in _config.GetCostToNext(State.level))
                _wallet.Spend(c.type, c.amount);

            State.level++;
            OnLevelUp?.Invoke(State.level);
            RefreshUnlocks(silent: false);
            return true;
        }

        // ── 지역 해금/이동 ────────────────────────────────────────────────────

        public bool IsRegionUnlocked(string regionId) => State.IsRegionUnlocked(regionId);

        public IReadOnlyList<RegionData> GetUnlockedRegions()
        {
            var result = new List<RegionData>();
            if (_config.regions == null) return result;
            foreach (var r in _config.regions)
                if (r != null && State.IsRegionUnlocked(r.regionId)) result.Add(r);
            return result;
        }

        public bool CanMigrateTo(string regionId) =>
            IsRegionUnlocked(regionId) && State.currentRegionId != regionId && _config.GetRegion(regionId) != null;

        public bool MigrateTo(string regionId)
        {
            if (!CanMigrateTo(regionId)) return false;
            State.currentRegionId = regionId;
            OnRegionChanged?.Invoke(_config.GetRegion(regionId));
            return true;
        }

        // ── 내부 ──────────────────────────────────────────────────────────────

        private void RefreshUnlocks(bool silent)
        {
            if (_config.regions == null) return;
            foreach (var r in _config.regions)
            {
                if (r == null) continue;
                if (r.requiredWorldTreeLevel <= State.level && !State.IsRegionUnlocked(r.regionId))
                {
                    State.unlockedRegionIds.Add(r.regionId);
                    if (!silent) OnRegionUnlocked?.Invoke(r);
                }
            }
        }
    }
}
