using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nyastar.WorldTree;

namespace Nyastar.WorldTree.Tests
{
    /// <summary>
    /// 세계수 핵심 로직(WorldTreeService) EditMode 단위 테스트. Unity 런타임/씬 없이 순수 검증.
    /// </summary>
    public class WorldTreeServiceTests
    {
        // 테스트용 지역 ID
        private const string Forest = "REGION_FAIRY_FOREST";
        private const string Ice    = "REGION_ICE_VALLEY";
        private const string Desert = "REGION_GOLDEN_DESERT";

        private WorldTreeConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = MakeConfig();
        }

        [TearDown]
        public void TearDown()
        {
            // SetUp 에서 만든 ScriptableObject 정리(에디터 메모리 누수 방지).
            if (_config != null)
            {
                if (_config.regions != null)
                    foreach (var r in _config.regions) if (r != null) Object.DestroyImmediate(r);
                Object.DestroyImmediate(_config);
            }
        }

        // ── 초기화 ───────────────────────────────────────────────────────────────

        [Test]
        public void Init_DefaultState_StartsAtLevel1_WithForestUnlockedAndCurrent()
        {
            var svc = NewService();

            Assert.AreEqual(1, svc.Level);
            Assert.IsTrue(svc.IsRegionUnlocked(Forest), "Lv1 에서 요정의 숲은 해금돼야 함");
            Assert.IsFalse(svc.IsRegionUnlocked(Ice), "Lv1 에서 얼음 계곡은 잠겨 있어야 함");
            Assert.AreEqual(Forest, svc.CurrentRegion.regionId, "현재 지역은 해금된 첫 지역으로 기본 배치");
        }

        [Test]
        public void Init_LoadedHighLevelState_SyncsUnlocksWithoutEvents()
        {
            var loaded = new WorldTreeState { level = 3 }; // 세이브에서 Lv3 로 로드
            int unlockedEvents = 0;

            var svc = new WorldTreeService(_config, loaded, FullWallet());
            svc.OnRegionUnlocked += _ => unlockedEvents++; // 생성 이후 구독

            Assert.IsTrue(svc.IsRegionUnlocked(Ice), "Lv3 로 로드되면 얼음 계곡까지 해금돼 있어야 함");
            Assert.IsFalse(svc.IsRegionUnlocked(Desert), "Lv3 에선 황금 사막은 아직 잠김");
            Assert.AreEqual(0, unlockedEvents, "로드 시점 해금 동기화는 이벤트를 발행하지 않아야 함");
        }

        // ── 성장 ───────────────────────────────────────────────────────────────

