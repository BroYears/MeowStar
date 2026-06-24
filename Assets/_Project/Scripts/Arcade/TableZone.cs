using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    public class TableZone : InteractionZone
    {
        // Active tables register themselves so the customer spawner can discover
        // expansion tables that come online later (e.g. via a world-tree gate)
        // without being re-wired with a fresh array.
        private static readonly List<TableZone> RegisteredZones = new();
        public static IReadOnlyList<TableZone> ActiveZones => RegisteredZones;

        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform moneySpawnPoint;
        [SerializeField] private MoneyPile moneyPilePrefab;
        [SerializeField] private float serveInterval = 0.25f;

        private ArcadeCustomer _customer;
        private float _serveTimer;

        public Vector3 SeatPosition => seatPoint != null ? seatPoint.position : transform.position;
        public Vector3 ServePosition => transform.position;
        public bool IsOccupied => _customer != null;
        public bool HasWaitingOrder => _customer != null && _customer.Current == ArcadeCustomer.State.Waiting;
        public ArcadeItemType WaitingOrderItem => HasWaitingOrder ? _customer.WantedItem : ArcadeItemType.None;

        private void OnEnable()
        {
            if (!RegisteredZones.Contains(this)) RegisteredZones.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RegisteredZones.Remove(this);
        }

        public void Configure(Transform seat, Transform moneyPoint, MoneyPile pilePrefab)
        {
            seatPoint = seat;
            moneySpawnPoint = moneyPoint;
            moneyPilePrefab = pilePrefab;
        }

        public void Seat(ArcadeCustomer customer)
        {
            _customer = customer;
        }

        public void OnCustomerLeft()
        {
            _customer = null;
        }

        public bool HasWaitingOrderFor(ArcadeItemType itemType)
        {
            return _customer != null
                && _customer.Current == ArcadeCustomer.State.Waiting
                && _customer.WantedItem == itemType;
        }

        public void SpawnMoneyPile(double amount)
        {
            if (moneyPilePrefab == null) return;

            Vector3 pos = moneySpawnPoint != null ? moneySpawnPoint.position : transform.position;
            var pile = Instantiate(moneyPilePrefab, pos, Quaternion.identity);
            pile.gameObject.SetActive(true);
            pile.Amount = amount;
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            TickServe(agent, dt);
        }

        public void TickServe(StackHolder agent, float dt)
        {
            if (_customer == null || _customer.Current != ArcadeCustomer.State.Waiting) return;
            if (agent.IsEmpty || agent.CurrentType != _customer.WantedItem) return;

            _serveTimer += dt;
            if (_serveTimer < serveInterval) return;
            _serveTimer = 0f;

            if (_customer.Receive(agent.CurrentType))
            {
                var dish = agent.Pop();
                if (dish != null) Destroy(dish.gameObject);
                Nyangsta.Audio.Sfx.Serve();
            }
        }
    }
}
