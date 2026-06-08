using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 세계수 시스템의 씬 진입점(영속 싱글턴). 실제 로직은 순수 C# <see cref="WorldTreeService"/> 에 위임한다.
    ///
    /// <para>정적 설정(<see cref="WorldTreeConfig"/>)은 인스펙터로 주입하고, 동적 상태/지갑은 Scene_Boot 의
    /// 세이브·경제 시스템이 <see cref="Initialize"/> 로 주입한다. UI 는 이 매니저의 이벤트만 구독한다(GDD 5.2).</para>
    /// </summary>
    public class WorldTreeManager : MonoBehaviour
    {
        public static WorldTreeManager Instance { get; private set; }

        [Header("정적 설정 (인스펙터 주입)")]
        [SerializeField] private WorldTreeConfig config;

        [Header("단독 실행용 — Boot 의 Initialize 가 없을 때만 기본값으로 자가 초기화")]
        [SerializeField] private bool autoInitializeForTesting = true;

        public WorldTreeService Service { get; private set; }
        public bool IsInitialized => Service != null;

        // UI 는 초기화 시점과 무관하게 구독할 수 있도록 매니저가 이벤트를 재노출(서비스 이벤트를 포워딩).
        public event Action<int> OnLevelUp;
        public event Action<RegionData> OnRegionUnlocked;
        public event Action<RegionData> OnRegionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!IsInitialized && autoInitializeForTesting)
                Initialize(new WorldTreeState(), new SimpleResourceWallet());
        }

        /// <summary>Boot 단계에서 세이브 로드 후 호출 — 서비스 생성 + 이벤트 포워딩 연결.</summary>
        public void Initialize(WorldTreeState state, IResourceWallet wallet)
        {
            if (config == null)
            {
                Debug.LogError("[WorldTree] WorldTreeConfig 가 주입되지 않았습니다. 인스펙터를 확인하세요.");
                return;
            }
            Service = new WorldTreeService(config, state, wallet);
            Service.OnLevelUp += l => OnLevelUp?.Invoke(l);
            Service.OnRegionUnlocked += r => OnRegionUnlocked?.Invoke(r);
            Service.OnRegionChanged += r => OnRegionChanged?.Invoke(r);
        }

        // ── 위임 API (씬/UI 편의) ──────────────────────────────────────────────
        public int Level => Service?.Level ?? 0;
        public bool IsMaxLevel => Service?.IsMaxLevel ?? false;
        public RegionData CurrentRegion => Service?.CurrentRegion;
        public WorldTreeBuff CumulativeBuff => Service?.CumulativeBuff ?? default;
        public WorldTreeConfig Config => config;

        public bool CanGrow(out string reason)
        {
            if (!IsInitialized) { reason = "초기화되지 않았습니다."; return false; }
            return Service.CanGrow(out reason);
        }

        public bool Grow() => IsInitialized && Service.Grow();
        public bool IsRegionUnlocked(string regionId) => IsInitialized && Service.IsRegionUnlocked(regionId);
        public IReadOnlyList<RegionData> GetUnlockedRegions() => Service?.GetUnlockedRegions() ?? new List<RegionData>();
        public bool CanMigrateTo(string regionId) => IsInitialized && Service.CanMigrateTo(regionId);
        public bool MigrateTo(string regionId) => IsInitialized && Service.MigrateTo(regionId);
    }
}