        [Test]
        public void CanGrow_False_WhenWalletEmpty()
        {
            var svc = new WorldTreeService(_config, new WorldTreeState(), new SimpleResourceWallet());

            Assert.IsFalse(svc.CanGrow(out var reason));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void Grow_DeductsCost_AndIncrementsLevel_AndFiresOnLevelUp()
        {
            var wallet = new SimpleResourceWallet();
            wallet.Add(ResourceType.Dew, 50); // Lv1->2 비용 = Dew 50
            var svc = new WorldTreeService(_config, new WorldTreeState(), wallet);

            int leveledTo = 0;
            svc.OnLevelUp += l => leveledTo = l;

            Assert.IsTrue(svc.Grow());
            Assert.AreEqual(2, svc.Level);
            Assert.AreEqual(2, leveledTo);
            Assert.AreEqual(0, wallet.GetAmount(ResourceType.Dew), "비용만큼 차감돼야 함");
        }

        [Test]
        public void Grow_Fails_WhenInsufficient_StateUnchanged()
        {
            var wallet = new SimpleResourceWallet();
            wallet.Add(ResourceType.Dew, 10); // 부족
            var svc = new WorldTreeService(_config, new WorldTreeState(), wallet);

            Assert.IsFalse(svc.Grow());
            Assert.AreEqual(1, svc.Level, "실패 시 레벨 불변");
            Assert.AreEqual(10, wallet.GetAmount(ResourceType.Dew), "실패 시 재화 불변");
        }

        [Test]
        public void Grow_MultiCost_RequiresAllResources()
        {
            // Lv2->3 비용 = Dew 100 + Essence 5
            var wallet = new SimpleResourceWallet();
            wallet.Add(ResourceType.Dew, 50 + 100); // Lv1->2(50) + Lv2->3 Dew(100)
            // Essence 없음 → 두 번째 성장은 실패해야
            var svc = new WorldTreeService(_config, new WorldTreeState(), wallet);

            Assert.IsTrue(svc.Grow(), "Lv1->2 는 성공");
            Assert.IsFalse(svc.Grow(), "Essence 부족으로 Lv2->3 실패");
            Assert.AreEqual(2, svc.Level);
        }

        [Test]
        public void Grow_ToRequiredLevel_UnlocksRegion_AndFiresOnRegionUnlocked()
        {
            var svc = new WorldTreeService(_config, new WorldTreeState(), FullWallet());
            var unlocked = new List<string>();
            svc.OnRegionUnlocked += r => unlocked.Add(r.regionId);

            svc.Grow(); // ->2
            Assert.IsFalse(svc.IsRegionUnlocked(Ice));
            svc.Grow(); // ->3 : 얼음 계곡(req 3) 해금
            Assert.IsTrue(svc.IsRegionUnlocked(Ice));
            CollectionAssert.Contains(unlocked, Ice, "Lv3 도달 시 얼음 계곡 해금 이벤트 발행");
        }

        [Test]
        public void Grow_StopsAtMaxLevel()
        {
            var svc = new WorldTreeService(_config, new WorldTreeState(), FullWallet());
            // MaxLevel = levelSteps(4) + 1 = 5
            for (int i = 0; i < 10; i++) svc.Grow();

            Assert.AreEqual(5, svc.Level);
            Assert.IsTrue(svc.IsMaxLevel);
            Assert.IsFalse(svc.CanGrow(out _));
            Assert.IsFalse(svc.Grow());
        }

        // ── 지역 이동 ────────────────────────────────────────────────────────────

        [Test]
        public void Migrate_ToLockedRegion_Fails()
        {
            var svc = NewService();
            Assert.IsFalse(svc.CanMigrateTo(Ice));
            Assert.IsFalse(svc.MigrateTo(Ice));
            Assert.AreEqual(Forest, svc.CurrentRegion.regionId);
        }

        [Test]
        public void Migrate_ToUnlockedRegion_Succeeds_AndFiresOnRegionChanged()
        {
            var svc = new WorldTreeService(_config, new WorldTreeState { level = 3 }, FullWallet());
            RegionData changedTo = null;
            svc.OnRegionChanged += r => changedTo = r;

            Assert.IsTrue(svc.CanMigrateTo(Ice));
            Assert.IsTrue(svc.MigrateTo(Ice));
            Assert.AreEqual(Ice, svc.CurrentRegion.regionId);
            Assert.IsNotNull(changedTo);
            Assert.AreEqual(Ice, changedTo.regionId);
        }

        [Test]
        public void Migrate_ToCurrentRegion_Fails()
        {
            var svc = NewService(); // 현재 Forest
            Assert.IsFalse(svc.CanMigrateTo(Forest), "이미 그 지역이면 이동 불가");
        }

        [Test]
        public void CumulativeBuff_CalculatesAccumulatedBuffs_CorrectlyForEachLevel()
        {
            var wallet = FullWallet();
            var svc = new WorldTreeService(_config, new WorldTreeState(), wallet);

            // Lv1 (기본 상태) - 모든 버프 0
            var buff1 = svc.CumulativeBuff;
            Assert.AreEqual(0f, buff1.gpsBonusMultiplier);
            Assert.AreEqual(0f, buff1.offlineLimitHoursAdd);
            Assert.AreEqual(0f, buff1.huntingTimeSecondsAdd);

            // Lv1 -> Lv2 레벨업
            Assert.IsTrue(svc.Grow());
            Assert.AreEqual(2, svc.Level);
            var buff2 = svc.CumulativeBuff;
            Assert.AreEqual(0.1f, buff2.gpsBonusMultiplier, 0.001f);
            Assert.AreEqual(1.0f, buff2.offlineLimitHoursAdd, 0.001f);
            Assert.AreEqual(0f, buff2.huntingTimeSecondsAdd, 0.001f);

            // Lv2 -> Lv3 레벨업
            Assert.IsTrue(svc.Grow());
            Assert.AreEqual(3, svc.Level);
            var buff3 = svc.CumulativeBuff;
            // Cumulative: 0.1f + 0.15f = 0.25f
            Assert.AreEqual(0.25f, buff3.gpsBonusMultiplier, 0.001f);
            Assert.AreEqual(1.0f, buff3.offlineLimitHoursAdd, 0.001f);
            Assert.AreEqual(2.0f, buff3.huntingTimeSecondsAdd, 0.001f);
        }

        // ── 헬퍼 ───────────────────────────────────────────────────────────────

        private WorldTreeService NewService() =>
            new WorldTreeService(_config, new WorldTreeState(), new SimpleResourceWallet());

        /// <summary>모든 레벨업/이동을 통과할 만큼 넉넉한 지갑.</summary>
        private static IResourceWallet FullWallet()
        {
            var w = new SimpleResourceWallet();
            w.Add(ResourceType.Dew, 100000);
            w.Add(ResourceType.Essence, 100000);
            return w;
        }

        private static WorldTreeConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<WorldTreeConfig>();
            c.regions = new[]
            {
                MakeRegion(Forest, RegionTheme.FairyForest, 1),
                MakeRegion(Ice,    RegionTheme.IceValley,   3),
                MakeRegion(Desert, RegionTheme.GoldenDesert, 5),
            };
            c.levelSteps = new[]
            {
                StepWithBuff(new WorldTreeBuff { gpsBonusMultiplier = 0.1f, offlineLimitHoursAdd = 1.0f }, Cost(ResourceType.Dew, 50)),                                  // Lv1->2
                StepWithBuff(new WorldTreeBuff { gpsBonusMultiplier = 0.15f, huntingTimeSecondsAdd = 2.0f }, Cost(ResourceType.Dew, 100), Cost(ResourceType.Essence, 5)),  // Lv2->3
                StepWithBuff(new WorldTreeBuff { gpsBonusMultiplier = 0.2f }, Cost(ResourceType.Dew, 200), Cost(ResourceType.Essence, 10)), // Lv3->4
                StepWithBuff(new WorldTreeBuff { gpsBonusMultiplier = 0.25f, offlineLimitHoursAdd = 2.0f, huntingTimeSecondsAdd = 3.0f }, Cost(ResourceType.Dew, 400), Cost(ResourceType.Essence, 20)), // Lv4->5
            };
            return c; // MaxLevel = 5
        }

        private static RegionData MakeRegion(string id, RegionTheme theme, int reqLevel)
        {
            var r = ScriptableObject.CreateInstance<RegionData>();
            r.regionId = id;
            r.displayName = id;
            r.theme = theme;
            r.requiredWorldTreeLevel = reqLevel;
            return r;
        }

        private static WorldTreeConfig.GrowthCost Cost(ResourceType type, int amount) =>
            new WorldTreeConfig.GrowthCost { type = type, amount = amount };

        private static WorldTreeConfig.LevelStep Step(params WorldTreeConfig.GrowthCost[] costs) =>
            new WorldTreeConfig.LevelStep { cost = costs };

        private static WorldTreeConfig.LevelStep StepWithBuff(WorldTreeBuff buff, params WorldTreeConfig.GrowthCost[] costs) =>
            new WorldTreeConfig.LevelStep { cost = costs, buff = buff };
    }
}
