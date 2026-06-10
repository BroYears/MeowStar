using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Stand here with gold to hire a staff member. Drains gold like BuildZone, then
    /// spawns a StaffAgent wired to the same gather/cook/serve loop the player runs.
    /// </summary>
    public class HireZone : InteractionZone
    {
        [SerializeField] private double totalCost = 150;
        [SerializeField] private double drainPerTick = 8;
        [SerializeField] private float tickInterval = 0.1f;
        [SerializeField] private TextMesh costLabel;

        private GatherZone _gatherZone;
        private CookStation _cookStation;
        private TableZone[] _tables;
        private ArcadeItemType _rawType;
        private ArcadeItemType _cookedType;
        private Transform _spawnPoint;

        private double _paid;
        private float _timer;
        private bool _hired;

        public void Configure(
            double cost,
            double tick,
            TextMesh label,
            GatherZone gather,
            CookStation cook,
            TableZone[] tables,
            ArcadeItemType raw,
            ArcadeItemType cooked,
            Transform spawnPoint)
        {
            totalCost = System.Math.Max(1, cost);
            drainPerTick = System.Math.Max(1, tick);
            costLabel = label;
            _gatherZone = gather;
            _cookStation = cook;
            _tables = tables;
            _rawType = raw;
            _cookedType = cooked;
            _spawnPoint = spawnPoint;
            UpdateLabel();
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_hired || _paid >= totalCost) return;

            _timer += dt;
            if (_timer < tickInterval) return;
            _timer = 0f;

            double remaining = totalCost - _paid;
            double tick = System.Math.Min(drainPerTick, remaining);
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.TrySpendGold(tick)) return;

            _paid += tick;
            UpdateLabel();
            if (_paid >= totalCost) Hire();
        }

        private void Hire()
        {
            _hired = true;
            SpawnStaff();
            gameObject.SetActive(false);
        }

        private void SpawnStaff()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Staff_Nyangsta";
            go.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);

            Vector3 pos = _spawnPoint != null ? _spawnPoint.position : transform.position;
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
            agent.Configure(_gatherZone, _cookStation, _tables, _rawType, _cookedType);
        }

        private void UpdateLabel()
        {
            if (costLabel != null)
                costLabel.text = $"HIRE\n{System.Math.Max(0, totalCost - _paid):N0}G";
        }
    }
}
