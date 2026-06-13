using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Customer;
using Nyangsta.Economy;
using Nyangsta.Hunting;
using Nyangsta.Save;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Builds the M1 graybox loop at runtime: gather fish, cook, serve, collect cash, build.
    /// This keeps the prototype reproducible from code and avoids hand-made prefab drift.
    /// </summary>
    public class ArcadePrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;

        private Material _groundMat;
        private Material _riverMat;
        private Material _zoneMat;
        private Material _playerMat;
        private Material _fishMat;
        private Material _dishMat;
        private Material _grillMat;
        private Material _tableMat;
        private Material _moneyMat;
        private Material _customerMat;
        private Material _buildMat;
        private Material _berryMat;
        private Material _juiceMat;
        private Material _berryFieldMat;
        private Material _staffMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureArcadeBootstrap()
        {
            if (Object.FindAnyObjectByType<SaveManager>() == null) return;
            if (Object.FindAnyObjectByType<ArcadePrototypeBootstrap>() != null) return;

            var go = new GameObject("_ArcadeM1Bootstrap(Auto)");
            go.AddComponent<ArcadePrototypeBootstrap>();
        }

        private void Start()
        {
            ApplyArcadeMode();
            if (buildOnStart && FindAnyObjectByType<ArcadePlayerController>() == null)
                BuildWorld();
        }

        public void BuildWorld()
        {
            CreateMaterials();
            ConfigureCameraAndLight();

            // Save-backed unlock progress; completed zones are restored at no cost below.
            var saveData = SaveManager.Instance != null ? SaveManager.Instance.Data : new SaveData();
            var progress = new ArcadeProgressService(saveData.arcadeCompletedZones);
            var upgrades = new ArcadeUpgradeService(saveData.arcadeUpgradeLevels);

            var root = new GameObject("_ArcadeM1_World");
            var prefabs = new GameObject("_ArcadeRuntimePrefabs");
            prefabs.transform.SetParent(root.transform);

            // Raw resources + their cooked products.
            var fishPrefab = MakeStackPrefab("FishPrefab", ArcadeItemType.Fish, "item_fish", new Vector3(0.48f, 0.24f, 0.76f), prefabs.transform);
            var dishPrefab = MakeStackPrefab("GrilledFishPrefab", ArcadeItemType.GrilledFish, "item_grilledfish", new Vector3(0.52f, 0.18f, 0.72f), prefabs.transform);
            var berryPrefab = MakeStackPrefab("BerryPrefab", ArcadeItemType.Berry, "item_berry", new Vector3(0.4f, 0.4f, 0.4f), prefabs.transform);
            var juicePrefab = MakeStackPrefab("BerryJuicePrefab", ArcadeItemType.BerryJuice, "item_juice", new Vector3(0.38f, 0.5f, 0.38f), prefabs.transform);
            var moneyPrefab = MakeMoneyPrefab(prefabs.transform);
            var customerPrefab = MakeCustomerPrefab(prefabs.transform);

            // Ground keeps its box collider (the player's CharacterController stands on it)
            // but the mesh is hidden and a tiled grass sprite is laid on top.
            MakeFloor("Ground", root.transform, Vector3.zero, new Vector3(17f, 0.4f, 10f), "tile_grass", 17f, 10f, bias: -200, keepCollider: true);
            MakeFloor("River", root.transform, new Vector3(-6.6f, 0.03f, -1.2f), new Vector3(2.4f, 0.08f, 6.4f), "tile_water", 2.4f, 6.4f, bias: -150, keepCollider: false);
            MakeFloor("BerryField", root.transform, new Vector3(-6.6f, 0.04f, 3.6f), new Vector3(2.6f, 0.06f, 2.6f), "tile_berryfield", 2.6f, 2.6f, bias: -150, keepCollider: false);
            MakeFloor("KitchenFloor", root.transform, new Vector3(-0.4f, 0.05f, 0f), new Vector3(5.2f, 0.08f, 6.4f), "tile_wood", 5.2f, 6.4f, bias: -120, keepCollider: false);

            var player = MakePlayer(root.transform);
            var stack = player.GetComponent<StackHolder>();

            // ---- Fish line (active from the start) ----
            var fishGather = MakeGatherZone(root.transform, "GatherZone_Fish", "item_fish", fishPrefab, new Vector3(-6.6f, 0.08f, -1.6f));
            var grill = MakeGrill(root.transform, "Grill", dishPrefab, ArcadeItemType.Fish, new Vector3(-1.2f, 0f, -1.6f), out var grillFacility);
            MakeUpgradeZone(root.transform, "UpgradeZone_Grill", "그릴 강화", new Vector3(0.4f, 0.08f, -1.6f), grill, grillFacility, upgrades);

            // ---- Berry line (built later via a build zone) ----
            var berryGather = MakeGatherZone(root.transform, "GatherZone_Berry", "item_berry", berryPrefab, new Vector3(-6.6f, 0.08f, 3.6f));
            var juicer = MakeGrill(root.transform, "Juicer", juicePrefab, ArcadeItemType.Berry, new Vector3(-1.2f, 0f, 1.8f), out var juicerFacility);
            // The berry line is locked behind a build zone below (grouped + deactivated there).

            // ---- Tables: table 1 open, tables 2 & 3 behind build zones ----
            var tables = new List<TableZone>();
            tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, -2.4f), true));
            tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, 0f), false));
            tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, 2.4f), false));
            var tableArray = tables.ToArray();

            MakeBuildZone(root.transform, "BuildZone_Table2", 30, tables[1].transform.parent.gameObject, new Vector3(2.3f, 0.08f, 0f), "테이블 2", progress);
            MakeBuildZone(root.transform, "BuildZone_Table3", 120, tables[2].transform.parent.gameObject, new Vector3(2.3f, 0.08f, 2.4f), "테이블 3", progress);

            // Build zone that unlocks the whole berry production line at once.
            // Reparent the station/gather *roots* (not the zone children) so the
            // bodies and output slots travel with the group.
            var berryLine = new GameObject("BerryLineGroup");
            berryLine.transform.SetParent(root.transform);
            berryGather.transform.SetParent(berryLine.transform, true);
            juicer.transform.parent.SetParent(berryLine.transform, true);
            // Juicer upgrade pad lives inside the group, so it locks/unlocks with the line.
            MakeUpgradeZone(berryLine.transform, "UpgradeZone_Juicer", "주스기 강화", new Vector3(0.4f, 0.08f, 1.8f), juicer, juicerFacility, upgrades);
            berryLine.SetActive(false);
            MakeBuildZone(root.transform, "BuildZone_BerryLine", 80, berryLine, new Vector3(-3.6f, 0.08f, 3.2f), "베리 라인", progress);

            MakeCustomerSpawner(root.transform, customerPrefab, tableArray, grill, juicer);

            // ---- Hire zones: automate each production line ----
            MakeHireZone(root.transform, 150, new Vector3(-3.6f, 0.08f, -3.6f),
                fishGather, grill, tableArray, ArcadeItemType.Fish, ArcadeItemType.GrilledFish, player.transform.position, "생선 직원", progress);
            MakeHireZone(root.transform, 220, new Vector3(-3.6f, 0.08f, 4.4f),
                berryGather, juicer, tableArray, ArcadeItemType.Berry, ArcadeItemType.BerryJuice, player.transform.position, "베리 직원", progress);

            // ---- Decorative props around the edges (storybook framing) ----
            var deco = new GameObject("Decor");
            deco.transform.SetParent(root.transform);
            MakeProp(deco.transform, "tree", new Vector3(-8.0f, 0f, -4.4f), 2.6f);
            MakeProp(deco.transform, "tree", new Vector3(7.6f, 0f, -4.6f), 2.4f);
            MakeProp(deco.transform, "tree", new Vector3(8.0f, 0f, 4.6f), 2.7f);
            MakeProp(deco.transform, "bush", new Vector3(-8.2f, 0f, 1.6f), 1.0f);
            MakeProp(deco.transform, "bush", new Vector3(6.4f, 0f, 0.2f), 1.0f);
            MakeProp(deco.transform, "fence", new Vector3(0.5f, 0f, -4.7f), 1.4f);
            MakeProp(deco.transform, "fence", new Vector3(3.6f, 0f, -4.7f), 1.4f);
            // Lanterns for the cozy cabin glow (matches the concept restaurant interiors).
            MakeProp(deco.transform, "lantern", new Vector3(2.0f, 0f, -3.4f), 1.3f);
            MakeProp(deco.transform, "lantern", new Vector3(6.2f, 0f, -3.4f), 1.3f);
            MakeProp(deco.transform, "lantern", new Vector3(-3.4f, 0f, -1.0f), 1.3f);

            var hud = new GameObject("ArcadeHUD");
            var hudView = hud.AddComponent<ArcadeHUD>();
            hudView.Configure(stack);

            // Visible mobile joystick, wired into the player controller.
            var joyGo = new GameObject("ArcadeJoystick");
            var joystick = joyGo.AddComponent<ArcadeJoystick>();
            joystick.AttachVisual(hudView.OverlayLayer);
            player.GetComponent<ArcadePlayerController>().Configure(joystick);

            // Offline income: hired staff earn while away; shows the settlement popup.
            var idleGo = new GameObject("ArcadeIdleService");
            idleGo.transform.SetParent(root.transform);
            idleGo.AddComponent<ArcadeIdleService>().Configure(hudView.ModalLayer);

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.gameObject.GetComponent<ArcadeCameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<ArcadeCameraFollow>();
                follow.Configure(player.transform);
            }

            // First-session arrow guide (FTUE); skipped once completed and saved.
            if (!saveData.arcadeTutorialDone)
            {
                var tutorial = new GameObject("ArcadeTutorialDirector");
                tutorial.transform.SetParent(root.transform);
                tutorial.AddComponent<ArcadeTutorialDirector>()
                    .Configure(stack, fishGather.transform, grill.transform, tables[0].transform);
            }
        }

        private GameObject MakePlayer(Transform parent)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Nyangsta";
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(-2.8f, 1f, 0f);

            Destroy(player.GetComponent<CapsuleCollider>());
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.38f;
            cc.height = 1.75f;
            cc.center = new Vector3(0f, 0.86f, 0f);

            // Chef-cat sprite stands on the ground; capsule pivot is centered so feet are at -h/2.
            var playerBb = AttachStandingSprite(player, "player", 2.0f, footOffsetY: -0.875f, bias: 0);
            if (playerBb != null)
                playerBb.gameObject.AddComponent<SpriteMotionAnimator>().ConfigureActor(player.transform);

            player.AddComponent<ArcadePlayerController>();
            var stack = player.AddComponent<StackHolder>();
            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(player.transform);
            anchor.localPosition = new Vector3(0f, 2.0f, 0f);
            stack.Configure(anchor, 3);
            return player;
        }

        private GatherZone MakeGatherZone(Transform parent, string name, string iconKey, ArcadeStackItem itemPrefab, Vector3 pos)
        {
            var zone = MakeZone(name, parent, pos, new Vector3(1.45f, 0.05f, 1.45f), new Color(0.25f, 0.75f, 1f, 0.5f));
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(zone.transform);
            spawn.localPosition = new Vector3(0f, 0.55f, 0f);
            var gather = zone.AddComponent<GatherZone>();
            gather.Configure(itemPrefab, 0.45f, spawn);
            // Icon bubble shows what this spot yields (replaces the graybox text label).
            var bubble = WorldBubble.Create(zone.transform, new Vector3(0f, 1.35f, -0.5f), 0.8f, 0.7f);
            bubble.SetIcon(ArcadeSprites.Get(iconKey), 0.4f, new Vector2(0f, 0.06f));
            return gather;
        }

        private CookStation MakeGrill(Transform parent, string label, ArcadeStackItem dishPrefab, ArcadeItemType input, Vector3 pos, out SpriteBillboard facilitySprite)
        {
            // Body and zone share a root so the whole station can be hidden/moved
            // as one unit (e.g. while locked behind a build zone).
            var stationRoot = new GameObject($"Station_{label}");
            stationRoot.transform.SetParent(parent);
            stationRoot.transform.position = pos;

            // Facility sprite (grill or juicer) stands on the ground at the station root.
            string facilityKey = input == ArcadeItemType.Berry ? "juicer" : "grill";
            var facility = new GameObject("FacilitySprite");
            facility.transform.SetParent(stationRoot.transform, false);
            var facilityBb = AttachStandingSprite(facility, facilityKey, 1.7f, footOffsetY: 0f, bias: 0);

            var zone = MakeZone($"CookZone_{label}", stationRoot.transform, new Vector3(0f, 0.08f, -0.9f), new Vector3(1.35f, 0.05f, 1.0f), new Color(1f, 0.65f, 0.2f, 0.55f), true);
            var slots = new Transform[3];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new GameObject($"OutputSlot_{i + 1}").transform;
                slots[i].SetParent(stationRoot.transform);
                slots[i].localPosition = new Vector3(-0.45f + i * 0.45f, 1.05f, 0.45f);
            }
            var cook = zone.AddComponent<CookStation>();
            cook.Configure(input, dishPrefab, 2f, 5, slots);
            // Facility sprite shakes while a dish is actually cooking.
            if (facilityBb != null)
                facilityBb.gameObject.AddComponent<SpriteMotionAnimator>().ConfigureFacility(() => cook.IsCooking);
            // Dish icon bubble over the cook zone (replaces the graybox text label).
            string dishKey = input == ArcadeItemType.Berry ? "item_juice" : "item_grilledfish";
            var bubble = WorldBubble.Create(zone.transform, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
            bubble.SetIcon(ArcadeSprites.Get(dishKey), 0.4f, new Vector2(0f, 0.06f));
            facilitySprite = facilityBb;
            return cook;
        }

        /// <summary>
        /// Repeatable upgrade pad next to a cook station: drains gold to raise the station's
        /// level (faster cooking + bigger buffer) and swells the facility sprite a little
        /// with each level so the growth reads on the map. Level is save-backed.
        /// </summary>
        private void MakeUpgradeZone(Transform parent, string zoneId, string displayName, Vector3 pos,
            CookStation cook, SpriteBillboard facility, ArcadeUpgradeService upgrades)
        {
            var zone = MakeZone(zoneId, parent, pos, new Vector3(1.0f, 0.05f, 1.0f), new Color(0.45f, 0.8f, 1f, 0.6f));
            AttachPad(zone, "pad_build", 1.5f);   // reuse the build pad art (no upgrade-specific sprite yet)

            var bubble = WorldBubble.Create(zone.transform, new Vector3(0f, 1.25f, -0.1f), 1.5f, 0.95f);
            bubble.SetIcon(ArcadeSprites.Get("money"), 0.26f, new Vector2(-0.38f, -0.08f));
            bubble.SetValue("", 28, new Vector2(0.10f, -0.08f));   // position the cost text by the coin

            Vector3 baseScale = facility != null ? facility.transform.localScale : Vector3.one;
            var up = zone.AddComponent<UpgradeZone>();
            up.Configure(displayName, 40, 1.6, 5, 6, bubble, level =>
            {
                cook.SetUpgradeLevel(level);
                if (facility != null) facility.transform.localScale = baseScale * (1f + 0.06f * level);
            });
            up.BindProgress(upgrades, zoneId);
            up.RestoreLevel();
        }

        private TableZone MakeTable(Transform parent, MoneyPile moneyPrefab, Vector3 pos, bool active)
        {
            var tableRoot = new GameObject("Table");
            tableRoot.transform.SetParent(parent);
            tableRoot.transform.position = pos;

            // Round table sprite stands on the ground at the table root.
            var tableSprite = new GameObject("TableSprite");
            tableSprite.transform.SetParent(tableRoot.transform, false);
            AttachStandingSprite(tableSprite, "table", 1.05f, footOffsetY: 0f, bias: 0);

            var zone = MakeZone("ServeZone", tableRoot.transform, new Vector3(0f, 0.08f, -0.88f), new Vector3(1.35f, 0.05f, 0.95f), new Color(0.55f, 1f, 0.45f, 0.55f), true);
            var seat = new GameObject("SeatPoint").transform;
            seat.SetParent(tableRoot.transform);
            seat.localPosition = new Vector3(0f, 0.95f, 0.82f);
            var money = new GameObject("MoneySpawnPoint").transform;
            money.SetParent(tableRoot.transform);
            money.localPosition = new Vector3(0.52f, 0.62f, -0.12f);

            var table = zone.AddComponent<TableZone>();
            table.Configure(seat, money, moneyPrefab);
            var bubble = WorldBubble.Create(tableRoot.transform, new Vector3(0f, 0.9f, -1.35f), 0.95f, 0.62f);
            bubble.SetTitle("테이블", 28, new Vector2(0f, 0.05f));

            tableRoot.SetActive(active);
            return table;
        }

        private void MakeBuildZone(Transform parent, string name, double cost, GameObject target, Vector3 pos, string displayName, ArcadeProgressService progress)
        {
            var zone = MakeZone(name, parent, pos, new Vector3(1.15f, 0.05f, 1.15f), new Color(1f, 0.9f, 0.25f, 0.62f));
            AttachPad(zone, "pad_build", 1.7f);
            var bubble = MakeCostBubble(zone.transform, displayName, cost);
            var build = zone.AddComponent<BuildZone>();
            build.Configure(cost, 5, 0.08f, target, bubble, displayName);

            // "Map expands" reveal: capture the target's full scale now (it may be
            // inactive, so Awake won't run) and let the build zone play/snap it.
            if (target != null)
            {
                var reveal = target.GetComponent<MapReveal>();
                if (reveal == null) reveal = target.AddComponent<MapReveal>();
                reveal.Init(target.transform.localScale);
                build.SetReveal(reveal);
            }

            build.BindProgress(progress, name);
            if (progress.IsComplete(name)) build.RestoreCompleted();
        }

        /// <summary>
        /// Bubble for build/hire pads: facility name on top, coin icon + remaining cost
        /// below. The zone scripts refresh the value text while gold drains.
        /// </summary>
        private WorldBubble MakeCostBubble(Transform zone, string displayName, double cost)
        {
            var bubble = WorldBubble.Create(zone, new Vector3(0f, 1.3f, -0.1f), 1.5f, 0.95f);
            bubble.SetTitle(displayName, 30, new Vector2(0f, 0.24f));
            bubble.SetIcon(ArcadeSprites.Get("money"), 0.26f, new Vector2(-0.38f, -0.08f));
            bubble.SetValue($"{cost:N0}", 30, new Vector2(0.10f, -0.08f));
            return bubble;
        }

        /// <summary>
        /// Hide a zone cylinder's mesh and lay a glowing pad sprite flat on the ground.
        /// The pad is parented to the zone's PARENT (a sibling of the cylinder) so the
        /// cylinder's flat non-uniform scale doesn't squash it; it still moves/hides with
        /// the zone's group because it shares the same parent transform.
        /// </summary>
        private void AttachPad(GameObject zone, string key, float worldSize)
        {
            HideMesh(zone);
            var sprite = ArcadeSprites.Get(key);
            if (sprite == null) return;

            var go = new GameObject($"Pad_{key}");
            go.transform.SetParent(zone.transform.parent, false);
            go.transform.position = new Vector3(zone.transform.position.x, 0.06f, zone.transform.position.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;
            float w = sprite.bounds.size.x;
            float s = w > 0f ? worldSize / w : 1f;
            go.transform.localScale = new Vector3(s, s, s);
        }

        private void MakeHireZone(
            Transform parent,
            double cost,
            Vector3 pos,
            GatherZone gather,
            CookStation cook,
            TableZone[] tables,
            ArcadeItemType raw,
            ArcadeItemType cooked,
            Vector3 staffSpawn,
            string displayName,
            ArcadeProgressService progress)
        {
            string zoneId = $"HireZone_{cooked}";
            var zone = MakeZone(zoneId, parent, pos, new Vector3(1.15f, 0.05f, 1.15f), new Color(0.4f, 0.7f, 1f, 0.62f));
            AttachPad(zone, "pad_hire", 1.7f);
            var bubble = MakeCostBubble(zone.transform, displayName, cost);

            var spawn = new GameObject("StaffSpawn").transform;
            spawn.SetParent(zone.transform);
            spawn.position = staffSpawn;

            var hire = zone.AddComponent<HireZone>();
            hire.Configure(cost, 8, bubble, gather, cook, tables, raw, cooked, spawn, displayName);
            hire.BindProgress(progress, zoneId);
            if (progress.IsComplete(zoneId)) hire.RestoreCompleted();
        }

        private void MakeCustomerSpawner(
            Transform parent,
            ArcadeCustomer prefab,
            TableZone[] tables,
            CookStation fishStation,
            CookStation berryStation)
        {
            var spawner = new GameObject("CustomerSpawner");
            spawner.transform.SetParent(parent);

            var spawn = new GameObject("Entrance").transform;
            spawn.SetParent(spawner.transform);
            spawn.position = new Vector3(7f, 1f, -3.6f);

            var exit = new GameObject("Exit").transform;
            exit.SetParent(spawner.transform);
            exit.position = new Vector3(7f, 1f, 3.6f);

            // Juice only sells once the berry cook station is live (gate on the station).
            var menu = new List<ArcadeCustomerSpawner.MenuOption>
            {
                new() { item = ArcadeItemType.GrilledFish, pay = 10, gate = fishStation },
                new() { item = ArcadeItemType.BerryJuice, pay = 16, gate = berryStation },
            };

            spawner.AddComponent<ArcadeCustomerSpawner>().Configure(prefab, tables, spawn, exit, 4.5f, menu);
        }

        private ArcadeStackItem MakeStackPrefab(string name, ArcadeItemType type, string spriteKey, Vector3 scale, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localScale = scale;
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;

            // Carried item sprite (billboard, rides the stack above the actor's head).
            AttachItemSprite(go, spriteKey, 0.55f);

            var item = go.AddComponent<ArcadeStackItem>();
            item.type = type;
            go.SetActive(false);
            return item;
        }

        private MoneyPile MakeMoneyPrefab(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MoneyPilePrefab";
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(0.58f, 0.12f, 0.42f);
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;

            AttachItemSprite(go, "money", 0.6f);

            var pile = go.AddComponent<MoneyPile>();
            var label = MakeLabel("+10G", go.transform, new Vector3(0f, 0.72f, 0f));
            label.fontSize = 48;
            label.characterSize = 0.04f;
            label.color = new Color(0.08f, 0.52f, 0.16f);
            label.transform.localScale = InverseScale(go.transform.localScale);
            pile.Amount = 10;
            pile.SetLabel(label);
            go.SetActive(false);
            return pile;
        }

        /// <summary>Hide a small prefab cube's mesh and ride a center-pivot sprite billboard on it.</summary>
        private void AttachItemSprite(GameObject host, string key, float worldHeight)
        {
            HideMesh(host);
            var sprite = ArcadeSprites.Get(key);
            if (sprite == null) return;

            var go = new GameObject($"Sprite_{key}");
            go.transform.SetParent(host.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            // Counter the host's non-uniform cube scale so the sprite stays square.
            Vector3 hs = host.transform.localScale;
            float h = sprite.bounds.size.y;
            float s = h > 0f ? worldHeight / h : 1f;
            go.transform.localScale = new Vector3(
                s / Mathf.Max(0.0001f, hs.x),
                s / Mathf.Max(0.0001f, hs.y),
                s / Mathf.Max(0.0001f, hs.z));
            go.AddComponent<SpriteBillboard>().Init(true, true, 50);
        }

        private ArcadeCustomer MakeCustomerPrefab(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "CustomerPrefab";
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            var col = go.GetComponent<CapsuleCollider>();
            col.isTrigger = true;

            // Body sprite billboard (the spawner swaps the illustration per customer).
            HideMesh(go);
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, -1.0f, 0f); // feet at capsule base
            var bodySr = bodyGo.AddComponent<SpriteRenderer>();
            bodySr.sprite = ArcadeSprites.GetGrounded("cust_rabbit");
            if (bodySr.sprite != null) ScaleToHeight(bodySr, 1.5f);
            bodyGo.AddComponent<SpriteBillboard>().Init(true, true, 0);
            // Walk bounce/lean; the source defaults to the parent capsule on the clone.
            bodyGo.AddComponent<SpriteMotionAnimator>();

            // Order bubble (food icon + count) floats above the head; the customer
            // script swaps its contents per state and drives the patience ring.
            var bubble = WorldBubble.Create(go.transform, new Vector3(0f, 1.55f, 0f), 0.95f, 0.8f);
            var customer = go.AddComponent<ArcadeCustomer>();
            customer.Configure(ArcadeItemType.GrilledFish, 1, 10, 30f);
            customer.SetBubble(bubble);
            customer.SetBodySprite(bodySr);

            go.SetActive(false);
            return customer;
        }

        private GameObject MakeZone(string name, Transform parent, Vector3 pos, Vector3 scale, Color color, bool local = false)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zone.name = name;
            zone.transform.SetParent(parent);
            if (local) zone.transform.localPosition = pos;
            else zone.transform.position = pos;
            zone.transform.localScale = scale;
            var renderer = zone.GetComponent<Renderer>();
            renderer.material = MaterialWithColor(name + "_Mat", color);
            var collider = zone.GetComponent<Collider>();
            collider.isTrigger = true;
            return zone;
        }

        private GameObject MakeCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool local = false)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            if (local) cube.transform.localPosition = pos;
            else cube.transform.position = pos;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = mat;
            return cube;
        }

        /// <summary>
        /// A floor patch. An invisible box provides the collider (kept only for the main
        /// ground, so the player's CharacterController can stand on it); a tiled sprite is
        /// laid flat on top as a SIBLING (not a child) to avoid the box's non-uniform scale.
        /// </summary>
        private GameObject MakeFloor(string name, Transform parent, Vector3 pos, Vector3 scale, string tileKey, float worldW, float worldH, int bias, bool keepCollider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = pos;
            box.transform.localScale = scale;

            var mr = box.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            if (!keepCollider)
            {
                var col = box.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var sprite = ArcadeSprites.GetTile(tileKey);
            if (sprite != null)
            {
                var go = new GameObject($"Tiles_{name}");
                go.transform.SetParent(parent, false);
                float topY = pos.y + scale.y * 0.5f + 0.01f + bias * 0.0001f; // tiny Y offset avoids z-fight
                go.transform.position = new Vector3(pos.x, topY, pos.z);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.sortingOrder = bias;
                sr.size = new Vector2(worldW, worldH); // world-unit footprint (scale stays 1)
            }
            return box;
        }

        private TextMesh MakeLabel(string text, Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 56;
            label.characterSize = 0.045f;
            label.color = Color.black;
            return label;
        }

        // ---- 2.5D sprite helpers ----

        /// <summary>
        /// Hide a host's mesh and attach a standing sprite billboard sized to worldHeight.
        /// The sprite's feet sit at the host pivot + footOffsetY (use a negative offset when
        /// the host pivot is at its center, e.g. a capsule). Collider stays for the 3D sim.
        /// </summary>
        private SpriteBillboard AttachStandingSprite(GameObject host, string key, float worldHeight, float footOffsetY = 0f, int bias = 0)
        {
            HideMesh(host);
            var sprite = ArcadeSprites.GetGrounded(key);
            if (sprite == null) return null;

            var go = new GameObject($"Sprite_{key}");
            go.transform.SetParent(host.transform, false);
            go.transform.localPosition = new Vector3(0f, footOffsetY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            ScaleToHeight(sr, worldHeight);

            var bb = go.AddComponent<SpriteBillboard>();
            bb.Init(true, true, bias);
            return bb;
        }

        /// <summary>
        /// Attach a flat ground sprite that lies on the XZ plane. When worldW/worldH are
        /// given it draws Tiled at exactly that world size (repeating the texture); the
        /// host's mesh is hidden but its collider stays.
        /// </summary>
        private SpriteRenderer AttachGroundSprite(GameObject host, string key, float worldW, float worldH, int bias)
        {
            HideMesh(host);
            var sprite = ArcadeSprites.Get(key);
            if (sprite == null) return null;

            var go = new GameObject($"Ground_{key}");
            go.transform.SetParent(host.transform, false);
            go.transform.localPosition = new Vector3(0f, HostTopLocalY(host) + 0.01f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lay flat on XZ

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            // World scale is driven entirely by sr.size; keep transform scale = 1.
            sr.size = new Vector2(worldW, worldH);
            sr.sortingOrder = bias;
            return sr;
        }

        // Local-space Y of the host's top surface (so ground sprites sit just above it).
        private static float HostTopLocalY(GameObject host)
        {
            return host.transform.localScale.y * 0.5f;
        }

        private static Vector3 InverseScale(Vector3 scale)
        {
            return new Vector3(
                1f / Mathf.Max(0.0001f, scale.x),
                1f / Mathf.Max(0.0001f, scale.y),
                1f / Mathf.Max(0.0001f, scale.z));
        }

        private static void HideMesh(GameObject host)
        {
            var mr = host.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        /// <summary>A purely decorative standing sprite (no collider), e.g. tree/bush/fence.</summary>
        private void MakeProp(Transform parent, string key, Vector3 groundPos, float worldHeight)
        {
            var sprite = ArcadeSprites.GetGrounded(key);
            if (sprite == null) return;

            var go = new GameObject($"Prop_{key}");
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos; // feet on the ground (bottom-pivot sprite)

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            ScaleToHeight(sr, worldHeight);
            go.AddComponent<SpriteBillboard>().Init(true, true, 0);
        }

        private static void ScaleToHeight(SpriteRenderer sr, float worldHeight)
        {
            float h = sr.sprite.bounds.size.y;
            if (h <= 0f) return;
            float s = worldHeight / h;
            sr.transform.localScale = new Vector3(s, s, s);
        }

        private void ConfigureCameraAndLight()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6.2f;                 // frame the wider 2.5D world
                cam.transform.position = new Vector3(-1.0f, 9.0f, -8.8f);
                cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.69f, 0.86f, 0.95f); // soft sky
                // Sort transparent sprites along the camera's view direction so any
                // billboards sharing a sortingOrder still resolve front-to-back.
                cam.transparencySortMode = TransparencySortMode.CustomAxis;
                cam.transparencySortAxis = new Vector3(0f, 1f, -1f).normalized;
            }

            var light = FindAnyObjectByType<Light>();
            if (light == null)
            {
                var go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
            }
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = new Color(1f, 0.98f, 0.92f);
            light.shadows = LightShadows.None;              // flat storybook lighting
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void CreateMaterials()
        {
            _groundMat = MaterialWithColor("M1_Ground", new Color(0.54f, 0.78f, 0.44f));
            _riverMat = MaterialWithColor("M1_River", new Color(0.2f, 0.55f, 0.95f));
            _zoneMat = MaterialWithColor("M1_Zone", new Color(0.85f, 0.95f, 1f, 0.5f));
            _playerMat = MaterialWithColor("M1_Player", new Color(1f, 0.55f, 0.24f));
            _fishMat = MaterialWithColor("M1_Fish", new Color(0.1f, 0.55f, 1f));
            _dishMat = MaterialWithColor("M1_Dish", new Color(1f, 0.62f, 0.16f));
            _grillMat = MaterialWithColor("M1_Grill", new Color(0.25f, 0.25f, 0.28f));
            _tableMat = MaterialWithColor("M1_Table", new Color(0.78f, 0.48f, 0.28f));
            _moneyMat = MaterialWithColor("M1_Money", new Color(0.12f, 0.78f, 0.28f));
            _customerMat = MaterialWithColor("M1_Customer", new Color(1f, 0.92f, 0.28f));
            _buildMat = MaterialWithColor("M1_Build", new Color(1f, 0.9f, 0.2f));
            _berryMat = MaterialWithColor("M1_Berry", new Color(0.78f, 0.16f, 0.42f));
            _juiceMat = MaterialWithColor("M1_Juice", new Color(0.92f, 0.32f, 0.6f));
            _berryFieldMat = MaterialWithColor("M1_BerryField", new Color(0.36f, 0.6f, 0.32f));
            _staffMat = MaterialWithColor("M1_Staff", new Color(0.45f, 0.85f, 1f));
        }

        private static void ApplyArcadeMode()
        {
            foreach (var ui in Object.FindObjectsByType<GameUIController>(FindObjectsInactive.Exclude))
                ui.gameObject.SetActive(false);
            foreach (var view in Object.FindObjectsByType<RestaurantView>(FindObjectsInactive.Exclude))
                view.gameObject.SetActive(false);
            foreach (var debug in Object.FindObjectsByType<DebugTester>(FindObjectsInactive.Exclude))
                debug.enabled = false;
            foreach (var customer in Object.FindObjectsByType<CustomerManager>(FindObjectsInactive.Exclude))
                customer.enabled = false;
            foreach (var hunt in Object.FindObjectsByType<HuntingManager>(FindObjectsInactive.Exclude))
                hunt.enabled = false;
            foreach (var idle in Object.FindObjectsByType<IdleIncomeManager>(FindObjectsInactive.Exclude))
                idle.enabled = false;
        }

        private static Material MaterialWithColor(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            return material;
        }
    }
}
