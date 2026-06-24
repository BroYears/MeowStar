using UnityEngine;
using Nyangsta.Economy;

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

        private const float WarningPatienceRatio = 0.34f;
        private const float WalkoutPenaltyMultiplier = 0.25f;

        private static readonly Color HAPPY_INK = new(0.24f, 0.16f, 0.10f);
        private static readonly Color ANGRY_RED = new(0.90f, 0.30f, 0.24f);
        private static readonly Color BUSY_ORANGE = new(0.93f, 0.48f, 0.15f);

        private TableZone _table;
        private Vector3 _exitPoint;
        private int _remaining;
        private float _eatTimer;
        private float _patienceTimer;
        private float _demandPressure;
        private float _crowdRatio;
        private bool _releasedTable;
        private bool _warningPlayed;

        public State Current { get; private set; } = State.Entering;
        public ArcadeItemType WantedItem => wantedItem;

        public void Configure(ArcadeItemType wanted, int count, double pay, float patience)
        {
            wantedItem = wanted;
            orderCount = Mathf.Max(1, count);
            payPerDish = pay > 0d ? pay : 0d;
            patienceSeconds = Mathf.Max(5f, patience);
        }

        public void SetDemandContext(float demandPressure, int occupiedTables, int activeTables)
        {
            _demandPressure = Mathf.Max(0f, demandPressure);
            _crowdRatio = activeTables > 0 ? Mathf.Clamp01(occupiedTables / (float)activeTables) : 0f;
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
            _eatTimer = 0f;
            _patienceTimer = 0f;
            _releasedTable = false;
            _warningPlayed = false;
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
                    float remainRatio = Mathf.Clamp01(1f - _patienceTimer / patienceSeconds);
                    if (bubble != null) bubble.SetGauge(remainRatio);
                    UpdateWaitingFeedback(remainRatio);
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
            if (paid)
            {
                ShowMessage("고마워!", HAPPY_INK);
                return;
            }

            double penalty = ApplyWalkoutPenalty();
            ShowMessage(penalty > 0d ? $"손실 -{penalty:N0}" : "이탈!", ANGRY_RED, penalty > 0d ? 24 : 30);
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
            ApplyOrderMoodTitle(1f, false);
        }

        /// <summary>Icon-only message (eating/leaving moods) replacing the order contents.</summary>
        private void ShowMessage(string message, Color color, int fontSize = 30)
        {
            if (bubble == null) return;
            bubble.SetIconVisible(false);
            bubble.SetValue(null);
            bubble.SetTitle(message, fontSize, new Vector2(0f, 0.08f), color);
            bubble.SetGauge(1f); // hide the patience ring
        }

        private void UpdateWaitingFeedback(float remainRatio)
        {
            ApplyOrderMoodTitle(remainRatio, true);
        }

        private void ApplyOrderMoodTitle(float remainRatio, bool playWarning)
        {
            if (bubble == null) return;

            if (Current == State.Waiting && remainRatio <= WarningPatienceRatio)
            {
                bubble.SetTitle("빨리!", 24, new Vector2(0f, 0.23f), ANGRY_RED);
                if (playWarning && !_warningPlayed)
                {
                    _warningPlayed = true;
                    Nyangsta.Audio.Sfx.Error();
                }
                return;
            }

            if (Current == State.Waiting && (_demandPressure >= 2f || _crowdRatio >= 0.85f))
            {
                bubble.SetTitle("혼잡", 22, new Vector2(0f, 0.23f), BUSY_ORANGE);
                return;
            }

            if ((Current == State.Entering || Current == State.Waiting) && _demandPressure >= 1f)
            {
                bubble.SetTitle("대기", 22, new Vector2(0f, 0.23f), BUSY_ORANGE);
                return;
            }

            bubble.SetTitle(null, 30, Vector2.zero);
        }

        private double ApplyWalkoutPenalty()
        {
            double requested = payPerDish * orderCount * WalkoutPenaltyMultiplier;
            var economy = EconomyManager.Instance;
            if (economy == null || requested <= 0d || economy.Gold <= 0d) return 0d;

            double penalty = requested > economy.Gold ? economy.Gold : requested;
            economy.AddGold(-penalty);
            Nyangsta.Audio.Sfx.Error();
            return penalty;
        }

        private static Sprite GetItemSprite(ArcadeItemType type) => ArcadeSprites.Get(type switch
        {
            ArcadeItemType.Fish => "item_fish",
            ArcadeItemType.Berry => "item_berry",
            ArcadeItemType.Wood => "item_wood",
            ArcadeItemType.BerryJuice => "item_juice",
            ArcadeItemType.Mushroom => "item_mushroom",
            ArcadeItemType.MushroomSkewer => "item_mushroomskewer",
            // 신규 후반 자원/요리는 그레이박스라 기존 아트를 재활용한다.
            ArcadeItemType.Salmon => "item_fish",
            ArcadeItemType.Honey => "item_berry",
            ArcadeItemType.HoneyDessert => "item_juice",
            _ => "item_grilledfish", // GrilledFish, SalmonSteak
        });
    }
}
