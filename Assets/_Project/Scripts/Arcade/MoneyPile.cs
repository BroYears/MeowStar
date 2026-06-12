using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    [RequireComponent(typeof(Collider))]
    public class MoneyPile : MonoBehaviour
    {
        private const int BILL_COUNT = 4;
        private const float BILL_DELAY_STEP = 0.06f;
        private const float BILL_BASE_DURATION = 0.26f;
        private const float BILL_DURATION_STEP = 0.04f;
        private const float POP_DURATION = 0.18f;

        [SerializeField] private double amount = 10;
        [SerializeField] private float flyDuration = 0.25f;
        [SerializeField] private TextMesh amountLabel;

        private Transform _target;
        private Vector3 _startPos;
        private float _t;
        private bool _rewarded;

        // Pre-built bill pieces (reused, no Instantiate at collect time).
        private Transform[] _bills;
        private float[] _billDelays;
        private float[] _billDurations;
        private Vector3[] _billSides;
        private float _billClock;
        private Transform _pop;
        private SpriteRenderer _popRenderer;
        private float _popT = -1f;

        public double Amount
        {
            get => amount;
            set
            {
                amount = System.Math.Max(0, value);
                UpdateLabel();
            }
        }

        public void SetLabel(TextMesh label)
        {
            amountLabel = label;
            UpdateLabel();
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            UpdateLabel();
            BuildBills();
        }

        private void BuildBills()
        {
            var sprite = ArcadeSprites.Get("money");
            _bills = new Transform[BILL_COUNT];
            _billDelays = new float[BILL_COUNT];
            _billDurations = new float[BILL_COUNT];
            _billSides = new Vector3[BILL_COUNT];
            for (int i = 0; i < BILL_COUNT; i++)
            {
                var go = new GameObject($"Bill_{i}");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                go.AddComponent<SpriteBillboard>().Init(true, true, 60);
                go.SetActive(false);
                _bills[i] = go.transform;
                _billDelays[i] = i * BILL_DELAY_STEP;
                _billDurations[i] = BILL_BASE_DURATION + i * BILL_DURATION_STEP;
            }

            // Arrival pop (yellow glow, scales up then fades).
            var popGo = new GameObject("Pop");
            popGo.transform.SetParent(transform, false);
            _popRenderer = popGo.AddComponent<SpriteRenderer>();
            _popRenderer.sprite = UI.UITheme.Glow;
            _popRenderer.color = UI.UITheme.Gold;
            popGo.AddComponent<SpriteBillboard>().Init(true, true, 61);
            popGo.SetActive(false);
            _pop = popGo.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_target != null) return;

            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;

            _target = holder.transform;
            _startPos = transform.position;
            _t = 0f;
            _billClock = 0f;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            HidePileVisuals();
            LaunchBills();
        }

        private void HidePileVisuals()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        private void LaunchBills()
        {
            float billScale = 0.34f;
            var sprite = _bills[0].GetComponent<SpriteRenderer>().sprite;
            float h = sprite != null ? sprite.bounds.size.y : 1f;
            float s = h > 0f ? billScale / h : 1f;
            for (int i = 0; i < BILL_COUNT; i++)
            {
                // Detach so world-space motion is unaffected by the pile's non-uniform scale.
                _bills[i].SetParent(null);
                _bills[i].position = _startPos + Vector3.up * 0.25f;
                _bills[i].localScale = Vector3.one * s;
                // Each bill curves to a different side for a scattered swoop.
                float side = (i % 2 == 0 ? 1f : -1f) * (0.3f + i * 0.18f);
                _billSides[i] = new Vector3(side, 0f, 0f);
                _bills[i].gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (_target == null) return;

            // Reward timing matches the original single-flight behavior.
            if (!_rewarded)
            {
                _t += Time.deltaTime / Mathf.Max(0.05f, flyDuration);
                if (_t >= 1f)
                {
                    _rewarded = true;
                    EconomyManager.Instance?.AddGold(amount);
                    Nyangsta.Audio.Sfx.Coin();
                    Nyangsta.Core.Haptics.Light();
                }
            }

            Vector3 targetPos = _target.position + Vector3.up * 1.15f;
            _billClock += Time.deltaTime;

            bool anyFlying = false;
            for (int i = 0; i < BILL_COUNT; i++)
            {
                if (_bills[i] == null || !_bills[i].gameObject.activeSelf) continue;

                float bt = (_billClock - _billDelays[i]) / _billDurations[i];
                if (bt < 0f) { anyFlying = true; continue; }
                if (bt >= 1f)
                {
                    _bills[i].gameObject.SetActive(false);
                    Destroy(_bills[i].gameObject);
                    _bills[i] = null;
                    if (i == BILL_COUNT - 1) StartPop(targetPos);
                    continue;
                }

                anyFlying = true;
                float arc = Mathf.Sin(bt * Mathf.PI);
                Vector3 pos = Vector3.Lerp(_startPos + Vector3.up * 0.25f, targetPos, bt);
                pos += _billSides[i] * arc;
                pos.y += arc * (0.6f + i * 0.12f);
                _bills[i].position = pos;
                float shrink = Mathf.Max(0.25f, 1f - bt * 0.5f);
                _bills[i].localScale = Vector3.one * (0.34f * shrink);
            }

            // Pop animation: quick scale-up + fade out.
            if (_popT >= 0f)
            {
                _popT += Time.deltaTime / POP_DURATION;
                float k = Mathf.Clamp01(_popT);
                _pop.position = targetPos;
                _pop.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.9f, k);
                var c = _popRenderer.color;
                c.a = 1f - k;
                _popRenderer.color = c;
                if (_popT >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (!anyFlying && _popT < 0f && _rewarded)
                Destroy(gameObject);
        }

        private void StartPop(Vector3 at)
        {
            if (_pop == null) return;
            _pop.SetParent(null);
            _pop.position = at;
            _pop.gameObject.SetActive(true);
            _popRenderer.enabled = true;
            _popT = 0f;
        }

        private void OnDestroy()
        {
            // Detached pieces are not children anymore; clean them up explicitly.
            if (_bills != null)
                for (int i = 0; i < BILL_COUNT; i++)
                    if (_bills[i] != null) Destroy(_bills[i].gameObject);
            if (_pop != null) Destroy(_pop.gameObject);
        }

        private void LateUpdate()
        {
            if (amountLabel != null)
                amountLabel.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
        }

        private void UpdateLabel()
        {
            if (amountLabel != null)
                amountLabel.text = $"+{amount:N0}G";
        }
    }
}
