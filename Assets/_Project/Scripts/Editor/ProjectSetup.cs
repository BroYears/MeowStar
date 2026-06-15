using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Hunting;
using Nyangsta.Customer;
using Nyangsta.Progression;
using Nyangsta.Save;
using Nyangsta.Ads;
using Nyangsta.UI;
using Nyangsta.Audio;
using Nyangsta.Arcade;

namespace Nyangsta.EditorTools
{
    /// <summary>
    /// One-click project bootstrap. Generates the sample content from GDD §5 as
    /// ScriptableObject assets, builds the GameDatabase, and creates a Bootstrap
    /// scene with all managers wired up.
    ///
    /// Run via menu: Nyangsta > Setup > Generate Everything.
    /// Safe to re-run: existing assets are overwritten by path.
    /// </summary>
    public static class ProjectSetup
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string RestaurantScenePath = "Assets/_Project/Scenes/Restaurant.unity";

        [MenuItem("Nyangsta/Setup/Generate Everything", priority = 0)]
        public static void GenerateEverything()
        {
            EnsureFolders();
            ImportArtAssets();
            var db = GenerateContent();
            CreateBootstrapScene(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("냥스타 셋업",
                "냥스타 키친 셋업 완료!\n\n" +
                "콘텐츠 SO + GameDatabase + 저장/경제 매니저 + M1 아케이드 그레이박스 월드가\n" +
                "그리고 Bootstrap 씬이 생성되었습니다.\n\n" +
                "Scenes/Bootstrap.unity 를 열고 Play 를 누르세요.\n" +
                "조이스틱/마우스 드래그로 생선 채집 → 그릴 → 테이블 → 돈 줍기 → 건설까지 확인할 수 있습니다.", "확인");
            Debug.Log("[Nyangsta] Setup complete.");
        }

        [MenuItem("Nyangsta/Setup/Generate Restaurant Scene", priority = 1)]
        public static void GenerateRestaurantScene()
        {
            EnsureFolders();
            ImportArtAssets();
            // 기존 GameDatabase 가 있으면 재사용(재생성 시 GUID 변경으로 아케이드 씬 참조가 깨지는 것 방지).
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>($"{DataRoot}/GameDatabase.asset");
            if (db == null) db = GenerateContent();
            CreateRestaurantScene(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("냥스타 레스토랑 셋업",
                "레스토랑(탭형) 씬이 생성되었습니다.\n\n" +
                "Scenes/Restaurant.unity 를 열고 Play 를 누르세요.\n" +
                "홈/사냥/업그레이드/이동/직원 탭 UI와 손님 서빙 루프가 실행됩니다.", "확인");
            Debug.Log("[Nyangsta] Restaurant scene generated.");
        }

        /// <summary>
        /// 레스토랑(탭형 타이쿤) 전용 씬. 아케이드 부트스트랩 대신 손님/사냥/방치 매니저를
        /// 모두 배치하고 RestaurantView 를 추가한다. 탭 UI(GameUIController)는 GameUIBootstrap 이
        /// 런타임에 자동 생성한다(씬에 ArcadePrototypeBootstrap 이 없으면 활성화됨).
        /// </summary>
        private static void CreateRestaurantScene(GameDatabase db)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var managers = new GameObject("_Managers");
            managers.AddComponent<SaveManager>();          // first (execution order enforced below)
            var gm = managers.AddComponent<GameManager>();
            managers.AddComponent<ProgressionManager>();
            var eco = managers.AddComponent<EconomyManager>();
            var hunt = managers.AddComponent<HuntingManager>();
            var cust = managers.AddComponent<CustomerManager>();
            managers.AddComponent<IdleIncomeManager>();
            managers.AddComponent<AdManager>();
            managers.AddComponent<UIManager>();
            managers.AddComponent<SfxManager>();

            // database 를 필요한 매니저에 모두 연결.
            AssignField(gm, "database", db);
            AssignField(eco, "database", db);
            AssignField(hunt, "database", db);
            AssignField(cust, "database", db);

            // 식당 월드 비주얼(배경·셰프·손님). 탭 UI 는 GameUIBootstrap 이 런타임 자동 생성.
            SetupCamera2D();
            var view = new GameObject("_RestaurantView").AddComponent<RestaurantView>();
            AssignField(view, "backgroundSprite",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Backgrounds/restaurant_bg.png"));
            AssignField(view, "chefSprite",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Characters/chef_nyastar.png"));

            EditorSceneManager.SaveScene(scene, RestaurantScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == RestaurantScenePath))
                scenes.Add(new EditorBuildSettingsScene(RestaurantScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            SetExecutionOrder();
        }

        /// <summary>정면 2D 직교 카메라(레스토랑 뷰용).</summary>
        private static void SetupCamera2D()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.83f, 0.93f);
        }

        // ------------------------------------------------------------------
        private static void EnsureFolders()
        {
            string[] subs = { "Ingredients", "Menus", "Customers", "Upgrades" };
            if (!AssetDatabase.IsValidFolder(DataRoot))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            foreach (var s in subs)
                if (!AssetDatabase.IsValidFolder($"{DataRoot}/{s}"))
                    AssetDatabase.CreateFolder(DataRoot, s);
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        }

        private static void ImportArtAssets()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Art",
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        // ------------------------------------------------------------------
        private static GameDatabase GenerateContent()
        {
            // --- Ingredients (GDD §5.1) ---
            var ing = new Dictionary<string, IngredientData>();
            ing["ING_001"] = MakeIngredient("ING_001", "작은 생선", Rarity.Common, RegionType.River, 0.50f);
            ing["ING_002"] = MakeIngredient("ING_002", "산딸기", Rarity.Common, RegionType.Forest, 0.45f);
            ing["ING_003"] = MakeIngredient("ING_003", "버섯", Rarity.Rare, RegionType.Forest, 0.20f);
            ing["ING_004"] = MakeIngredient("ING_004", "큰 연어", Rarity.Rare, RegionType.River, 0.15f);
            ing["ING_005"] = MakeIngredient("ING_005", "꿀", Rarity.Epic, RegionType.Cave, 0.08f);
            ing["ING_006"] = MakeIngredient("ING_006", "황금 송어", Rarity.Legendary, RegionType.Snow, 0.02f);

            // --- Menus (GDD §5.2) ---
            var menu = new Dictionary<string, MenuData>();
            menu["MENU_001"] = MakeMenu("MENU_001", "생선구이", 2f, 10, Req(ing["ING_001"], 1));
            menu["MENU_002"] = MakeMenu("MENU_002", "베리 타르트", 4f, 25, Req(ing["ING_002"], 2));
            menu["MENU_003"] = WithUnlock(MakeMenu("MENU_003", "버섯 수프", 5f, 40, Req(ing["ING_003"], 1), Req(ing["ING_001"], 1)), 2);
            menu["MENU_004"] = WithUnlock(MakeMenu("MENU_004", "연어 스테이크", 8f, 90, Req(ing["ING_004"], 1)), 2, RegionType.River, true);
            menu["MENU_005"] = WithUnlock(MakeMenu("MENU_005", "허니 디저트", 10f, 160, Req(ing["ING_005"], 1), Req(ing["ING_002"], 1)), 3, RegionType.Cave, true);
            menu["MENU_006"] = WithUnlock(MakeMenu("MENU_006", "황금 정식", 15f, 500, Req(ing["ING_006"], 1), Req(ing["ING_005"], 1)), 4, RegionType.Snow, true);

            // --- Customers (GDD §5.3) ---
            const string CustDir = "Assets/_Project/Art/Sprites/Customers";
            var cust = new List<CustomerData>
            {
                MakeCustomer("CUST_001", "토끼", menu["MENU_002"], RegionType.Forest, 1.0f, 20f, 1, $"{CustDir}/customer_rabbit.png"),
                MakeCustomer("CUST_002", "너구리", menu["MENU_001"], RegionType.Forest, 1.0f, 18f, 1, $"{CustDir}/customer_raccoon.png"),
                MakeCustomer("CUST_003", "여우", menu["MENU_003"], RegionType.River, 1.2f, 16f, 2, $"{CustDir}/customer_fox.png"),
                MakeCustomer("CUST_004", "곰", menu["MENU_004"], RegionType.River, 1.5f, 14f, 2, $"{CustDir}/customer_bear.png"),
                MakeCustomer("CUST_005", "펭귄", menu["MENU_005"], RegionType.Cave, 1.8f, 12f, 3, $"{CustDir}/customer_penguin.png"),
                MakeCustomer("CUST_006", "VIP 고양이", menu["MENU_006"], RegionType.Snow, 3.0f, 10f, 4, $"{CustDir}/customer_vipcat.png"),
            };

            // --- Upgrades (GDD §5.4) ---
            var up = new List<UpgradeData>
            {
                MakeUpgrade("UPG_HUNT_TOOL", "사냥 도구", UpgradeCategory.Hunting, 150, 1.15f, 1, 0),
                MakeUpgrade("UPG_STAMINA", "스태미나 한도", UpgradeCategory.Hunting, 300, 1.18f, 1, 0),
                MakeUpgrade("UPG_COOK_SPEED", "조리 속도", UpgradeCategory.Cooking, 200, 1.15f, 0.05f, 0),
                MakeUpgrade("UPG_COOK_STATION", "동시 조리대", UpgradeCategory.Cooking, 1000, 1.30f, 1, 0),
                MakeUpgrade("UPG_SEATS", "좌석 수", UpgradeCategory.Restaurant, 500, 1.20f, 1, 0),
                MakeUpgrade("UPG_REPUTATION", "식당 평판", UpgradeCategory.Restaurant, 800, 1.22f, 1, 0),
                MakeUpgrade("UPG_STAFF", "직원 고용", UpgradeCategory.Idle, 2000, 1.25f, 1, 0),
                MakeUpgrade("UPG_OFFLINE_CAP", "오프라인 상한", UpgradeCategory.Idle, 1500, 1.20f, 1, 0),
            };

            // --- Database ---
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            db.ingredients = new List<IngredientData>(ing.Values);
            db.menus = new List<MenuData>(menu.Values);
            db.customers = cust;
            db.upgrades = up;
            SaveAsset(db, $"{DataRoot}/GameDatabase.asset");
            return db;
        }

        // ------------------------------------------------------------------
        private static void CreateBootstrapScene(GameDatabase db)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var managers = new GameObject("_Managers");

            // SaveManager first — others read SaveData in Awake. Execution order is
            // enforced separately (see SetExecutionOrder), component order is cosmetic.
            managers.AddComponent<SaveManager>();
            var gm = managers.AddComponent<GameManager>();
            managers.AddComponent<ProgressionManager>();
            var eco = managers.AddComponent<EconomyManager>();
            managers.AddComponent<AdManager>();
            managers.AddComponent<UIManager>();
            managers.AddComponent<SfxManager>();          // procedural audio

            // Wire the database into every manager that needs it (private serialized field).
            AssignField(gm, "database", db);
            AssignField(eco, "database", db);

            // --- M1 arcade-idle graybox (GDD v0.2 §12) ---
            SetupCamera3D();
            var arcade = new GameObject("_ArcadeM1Bootstrap");
            arcade.AddComponent<ArcadePrototypeBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register the scene in Build Settings as scene 0.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            SetExecutionOrder();
        }

        /// <summary>Configure Main Camera as a tilted top-down 3D view.</summary>
        private static void SetupCamera3D()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.transform.position = new Vector3(-2.8f, 8.5f, -8.5f);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.83f, 0.93f);
        }

        /// <summary>Force SaveManager to run before the managers that read SaveData.</summary>
        private static void SetExecutionOrder()
        {
            var mono = FindMonoScript(typeof(SaveManager));
            if (mono != null) MonoImporter.SetExecutionOrder(mono, -100);
        }

        // ---- asset factory helpers ----
        private static IngredientData MakeIngredient(string id, string name, Rarity r, RegionType region, float rate)
        {
            var a = ScriptableObject.CreateInstance<IngredientData>();
            a.id = id; a.displayName = name; a.rarity = r; a.region = region; a.spawnRate = rate;
            a.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Sprites/Ingredients/{id}.png");
            SaveAsset(a, $"{DataRoot}/Ingredients/{id}.asset");
            return a;
        }

        private static MenuData MakeMenu(string id, string name, float cook, int price, params IngredientRequirement[] reqs)
        {
            var a = ScriptableObject.CreateInstance<MenuData>();
            a.id = id; a.displayName = name; a.cookTime = cook; a.sellPrice = price; a.requirements = reqs;
            SaveAsset(a, $"{DataRoot}/Menus/{id}.asset");
            return a;
        }

        private static MenuData WithUnlock(MenuData menu, int restaurantLevel, RegionType requiredRegion = RegionType.Forest, bool requiresRegion = false)
        {
            menu.unlock = new UnlockCondition
            {
                restaurantLevel = restaurantLevel,
                requiredRegion = requiredRegion,
                requiresRegion = requiresRegion
            };
            return menu;
        }

        private static CustomerData MakeCustomer(string id, string name, MenuData pref, RegionType homeRegion, float mult, float patience, int stage, string spritePath)
        {
            var a = ScriptableObject.CreateInstance<CustomerData>();
            a.id = id; a.displayName = name; a.preferredMenu = pref;
            a.homeRegion = homeRegion;
            a.payMultiplier = mult; a.patienceSeconds = patience; a.unlockStage = stage;
            if (!string.IsNullOrEmpty(spritePath))
                a.icon = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            SaveAsset(a, $"{DataRoot}/Customers/{id}.asset");
            return a;
        }

        private static UpgradeData MakeUpgrade(string id, string name, UpgradeCategory cat, double baseCost, float growth, float effect, int maxLevel)
        {
            var a = ScriptableObject.CreateInstance<UpgradeData>();
            a.id = id; a.displayName = name; a.category = cat;
            a.baseCost = baseCost; a.costGrowth = growth; a.effectPerLevel = effect; a.maxLevel = maxLevel;
            SaveAsset(a, $"{DataRoot}/Upgrades/{id}.asset");
            return a;
        }

        private static IngredientRequirement Req(IngredientData ing, int amount)
            => new IngredientRequirement { ingredient = ing, amount = amount };

        private static void SaveAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void AssignField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning($"[Nyangsta] Field '{fieldName}' not found on {target.GetType().Name}.");
        }

        private static MonoScript FindMonoScript(System.Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() == type) return ms;
            }
            return null;
        }
    }
}
