using UnityEngine;
using Nyangsta.Economy;
using System.Collections.Generic;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Stand here with gold to hire a staff member. Drains gold like BuildZone, then
    /// spawns a StaffAgent wired to the same gather/cook/serve loop the player runs.
    /// </summary>
    public class HireZone : InteractionZone
    {
        private static readonly List<HireZone> RegisteredZones = new();

        [SerializeField] private double totalCost = 150;
        [SerializeField] private double drainPerTick = 8;
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private WorldBubble costBubble;
        [SerializeField] private string displayName = "직원 고용";

        [SerializeField] private Transform gatherZone;
        [SerializeField] private Transform cookStation;
        [SerializeField] private TableZone[] tables;
        [SerializeField] private ArcadeItemType rawType;
        [SerializeField] private ArcadeItemType cookedType;
        [SerializeField] private Transform spawnPoint;

        private double _paid;
        private float _timer;
        private bool _hired;
        private string _zoneId;
        private ArcadeProgressService _progress;

        public static IReadOnlyList<HireZone> ActiveZones => RegisteredZones;
        public string DisplayName => displayName;
        public double RemainingCost => System.Math.Max(0, totalCost - _paid);
        public bool IsComplete => _hired || _paid >= totalCost;
        public bool IsLockedByFacility => !IsFacilityReady();

        public void Configure(
            double cost,
            double tick,
            WorldBubble bubble,
            Transform gather,
            Transform cook,
            TableZone[] tableZones,
            ArcadeItemType raw,
            ArcadeItemType cooked,
            Transform spawn,
            string name)
        {
            totalCost = System.Math.Max(1, cost);
            drainPerTick = System.Math.Max(1, tick);
            costBubble = bubble;
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            gatherZone = gather;
            cookStation = cook;
            tables = tableZones;
            rawType = raw;
            cookedType = cooked;
            spawnPoint = spawn;
            UpdateLabel();
        }

        private void OnEnable()
        {
            if (!RegisteredZones.Contains(this)) RegisteredZones.Add(this);
            UpdateLabel();
        }

        private void OnDisable()
        {
            RegisteredZones.Remove(this);
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_hired || _paid >= totalCost || !IsPlayer(agent) || !IsFacilityReady())
            {
                UpdateLabel();
                return;
            }

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
            if (_paid >= totalCost) Hire();
        }

        /// <summary>Hook this zone into the save-backed progress tracker.</summary>
        public void BindProgress(ArcadeProgressService progress, string zoneId)
        {
            _progress = progress;
            _zoneId = zoneId;
        }

        /// <summary>Re-applies a previously saved hire: spawns the staff at no cost.</summary>
        public void RestoreCompleted()
        {
            _hired = true;
            _paid = totalCost;
            SpawnStaff();
            gameObject.SetActive(false);
        }

        private void Hire()
        {
            _hired = true;
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Medium();
            SpawnStaff();
            if (_progress != null && _progress.MarkComplete(_zoneId))
                Save.SaveManager.Instance?.Save();
            gameObject.SetActive(false);
        }

        private void SpawnStaff()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Staff_Nyangsta";
            go.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            pos.y = 1f;
            go.transform.position = pos;

            // Hide the capsule mesh and stand a staff-cat sprite on the ground.
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var spriteGo = new GameObject("Sprite_staff");
            spriteGo.transform.SetParent(go.transform, false);
            spriteGo.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = ArcadeSprites.GetGrounded("staff");
            if (sr.sprite != null)
            {
                float h = sr.sprite.bounds.size.y;
                if (h > 0f) spriteGo.transform.localScale = Vector3.one * (1.9f / h);
            }
            spriteGo.AddComponent<SpriteBillboard>().Init(true, true, 0);
            spriteGo.AddComponent<SpriteMotionAnimator>().ConfigureActor(go.transform);

            // Trigger collider so the staff fires zone OnTriggerStay like the player does.
            var capsule = go.GetComponent<CapsuleCollider>();
            capsule.isTrigger = true;

            // Trigger-vs-trigger needs a Rigidbody on one side to raise events; the
            // player gets this from its CharacterController, so the staff needs an
            // explicit kinematic body or zones would never see it.
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var stack = go.AddComponent<StackHolder>();
            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(go.transform);
            anchor.localPosition = new Vector3(0f, 2.0f, 0f);
            stack.Configure(anchor, 3);

            var agent = go.AddComponent<StaffAgent>();
            agent.Configure(gatherZone, cookStation, tables, rawType, cookedType);
        }

        private void UpdateLabel()
        {
            if (costBubble == null) return;
            bool ready = IsFacilityReady();
            costBubble.SetIconVisible(ready);
            costBubble.SetValue(ready ? $"{RemainingCost:N0}" : "잠김");
        }

        private bool IsFacilityReady()
        {
            bool gatherReady = gatherZone == null || gatherZone.gameObject.activeInHierarchy;
            bool cookReady = cookStation == null || cookStation.gameObject.activeInHierarchy;
            return gatherReady && cookReady;
        }

        private static bool IsPlayer(StackHolder agent)
        {
            return agent != null && agent.GetComponentInParent<ArcadePlayerController>() != null;
        }
    }
}
