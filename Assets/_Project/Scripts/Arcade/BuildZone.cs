using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    public class BuildZone : InteractionZone
    {
        [SerializeField] private double totalCost = 100;
        [SerializeField] private double drainPerTick = 5;
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private GameObject targetToActivate;
        [SerializeField] private TextMesh costLabel;

        private double _paid;
        private float _timer;

        public void Configure(double cost, double tick, float interval, GameObject target, TextMesh label)
        {
            totalCost = Mathf.Max(1f, (float)cost);
            drainPerTick = Mathf.Max(1f, (float)tick);
            tickInterval = Mathf.Max(0.02f, interval);
            targetToActivate = target;
            costLabel = label;
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        private void Start()
        {
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_paid >= totalCost) return;

            _timer += dt;
            if (_timer < tickInterval) return;
            _timer = 0f;

            double remaining = totalCost - _paid;
            double tick = System.Math.Min(drainPerTick, remaining);
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpendGold(tick)) return;

            _paid += tick;
            UpdateLabel();
            if (_paid >= totalCost) Complete();
        }

        private void Complete()
        {
            if (targetToActivate != null) targetToActivate.SetActive(true);
            gameObject.SetActive(false);
        }

        private void UpdateLabel()
        {
            if (costLabel != null)
                costLabel.text = $"{System.Math.Max(0, totalCost - _paid):N0}G";
        }
    }
}
