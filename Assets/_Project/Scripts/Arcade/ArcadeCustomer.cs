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
        [SerializeField] private WorldBubble bubble;
        [SerializeField] private SpriteRenderer bodySprite;

        private static readonly Color HAPPY_INK = new(0.24f, 0.16f, 0.10f);
        private static readonly Color ANGRY_RED = new(0.90f, 0.30f, 0.24f);

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

        public void SetBubble(WorldBubble orderBubble)
        {
            bubble = orderBubble;
            UpdateBubble();
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
            // Let the procedural animator treat the resized scale as its rest pose.
            var anim = bodySprite.GetComponent<SpriteMotionAnimator>();
            if (anim != null) anim.CaptureBaseScale();
        }

        public void Init(TableZone table, Vector3 exitPoint)
        {
            _table = table;
            _exitPoint = exitPoint;
            _remaining = orderCount;
            Current = State.Entering;
            UpdateBubble();
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
                    if (bubble != null) bubble.SetGauge(1f - _patienceTimer / patienceSeconds);
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
        }

        public bool Receive(ArcadeItemType itemType)
        {
            if (Current != State.Waiting || itemType != wantedItem || _remaining <= 0) return false;

            _remaining--;
            UpdateBubble();
            if (_remaining <= 0)
            {
                Current = State.Eating;
                _eatTimer = 0f;
                ShowMessage("냠냠", HAPPY_INK);
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
            ShowMessage(paid ? "고마워!" : "흥!", paid ? HAPPY_INK : ANGRY_RED);
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

        private void UpdateBubble()
        {
            if (bubble == null) return;
            bubble.SetIcon(GetItemSprite(wantedItem), 0.42f, new Vector2(-0.06f, 0.10f));
            bubble.SetValue($"x{_remaining}", 28, new Vector2(0.26f, -0.14f));
            bubble.SetTitle(null, 30, Vector2.zero);
        }

        /// <summary>Icon-only message (eating/leaving moods) replacing the order contents.</summary>
        private void ShowMessage(string message, Color color)
        {
            if (bubble == null) return;
            bubble.SetIconVisible(false);
            bubble.SetValue(null);
            bubble.SetTitle(message, 30, new Vector2(0f, 0.08f), color);
            bubble.SetGauge(1f); // hide the patience ring
        }

        private static Sprite GetItemSprite(ArcadeItemType type) => ArcadeSprites.Get(type switch
        {
            ArcadeItemType.Fish => "item_fish",
            ArcadeItemType.Berry => "item_berry",
            ArcadeItemType.BerryJuice => "item_juice",
            _ => "item_grilledfish",
        });
    }
}
