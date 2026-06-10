using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Customers
{
    /// <summary>
    /// 동물 손님 FSM (GDD 4.4): 입장 → 착석 → 대기(주문) → 식사 → 결제(돈 떨굼) → 퇴장.
    /// M1: NavMesh 없이 직선 이동(그레이박스 맵엔 장애물 없음). M2에서 NavMeshAgent로 교체.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        public enum State { Entering, Waiting, Eating, Leaving }

        [Header("Order")]
        public ItemType wantedItem = ItemType.GrilledFish;
        public int orderCount = 1;
        public long payPerDish = 10;

        [Header("Timing")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float eatDuration = 2f;
        [SerializeField] private float patienceSeconds = 30f;

        public State Current { get; private set; } = State.Entering;
        public int Remaining { get; private set; }

        private TableZone _table;
        private Vector3 _exitPoint;
        private float _eatTimer;
        private float _patienceTimer;

        public void Init(TableZone table, Vector3 exitPoint)
        {
            _table = table;
            _exitPoint = exitPoint;
            Remaining = orderCount;
        }

        private void Update()
        {
            switch (Current)
            {
                case State.Entering:
                    if (MoveTo(_table.SeatPosition)) Current = State.Waiting;
                    break;

                case State.Waiting:
                    _patienceTimer += Time.deltaTime;
                    if (_patienceTimer >= patienceSeconds) StartLeaving(angry: true);
                    break;

                case State.Eating:
                    _eatTimer += Time.deltaTime;
                    if (_eatTimer >= eatDuration)
                    {
                        _table.SpawnMoneyPile(payPerDish * orderCount); // 결제: 테이블에 지폐 더미
                        StartLeaving(angry: false);
                    }
                    break;

                case State.Leaving:
                    if (MoveTo(_exitPoint)) Destroy(gameObject);
                    break;
            }
        }

        /// <summary>서빙 받음. 주문 수량을 다 받으면 식사 시작. true = 1개 수령 성공.</summary>
        public bool Receive(ItemType type)
        {
            if (Current != State.Waiting || type != wantedItem || Remaining <= 0) return false;
            Remaining--;
            if (Remaining <= 0) { Current = State.Eating; _eatTimer = 0f; }
            return true;
        }

        private void StartLeaving(bool angry)
        {
            // TODO(B): angry면 💢 말풍선, 만족이면 ♥ 파티클
            _table.OnCustomerLeft();
            Current = State.Leaving;
        }

        private bool MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 dir = target - transform.position;
            if (dir.magnitude < 0.1f) return true;
            transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);
            return false;
        }
    }
}
