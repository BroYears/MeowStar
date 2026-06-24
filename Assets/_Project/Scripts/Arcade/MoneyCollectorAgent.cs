using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Dedicated hall staff that patrols money piles and triggers the existing
    /// MoneyPile fly-to-wallet collection by physically touching the pile.
    /// </summary>
    [RequireComponent(typeof(StackHolder))]
    public class MoneyCollectorAgent : MonoBehaviour
    {
        private enum State { Searching, Collecting, ReturningHome }
        private const float SEARCH_STATUS_SECONDS = 0.35f;

        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float arriveDistance = 0.28f;
        [SerializeField] private float repathInterval = 0.25f;

        private Vector3 _home;
        private MoneyPile _target;
        private float _repathTimer;
        private float _searchStatusTimer;
        private State _state = State.Searching;
        private WorldBubble _statusBubble;
        private string _lastStatus;

        public void Configure(Vector3 home)
        {
            _home = home;
        }

        private void Awake()
        {
            _home = transform.position;
        }

        private void Start()
        {
            _statusBubble = WorldBubble.Create(transform, new Vector3(0f, 2.3f, 0f), 1.25f, 0.5f);
            SetStatus("대기");
        }

        private void Update()
        {
            _repathTimer += Time.deltaTime;
            if (_searchStatusTimer > 0f) _searchStatusTimer -= Time.deltaTime;

            if (_target != null && !IsValidTarget(_target))
            {
                _target = null;
                SetState(State.Searching);
                ShowSearchStatus();
            }

            if (_target == null || _repathTimer >= repathInterval)
            {
                _repathTimer = 0f;
                _target = FindNearestPile();
            }

            if (_target != null)
            {
                SetState(State.Collecting);
                TickCollecting();
            }
            else
            {
                SetState(State.ReturningHome);
                TickReturningHome();
            }
        }

        private void TickCollecting()
        {
            if (!IsValidTarget(_target))
            {
                _target = null;
                SetState(State.Searching);
                ShowSearchStatus();
                return;
            }

            SetStatus("돈 회수");
            if (!MoveTo(_target.transform.position)) return;

            if (!_target.TryCollect(transform))
            {
                _target = null;
                SetState(State.Searching);
                ShowSearchStatus();
                return;
            }

            _target = null;
            SetState(State.Searching);
            _repathTimer = repathInterval;
            ShowSearchStatus();
        }

        private void TickReturningHome()
        {
            bool atHome = MoveTo(_home);
            SetStatus(_searchStatusTimer > 0f ? "재탐색" : atHome ? "대기" : "복귀");
        }

        private MoneyPile FindNearestPile()
        {
            MoneyPile best = null;
            float bestSqr = float.MaxValue;
            var piles = MoneyPile.ActivePiles;

            for (int i = 0; i < piles.Count; i++)
            {
                var pile = piles[i];
                if (!IsValidTarget(pile)) continue;

                float sqr = (pile.transform.position - transform.position).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = pile;
            }

            return best;
        }

        private static bool IsValidTarget(MoneyPile pile)
            => pile != null && pile.gameObject.activeInHierarchy && !pile.IsCollecting;

        private void SetState(State state)
        {
            if (_state == state) return;
            _state = state;
        }

        private void ShowSearchStatus()
        {
            _searchStatusTimer = SEARCH_STATUS_SECONDS;
            SetStatus("재탐색");
        }

        private bool MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.magnitude <= arriveDistance) return true;

            Vector3 dir = delta.normalized;
            transform.position += dir * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir, Vector3.up), rotateSpeed * Time.deltaTime);
            return false;
        }

        private void SetStatus(string status)
        {
            if (_statusBubble == null || status == _lastStatus) return;
            _lastStatus = status;
            _statusBubble.SetTitle(status, 24, new Vector2(0f, 0.04f));
        }
    }
}
