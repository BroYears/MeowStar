using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    public class CookStation : InteractionZone
    {
        [SerializeField] private ArcadeItemType inputType = ArcadeItemType.Fish;
        [SerializeField] private ArcadeStackItem outputPrefab;
        [SerializeField] private float cookTime = 2f;
        [SerializeField] private int inputBufferMax = 5;
        [SerializeField] private Transform[] outputSlots;
        [SerializeField] private float transferInterval = 0.2f;

        // Upgrade scaling (per level): cook time shrinks multiplicatively, buffer grows.
        [SerializeField] private float cookTimePerLevel = 0.82f;
        [SerializeField] private int bufferPerLevel = 2;
        [SerializeField] private float minCookTime = 0.25f;

        private float _baseCookTime;
        private int _baseBuffer;

        private readonly List<ArcadeStackItem> _outputs = new();
        private int _inputBuffer;
        private float _cookTimer;
        private float _transferTimer;

        public ArcadeItemType InputType => inputType;
        public ArcadeItemType OutputType => outputPrefab != null ? outputPrefab.type : ArcadeItemType.None;
        public bool HasOutput => _outputs.Count > 0;
        public bool HasPendingWork => HasOutput || IsCooking;

        /// <summary>True while a dish is actually being worked on (drives the wobble FX).</summary>
        public bool IsCooking => _inputBuffer > 0 && outputSlots != null && _outputs.Count < outputSlots.Length;

        public void Configure(
            ArcadeItemType requiredInput,
            ArcadeStackItem output,
            float seconds,
            int bufferMax,
            Transform[] slots)
        {
            inputType = requiredInput;
            outputPrefab = output;
            cookTime = Mathf.Max(0.1f, seconds);
            inputBufferMax = Mathf.Max(1, bufferMax);
            outputSlots = slots;

            // Remember the level-0 values so upgrade levels scale from a fixed base.
            _baseCookTime = cookTime;
            _baseBuffer = inputBufferMax;
        }

        /// <summary>Apply a facility upgrade level: faster cooking and a bigger input buffer.</summary>
        public void SetUpgradeLevel(int level)
        {
            level = Mathf.Max(0, level);
            cookTime = Mathf.Max(minCookTime, _baseCookTime * Mathf.Pow(cookTimePerLevel, level));
            inputBufferMax = _baseBuffer + bufferPerLevel * level;
        }

        private void Update()
        {
            int slotCount = outputSlots != null ? outputSlots.Length : 0;
            if (_inputBuffer <= 0 || outputPrefab == null || slotCount == 0 || _outputs.Count >= slotCount) return;

            _cookTimer += Time.deltaTime;
            if (_cookTimer < cookTime) return;

            _cookTimer = 0f;
            _inputBuffer--;

            Transform slot = outputSlots[_outputs.Count];
            var dish = Instantiate(outputPrefab, slot.position, slot.rotation);
            dish.gameObject.SetActive(true);
            var col = dish.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            _outputs.Add(dish);
            Nyangsta.Audio.Sfx.CookDone();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            TickStation(agent, dt);
        }

        public void TickStation(StackHolder agent, float dt)
        {
            _transferTimer += dt;
            if (_transferTimer < transferInterval) return;

            if (agent.CurrentType == inputType && _inputBuffer < inputBufferMax)
            {
                var item = agent.Pop();
                if (item != null)
                {
                    Destroy(item.gameObject);
                    _inputBuffer++;
                    _transferTimer = 0f;
                }
                return;
            }

            if (HasOutput && outputPrefab != null && agent.CanAccept(outputPrefab.type))
            {
                int last = _outputs.Count - 1;
                var dish = _outputs[last];
                _outputs.RemoveAt(last);

                if (agent.Push(dish)) _transferTimer = 0f;
                else _outputs.Add(dish);
            }
        }
    }
}
