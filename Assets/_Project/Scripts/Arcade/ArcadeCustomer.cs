using UnityEngine;

namespace Nyangsta.Arcade
{
    public class ArcadeCustomer : MonoBehaviour
    {
        public enum State { Entering, Waiting, Eating, Leaving }

        [SerializeField] private ArcadeItemType wantedItem = ArcadeItemType.GrilledFish;
        [SerializeField] private int orderCount = 1;
        [SerializeField] private double payPerDish = 10;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float eatDuration = 2f;
        [SerializeField] private float patienceSeconds = 30f;
        [SerializeField] private TextMesh label;
        [SerializeField] private SpriteRenderer bodySprite;

        private TableZone _table;
        private Vector3 _exitPoint;
        private int _remaining;
        private float _eatTimer;
        private float _patienceTimer;
        private bool _releasedTable;

        public State Current { get; private set; } = State.Entering;
        public ArcadeItemType WantedItem => wantedItem;

        public void Configure(ArcadeItemType wanted, int count, double pay, float patience)
        {
            wantedItem = wanted;
            orderCount = Mathf.Max(1, count);
            payPerDish = pay;
            patienceSeconds = patience;
        }

        public void SetLabel(TextMesh orderLabel)
        {
            label = orderLabel;
            UpdateLabel();
        }

        public void SetBodySprite(SpriteRenderer sr)
        {
            bodySprite = sr;
        }

        /// <summary>Swap the body illustration (used by the spawner to vary customers).</summary>
        public void ApplySprite(Sprite sprite)
        {
            if (bodySprite == null || sprite == null) return;
            bodySprite.sprite = sprite;
            // Re-size to a consistent standing height regardless of source art bounds.
            float h = sprite.bounds.size.y;
            if (h > 0f)
            {
                float s = 1.5f / h;
                bodySprite.transform.localScale = new Vector3(s, s, s);
            }
        }

        public void Init(TableZone table, Vector3 exitPoint)
        {
            _table = table;
            _exitPoint = exitPoint;
            _remaining = orderCount;
            Current = State.Entering;
            UpdateLabel();
        }

        private void Update()
        {
            if (_table == null) return;

            switch (Current)
            {
                case State.Entering:
                    if (MoveTo(_table.SeatPosition)) Current = State.Waiting;
                    break;
                case State.Waiting:
                    _patienceTimer += Time.deltaTime;
                    if (_patienceTimer >= patienceSeconds) StartLeaving(false);
                    break;
                case State.Eating:
                    _eatTimer += Time.deltaTime;
                    if (_eatTimer >= eatDuration)
                    {
                        _table.SpawnMoneyPile(payPerDish * orderCount);
                        StartLeaving(true);
                    }
                    break;
                case State.Leaving:
                    if (MoveTo(_exitPoint)) Destroy(gameObject);
                    break;
            }

            if (label != null)
                label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
        }

        public bool Receive(ArcadeItemType itemType)
        {
            if (Current != State.Waiting || itemType != wantedItem || _remaining <= 0) return false;

            _remaining--;
            UpdateLabel();
            if (_remaining <= 0)
            {
                Current = State.Eating;
                _eatTimer = 0f;
                if (label != null) label.text = "...";
            }
            return true;
        }

        private void StartLeaving(bool paid)
        {
            if (!_releasedTable)
            {
                _table.OnCustomerLeft();
                _releasedTable = true;
            }

            Current = State.Leaving;
            if (label != null) label.text = paid ? "OK" : "!";
        }

        private bool MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.magnitude < 0.08f) return true;

            Vector3 dir = delta.normalized;
            transform.position += dir * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);
            return false;
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            label.text = $"{wantedItem} x{_remaining}";
        }
    }
}
