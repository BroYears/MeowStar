using Nyangsta.Stacking;
using Nyangsta.Zones;
using UnityEngine;

namespace Nyangsta.Customers
{
    /// <summary>
    /// 테이블 = 서빙 존 (GDD 4.1). 손님이 앉아 있고 플레이어가 음식을 들고 존에 서면 자동 서빙.
    /// MoneyPile 스폰 위치도 담당.
    /// </summary>
    public class TableZone : InteractionZone
    {
        [Header("Table")]
        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform moneySpawnPoint;
        [SerializeField] private MoneyPile moneyPilePrefab;
        [SerializeField] private float serveInterval = 0.25f;

        public Vector3 SeatPosition => seatPoint != null ? seatPoint.position : transform.position;
        public bool IsOccupied => _customer != null;

        private Customer _customer;
        private float _serveTimer;

        public void Seat(Customer customer) => _customer = customer;
        public void OnCustomerLeft() => _customer = null;

        public void SpawnMoneyPile(long amount)
        {
            Vector3 pos = moneySpawnPoint != null ? moneySpawnPoint.position : transform.position;
            MoneyPile pile = Instantiate(moneyPilePrefab, pos, Quaternion.identity);
            pile.amount = amount;
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_customer == null || _customer.Current != Customer.State.Waiting) return;
            if (agent.IsEmpty || agent.CurrentType != _customer.wantedItem) return;

            _serveTimer += dt;
            if (_serveTimer < serveInterval) return;
            _serveTimer = 0f;

            if (_customer.Receive(agent.CurrentType))
            {
                StackItem dish = agent.Pop();
                if (dish != null) Destroy(dish.gameObject);
                // TODO(B): 접시가 테이블로 슉 날아가는 트윈 + 햅틱
            }
        }
    }
}
