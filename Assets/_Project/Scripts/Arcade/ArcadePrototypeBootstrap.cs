using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // Only bootstrap when in Arcade scenes
            if (!sceneName.Contains("Arcade")) return;

            if (Object.FindAnyObjectByType<SaveManager>() == null) return;
            // Exit early if the developer added an ArcadeSceneManager (Option A) to run their pre-designed scene.
            if (Object.FindAnyObjectByType<ArcadeSceneManager>() != null) return;
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
            var mushroomPrefab = MakeStackPrefab("MushroomPrefab", ArcadeItemType.Mushroom, "item_mushroom", new Vector3(0.45f, 0.45f, 0.45f), prefabs.transform);
            var soupPrefab = MakeStackPrefab("MushroomSoupPrefab", ArcadeItemType.MushroomSkewer, "item_soup", new Vector3(0.48f, 0.48f, 0.48f), prefabs.transform);
            var moneyPrefab = MakeMoneyPrefab(prefabs.transform);
            var customerPrefab = MakeCustomerPrefab(prefabs.transform);

            // Detect current scene for layout routing
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool buildForest = sceneName.Contains("ArcadeForest");
            bool buildRestaurant = sceneName.Contains("ArcadeRestaurant");

            if (!buildForest && !buildRestaurant)
            {
                // Fallback to single unified scene for testing if loaded from custom editor scenes
                buildForest = true;
                buildRestaurant = true;
            }

            // Ground keeps its box collider (the player's CharacterController stands on it)
            // but the mesh is hidden. Expanded Z to 12f for a more spacious visual layout.
            MakeFloor("Ground", root.transform, Vector3.zero, new Vector3(17f, 0.4f, 12f), "", 17f, 12f, bias: -200, keepCollider: true);

            var player = MakePlayer(root.transform);
            var stack = player.GetComponent<StackHolder>();

            // Setup player spawn position based on scene
            if (buildForest && !buildRestaurant)
            {
                player.transform.position = new Vector3(-2.8f, 1f, 0f);
            }
            else if (buildRestaurant && !buildForest)
            {
                // Spawn near the forest-to-restaurant gateway on the left
                player.transform.position = new Vector3(-6.5f, 1f, 0f);
            }

            // Restore stack contents if any were preserved during transition
            if (ArcadeStackPreserver.HasSavedStack())
            {
                ArcadeStackPreserver.RestoreStack(stack, fishPrefab, berryPrefab, mushroomPrefab, dishPrefab, juicePrefab, soupPrefab);
            }

            // --- Forest / Outdoor Scene Layout ---
            if (buildForest)
            {
                if (buildRestaurant)
                {
                    // Unified single scene floor layouts
                    MakeFloor("OutsideFloor", root.transform, new Vector3(-6.05f, 0f, 0f), new Vector3(4.9f, 0.4f, 12f), "tile_grass", 4.9f, 12f, bias: -200, keepCollider: false);
                }
                else
                {
                    // Full Forest scene grass layout
                    MakeFloor("OutsideFloor", root.transform, Vector3.zero, new Vector3(17f, 0.4f, 12f), "tile_grass", 17f, 12f, bias: -200, keepCollider: false);
                }

                // Three outdoor gathering floors stacked vertically along the Z axis
                MakeFloor("River", root.transform, new Vector3(-6.6f, 0.03f, -3.2f), new Vector3(2.4f, 0.08f, 4.4f), "tile_water", 2.4f, 4.4f, bias: -150, keepCollider: false);
                MakeFloor("BerryField", root.transform, new Vector3(-6.6f, 0.04f, 0.8f), new Vector3(2.6f, 0.06f, 2.6f), "tile_berryfield", 2.6f, 2.6f, bias: -150, keepCollider: false);
                MakeFloor("CaveFloor", root.transform, new Vector3(-6.6f, 0.05f, 4.2f), new Vector3(2.6f, 0.06f, 2.6f), "tile_cave", 2.6f, 2.6f, bias: -150, keepCollider: false);

                var fishGather = MakeGatherZone(root.transform, "GatherZone_Fish", "item_fish", fishPrefab, new Vector3(-6.6f, 0.08f, -3.2f));
                var berryGather = MakeGatherZone(root.transform, "GatherZone_Berry", "item_berry", berryPrefab, new Vector3(-6.6f, 0.08f, 0.8f));
                var mushroomGather = MakeGatherZone(root.transform, "GatherZone_Mushroom", "item_mushroom", mushroomPrefab, new Vector3(-6.6f, 0.08f, 4.2f));

                if (!buildRestaurant)
                {
                    // Split Forest Scene: Create 3 Senders DropBox to store harvested resources
                    
                    // Fish DropBox Sender
                    var boxFishGo = new GameObject("DropBox_Fish");
                    boxFishGo.transform.SetParent(root.transform);
                    boxFishGo.transform.position = new Vector3(-1.2f, 0f, -2.4f);
                    AttachStandingSprite(boxFishGo, "counter", 1.2f);
                    var boxFishZone = MakeZone("DropBoxZone_Fish", boxFishGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.1f, 0.8f, 0.8f, 0.5f), true);
                    AttachGlowPad(boxFishZone, new Color(0.1f, 0.8f, 0.8f, 0.35f), 1.3f);
                    var dropBoxFish = boxFishZone.AddComponent<ArcadeDropBox>();
                    dropBoxFish.Configure(false, ArcadeItemType.Fish, fishPrefab);
                    var bubbleFish = WorldBubble.Create(boxFishZone.transform, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleFish.SetIcon(ArcadeSprites.Get("item_fish"), 0.4f, new Vector2(0f, 0.06f));

                    // Berry DropBox Sender
                    var boxBerryGo = new GameObject("DropBox_Berry");
                    boxBerryGo.transform.SetParent(root.transform);
                    boxBerryGo.transform.position = new Vector3(-1.2f, 0f, 0.8f);
                    AttachStandingSprite(boxBerryGo, "counter", 1.2f);
                    var boxBerryZone = MakeZone("DropBoxZone_Berry", boxBerryGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.1f, 0.8f, 0.8f, 0.5f), true);
                    AttachGlowPad(boxBerryZone, new Color(0.1f, 0.8f, 0.8f, 0.35f), 1.3f);
                    var dropBoxBerry = boxBerryZone.AddComponent<ArcadeDropBox>();
                    dropBoxBerry.Configure(false, ArcadeItemType.Berry, berryPrefab);
                    var bubbleBerry = WorldBubble.Create(boxBerryZone.transform, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleBerry.SetIcon(ArcadeSprites.Get("item_berry"), 0.4f, new Vector2(0f, 0.06f));

                    // Mushroom DropBox Sender
                    var boxMushroomGo = new GameObject("DropBox_Mushroom");
                    boxMushroomGo.transform.SetParent(root.transform);
                    boxMushroomGo.transform.position = new Vector3(-1.2f, 0f, 4.2f);
                    AttachStandingSprite(boxMushroomGo, "counter", 1.2f);
                    var boxMushroomZone = MakeZone("DropBoxZone_Mushroom", boxMushroomGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.1f, 0.8f, 0.8f, 0.5f), true);
                    AttachGlowPad(boxMushroomZone, new Color(0.1f, 0.8f, 0.8f, 0.35f), 1.3f);
                    var dropBoxMushroom = boxMushroomZone.AddComponent<ArcadeDropBox>();
                    dropBoxMushroom.Configure(false, ArcadeItemType.Mushroom, mushroomPrefab);
                    var bubbleMushroom = WorldBubble.Create(boxMushroomZone.transform, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleMushroom.SetIcon(ArcadeSprites.Get("item_mushroom"), 0.4f, new Vector2(0f, 0.06f));

                    // Scene Gate to Restaurant (on the right)
                    var gateGo = new GameObject("SceneGate_ToRestaurant");
                    gateGo.transform.SetParent(root.transform);
                    gateGo.transform.position = new Vector3(7.5f, 0f, 0f);
                    var gateCol = gateGo.AddComponent<BoxCollider>();
                    gateCol.size = new Vector3(1.0f, 2.0f, 4.0f);
                    var gate = gateGo.AddComponent<ArcadeSceneGate>();
                    gate.ConfigureGate("ArcadeRestaurant", 1.5f);
                    AttachStandingSprite(gateGo, "fireplace", 2.2f);
                    var gateLabel = MakeLabel("식당 입장", gateGo.transform, new Vector3(0f, 2.2f, -0.5f));
                    gateLabel.fontSize = 32;

                    // Hire Zones for Forest: automates gathering items and dropping them off to DropBox Senders
                    MakeHireZone(root.transform, 150, new Vector3(-3.6f, 0.08f, -2.4f),
                        fishGather.transform, boxFishZone.transform, null, ArcadeItemType.Fish, ArcadeItemType.None, player.transform.position, "생선 채집원", progress);

                    // Berry automation group
                    var berryLine = new GameObject("BerryLineGroup");
                    berryLine.transform.SetParent(root.transform);
                    berryGather.transform.SetParent(berryLine.transform, true);
                    boxBerryGo.transform.SetParent(berryLine.transform, true);
                    berryLine.SetActive(false);
                    MakeBuildZone(root.transform, "BuildZone_BerryLine", 80, berryLine, new Vector3(-3.6f, 0.08f, 0.8f), "베리 라인", progress);

                    MakeHireZone(root.transform, 220, new Vector3(-3.6f, 0.08f, 0.0f),
                        berryGather.transform, boxBerryZone.transform, null, ArcadeItemType.Berry, ArcadeItemType.None, player.transform.position, "베리 채집원", progress);

                    // Mushroom automation group
                    var mushroomLine = new GameObject("MushroomLineGroup");
                    mushroomLine.transform.SetParent(root.transform);
                    mushroomGather.transform.SetParent(mushroomLine.transform, true);
                    boxMushroomGo.transform.SetParent(mushroomLine.transform, true);
                    mushroomLine.SetActive(false);
                    MakeBuildZone(root.transform, "BuildZone_MushroomLine", 180, mushroomLine, new Vector3(-3.6f, 0.08f, 3.8f), "가마솥 라인", progress);

                    MakeHireZone(root.transform, 280, new Vector3(-3.6f, 0.08f, 2.4f),
                        mushroomGather.transform, boxMushroomZone.transform, null, ArcadeItemType.Mushroom, ArcadeItemType.None, player.transform.position, "버섯 채집원", progress);
                }
            }

            // --- Restaurant / Indoor Scene Layout ---
            if (buildRestaurant)
            {
                if (buildForest)
                {
                    // Unified single scene floor layouts
                    MakeFloor("InsideFloor", root.transform, new Vector3(2.45f, 0f, 0f), new Vector3(12.1f, 0.4f, 12f), "tile_wood", 12.1f, 12f, bias: -200, keepCollider: false);
                }
                else
                {
                    // Full Restaurant scene wood layout
                    MakeFloor("InsideFloor", root.transform, Vector3.zero, new Vector3(17f, 0.4f, 12f), "tile_wood", 17f, 12f, bias: -200, keepCollider: false);
                }

                // Split Restaurant Scene: Create 3 Receivers DropBox to pull harvested resources
                GameObject boxFishRecvGo = null, boxBerryRecvGo = null, boxMushroomRecvGo = null;
                Transform boxFishRecvZone = null, boxBerryRecvZone = null, boxMushroomRecvZone = null;

                if (!buildForest)
                {
                    // Fish DropBox Receiver
                    boxFishRecvGo = new GameObject("DropBox_Fish_Recv");
                    boxFishRecvGo.transform.SetParent(root.transform);
                    boxFishRecvGo.transform.position = new Vector3(-6.6f, 0f, -3.2f);
                    AttachStandingSprite(boxFishRecvGo, "counter", 1.2f);
                    boxFishRecvZone = MakeZone("DropBoxZone_Fish_Recv", boxFishRecvGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.8f, 0.8f, 0.1f, 0.5f), true).transform;
                    AttachGlowPad(boxFishRecvZone.gameObject, new Color(0.8f, 0.8f, 0.1f, 0.35f), 1.3f);
                    var dropBoxFishRecv = boxFishRecvZone.gameObject.AddComponent<ArcadeDropBox>();
                    dropBoxFishRecv.Configure(true, ArcadeItemType.Fish, fishPrefab);
                    var bubbleFishRecv = WorldBubble.Create(boxFishRecvZone, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleFishRecv.SetIcon(ArcadeSprites.Get("item_fish"), 0.4f, new Vector2(0f, 0.06f));

                    // Berry DropBox Receiver
                    boxBerryRecvGo = new GameObject("DropBox_Berry_Recv");
                    boxBerryRecvGo.transform.SetParent(root.transform);
                    boxBerryRecvGo.transform.position = new Vector3(-6.6f, 0f, 0.8f);
                    AttachStandingSprite(boxBerryRecvGo, "counter", 1.2f);
                    boxBerryRecvZone = MakeZone("DropBoxZone_Berry_Recv", boxBerryRecvGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.8f, 0.8f, 0.1f, 0.5f), true).transform;
                    AttachGlowPad(boxBerryRecvZone.gameObject, new Color(0.8f, 0.8f, 0.1f, 0.35f), 1.3f);
                    var dropBoxBerryRecv = boxBerryRecvZone.gameObject.AddComponent<ArcadeDropBox>();
                    dropBoxBerryRecv.Configure(true, ArcadeItemType.Berry, berryPrefab);
                    var bubbleBerryRecv = WorldBubble.Create(boxBerryRecvZone, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleBerryRecv.SetIcon(ArcadeSprites.Get("item_berry"), 0.4f, new Vector2(0f, 0.06f));

                    // Mushroom DropBox Receiver
                    boxMushroomRecvGo = new GameObject("DropBox_Mushroom_Recv");
                    boxMushroomRecvGo.transform.SetParent(root.transform);
                    boxMushroomRecvGo.transform.position = new Vector3(-6.6f, 0f, 4.2f);
                    AttachStandingSprite(boxMushroomRecvGo, "counter", 1.2f);
                    boxMushroomRecvZone = MakeZone("DropBoxZone_Mushroom_Recv", boxMushroomRecvGo.transform, new Vector3(0f, 0.08f, -0.6f), new Vector3(1.2f, 0.05f, 0.8f), new Color(0.8f, 0.8f, 0.1f, 0.5f), true).transform;
                    AttachGlowPad(boxMushroomRecvZone.gameObject, new Color(0.8f, 0.8f, 0.1f, 0.35f), 1.3f);
                    var dropBoxMushroomRecv = boxMushroomRecvZone.gameObject.AddComponent<ArcadeDropBox>();
                    dropBoxMushroomRecv.Configure(true, ArcadeItemType.Mushroom, mushroomPrefab);
                    var bubbleMushroomRecv = WorldBubble.Create(boxMushroomRecvZone, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
                    bubbleMushroomRecv.SetIcon(ArcadeSprites.Get("item_mushroom"), 0.4f, new Vector2(0f, 0.06f));
                }

                // Kitchen and Cooking stations (Always exist in Restaurant)
                var grill = MakeGrill(root.transform, "Grill", dishPrefab, ArcadeItemType.Fish, new Vector3(-1.2f, 0f, -2.4f), out var grillFacility);
                MakeUpgradeZone(root.transform, "UpgradeZone_Grill", "그릴 강화", new Vector3(0.4f, 0.08f, -2.4f), grill, grillFacility, upgrades);

                var juicer = MakeGrill(root.transform, "Juicer", juicePrefab, ArcadeItemType.Berry, new Vector3(-1.2f, 0f, 0.8f), out var juicerFacility);
                var juicerUpgradeZone = new GameObject("UpgradeZone_Juicer_Holder");
                juicerUpgradeZone.transform.SetParent(root.transform);
                MakeUpgradeZone(juicerUpgradeZone.transform, "UpgradeZone_Juicer", "주스기 강화", new Vector3(0.4f, 0.08f, 0.8f), juicer, juicerFacility, upgrades);

                var soupPot = MakeGrill(root.transform, "SoupPot", soupPrefab, ArcadeItemType.Mushroom, new Vector3(-1.2f, 0f, 4.2f), out var soupPotFacility);
                var soupPotUpgradeZone = new GameObject("UpgradeZone_SoupPot_Holder");
                soupPotUpgradeZone.transform.SetParent(root.transform);
                MakeUpgradeZone(soupPotUpgradeZone.transform, "UpgradeZone_SoupPot", "가마솥 강화", new Vector3(0.4f, 0.08f, 4.2f), soupPot, soupPotFacility, upgrades);

                // Tables
                var tables = new List<TableZone>();
                tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, -2.4f), true));
                tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, 0f), false));
                tables.Add(MakeTable(root.transform, moneyPrefab, new Vector3(4.4f, 0f, 2.4f), false));
                var tableArray = tables.ToArray();

                var carpet1 = new GameObject("Carpet_1"); carpet1.transform.SetParent(root.transform); carpet1.transform.position = new Vector3(4.4f, 0.015f, -2.4f); AttachGroundSprite(carpet1, "carpet", 2.3f, 1.6f, -95);
                var carpet2 = new GameObject("Carpet_2"); carpet2.transform.SetParent(root.transform); carpet2.transform.position = new Vector3(4.4f, 0.015f, 0f); AttachGroundSprite(carpet2, "carpet", 2.3f, 1.6f, -95);
                var carpet3 = new GameObject("Carpet_3"); carpet3.transform.SetParent(root.transform); carpet3.transform.position = new Vector3(4.4f, 0.015f, 2.4f); AttachGroundSprite(carpet3, "carpet", 2.3f, 1.6f, -95);

                MakeBuildZone(root.transform, "BuildZone_Table2", 30, tables[1].transform.parent.gameObject, new Vector3(2.3f, 0.08f, 0f), "테이블 2", progress);
                MakeBuildZone(root.transform, "BuildZone_Table3", 120, tables[2].transform.parent.gameObject, new Vector3(2.3f, 0.08f, 2.4f), "테이블 3", progress);

                // Lock lines by build zones in Restaurant
                if (!buildForest)
                {
                    // Berry cooking group (DropBox Receiver + Juicer + upgrades)
                    var berryCookLine = new GameObject("BerryCookLineGroup");
                    berryCookLine.transform.SetParent(root.transform);
                    boxBerryRecvGo.transform.SetParent(berryCookLine.transform, true);
                    juicer.transform.parent.SetParent(berryCookLine.transform, true);
                    juicerUpgradeZone.transform.SetParent(berryCookLine.transform, true);
                    berryCookLine.SetActive(false);
                    MakeBuildZone(root.transform, "BuildZone_BerryLine", 80, berryCookLine, new Vector3(-3.6f, 0.08f, 0.8f), "베리 요리", progress);

                    // Mushroom cooking group (DropBox Receiver + SoupPot + upgrades)
                    var mushroomCookLine = new GameObject("MushroomCookLineGroup");
                    mushroomCookLine.transform.SetParent(root.transform);
                    boxMushroomRecvGo.transform.SetParent(mushroomCookLine.transform, true);
                    soupPot.transform.parent.SetParent(mushroomCookLine.transform, true);
                    soupPotUpgradeZone.transform.SetParent(mushroomCookLine.transform, true);
                    mushroomCookLine.SetActive(false);
                    MakeBuildZone(root.transform, "BuildZone_MushroomLine", 180, mushroomCookLine, new Vector3(-3.6f, 0.08f, 3.8f), "가마솥 요리", progress);

                    // Scene Gate to Forest (on the left)
                    var gateGo = new GameObject("SceneGate_ToForest");
                    gateGo.transform.SetParent(root.transform);
                    gateGo.transform.position = new Vector3(-7.5f, 0f, 0f);
                    var gateCol = gateGo.AddComponent<BoxCollider>();
                    gateCol.size = new Vector3(1.0f, 2.0f, 4.0f);
                    var gate = gateGo.AddComponent<ArcadeSceneGate>();
                    gate.ConfigureGate("ArcadeForest", 1.5f);
                    AttachStandingSprite(gateGo, "fireplace", 2.2f);
                    var gateLabel = MakeLabel("야외 이동", gateGo.transform, new Vector3(0f, 2.2f, -0.5f));
                    gateLabel.fontSize = 32;

                    // Hired staff for Restaurant: pull raw from Receivers, cook at station, serve to tables
                    MakeHireZone(root.transform, 150, new Vector3(-3.6f, 0.08f, -2.4f),
                        boxFishRecvZone, grill.transform, tableArray, ArcadeItemType.Fish, ArcadeItemType.GrilledFish, player.transform.position, "생선 요리원", progress);

                    MakeHireZone(root.transform, 220, new Vector3(-3.6f, 0.08f, 0.0f),
                        boxBerryRecvZone, juicer.transform, tableArray, ArcadeItemType.Berry, ArcadeItemType.BerryJuice, player.transform.position, "베리 요리원", progress);

                    MakeHireZone(root.transform, 280, new Vector3(-3.6f, 0.08f, 2.4f),
                        boxMushroomRecvZone, soupPot.transform, tableArray, ArcadeItemType.Mushroom, ArcadeItemType.MushroomSkewer, player.transform.position, "버섯 요리원", progress);
                }
                else
                {
                    // Unified single scene automation (original code)
                    // Hired staff directly loops gather -> cook -> serve
                    // Find gather zones built in the forest part of this unified scene
                    var fishGather = root.transform.Find("GatherZone_Fish");
                    var berryGather = root.transform.Find("BerryLineGroup/GatherZone_Berry");
                    var mushroomGather = root.transform.Find("MushroomLineGroup/GatherZone_Mushroom");

                    // Build zones for berry / mushroom lines (already structured under buildForest block)
                    // If unified, the original MakeHireZone works:
                    if (fishGather != null)
                        MakeHireZone(root.transform, 150, new Vector3(-3.6f, 0.08f, -2.4f),
                            fishGather, grill.transform, tableArray, ArcadeItemType.Fish, ArcadeItemType.GrilledFish, player.transform.position, "생선 직원", progress);
                    
                    if (berryGather != null)
                        MakeHireZone(root.transform, 220, new Vector3(-3.6f, 0.08f, 0.0f),
                            berryGather, juicer.transform, tableArray, ArcadeItemType.Berry, ArcadeItemType.BerryJuice, player.transform.position, "베리 직원", progress);

                    if (mushroomGather != null)
                        MakeHireZone(root.transform, 280, new Vector3(-3.6f, 0.08f, 2.4f),
                            mushroomGather, soupPot.transform, tableArray, ArcadeItemType.Mushroom, ArcadeItemType.MushroomSkewer, player.transform.position, "버섯 직원", progress);
                }

                MakeCustomerSpawner(root.transform, customerPrefab, tableArray, grill, juicer, soupPot);
            }

            // --- Common / Shared Scene Elements (HUD, Camera, Particles, Props) ---

            // Decorative props (Storybook cozy layout)
            var deco = new GameObject("Decor");
            deco.transform.SetParent(root.transform);

            if (buildForest && buildRestaurant)
            {
                // Unified: Original prop placements
                MakeProp(deco.transform, "tree", new Vector3(-8.0f, 0f, -5.2f), 2.6f);
                MakeProp(deco.transform, "tree", new Vector3(7.6f, 0f, -5.4f), 2.4f);
                MakeProp(deco.transform, "tree", new Vector3(8.0f, 0f, 5.4f), 2.7f);
                MakeProp(deco.transform, "bush", new Vector3(-8.2f, 0f, 2.0f), 1.0f);
                MakeProp(deco.transform, "bush", new Vector3(6.4f, 0f, 0.2f), 1.0f);
                MakeProp(deco.transform, "tree", new Vector3(-7.5f, 0f, 0.5f), 5.8f);

                // Divider Wall
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, -5.2f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, -3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, -0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, 0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, 3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, 5.2f), 1.8f);

                // Cabin Enclosure Walls (Z = 5.7f, -5.7f, X = 8.2f)
                MakeProp(deco.transform, "wall", new Vector3(-2.7f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-0.9f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(0.9f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(2.7f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(4.5f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.3f, 0f, 5.7f), 1.8f);

                MakeProp(deco.transform, "wall", new Vector3(-2.7f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-0.9f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(0.9f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(2.7f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(4.5f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.3f, 0f, -5.7f), 1.8f);

                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, -2.4f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, -0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, 0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, 2.4f), 1.8f);

                // Cozy elements
                var fireplaceGo = new GameObject("Fireplace");
                fireplaceGo.transform.SetParent(root.transform);
                fireplaceGo.transform.position = new Vector3(2.7f, 0f, 5.5f);
                AttachStandingSprite(fireplaceGo, "fireplace", 2.2f);
                var fpLightGo = new GameObject("FireplaceLight");
                fpLightGo.transform.SetParent(fireplaceGo.transform, false);
                fpLightGo.transform.localPosition = new Vector3(0f, 0.4f, -0.3f);
                var fpLight = fpLightGo.AddComponent<Light>();
                fpLight.type = LightType.Point;
                fpLight.color = new Color(1.0f, 0.45f, 0.1f);
                fpLight.range = 5.5f;
                fpLight.intensity = 2.0f;
                fpLightGo.AddComponent<LightFlicker>();

                var counterGo = new GameObject("RegisterCounter");
                counterGo.transform.SetParent(root.transform);
                counterGo.transform.position = new Vector3(5.8f, 0f, -4.5f);
                AttachStandingSprite(counterGo, "counter", 1.6f);

                var plantGo1 = new GameObject("IndoorPlant_1");
                plantGo1.transform.SetParent(root.transform);
                plantGo1.transform.position = new Vector3(-2.8f, 0f, 5.3f);
                AttachStandingSprite(plantGo1, "plant", 1.6f);

                var plantGo2 = new GameObject("IndoorPlant_2");
                plantGo2.transform.SetParent(root.transform);
                plantGo2.transform.position = new Vector3(7.6f, 0f, 3.2f);
                AttachStandingSprite(plantGo2, "plant", 1.6f);

                MakeProp(deco.transform, "lantern", new Vector3(2.0f, 0f, -4.2f), 1.3f);
                MakeProp(deco.transform, "lantern", new Vector3(6.2f, 0f, -4.2f), 1.3f);
                MakeProp(deco.transform, "lantern", new Vector3(-3.4f, 0f, -1.8f), 1.3f);
            }
            else if (buildForest)
            {
                // Split Forest Scene decorations: natural trees, bushes
                MakeProp(deco.transform, "tree", new Vector3(-8.0f, 0f, -5.2f), 2.6f);
                MakeProp(deco.transform, "tree", new Vector3(8.0f, 0f, -5.2f), 2.5f);
                MakeProp(deco.transform, "tree", new Vector3(8.0f, 0f, 5.2f), 2.7f);
                MakeProp(deco.transform, "bush", new Vector3(-8.2f, 0f, 2.0f), 1.0f);
                MakeProp(deco.transform, "bush", new Vector3(6.4f, 0f, 0.2f), 1.0f);
                MakeProp(deco.transform, "tree", new Vector3(-7.5f, 0f, 0.5f), 5.8f);

                // Right boundary wall divider representing log cabin entrance facade
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, -5.2f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, -3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, -2.0f), 1.8f);
                // Doorway gap for gate at Z = 0
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, 2.0f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, 3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(6.8f, 0f, 5.2f), 1.8f);
            }
            else if (buildRestaurant)
            {
                // Split Restaurant Scene decorations: indoor elements, log-cabin outer walls
                
                // Left boundary entrance wall (matches forest exit facade)
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, -5.2f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, -3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, -2.0f), 1.8f);
                // Doorway gap for gate at Z = 0
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, 2.0f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, 3.6f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-6.8f, 0f, 5.2f), 1.8f);

                // Cabin Enclosure Walls (Z = 5.7f, -5.7f, X = 8.2f)
                MakeProp(deco.transform, "wall", new Vector3(-5.4f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-1.8f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(0.0f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(1.8f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(3.6f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(5.4f, 0f, 5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(7.2f, 0f, 5.7f), 1.8f);

                MakeProp(deco.transform, "wall", new Vector3(-5.4f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-3.6f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(-1.8f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(0.0f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(1.8f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(3.6f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(5.4f, 0f, -5.7f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(7.2f, 0f, -5.7f), 1.8f);

                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, -2.4f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, -0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, 0.8f), 1.8f);
                MakeProp(deco.transform, "wall", new Vector3(8.2f, 0f, 2.4f), 1.8f);

                // Cozy elements
                var fireplaceGo = new GameObject("Fireplace");
                fireplaceGo.transform.SetParent(root.transform);
                fireplaceGo.transform.position = new Vector3(2.7f, 0f, 5.5f);
                AttachStandingSprite(fireplaceGo, "fireplace", 2.2f);
                var fpLightGo = new GameObject("FireplaceLight");
                fpLightGo.transform.SetParent(fireplaceGo.transform, false);
                fpLightGo.transform.localPosition = new Vector3(0f, 0.4f, -0.3f);
                var fpLight = fpLightGo.AddComponent<Light>();
                fpLight.type = LightType.Point;
                fpLight.color = new Color(1.0f, 0.45f, 0.1f);
                fpLight.range = 5.5f;
                fpLight.intensity = 2.0f;
                fpLightGo.AddComponent<LightFlicker>();

                var counterGo = new GameObject("RegisterCounter");
                counterGo.transform.SetParent(root.transform);
                counterGo.transform.position = new Vector3(5.8f, 0f, -4.5f);
                AttachStandingSprite(counterGo, "counter", 1.6f);

                var plantGo1 = new GameObject("IndoorPlant_1");
                plantGo1.transform.SetParent(root.transform);
                plantGo1.transform.position = new Vector3(-2.8f, 0f, 5.3f);
                AttachStandingSprite(plantGo1, "plant", 1.6f);

                var plantGo2 = new GameObject("IndoorPlant_2");
                plantGo2.transform.SetParent(root.transform);
                plantGo2.transform.position = new Vector3(7.6f, 0f, 3.2f);
                AttachStandingSprite(plantGo2, "plant", 1.6f);

                MakeProp(deco.transform, "lantern", new Vector3(2.0f, 0f, -4.2f), 1.3f);
                MakeProp(deco.transform, "lantern", new Vector3(6.2f, 0f, -4.2f), 1.3f);
                MakeProp(deco.transform, "lantern", new Vector3(-3.4f, 0f, -1.8f), 1.3f);
            }

            var hud = new GameObject("ArcadeHUD");
            var hudView = hud.AddComponent<ArcadeHUD>();
            hudView.Configure(stack);

            var joyGo = new GameObject("ArcadeJoystick");
            var joystick = joyGo.AddComponent<ArcadeJoystick>();
            joystick.AttachVisual(hudView.OverlayLayer);
            player.GetComponent<ArcadePlayerController>().Configure(joystick);

            var idleGo = new GameObject("ArcadeIdleService");
            idleGo.transform.SetParent(root.transform);
            idleGo.AddComponent<ArcadeIdleService>().Configure(hudView.ModalLayer);

            // Leaf drift particles (only in Forest scene)
            if (buildForest)
            {
                var leafParticles = CreateLeafDrift(root.transform);
                player.AddComponent<ZoneAtmosphereTrigger>().Configure(leafParticles, buildRestaurant ? -3.6f : 999f);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.gameObject.GetComponent<ArcadeCameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<ArcadeCameraFollow>();
                follow.Configure(player.transform);
            }

            // FTUE guide (skipped in Restaurant, or if saved as completed)
            if (buildForest && !saveData.arcadeTutorialDone)
            {
                var tutorial = new GameObject("ArcadeTutorialDirector");
                tutorial.transform.SetParent(root.transform);
                
                // If Restaurant is in another scene, grill is not present, so we point guide to the Forest DropBox
                var guideTarget = root.transform.Find("DropBox_Fish/DropBoxZone_Fish") ?? root.transform.Find("Station_Grill/CookZone_Grill");
                var fishGather = root.transform.Find("GatherZone_Fish");

                if (fishGather != null && guideTarget != null)
                {
                    tutorial.AddComponent<ArcadeTutorialDirector>()
                        .Configure(stack, fishGather, guideTarget, null);
                }
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
            AttachGlowPad(zone, new Color(0.35f, 0.75f, 1f, 0.35f), 1.6f); // teal glow
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

            // Facility sprite (grill, juicer, or soup_pot) stands on the ground at the station root.
            string facilityKey = "grill";
            if (input == ArcadeItemType.Berry) facilityKey = "juicer";
            else if (input == ArcadeItemType.Mushroom) facilityKey = "soup_pot";

            var facility = new GameObject("FacilitySprite");
            facility.transform.SetParent(stationRoot.transform, false);
            var facilityBb = AttachStandingSprite(facility, facilityKey, 1.7f, footOffsetY: 0f, bias: 0);

            var zone = MakeZone($"CookZone_{label}", stationRoot.transform, new Vector3(0f, 0.08f, -0.9f), new Vector3(1.35f, 0.05f, 1.0f), new Color(1f, 0.65f, 0.2f, 0.55f), true);
            AttachGlowPad(zone, new Color(1f, 0.65f, 0.25f, 0.35f), 1.5f); // warm orange glow

            // Add warm glowing fire light for the campfire grill & soup pot (Cats&Soup style)
            Light grillLight = null;
            if (facilityKey == "grill" || facilityKey == "soup_pot")
            {
                var fireLightGo = new GameObject("GrillFireLight");
                fireLightGo.transform.SetParent(stationRoot.transform, false);
                fireLightGo.transform.localPosition = new Vector3(0f, 0.6f, -0.2f);
                grillLight = fireLightGo.AddComponent<Light>();
                grillLight.type = LightType.Point;
                grillLight.color = new Color(1.0f, 0.5f, 0.15f); // intense fire orange
                grillLight.range = 4.5f;
                grillLight.intensity = 1.8f;
                grillLight.shadows = LightShadows.None;
                fireLightGo.AddComponent<LightFlicker>();
            }

            // We will NOT spawn the choppy, flat 2D flipbook fire animation,
            // as it looks like a flat cardboard cutout. The volumetric 3D particle fire
            // will handle all fire rendering beautifully.
            SpriteFlipbookPlayer flipbook = null;

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
            string dishKey = "item_grilledfish";
            if (input == ArcadeItemType.Berry) dishKey = "item_juice";
            else if (input == ArcadeItemType.Mushroom) dishKey = "item_soup";

            var bubble = WorldBubble.Create(zone.transform, new Vector3(0f, 1.25f, -0.35f), 0.8f, 0.7f);
            bubble.SetIcon(ArcadeSprites.Get(dishKey), 0.4f, new Vector2(0f, 0.06f));
            facilitySprite = facilityBb;

            // Add interactive sparks/steam/flames VFX during cooking
            AddStationVFX(cook, stationRoot, input, flipbook, grillLight);

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
            // Reuse the build pad art but tint it cool cyan so upgrade pads read apart
            // from the warm-yellow build pads (no upgrade-specific sprite yet).
            AttachPad(zone, "pad_build", 1.5f, new Color(0.55f, 0.85f, 1f, 1f));

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
            AttachGlowPad(zone, new Color(0.45f, 0.85f, 0.45f, 0.35f), 1.5f); // mint green glow
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
        private void AttachPad(GameObject zone, string key, float worldSize, Color? tint = null)
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
            if (tint.HasValue) sr.color = tint.Value;
            sr.sortingOrder = -100;
            float w = sprite.bounds.size.x;
            float s = w > 0f ? worldSize / w : 1f;
            go.transform.localScale = new Vector3(s, s, s);
        }

        private void MakeHireZone(
            Transform parent,
            double cost,
            Vector3 pos,
            Transform gather,
            Transform cook,
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
            CookStation berryStation,
            CookStation mushroomStation)
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
            // Mushroom soup sells once the mushroom cauldron is live.
            var menu = new List<ArcadeCustomerSpawner.MenuOption>
            {
                new() { item = ArcadeItemType.GrilledFish, pay = 10, gate = fishStation },
                new() { item = ArcadeItemType.BerryJuice, pay = 16, gate = berryStation },
                new() { item = ArcadeItemType.MushroomSkewer, pay = 25, gate = mushroomStation },
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

                if (name == "River")
                {
                    // TessellatedWater generates vertices at world size directly; keep localScale at 1.
                    go.transform.localScale = Vector3.one;

                    var waterMesh = go.AddComponent<TessellatedWater>();
                    waterMesh.width = worldW;
                    waterMesh.height = worldH;

                    var riverMr = go.AddComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    mat.mainTexture = sprite.texture;
                    
                    // Enable texture repeating for tiling
                    sprite.texture.wrapMode = TextureWrapMode.Repeat;
                    
                    // Map tiling scale relative to pixels per unit
                    float tileW = worldW / (sprite.texture.width / sprite.pixelsPerUnit);
                    float tileH = worldH / (sprite.texture.height / sprite.pixelsPerUnit);
                    mat.mainTextureScale = new Vector2(tileW, tileH);
                    riverMr.material = mat;

                    // Add dynamic scrolling along river length (Y axis)
                    var scroller = go.AddComponent<WaterScroller>();
                    scroller.scrollSpeedX = 0.0f;
                    scroller.scrollSpeedY = -0.04f; // Flow downward

                    // Add soft circular ripple rings and glistening sparkles
                    var rippleEff = go.AddComponent<WaterRippleEffect>();
                    rippleEff.Configure(worldW, worldH, bias);
                }
                else
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.sortingOrder = bias;
                    sr.size = new Vector2(worldW, worldH); // world-unit footprint (scale stays 1)
                }
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

            // Add cozy warm light source to lantern props
            if (key == "lantern")
            {
                var lightGo = new GameObject("LanternLight");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, worldHeight * 0.7f, -0.2f);
                var pl = lightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1.0f, 0.65f, 0.35f); // warm amber glow
                pl.range = 3.5f;
                pl.intensity = 1.8f;
                pl.shadows = LightShadows.None;
            }
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
                cam.backgroundColor = new Color(0.97f, 0.92f, 0.84f); // 따뜻한 크림 배경 (Cats&Soup 톤)
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
            light.intensity = 1.05f;
            light.color = new Color(1f, 0.95f, 0.86f);      // 따뜻한 햇살 톤
            light.shadows = LightShadows.None;              // flat storybook lighting
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 낮은 대비의 따뜻한 앰비언트 — 그림자가 어둡지 않게 떠서 아늑한 디오라마 느낌(Cats&Soup)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.86f, 0.82f, 0.76f);

            ApplyCozyPostFx();
        }

        /// <summary>
        /// Cats&Soup 톤의 부드러운 포스트프로세싱을 런타임 구성 — 은은한 블룸 + 따뜻한 컬러그레이딩
        /// + 살짝 비네팅. URP Volume/Profile 을 코드로 만들어 글로벌 적용한다.
        /// </summary>
        private void ApplyCozyPostFx()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null) camData.renderPostProcessing = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.7f);     // 은은한 빛 번짐
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.75f);
            bloom.tint.Override(new Color(1f, 0.96f, 0.9f));

            var grade = profile.Add<ColorAdjustments>();
            grade.postExposure.Override(0.1f);
            grade.contrast.Override(-6f);       // 낮은 대비 → 말랑한 느낌
            grade.saturation.Override(6f);      // 살짝 화사
            grade.colorFilter.Override(new Color(1f, 0.97f, 0.92f)); // 따뜻한 필터

            var wb = profile.Add<WhiteBalance>();
            wb.temperature.Override(12f);       // 전체적으로 따뜻하게

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.85f);
            vignette.color.Override(new Color(0.32f, 0.24f, 0.16f));

            var volGo = new GameObject("_CozyPostFX");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.profile = profile;
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

        // ---- Procedural Visual FX & Lighting (Cats&Soup style) ----

        private void AttachGlowPad(GameObject zone, Color color, float worldSize)
        {
            HideMesh(zone);
            var go = new GameObject("GlowPad");
            go.transform.SetParent(zone.transform.parent, false);
            // Sits flat slightly above ground level to prevent Z-fighting
            go.transform.position = new Vector3(zone.transform.position.x, 0.055f, zone.transform.position.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UITheme.Glow;
            sr.color = color;
            sr.sortingOrder = -90;

            float w = UITheme.Glow.bounds.size.x;
            float s = w > 0f ? worldSize / w : 1f;
            go.transform.localScale = new Vector3(s, s, s);
        }

        private ParticleSystem CreateLeafDrift(Transform parent)
        {
            var go = new GameObject("LeafDriftParticles");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(-5f, 6f, 5f);
            go.transform.rotation = Quaternion.Euler(45f, 135f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.01f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(2.0f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 1f, 16f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.25f, -0.08f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.3f, -0.7f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.68f, 0.88f, 0.58f), 0f), // Soft forest green
                    new GradientColorKey(new Color(0.75f, 0.92f, 0.65f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.15f),
                    new GradientAlphaKey(0.8f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.material = new Material(Shader.Find("Sprites/Default"));
            var leafSprite = ArcadeSprites.Get("item_berry"); // fallback colored sprite shape
            if (leafSprite != null) psr.material.mainTexture = leafSprite.texture;

            ps.Play();
            return ps;
        }

        private void AddStationVFX(CookStation station, GameObject stationRoot, ArcadeItemType input, SpriteFlipbookPlayer flipbook = null, Light grillLight = null)
        {
            bool isJuicer = input == ArcadeItemType.Berry;
            bool isMushroom = input == ArcadeItemType.Mushroom;

            // Sparks / Splash droplets
            var sparksGo = new GameObject("VFX_Sparks");
            sparksGo.transform.SetParent(stationRoot.transform, false);
            sparksGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            sparksGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Point up

            var psSparks = sparksGo.AddComponent<ParticleSystem>();
            psSparks.Stop();
            var mainS = psSparks.main;
            mainS.duration = 1f;
            mainS.loop = true;
            mainS.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            mainS.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            mainS.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            mainS.gravityModifier = new ParticleSystem.MinMaxCurve(isJuicer ? 0.5f : (isMushroom ? 0.1f : -0.3f));
            mainS.simulationSpace = ParticleSystemSimulationSpace.World;
            mainS.maxParticles = 40;

            var emitS = psSparks.emission;
            emitS.enabled = false;
            emitS.rateOverTime = new ParticleSystem.MinMaxCurve(18f);

            var shapeS = psSparks.shape;
            shapeS.enabled = true;
            shapeS.shapeType = ParticleSystemShapeType.Cone;
            shapeS.angle = 25f;
            shapeS.radius = 0.18f;

            if (input == ArcadeItemType.Fish)
            {
                var noiseS = psSparks.noise;
                noiseS.enabled = true;
                noiseS.strength = 0.35f;
                noiseS.frequency = 3.5f;
            }

            var colS = psSparks.colorOverLifetime;
            colS.enabled = true;
            var gradS = new Gradient();
            if (isJuicer)
            {
                // Berry juice droplets
                gradS.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.92f, 0.32f, 0.6f), 0f),
                        new GradientColorKey(new Color(1f, 0.62f, 0.8f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (isMushroom)
            {
                // Mushroom soup bubbles (purple / magenta)
                gradS.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.77f, 0.37f, 0.82f), 0f),
                        new GradientColorKey(new Color(0.92f, 0.61f, 0.95f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else
            {
                // Fire sparks
                gradS.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1.0f, 0.78f, 0.25f), 0f),
                        new GradientColorKey(new Color(1.0f, 0.35f, 0.05f), 0.75f),
                        new GradientColorKey(new Color(0.25f, 0.25f, 0.25f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0f),
                        new GradientAlphaKey(1.0f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            colS.color = gradS;

            var psrS = sparksGo.GetComponent<ParticleSystemRenderer>();
            psrS.renderMode = ParticleSystemRenderMode.Billboard;
            psrS.material = new Material(Shader.Find("Sprites/Default"));
            var sparkSprite = ArcadeSprites.Get("money"); // small dot
            if (sparkSprite != null) psrS.material.mainTexture = sparkSprite.texture;

            psSparks.Play();

            // Steam / Sweet bubbles
            var steamGo = new GameObject("VFX_Steam");
            steamGo.transform.SetParent(stationRoot.transform, false);
            steamGo.transform.localPosition = new Vector3(0f, 0.7f, 0.1f);

            var psSteam = steamGo.AddComponent<ParticleSystem>();
            psSteam.Stop();
            var mainM = psSteam.main;
            mainM.duration = 2f;
            mainM.loop = true;
            mainM.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.0f);
            mainM.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            mainM.startSize = new ParticleSystem.MinMaxCurve(isJuicer ? 0.18f : 0.2f, isJuicer ? 0.35f : 0.4f);
            mainM.simulationSpace = ParticleSystemSimulationSpace.World;
            mainM.maxParticles = 25;

            var emitM = psSteam.emission;
            emitM.enabled = false;
            emitM.rateOverTime = new ParticleSystem.MinMaxCurve(isJuicer ? 4f : 8f);

            var shapeM = psSteam.shape;
            shapeM.enabled = true;
            shapeM.shapeType = ParticleSystemShapeType.Sphere;
            shapeM.radius = 0.25f;

            var colM = psSteam.colorOverLifetime;
            colM.enabled = true;
            var gradM = new Gradient();
            if (isJuicer)
            {
                // Sweet bubbles
                gradM.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1.0f, 0.9f, 0.95f), 0f),
                        new GradientColorKey(new Color(0.95f, 0.78f, 0.88f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.6f, 0.2f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else if (isMushroom)
            {
                // Savory soup steam (soft purple-ish grey)
                gradM.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.88f, 0.82f, 0.92f), 0f),
                        new GradientColorKey(new Color(0.82f, 0.76f, 0.86f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.3f, 0.2f),
                        new GradientAlphaKey(0.3f, 0.8f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            else
            {
                // Cozy grey cooking steam
                gradM.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.94f, 0.94f, 0.94f), 0f),
                        new GradientColorKey(new Color(0.88f, 0.88f, 0.88f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.25f, 0.25f),
                        new GradientAlphaKey(0.25f, 0.75f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
            colM.color = gradM;

            var szM = psSteam.sizeOverLifetime;
            szM.enabled = true;
            szM.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1.6f)));

            var psrM = steamGo.GetComponent<ParticleSystemRenderer>();
            psrM.renderMode = ParticleSystemRenderMode.Billboard;
            psrM.material = new Material(Shader.Find("Sprites/Default"));
            var circleSprite = UITheme.Circle;
            if (circleSprite != null) psrM.material.mainTexture = circleSprite.texture;

            psSteam.Play();

            // Soft Volumetric Flame particles (For the campfire grill / soup pot)
            ParticleSystem psFlames = null;
            ParticleSystem psEmbers = null;
            if (input == ArcadeItemType.Fish || input == ArcadeItemType.Mushroom)
            {
                // 1. Embers (glowing coals at base)
                var embersGo = new GameObject("VFX_Embers");
                embersGo.transform.SetParent(stationRoot.transform, false);
                embersGo.transform.localPosition = new Vector3(0f, 0.45f, -0.05f);
                embersGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                psEmbers = embersGo.AddComponent<ParticleSystem>();
                psEmbers.Stop();
                var mainE = psEmbers.main;
                mainE.duration = 1f;
                mainE.loop = true;
                mainE.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                mainE.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                mainE.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                mainE.simulationSpace = ParticleSystemSimulationSpace.World;
                mainE.maxParticles = 20;

                var emitE = psEmbers.emission;
                emitE.enabled = true;
                emitE.rateOverTime = new ParticleSystem.MinMaxCurve(6f);

                var shapeE = psEmbers.shape;
                shapeE.enabled = true;
                shapeE.shapeType = ParticleSystemShapeType.Circle;
                shapeE.radius = 0.2f;

                var colE = psEmbers.colorOverLifetime;
                colE.enabled = true;
                var gradE = new Gradient();
                gradE.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1.0f, 0.3f, 0f), 0f),
                        new GradientColorKey(new Color(0.6f, 0.1f, 0f), 0.7f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.7f, 0.2f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                colE.color = gradE;

                var psrE = embersGo.GetComponent<ParticleSystemRenderer>();
                psrE.renderMode = ParticleSystemRenderMode.Billboard;
                psrE.material = new Material(Shader.Find("Sprites/Default"));
                var glowSprite = UITheme.Glow;
                if (glowSprite != null) psrE.material.mainTexture = glowSprite.texture;

                psEmbers.Play();

                // 2. Rising volumetric flame tongues
                var flamesGo = new GameObject("VFX_Flames");
                flamesGo.transform.SetParent(stationRoot.transform, false);
                flamesGo.transform.localPosition = new Vector3(0f, 0.5f, -0.05f);
                flamesGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                psFlames = flamesGo.AddComponent<ParticleSystem>();
                psFlames.Stop();
                var mainF = psFlames.main;
                mainF.duration = 1f;
                mainF.loop = true;
                mainF.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
                mainF.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                mainF.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
                mainF.startRotation = new ParticleSystem.MinMaxCurve(-12f * Mathf.Deg2Rad, 12f * Mathf.Deg2Rad);
                mainF.simulationSpace = ParticleSystemSimulationSpace.World;
                mainF.maxParticles = 40;

                var emitF = psFlames.emission;
                emitF.enabled = true;
                emitF.rateOverTime = new ParticleSystem.MinMaxCurve(12f);

                var shapeF = psFlames.shape;
                shapeF.enabled = true;
                shapeF.shapeType = ParticleSystemShapeType.Cone;
                shapeF.angle = 12f;
                shapeF.radius = 0.15f;

                var colF = psFlames.colorOverLifetime;
                colF.enabled = true;
                var gradF = new Gradient();
                gradF.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1.0f, 1.0f, 0.9f), 0f),      // Hot core
                        new GradientColorKey(new Color(1.0f, 0.55f, 0.05f), 0.3f),   // Orange body
                        new GradientColorKey(new Color(0.9f, 0.12f, 0.02f), 0.7f),   // Red tip
                        new GradientColorKey(new Color(0.2f, 0.15f, 0.15f), 1.0f)    // Soot
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(0.9f, 0.15f),
                        new GradientAlphaKey(0.9f, 0.6f),
                        new GradientAlphaKey(0f, 1.0f)
                    }
                );
                colF.color = gradF;

                var szF = psFlames.sizeOverLifetime;
                szF.enabled = true;
                var curveF = new AnimationCurve(new Keyframe(0f, 1.0f), new Keyframe(0.6f, 0.8f), new Keyframe(1f, 0f));
                szF.size = new ParticleSystem.MinMaxCurve(1f, curveF);

                // Enable Noise to make the flames dance
                var noiseF = psFlames.noise;
                noiseF.enabled = true;
                noiseF.strength = 0.18f;
                noiseF.frequency = 2.2f;
                noiseF.octaveCount = 1;

                var psrF = flamesGo.GetComponent<ParticleSystemRenderer>();
                psrF.renderMode = ParticleSystemRenderMode.Billboard;
                psrF.material = new Material(Shader.Find("Sprites/Default"));
                
                var flameSprite = UITheme.Flame;
                if (flameSprite != null) psrF.material.mainTexture = flameSprite.texture;

                psFlames.Play();
            }

            var vfx = stationRoot.AddComponent<CookStationVFX>();
            vfx.Init(station, psSparks, psSteam, psFlames, flipbook, psEmbers, grillLight);
        }
    }

    // ---- VFX Controllers ----

    public class CookStationVFX : MonoBehaviour
    {
        private CookStation _station;
        private ParticleSystem _sparks;
        private ParticleSystem _steam;
        private ParticleSystem _flames;
        private ParticleSystem _embers;
        private SpriteFlipbookPlayer _flipbook;
        private Light _light;
        
        private float _baseFlamesRate = 12f;
        private float _baseEmbersRate = 6f;
        private float _baseLightIntensity = 1.8f;
        private float _baseLightRange = 4.5f;

        public void Init(CookStation station, ParticleSystem sparks, ParticleSystem steam, ParticleSystem flames = null, SpriteFlipbookPlayer flipbook = null, ParticleSystem embers = null, Light lightRef = null)
        {
            _station = station;
            _sparks = sparks;
            _steam = steam;
            _flames = flames;
            _flipbook = flipbook;
            _embers = embers;
            _light = lightRef;
            
            if (_flames != null)
            {
                _baseFlamesRate = _flames.emission.rateOverTime.constant;
            }
            if (_embers != null)
            {
                _baseEmbersRate = _embers.emission.rateOverTime.constant;
            }
            if (_light != null)
            {
                _baseLightIntensity = _light.intensity;
                _baseLightRange = _light.range;
            }
        }

        private void Update()
        {
            if (_station == null) return;
            bool active = _station.IsCooking;

            if (_sparks != null)
            {
                var emission = _sparks.emission;
                if (emission.enabled != active) emission.enabled = active;
            }
            if (_steam != null)
            {
                var emission = _steam.emission;
                if (emission.enabled != active) emission.enabled = active;
            }
            if (_flames != null)
            {
                var emission = _flames.emission;
                // Idle: burn softly; Cooking: flare up!
                float targetRate = active ? _baseFlamesRate * 2.8f : _baseFlamesRate * 0.7f;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(targetRate);
            }
            if (_embers != null)
            {
                var emission = _embers.emission;
                float targetRate = active ? _baseEmbersRate * 2.2f : _baseEmbersRate * 0.8f;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(targetRate);
            }
            if (_flipbook != null)
            {
                _flipbook.SetSpeedMultiplier(active ? 1.6f : 0.8f);
            }
            if (_light != null)
            {
                // Smoothly interpolate the base intensity and range of the fire light based on active cooking
                float targetIntensity = active ? _baseLightIntensity * 2.0f : _baseLightIntensity;
                float targetRange = active ? _baseLightRange * 1.35f : _baseLightRange;
                
                var flicker = _light.GetComponent<LightFlicker>();
                if (flicker != null)
                {
                    flicker.SetBaseIntensity(Mathf.Lerp(flicker.GetBaseIntensity(), targetIntensity, Time.deltaTime * 4f));
                }
                _light.range = Mathf.Lerp(_light.range, targetRange, Time.deltaTime * 4f);
            }
        }
    }

    public class LightFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            // Flicker intensity dynamically (cozy flame effect)
            _light.intensity = _baseIntensity * (1f + UnityEngine.Random.Range(-0.15f, 0.15f) * Mathf.Sin(Time.time * 28f));
        }

        public void SetBaseIntensity(float val)
        {
            _baseIntensity = val;
        }

        public float GetBaseIntensity()
        {
            return _baseIntensity;
        }
    }

    // ---- 2D Sprite Flipbook Player (Cats&Soup style) ----

    public class SpriteFlipbookPlayer : MonoBehaviour
    {
        [SerializeField] private float fps = 10f;
        
        private SpriteRenderer _sr;
        private Sprite[] _frames;
        private int _currentFrame;
        private float _timer;
        private float _speedMultiplier = 1.0f;

        public void Init(string key, float worldHeight, float speedMult = 1.0f)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            var tex = Resources.Load<Texture2D>($"Arcade/{key}");
            if (tex == null) return;

            // Slice 2x2 grid (4 frames)
            _frames = new Sprite[4];
            int w = tex.width / 2;
            int h = tex.height / 2;
            float ppu = 256f;

            // Pivot at bottom center (0.5, 0.04) so it stands on the grill
            _frames[0] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), ppu);
            _frames[1] = Sprite.Create(tex, new Rect(w, 0, w, h), new Vector2(0.5f, 0.04f), ppu);
            _frames[2] = Sprite.Create(tex, new Rect(0, h, w, h), new Vector2(0.5f, 0.04f), ppu);
            _frames[3] = Sprite.Create(tex, new Rect(w, h, w, h), new Vector2(0.5f, 0.04f), ppu);

            _speedMultiplier = speedMult;
            _sr.sprite = _frames[0];
            
            float sh = _frames[0].bounds.size.y;
            if (sh > 0f)
            {
                float s = worldHeight / sh;
                transform.localScale = new Vector3(s, s, s);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            _speedMultiplier = mult;
        }

        private void Update()
        {
            if (_frames == null || _frames.Length == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= 1f / (fps * _speedMultiplier))
            {
                _timer = 0f;
                _currentFrame = (_currentFrame + 1) % _frames.Length;
                _sr.sprite = _frames[_currentFrame];
            }
        }
    }

    public class WaterScroller : MonoBehaviour
    {
        private Material _material;
        private Vector2 _offset;
        public float scrollSpeedX = 0.02f;
        public float scrollSpeedY = 0.08f;

        private void Start()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _material = mr.material;
            }
        }

        private void Update()
        {
            if (_material != null)
            {
                _offset.x += scrollSpeedX * Time.deltaTime;
                _offset.y += scrollSpeedY * Time.deltaTime;
                _material.mainTextureOffset = _offset;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }

    public class WaterRippleEffect : MonoBehaviour
    {
        private float _width;
        private float _height;
        private int _sortingOrder;
        private float _spawnTimer;

        public void Configure(float width, float height, int sortingOrder)
        {
            _width = width;
            _height = height;
            _sortingOrder = sortingOrder;
        }

        private void Update()
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                if (UnityEngine.Random.value < 0.45f)
                {
                    SpawnRipple();
                }
                else
                {
                    SpawnGlisten();
                }
                _spawnTimer = UnityEngine.Random.Range(0.4f, 0.8f);
            }
        }

        private void SpawnRipple()
        {
            var ripple = new GameObject("WaterRipple");
            ripple.transform.SetParent(transform.parent, false);
            
            float rx = UnityEngine.Random.Range(-_width * 0.5f, _width * 0.5f);
            float rz = UnityEngine.Random.Range(-_height * 0.5f, _height * 0.5f);
            Vector3 center = transform.position;
            ripple.transform.position = new Vector3(center.x + rx, center.y + 0.005f, center.z + rz);
            ripple.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = ripple.AddComponent<SpriteRenderer>();
            sr.sprite = UITheme.Ring;
            sr.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            sr.sortingOrder = _sortingOrder + 1;

            var animator = ripple.AddComponent<RippleAnimator>();
            animator.duration = UnityEngine.Random.Range(1.6f, 2.5f);
            animator.startScale = UnityEngine.Random.Range(0.08f, 0.15f);
            animator.targetScale = UnityEngine.Random.Range(0.45f, 0.75f);
        }

        private void SpawnGlisten()
        {
            var glisten = new GameObject("WaterGlisten");
            glisten.transform.SetParent(transform.parent, false);

            float rx = UnityEngine.Random.Range(-_width * 0.5f, _width * 0.5f);
            float rz = UnityEngine.Random.Range(-_height * 0.5f, _height * 0.5f);
            Vector3 center = transform.position;
            glisten.transform.position = new Vector3(center.x + rx, center.y + 0.005f, center.z + rz);
            glisten.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = glisten.AddComponent<SpriteRenderer>();
            sr.sprite = UITheme.Glow;
            sr.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            sr.sortingOrder = _sortingOrder + 1;

            var animator = glisten.AddComponent<GlistenAnimator>();
            animator.duration = UnityEngine.Random.Range(0.7f, 1.3f);
            animator.maxAlpha = UnityEngine.Random.Range(0.5f, 0.85f);
            animator.maxScale = UnityEngine.Random.Range(0.04f, 0.09f);
        }
    }

    public class RippleAnimator : MonoBehaviour
    {
        public float duration;
        public float startScale;
        public float targetScale;
        private SpriteRenderer _sr;
        private float _elapsed;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = new Vector3(startScale, startScale, 1f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float currentScale = Mathf.Lerp(startScale, targetScale, t);
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            float alpha = 0f;
            if (t < 0.2f)
            {
                alpha = Mathf.Lerp(0f, 0.28f, t / 0.2f);
            }
            else
            {
                alpha = Mathf.Lerp(0.28f, 0f, (t - 0.2f) / 0.8f);
            }
            _sr.color = new Color(0.9f, 0.95f, 1f, alpha);
        }
    }

    public class GlistenAnimator : MonoBehaviour
    {
        public float duration;
        public float maxAlpha;
        public float maxScale;
        private SpriteRenderer _sr;
        private float _elapsed;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float scale = Mathf.PingPong(t * 2f, 1f) * maxScale;
            transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = Mathf.Sin(t * Mathf.PI) * maxAlpha;
            _sr.color = new Color(1.0f, 1.0f, 1.0f, alpha);
        }
    }

    public class TessellatedWater : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Vector3[] _baseVertices;
        private Vector3[] _deformedVertices;
        
        public float width = 2.4f;
        public float height = 6.4f;
        public int segmentsX = 8;
        public int segmentsY = 24;

        public float waveHeight = 0.035f;
        public float waveFrequency = 2.2f;
        public float waveSpeed = 1.8f;

        private void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            
            _mesh = new Mesh();
            _mesh.name = "WaterGrid";
            _meshFilter.mesh = _mesh;

            GenerateGridMesh();
        }

        private void GenerateGridMesh()
        {
            int vertexCount = (segmentsX + 1) * (segmentsY + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segmentsX * segmentsY * 6];

            float dx = width / segmentsX;
            float dy = height / segmentsY;

            int v = 0;
            for (int y = 0; y <= segmentsY; y++)
            {
                for (int x = 0; x <= segmentsX; x++)
                {
                    // Center the grid around origin in local space
                    float lx = x * dx - width * 0.5f;
                    float ly = y * dy - height * 0.5f;
                    
                    // rotated 90 on X, local X is world X, local Y is world Z.
                    // Local Z drives height/displacement
                    vertices[v] = new Vector3(lx, ly, 0f);
                    
                    uvs[v] = new Vector2((float)x / segmentsX, (float)y / segmentsY);
                    v++;
                }
            }

            int t = 0;
            for (int y = 0; y < segmentsY; y++)
            {
                for (int x = 0; x < segmentsX; x++)
                {
                    int row1 = y * (segmentsX + 1) + x;
                    int row2 = (y + 1) * (segmentsX + 1) + x;

                    triangles[t++] = row1;
                    triangles[t++] = row2;
                    triangles[t++] = row1 + 1;

                    triangles[t++] = row1 + 1;
                    triangles[t++] = row2;
                    triangles[t++] = row2 + 1;
                }
            }

            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _baseVertices = vertices;
            _deformedVertices = new Vector3[vertices.Length];
        }

        private void Update()
        {
            if (_baseVertices == null) return;

            float time = Time.time * waveSpeed;
            for (int i = 0; i < _baseVertices.Length; i++)
            {
                Vector3 vertex = _baseVertices[i];
                
                // Sinusoidal wave along river length (local Y) with a slight local X dependency
                float wave = Mathf.Sin(vertex.y * waveFrequency + vertex.x * 0.6f + time) * waveHeight;
                // Secondary wave for organic layering
                wave += Mathf.Sin(vertex.y * waveFrequency * 2.0f - time * 1.2f) * waveHeight * 0.3f;

                _deformedVertices[i] = new Vector3(vertex.x, vertex.y, wave);
            }

            _mesh.vertices = _deformedVertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }

    public class ZoneAtmosphereTrigger : MonoBehaviour
    {
        private ParticleSystem _particles;
        private float _boundaryX = -3.6f;
        private bool _wasInside;

        public void Configure(ParticleSystem particles, float boundaryX = -3.6f)
        {
            _particles = particles;
            _boundaryX = boundaryX;
            // Initialize state
            _wasInside = transform.position.x > _boundaryX;
            UpdateAtmosphere(_wasInside, true);
        }

        private void Update()
        {
            if (_particles == null) return;

            bool isInside = transform.position.x > _boundaryX;
            if (isInside != _wasInside)
            {
                _wasInside = isInside;
                UpdateAtmosphere(isInside, false);
            }
        }

        private void UpdateAtmosphere(bool isInside, bool immediate)
        {
            if (_particles == null) return;

            if (isInside)
            {
                // Stop emitting. If immediate, clear existing particles too.
                if (immediate)
                {
                    _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                else
                {
                    _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            else
            {
                if (!_particles.isPlaying)
                {
                    _particles.Play();
                }
            }
        }
    }
}
