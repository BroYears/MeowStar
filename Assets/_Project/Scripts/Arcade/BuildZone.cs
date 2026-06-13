using UnityEngine;
using Nyangsta.Economy;
using System.Collections.Generic;

namespace Nyangsta.Arcade
{
    public class BuildZone : InteractionZone
    {
        private static readonly List<BuildZone> RegisteredZones = new();

        [SerializeField] private double totalCost = 100;
        [SerializeField] private double drainPerTick = 5;
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private GameObject targetToActivate;
        [SerializeField] private WorldBubble costBubble;
        [SerializeField] private string displayName = "새 시설";

        [SerializeField] private MapReveal reveal;

        private double _paid;
        private float _timer;
        private bool _completed;
        private string _zoneId;
        private ArcadeProgressService _progress;

        public static IReadOnlyList<BuildZone> ActiveZones => RegisteredZones;
        public string DisplayName => displayName;
        public double RemainingCost => System.Math.Max(0, totalCost - _paid);
        public bool IsComplete => _completed || _paid >= totalCost;

        public void Configure(double cost, double tick, float interval, GameObject target, WorldBubble bubble, string name)
        {
            totalCost = System.Math.Max(1, cost);
            drainPerTick = System.Math.Max(1, tick);
            tickInterval = Mathf.Max(0.02f, interval);
            targetToActivate = target;
            costBubble = bubble;
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        private void OnEnable()
        {
            if (!RegisteredZones.Contains(this)) RegisteredZones.Add(this);
        }

        private void OnDisable()
        {
            RegisteredZones.Remove(this);
        }

        private void Start()
        {
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_paid >= totalCost || !IsPlayer(agent)) return;

            _timer += dt;
            if (_timer < tickInterval) return;
            _timer = 0f;

            double remaining = totalCost - _paid;
            double tick = System.Math.Min(drainPerTick, remaining);
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpendGold(tick)) return;

            _paid += tick;
            Nyangsta.Audio.Sfx.CoinTick();
            UpdateLabel();
            if (_paid >= totalCost) Complete();
        }

        /// <summary>Hook this zone into the save-backed progress tracker.</summary>
        public void BindProgress(ArcadeProgressService progress, string zoneId)
        {
            _progress = progress;
            _zoneId = zoneId;
        }

        /// <summary>Optional "map expands" reveal played when the target is unlocked.</summary>
        public void SetReveal(MapReveal mapReveal) => reveal = mapReveal;

        /// <summary>Re-applies a previously saved completion: activates the target at no cost.</summary>
        public void RestoreCompleted()
        {
            _completed = true;
            _paid = totalCost;
            if (targetToActivate != null) targetToActivate.SetActive(true);
            if (reveal != null) reveal.SnapInstant();   // already built: appear, don't replay
            gameObject.SetActive(false);
        }

        private void Complete()
        {
            _completed = true;
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Medium();
            if (targetToActivate != null) targetToActivate.SetActive(true);
            if (reveal != null) reveal.PlayAnimated();   // fresh unlock: bloom into the world
            if (_progress != null && _progress.MarkComplete(_zoneId))
                Save.SaveManager.Instance?.Save();
            gameObject.SetActive(false);
        }

        private void UpdateLabel()
        {
            if (costBubble != null)
                costBubble.SetValue($"{RemainingCost:N0}");
        }

        private static bool IsPlayer(StackHolder agent)
        {
            return agent != null && agent.GetComponentInParent<ArcadePlayerController>() != null;
        }
    }
}
