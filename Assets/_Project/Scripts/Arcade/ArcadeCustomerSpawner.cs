using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    public class ArcadeCustomerSpawner : MonoBehaviour
    {
        /// <summary>One thing a customer can order, gated by whether its station is live.</summary>
        public class MenuOption
        {
            public ArcadeItemType item;
            public double pay;
            public MonoBehaviour gate; // null = always available; else must be active in hierarchy
        }

        [SerializeField] private ArcadeCustomer customerPrefab;
        [SerializeField] private TableZone[] tables;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float spawnInterval = 5f;

        // Each extra table past the first shortens the wait, so expansions feel
        // busier instead of leaving most tables idle (the 4.5s fixed cadence used
        // to cap throughput at ~40% once 3 tables were open). Clamped so the queue
        // never becomes a frantic spam at high table counts.
        [SerializeField] private float intervalPerExtraTable = 0.7f;
        [SerializeField] private float minSpawnInterval = 2.2f;

        private const float MaxDemandPressure = 4f;
        private const float PressureIntervalDiscount = 0.35f;
        private const float PressurePatiencePenalty = 3.5f;
        private const float MinCustomerPatience = 10f;

        /// <summary>
        /// A customer archetype: ties the illustration to order size, tip multiplier and
        /// patience, plus how late it appears. More open tables (standing in for the GDD's
        /// "restaurant level") unlock fancier, higher-paying but less patient guests.
        /// </summary>
        private class CustomerKind
        {
            public string art;
            public int orderCount;
            public double payMultiplier;
            public float patience;
            public int minTables; // appears only once this many tables are active
        }

        // GDD 11.2: 토끼/너구리는 시작, 여우/곰은 Lv.2(테이블 2개), 펭귄/VIP는 Lv.3(테이블 3개+).
        private static readonly CustomerKind[] Kinds =
        {
            new() { art = "cust_rabbit",  orderCount = 1, payMultiplier = 1.0, patience = 36f, minTables = 1 },
            new() { art = "cust_raccoon", orderCount = 2, payMultiplier = 1.0, patience = 30f, minTables = 1 },
            new() { art = "cust_fox",     orderCount = 2, payMultiplier = 1.2, patience = 30f, minTables = 2 },
            new() { art = "cust_bear",    orderCount = 3, payMultiplier = 1.5, patience = 22f, minTables = 2 },
            new() { art = "cust_penguin", orderCount = 2, payMultiplier = 1.8, patience = 20f, minTables = 3 },
            new() { art = "cust_vipcat",  orderCount = 1, payMultiplier = 3.0, patience = 16f, minTables = 3 },
        };

        private List<MenuOption> _menu;
        private float _timer;
        private float _demandPressure;
        private int _orderRotation;
        private int _kindRotation;

        private struct TableStats
        {
            public int active;
            public int occupied;
            public int waiting;

            public int FreeSeats => Mathf.Max(0, active - occupied);
            public float Occupancy => active > 0 ? occupied / (float)active : 0f;
        }

        public void Configure(
            ArcadeCustomer prefab,
            TableZone[] tableZones,
            Transform spawn,
            Transform exit,
            float interval,
            List<MenuOption> menu)
        {
            customerPrefab = prefab;
            tables = tableZones;
            spawnPoint = spawn;
            exitPoint = exit;
            spawnInterval = Mathf.Max(1f, interval);
            _menu = menu;
            _timer = spawnInterval * 0.7f;
            _demandPressure = 0f;
        }

        private void Update()
        {
            if (customerPrefab == null || spawnPoint == null || exitPoint == null) return;

            var stats = CountTables();
            if (stats.active <= 0) return;

            _timer += Time.deltaTime;
            float interval = CurrentSpawnInterval(stats);

            if (stats.FreeSeats <= 0)
            {
                BuildDemandPressure(stats, interval, Time.deltaTime);
                return;
            }

            if (_timer < interval && _demandPressure < 1f) return;

            var table = FindFreeTable();
            if (table == null)
            {
                BuildDemandPressure(stats, interval, Time.deltaTime);
                return;
            }

            float pressure = Mathf.Clamp(_demandPressure, 0f, MaxDemandPressure);
            var order = PickOrder(pressure);
            if (order == null) return; // nothing sellable yet

            _timer = 0f;
            var spawnPos = spawnPoint.position;
            if (pressure > 0.5f)
                spawnPos.z += Random.Range(-0.18f, 0.18f) * Mathf.Min(3f, pressure);

            var customer = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
            customer.gameObject.SetActive(true);

            // Each archetype carries its own order size, tip multiplier, patience and art,
            // so a bear (3 dishes, +50% tip, impatient) plays very differently from a rabbit.
            var kind = PickKind(stats.active);
            int orderCount = kind.orderCount;
            if (pressure >= 2.5f && stats.active >= 2 && orderCount < 3) orderCount++;

            float patience = Mathf.Max(
                MinCustomerPatience,
                kind.patience - pressure * PressurePatiencePenalty - stats.waiting * 1.5f);

            double pay = order.pay * kind.payMultiplier;
            if (pressure >= 2f && stats.Occupancy >= 0.75f) pay *= 1.1d;

            customer.Configure(order.item, orderCount, pay, patience);
            customer.SetDemandContext(pressure, stats.occupied, stats.active);
            customer.ApplySprite(ArcadeSprites.GetGrounded(kind.art));
            customer.Init(table, exitPoint.position);
            table.Seat(customer);

            _demandPressure = Mathf.Max(0f, _demandPressure - 1f);
            if (_demandPressure >= 1f) _timer = interval;
        }

        // Choose from unlocked menu items by current order backlog and station stock,
        // so the restaurant asks for what it can actually move instead of pure rotation.
        private MenuOption PickOrder(float pressure)
        {
            if (_menu == null || _menu.Count == 0) return null;

            MenuOption best = null;
            float bestScore = float.MinValue;
            int bestIndex = -1;

            for (int i = 0; i < _menu.Count; i++)
            {
                int index = (_orderRotation + i) % _menu.Count;
                var option = _menu[index];
                if (!IsAvailable(option)) continue;

                float score = ScoreOption(option, pressure);
                score += (_menu.Count - i) * 0.01f; // stable tie-breaker that still rotates.
                if (score <= bestScore) continue;

                best = option;
                bestScore = score;
                bestIndex = index;
            }

            if (bestIndex >= 0) _orderRotation = (bestIndex + 1) % _menu.Count;
            return best;
        }

        private float ScoreOption(MenuOption option, float pressure)
        {
            int waiting = CountWaitingOrders(option.item);
            float score = 8f - waiting * (3f + pressure);

            if (option.gate is CookStation station)
            {
                if (station.HasOutput) score += 4.5f;
                if (station.HasPendingWork) score += 1.5f;
                if (!station.HasPendingWork && pressure >= 1.5f) score -= 1.5f;
            }

            if (waiting == 0 && pressure >= 1f) score += pressure * 0.8f;
            score += Mathf.Clamp((float)option.pay / 25f, 0f, 3f);
            return score;
        }

        private static bool IsAvailable(MenuOption option)
        {
            if (option == null || option.item == ArcadeItemType.None) return false;
            return option.gate == null || option.gate.isActiveAndEnabled;
        }

        // Pick a customer archetype unlocked at the current table count, rotating across
        // the unlocked set so early runs stay gentle (rabbit/raccoon) and later ones mix
        // in the demanding, high-tip guests (bear/penguin/VIP).
        private CustomerKind PickKind(int activeTables)
        {
            for (int i = 0; i < Kinds.Length; i++)
            {
                var kind = Kinds[(_kindRotation + i) % Kinds.Length];
                if (kind.minTables <= activeTables)
                {
                    _kindRotation = (_kindRotation + i + 1) % Kinds.Length;
                    return kind;
                }
            }
            return Kinds[0]; // rabbit is always available
        }

        // Spawn cadence tightens as more tables open so expansions stay busy.
        private float CurrentSpawnInterval(TableStats stats)
        {
            int extra = Mathf.Max(0, stats.active - 1);
            float interval = spawnInterval - extra * intervalPerExtraTable;
            interval -= Mathf.Clamp(_demandPressure, 0f, MaxDemandPressure) * PressureIntervalDiscount;
            return Mathf.Max(minSpawnInterval, interval);
        }

        private void BuildDemandPressure(TableStats stats, float interval, float dt)
        {
            float crowdFactor = 1f + stats.waiting * 0.25f + stats.Occupancy * 0.5f;
            _demandPressure = Mathf.Min(
                MaxDemandPressure,
                _demandPressure + dt / Mathf.Max(1f, interval) * crowdFactor);
        }

        private TableStats CountTables()
        {
            var stats = new TableStats();
            var active = TableZone.ActiveZones;
            for (int i = 0; i < active.Count; i++) CountTable(active[i], ref stats);

            if (stats.active == 0 && tables != null)
            {
                foreach (var table in tables) CountTable(table, ref stats);
            }

            return stats;
        }

        private static void CountTable(TableZone table, ref TableStats stats)
        {
            if (table == null || !table.gameObject.activeInHierarchy) return;
            stats.active++;
            if (table.IsOccupied) stats.occupied++;
            if (table.HasWaitingOrder) stats.waiting++;
        }

        private int CountWaitingOrders(ArcadeItemType item)
        {
            int count = 0;
            var active = TableZone.ActiveZones;
            for (int i = 0; i < active.Count; i++)
            {
                var table = active[i];
                if (table != null && table.gameObject.activeInHierarchy && table.HasWaitingOrderFor(item))
                    count++;
            }

            if (active.Count == 0 && tables != null)
            {
                foreach (var table in tables)
                    if (table != null && table.gameObject.activeInHierarchy && table.HasWaitingOrderFor(item))
                        count++;
            }

            return count;
        }

        // Discover tables from the live registry so expansion tables unlocked later
        // (world-tree gate, build zones) are served without re-wiring this spawner.
        private TableZone FindFreeTable()
        {
            var active = TableZone.ActiveZones;
            for (int i = 0; i < active.Count; i++)
            {
                var table = active[i];
                if (table != null && table.gameObject.activeInHierarchy && !table.IsOccupied)
                    return table;
            }

            if (tables != null)
            {
                foreach (var table in tables)
                {
                    if (table != null && table.gameObject.activeInHierarchy && !table.IsOccupied)
                        return table;
                }
            }

            return null;
        }
    }
}
