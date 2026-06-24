using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Late-game automation: a hired worker that walks the same loop the player does
    /// by hand — gather raw → drop at the cook station → carry cooked dishes to a free
    /// table and serve. Reuses StackHolder and the existing InteractionZone triggers,
    /// so the staff member triggers GatherZone / CookStation / TableZone exactly like
    /// the player. The agent only decides WHERE to stand; the zones do the work.
    /// </summary>
    [RequireComponent(typeof(StackHolder))]
    public class StaffAgent : MonoBehaviour
    {
        private enum Job { GoGather, GoCook, GoServe, ReturnHome }

        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float arriveDistance = 0.35f;
        [SerializeField] private float repathInterval = 0.5f;

        private StackHolder _stack;
        private GatherZone _gatherZone;
        private CookStation _cookStation;
        private TableZone[] _tables;
        private ArcadeItemType _rawType;
        private ArcadeItemType _cookedType;
        private Vector3 _home;

        private Job _job = Job.GoGather;
        private TableZone _targetTable;
        private float _repathTimer;
        private WorldBubble _statusBubble;
        private string _lastStatus;

        public void Configure(
            GatherZone gather,
            CookStation cook,
            TableZone[] tables,
            ArcadeItemType raw,
            ArcadeItemType cooked)
        {
            _gatherZone = gather;
            _cookStation = cook;
            _tables = tables;
            _rawType = raw != ArcadeItemType.None
                ? raw
                : gather != null ? gather.ProducedType : ArcadeItemType.None;
            _cookedType = cooked != ArcadeItemType.None
                ? cooked
                : cook != null ? cook.OutputType : ArcadeItemType.None;
        }

        private void Awake()
        {
            _stack = GetComponent<StackHolder>();
            _home = transform.position;
        }

        private void Start()
        {
            _statusBubble = WorldBubble.Create(transform, new Vector3(0f, 2.3f, 0f), 1.35f, 0.52f);
            SetStatus("출근");
        }

        private void Update()
        {
            _repathTimer += Time.deltaTime;
            bool forceRepath = _repathTimer >= repathInterval;
            if (forceRepath) _repathTimer = 0f;

            ChooseJob(forceRepath);

            switch (_job)
            {
                case Job.GoGather: TickGather(); break;
                case Job.GoCook: TickCook(); break;
                case Job.GoServe: TickServe(); break;
                case Job.ReturnHome: TickReturnHome(); break;
            }
        }

        private void ChooseJob(bool forceRepath)
        {
            if (IsCarryingCooked())
            {
                RefreshServeTarget(forceRepath);
                _job = _targetTable != null || !CanCollectCookedOutput()
                    ? Job.GoServe
                    : Job.GoCook;
                return;
            }

            _targetTable = null;

            if (IsCarryingRaw())
            {
                _job = CanUseCookStation() ? Job.GoCook : Job.ReturnHome;
                return;
            }

            if (CanCollectCookedOutput())
            {
                _job = Job.GoCook;
                return;
            }

            bool hasOrder = HasWaitingOrder();
            if (hasOrder)
            {
                if (CanUseCookStation() && _cookStation.HasPendingWork)
                {
                    _job = Job.GoCook;
                    return;
                }

                _job = CanUseGatherZone() ? Job.GoGather : Job.ReturnHome;
                return;
            }

            _job = CanUseCookStation() && _cookStation.HasPendingWork
                ? Job.GoCook
                : Job.ReturnHome;
        }

        // Gather only when a real waiting order needs this station's ingredient.
        private void TickGather()
        {
            if (!CanUseGatherZone() || !HasWaitingOrder())
            {
                _job = Job.ReturnHome;
                SetStatus("복귀");
                return;
            }

            if (IsCarryingRaw())
            {
                _job = Job.GoCook;
                SetStatus("조리로 이동");
                return;
            }

            SetStatus("재료 수집");
            if (MoveTo(_gatherZone.transform.position))
            {
                _gatherZone.TickGather(_stack, Time.deltaTime);
                if (IsCarryingRaw())
                {
                    _job = Job.GoCook;
                    SetStatus("조리로 이동");
                }
            }
        }

        // Carry raw to the grill. CookStation swaps raw for cooked on contact;
        // once we hold a cooked dish, move on to serving.
        private void TickCook()
        {
            if (!CanUseCookStation())
            {
                _job = Job.ReturnHome;
                SetStatus("시설 대기");
                return;
            }

            RefreshServeTarget(false);
            if (IsCarryingCooked() && _targetTable != null)
            {
                _job = Job.GoServe;
                SetStatus("서빙");
                return;
            }

            if (IsCarryingCooked() && !CanCollectCookedOutput())
            {
                _job = Job.GoServe;
                SetStatus("주문 대기");
                return;
            }

            if (_stack.IsEmpty && !_cookStation.HasPendingWork && !HasWaitingOrder())
            {
                _job = Job.ReturnHome;
                SetStatus("복귀");
                return;
            }

            SetStatus(GetCookStatus());
            if (MoveTo(_cookStation.transform.position))
            {
                _cookStation.TickStation(_stack, Time.deltaTime);

                if (IsCarryingCooked())
                {
                    RefreshServeTarget(true);
                    _job = _targetTable != null ? Job.GoServe : Job.GoCook;
                }
                else if (_stack.IsEmpty && !_cookStation.HasPendingWork)
                {
                    _job = HasWaitingOrder() && CanUseGatherZone() ? Job.GoGather : Job.ReturnHome;
                }
            }
        }

        // Take cooked dishes to a waiting table; when empty, loop back to gather.
        private void TickServe()
        {
            if (!IsCarryingCooked())
            {
                _targetTable = null;
                ChooseJob(true);
                return;
            }

            RefreshServeTarget(false);

            // No customer waiting yet: hold finished food near the station/home and react fast.
            if (_targetTable == null)
            {
                SetStatus("주문 대기");
                if (CanUseCookStation()) MoveTo(_cookStation.transform.position);
                else MoveTo(_home);
                return;
            }

            SetStatus("서빙");
            if (MoveTo(_targetTable.ServePosition))
            {
                _targetTable.TickServe(_stack, Time.deltaTime);
                if (!IsServable(_targetTable)) _targetTable = null;
            }
        }

        private void TickReturnHome()
        {
            _targetTable = null;
            SetStatus(MoveTo(_home) ? "대기" : "복귀");
        }

        private bool IsServable(TableZone table)
            => table != null && table.gameObject.activeInHierarchy && table.HasWaitingOrderFor(_cookedType);

        private bool IsCarryingRaw()
            => !_stack.IsEmpty && _stack.CurrentType == _rawType;

        private bool IsCarryingCooked()
            => !_stack.IsEmpty && _stack.CurrentType == _cookedType;

        private bool CanUseGatherZone()
            => _gatherZone != null && _gatherZone.gameObject.activeInHierarchy
                && _gatherZone.ProducedType == _rawType
                && (_stack.IsEmpty || _stack.CanAccept(_rawType));

        private bool CanUseCookStation()
            => _cookStation != null && _cookStation.gameObject.activeInHierarchy;

        private bool CanCollectCookedOutput()
            => CanUseCookStation()
                && _cookStation.HasOutput
                && _cookStation.OutputType == _cookedType
                && _stack.CanAccept(_cookStation.OutputType);

        private bool HasWaitingOrder()
        {
            var activeTables = TableZone.ActiveZones;
            for (int i = 0; i < activeTables.Count; i++)
            {
                if (IsServable(activeTables[i])) return true;
            }

            if (_tables == null) return false;
            foreach (var table in _tables)
                if (IsServable(table)) return true;
            return false;
        }

        private void RefreshServeTarget(bool force)
        {
            if (!force && IsServable(_targetTable)) return;
            _targetTable = FindServableTable();
        }

        private string GetCookStatus()
        {
            if (IsCarryingRaw()) return "재료 투입";
            if (CanCollectCookedOutput()) return "완성 수거";
            if (_cookStation.HasPendingWork) return "완성 대기";
            return "조리 준비";
        }

        private TableZone FindServableTable()
        {
            var activeTables = TableZone.ActiveZones;
            for (int i = 0; i < activeTables.Count; i++)
            {
                var table = activeTables[i];
                if (IsServable(table)) return table;
            }

            if (_tables == null) return null;
            foreach (var table in _tables)
                if (IsServable(table)) return table;
            return null;
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
