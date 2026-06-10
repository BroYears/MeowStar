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

        private static readonly string[] CustomerArt =
            { "cust_rabbit", "cust_raccoon", "cust_fox", "cust_bear", "cust_penguin", "cust_vipcat" };

        private List<MenuOption> _menu;
        private float _timer;
        private int _orderRotation;
        private int _artRotation;

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
        }

        private void Update()
        {
            if (customerPrefab == null || tables == null || spawnPoint == null || exitPoint == null) return;

            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;

            var table = FindFreeTable();
            if (table == null) return;

            var order = PickOrder();
            if (order == null) return; // nothing sellable yet

            _timer = 0f;
            var customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            customer.gameObject.SetActive(true);
            customer.Configure(order.item, 1, order.pay, 30f);
            // Vary the body illustration so the queue isn't all the same animal.
            customer.ApplySprite(ArcadeSprites.GetGrounded(CustomerArt[_artRotation % CustomerArt.Length]));
            _artRotation++;
            customer.Init(table, exitPoint.position);
            table.Seat(customer);
        }

        // Round-robin across whatever menu items are currently unlocked, so once the
        // berry line is built, customers start ordering juice too.
        private MenuOption PickOrder()
        {
            if (_menu == null || _menu.Count == 0) return null;

            for (int i = 0; i < _menu.Count; i++)
            {
                var option = _menu[(_orderRotation + i) % _menu.Count];
                if (option == null) continue;
                if (option.gate != null && !option.gate.isActiveAndEnabled) continue;
                _orderRotation = (_orderRotation + i + 1) % _menu.Count;
                return option;
            }
            return null;
        }

        private TableZone FindFreeTable()
        {
            foreach (var table in tables)
            {
                if (table != null && table.gameObject.activeInHierarchy && !table.IsOccupied)
                    return table;
            }
            return null;
        }
    }
}
