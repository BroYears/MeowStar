using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Late-game automation: a hired worker that walks the same loop the player does
    /// by hand — gather raw → drop at the cook station → carry cooked dishes to a free
    /// table and serve. Reuses StackHolder and the existing InteractionZone triggers,
    /// so the staff member triggers GatherZone / CookStation / TableZone exactly like
    /// the player. The agent only decides WHERE to stand; the zones do the work.
    /// Supports split scenes by targeting DropBox transforms.
    /// </summary>
    [RequireComponent(typeof(StackHolder))]
    public class StaffAgent : MonoBehaviour
    {
        private enum Job { GoGather, GoCook, GoServe }

        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float rotateSpeed = 12f;
        [SerializeField] private float arriveDistance = 0.35f;
        [SerializeField] private float repathInterval = 0.5f;

        private StackHolder _stack;
        private Transform _gatherTarget;
        private Transform _cookTarget;
        private TableZone[] _tables;
        private ArcadeItemType _rawType;
        private ArcadeItemType _cookedType;

        private Job _job = Job.GoGather;
        private TableZone _targetTable;
        private float _repathTimer;

        public void Configure(
            Transform gatherTarget,
            Transform cookTarget,
            TableZone[] tables,
            ArcadeItemType raw,
            ArcadeItemType cooked)
        {
            _gatherTarget = gatherTarget;
            _cookTarget = cookTarget;
            _tables = tables;
            _rawType = raw;
            _cookedType = cooked;
        }

        private void Awake()
        {
            _stack = GetComponent<StackHolder>();
        }

        private void Update()
        {
            switch (_job)
            {
                case Job.GoGather: TickGather(); break;
                case Job.GoCook: TickCook(); break;
                case Job.GoServe: TickServe(); break;
            }
        }

        // Stand on the gather node until the stack is full of raw, then go cook.
        private void TickGather()
        {
            if (_gatherTarget == null || (_stack.IsFull && _stack.CurrentType == _rawType))
            {
                _job = Job.GoCook;
                return;
            }

            // Carrying cooked food already? Skip straight to serving.
            if (!_stack.IsEmpty && _stack.CurrentType == _cookedType)
            {
                _job = Job.GoServe;
                return;
            }

            MoveTo(_gatherTarget.position);
            // GatherZone or DropBox fills the stack via its own OnTriggerStay once we're inside.
        }

        // Carry raw to the grill. CookStation swaps raw for cooked on contact;
        // once we hold a cooked dish, move on to serving.
        private void TickCook()
        {
            if (_cookTarget == null)
            {
                _job = Job.GoServe;
                return;
            }

            if (!_stack.IsEmpty && _stack.CurrentType == _cookedType)
            {
                _job = Job.GoServe;
                return;
            }

            // Nothing left to drop and nothing cooked picked up — go refill.
            if (_stack.IsEmpty)
            {
                _job = Job.GoGather;
                return;
            }

            MoveTo(_cookTarget.position);
        }

        // Take cooked dishes to a waiting table; when empty, loop back to gather.
        private void TickServe()
        {
            if (_stack.IsEmpty || _stack.CurrentType != _cookedType)
            {
                _targetTable = null;
                _job = Job.GoGather;
                return;
            }

            _repathTimer += Time.deltaTime;
            if (_targetTable == null || !IsServable(_targetTable) || _repathTimer >= repathInterval)
            {
                _repathTimer = 0f;
                _targetTable = FindServableTable();
            }

            // No customer waiting yet — idle near the kitchen so we react fast.
            if (_targetTable == null)
            {
                if (_cookTarget != null) MoveTo(_cookTarget.position);
                return;
            }

            MoveTo(_targetTable.SeatPosition);
            // TableZone.OnAgentStay serves the customer once we're in range.
        }

        private bool IsServable(TableZone table)
            => table != null && table.gameObject.activeInHierarchy && table.IsOccupied;

        private TableZone FindServableTable()
        {
            if (_tables == null) return null;
            foreach (var table in _tables)
                if (IsServable(table)) return table;
            return null;
        }

        private void MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.magnitude <= arriveDistance) return;

            Vector3 dir = delta.normalized;
            transform.position += dir * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir, Vector3.up), rotateSpeed * Time.deltaTime);
        }
    }
}
