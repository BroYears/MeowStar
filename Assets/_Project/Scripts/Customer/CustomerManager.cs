using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Progression;
using Nyangsta.Save;

namespace Nyangsta.Customer
{
    /// <summary>
    /// Spawns customers on a schedule (up to seat capacity) and ticks their FSM.
    /// On serve, auto-cooks a preferred (or available) menu, pays gold, frees seat.
    /// </summary>
    public class CustomerManager : Singleton<CustomerManager>
    {
        [Header("Tuning")]
        [SerializeField] private float spawnInterval = 4f;
        [SerializeField] private int baseSeats = 3;
        [SerializeField] private float baseCookSeconds = 2f;
        [SerializeField] private float minCookSeconds = 0.35f;
        [SerializeField] private float reputationArrivalBonus = 0.08f;
        [SerializeField] private float reputationRevenueBonus = 0.05f;

        [Header("Content")]
        [SerializeField] private GameDatabase database;

        private readonly List<CustomerInstance> _active = new();
        private float _spawnTimer;
        private float _cookTimer;

        public int SeatCount => baseSeats + Mathf.RoundToInt(UpgradeEffect("UPG_SEATS"));
        public int CookStationCount =>
            1 + Mathf.RoundToInt(UpgradeEffect("UPG_COOK_STATION")) +
            Mathf.RoundToInt(StaffEffect(StaffEffectType.ServiceSlots));
        public float CurrentCookSeconds =>
            Mathf.Max(minCookSeconds, baseCookSeconds / (1f + UpgradeEffect("UPG_COOK_SPEED") + StaffEffect(StaffEffectType.CookSpeed)));
        public float CurrentSpawnInterval =>
            Mathf.Max(0.75f, spawnInterval / (1f + UpgradeEffect("UPG_REPUTATION") * reputationArrivalBonus));
        public float RevenueMultiplier =>
            1f + UpgradeEffect("UPG_REPUTATION") * reputationRevenueBonus + StaffEffect(StaffEffectType.RevenueMultiplier);
        public float CookTimerNormalized =>
            CurrentCookSeconds > 0f ? Mathf.Clamp01(_cookTimer / CurrentCookSeconds) : 0f;
        public IReadOnlyList<CustomerInstance> Active => _active;

        protected override void OnAwake()
        {
            if (database != null) database.BuildLookup();
        }

        private void Update()
        {
            // Dependencies may not have finished Awake yet on the very first frames.
            if (SaveManager.Instance == null || database == null) return;

            float dt = Time.deltaTime;

            _spawnTimer += dt;
            if (_spawnTimer >= CurrentSpawnInterval && _active.Count < SeatCount)
            {
                _spawnTimer = 0f;
                TrySpawn();
            }

            _cookTimer -= dt;
            if (_cookTimer <= 0f)
            {
                int served = TryServeWaitingCustomers(CookStationCount);
                if (served > 0) _cookTimer = CurrentCookSeconds;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c.Tick(dt))
                {
                    if (c.LeftAngry) GameEvents.RaiseCustomerLeftAngry(c.Data);
                    _active.RemoveAt(i);
                }
            }
        }

        private void TrySpawn()
        {
            var pool = GetUnlockedCustomers();
            if (pool.Count == 0) return;
            int seat = FindFreeSeat();
            if (seat < 0) return;
            var data = pool[Random.Range(0, pool.Count)];
            _active.Add(new CustomerInstance(data, seat));
        }

        private int FindFreeSeat()
        {
            for (int seat = 0; seat < SeatCount; seat++)
            {
                bool taken = false;
                foreach (var c in _active)
                    if (c.SeatIndex == seat) { taken = true; break; }
                if (!taken) return seat;
            }
            return -1;
        }

        private List<CustomerData> GetUnlockedCustomers()
        {
            int stage = SaveManager.Instance.Data.restaurantLevel;
            RegionType currentRegion = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.CurrentRegion
                : RegionType.Forest;
            var pool = new List<CustomerData>();
            if (database == null) return pool;
            foreach (var c in database.customers)
                if (c != null && c.unlockStage <= stage && c.homeRegion == currentRegion)
                    pool.Add(c);
            return pool;
        }

        private int TryServeWaitingCustomers(int maxServes)
        {
            int served = 0;
            while (served < maxServes)
            {
                var next = FindMostUrgentServeableCustomer();
                if (next == null || !TryServe(next)) break;
                served++;
            }
            return served;
        }

        private CustomerInstance FindMostUrgentServeableCustomer()
        {
            CustomerInstance best = null;
            float lowestPatience = float.MaxValue;

            foreach (var c in _active)
            {
                if (c.State != CustomerState.Waiting && c.State != CustomerState.Ordering) continue;
                var menu = c.Data.preferredMenu;
                if (menu == null || !EconomyManager.Instance.CanCook(menu)) continue;

                if (c.PatienceRemaining < lowestPatience)
                {
                    best = c;
                    lowestPatience = c.PatienceRemaining;
                }
            }

            return best;
        }

        private bool TryServe(CustomerInstance c)
        {
            var menu = c.Data.preferredMenu;
            if (menu == null || !EconomyManager.Instance.CanCook(menu)) return false;

            if (!EconomyManager.Instance.TryCook(menu)) return false;

            int paid = c.Serve(menu, RevenueMultiplier);
            if (paid > 0)
            {
                EconomyManager.Instance.AddGold(paid);
                GameEvents.RaiseCustomerServed(c.Data);
                return true;
            }
            return false;
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
