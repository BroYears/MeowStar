using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Progression;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Essence-gated expansion pad. Standing on it grows the world tree once the
    /// player has banked enough <see cref="Progression.ProgressionManager.WorldTreeEssence"/>
    /// (earned from the hunt mini-game), then reveals a locked expansion group on the map.
    ///
    /// Unlike <see cref="BuildZone"/> (which drains gold tick-by-tick), essence is a scarce
    /// hunt-only currency that is already persisted by <see cref="ProgressionManager"/>, so
    /// this zone does NOT do partial payment — it simply triggers a one-shot grow when the
    /// banked essence reaches the world-tree's next-level cost. The grow itself debits
    /// essence and unlocks the matching region inside ProgressionManager.
    /// </summary>
    public class WorldTreeGateZone : InteractionZone
    {
        private static readonly List<WorldTreeGateZone> RegisteredZones = new();

        [SerializeField] private int requiredTreeLevel = 2;   // tree level this pad unlocks
        [SerializeField] private GameObject targetToActivate; // expansion group revealed on unlock
        [SerializeField] private WorldBubble costBubble;
        [SerializeField] private string displayName = "세계수 성장";
        [SerializeField] private MapReveal reveal;

        private bool _completed;
        private float _labelTimer;
        private string _lastLabel;

        private const float LabelRefreshInterval = 0.25f;

        public static IReadOnlyList<WorldTreeGateZone> ActiveZones => RegisteredZones;
        public string DisplayName => displayName;
        public bool IsComplete => _completed;

        /// <summary>Essence still needed before this pad can grow the tree.</summary>
        public int RemainingEssence
        {
            get
            {
                var p = ProgressionManager.Instance;
                if (p == null) return 0;
                return Mathf.Max(0, p.NextWorldTreeCost - p.WorldTreeEssence);
            }
        }

        public void Configure(int treeLevel, GameObject target, WorldBubble bubble, string name)
        {
            requiredTreeLevel = Mathf.Max(2, treeLevel);
            targetToActivate = target;
            costBubble = bubble;
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        public void SetReveal(MapReveal mapReveal) => reveal = mapReveal;

        private void OnEnable()
        {
            if (!RegisteredZones.Contains(this)) RegisteredZones.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RegisteredZones.Remove(this);
        }

        private void Update()
        {
            _labelTimer += Time.deltaTime;
            if (_labelTimer < LabelRefreshInterval) return;
            _labelTimer = 0f;
            UpdateLabel();
        }

        private void Start()
        {
            // Already grown this far in a past session: reveal at no cost.
            var p = ProgressionManager.Instance;
            if (p != null && p.WorldTreeLevel >= requiredTreeLevel)
            {
                RestoreCompleted();
                return;
            }
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_completed || !IsPlayer(agent)) return;

            var p = ProgressionManager.Instance;
            if (p == null || p.WorldTreeLevel >= requiredTreeLevel) return;

            // Wait until the banked essence covers the next grow, then grow in one shot.
            // ProgressionManager.TryGrowWorldTree debits the essence and unlocks the region.
            if (p.WorldTreeEssence < p.NextWorldTreeCost)
            {
                UpdateLabel();
                return;
            }

            if (p.TryGrowWorldTree())
            {
                TravelToGateRegion(p);
                Complete();
            }
        }

        /// <summary>Re-applies a saved unlock: activates the expansion group at no cost.</summary>
        public void RestoreCompleted()
        {
            if (_completed) return;
            _completed = true;
            if (targetToActivate != null) targetToActivate.SetActive(true);
            if (reveal != null) reveal.SnapInstant();
            gameObject.SetActive(false);
        }

        private void Complete()
        {
            _completed = true;
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Medium();
            if (targetToActivate != null) targetToActivate.SetActive(true);
            if (reveal != null) reveal.PlayAnimated();
            gameObject.SetActive(false);
        }

        private void UpdateLabel(bool force = false)
        {
            if (costBubble == null) return;

            var p = ProgressionManager.Instance;
            string label;
            if (p == null)
            {
                label = "잠김";
            }
            else
            {
                int remaining = RemainingEssence;
                label = remaining <= 0 ? "성장 가능" : $"정수 {Num.Short(remaining)}";
            }

            if (!force && label == _lastLabel) return;
            _lastLabel = label;
            costBubble.SetValue(label);
        }

        private void TravelToGateRegion(ProgressionManager progression)
        {
            if (progression == null) return;

            foreach (var rule in progression.Regions)
            {
                if (rule == null || rule.requiredWorldTreeLevel != requiredTreeLevel) continue;
                if (progression.IsRegionUnlocked(rule.region))
                    progression.TryTravelTo(rule.region);
                return;
            }
        }

        private static bool IsPlayer(StackHolder agent)
        {
            return agent != null && agent.GetComponentInParent<ArcadePlayerController>() != null;
        }
    }
}
