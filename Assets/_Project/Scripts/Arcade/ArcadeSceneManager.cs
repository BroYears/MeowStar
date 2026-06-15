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
    /// Binds pre-existing scene objects for the 2.5D arcade (Player, Zones, Spawners)
    /// to the save-backed progress and upgrade services. This replaces the programmatic
    /// bootstrap, allowing developers to design the map visually in the Unity Editor (Option A).
    /// </summary>
    public class ArcadeSceneManager : MonoBehaviour
    {
        [Header("Services & Configuration")]
        [SerializeField] private bool autoBindOnStart = true;
        
        [Header("Scene References")]
        [SerializeField] private ArcadePlayerController player;
        [SerializeField] private ArcadeCustomerSpawner spawner;
        [SerializeField] private BuildZone[] buildZones;
        [SerializeField] private HireZone[] hireZones;
        
        // Upgrade Zones in the scene
        [System.Serializable]
        public struct UpgradeZoneRef
        {
            public string zoneId;
            public UpgradeZone upgradeZone;
            public CookStation cookStation;
            public SpriteBillboard facilitySprite; // Visual body that swells with upgrades
            public string displayName;
            public double baseCost;
            public double costGrowth;
            public int maxLevel;
            public double drainPerTick;
            public WorldBubble costBubble;
        }
        [SerializeField] private List<UpgradeZoneRef> upgradeZones = new();

        [System.Serializable]
        public struct MenuOptionRef
        {
            public ArcadeItemType item;
            public double pay;
            public MonoBehaviour gate; // CookStation or other active constraint
        }
        
        [Header("Customer Spawner Configuration")]
        [SerializeField] private ArcadeCustomer customerPrefab;
        [SerializeField] private TableZone[] tables;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float spawnInterval = 4.5f;
        [SerializeField] private List<MenuOptionRef> menuOptions = new();

        private void Start()
        {
            if (autoBindOnStart)
            {
                InitializeArcadeWorld();
            }
        }

        public void InitializeArcadeWorld()
        {
            // 1. Disable legacy 2D restaurant elements
            ApplyArcadeMode();

            // 2. Fetch Save-backed progress services
            var saveData = SaveManager.Instance != null ? SaveManager.Instance.Data : new SaveData();
            var progress = new ArcadeProgressService(saveData.arcadeCompletedZones);
            var upgrades = new ArcadeUpgradeService(saveData.arcadeUpgradeLevels);

            // 3. Bind Build Zones
            foreach (var build in buildZones)
            {
                if (build == null) continue;
                build.BindProgress(progress, build.gameObject.name);
                if (progress.IsComplete(build.gameObject.name))
                {
                    build.RestoreCompleted();
                }
            }

            // 4. Bind Hire Zones
            foreach (var hire in hireZones)
            {
                if (hire == null) continue;
                hire.BindProgress(progress, hire.gameObject.name);
                if (progress.IsComplete(hire.gameObject.name))
                {
                    hire.RestoreCompleted();
                }
            }

            // 5. Bind Upgrade Zones
            foreach (var upRef in upgradeZones)
            {
                if (upRef.upgradeZone == null) continue;
                
                var cook = upRef.cookStation;
                var facility = upRef.facilitySprite;
                Vector3 baseScale = facility != null ? facility.transform.localScale : Vector3.one;
                
                upRef.upgradeZone.Configure(
                    upRef.displayName,
                    upRef.baseCost,
                    upRef.costGrowth,
                    upRef.maxLevel,
                    upRef.drainPerTick,
                    upRef.costBubble,
                    level =>
                    {
                        if (cook != null) cook.SetUpgradeLevel(level);
                        if (facility != null) facility.transform.localScale = baseScale * (1f + 0.06f * level);
                    }
                );
                
                upRef.upgradeZone.BindProgress(upgrades, upRef.zoneId);
                upRef.upgradeZone.RestoreLevel();
            }

            // 6. Setup Customer Spawner
            if (spawner != null)
            {
                var menuList = new List<ArcadeCustomerSpawner.MenuOption>();
                foreach (var opt in menuOptions)
                {
                    menuList.Add(new ArcadeCustomerSpawner.MenuOption
                    {
                        item = opt.item,
                        pay = opt.pay,
                        gate = opt.gate
                    });
                }
                spawner.Configure(customerPrefab, tables, spawnPoint, exitPoint, spawnInterval, menuList);
            }

            // 7. Setup HUD & Joystick (only if player exists)
            if (player != null)
            {
                var stack = player.GetComponent<StackHolder>();
                
                // Build HUD Canvas
                var hudGo = new GameObject("ArcadeHUD");
                var hudView = hudGo.AddComponent<ArcadeHUD>();
                hudView.Configure(stack);

                // Build Mobile Joystick
                var joyGo = new GameObject("ArcadeJoystick");
                var joystick = joyGo.AddComponent<ArcadeJoystick>();
                joystick.AttachVisual(hudView.OverlayLayer);
                player.Configure(joystick);

                // Build Idle Service (offline earnings)
                var idleGo = new GameObject("ArcadeIdleService");
                idleGo.transform.SetParent(transform);
                idleGo.AddComponent<ArcadeIdleService>().Configure(hudView.ModalLayer);

                // Camera follow
                var cam = Camera.main;
                if (cam != null)
                {
                    var follow = cam.gameObject.GetComponent<ArcadeCameraFollow>();
                    if (follow == null) follow = cam.gameObject.AddComponent<ArcadeCameraFollow>();
                    follow.Configure(player.transform);
                }
            }
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
    }
}
