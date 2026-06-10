using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Customer;
using Nyangsta.Progression;

namespace Nyangsta.UI
{
    public enum GameTab { Home, Hunt, Upgrade, Travel, Staff }

    /// <summary>A full-screen screen/panel managed by <see cref="GameUIController"/>.</summary>
    public interface IGamePanel
    {
        void Build(RectTransform layer, GameUIController controller);
        RectTransform Root { get; }
        void OnShow();
        void OnHide();
    }

    /// <summary>
    /// The heart of the playable UI. Creates the EventSystem (new Input System
    /// module — required since the project runs Input System only), a responsive
    /// overlay canvas, the HUD, the bottom tab bar and every screen, then wires
    /// game events to juice (coin pops, toasts, sounds). Built entirely in code so
    /// it needs no prefabs.
    /// </summary>
    public class GameUIController : Singleton<GameUIController>
    {
        public float TopSafe => 150f;
        public float BottomSafe => 178f;

        private Canvas _canvas;
        private RectTransform _panelsLayer, _modalLayer, _fxLayer;
        private CanvasGroup _hudGroup, _navGroup;
        private HudBar _hud;

        private readonly Dictionary<GameTab, IGamePanel> _panels = new();
        private readonly Dictionary<GameTab, NavButton> _navButtons = new();
        private GameTab _current = GameTab.Home;
        private bool _ready;
        private bool _built;
        private double _pendingOffline;
        private int _lastTreeLevel = 1;

        // Build the whole interface in Start so every manager has finished Awake
        // (the singletons read each other while constructing the HUD/panels).
        private void Start()
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildPanelsLayer();   // bottom
            BuildPanels();
            BuildHud();           // above panels
            BuildNav();           // above hud
            BuildTopLayers();     // modals + FX on top of everything
            _lastTreeLevel = ProgressionManager.Instance != null ? ProgressionManager.Instance.WorldTreeLevel : 1;
            SelectTab(GameTab.Home, instant: true, silent: true);
            _built = true;
            if (_pendingOffline > 0) { OfflinePopup.Present(_modalLayer, _pendingOffline); _pendingOffline = 0; }
            StartCoroutine(MarkReady());
        }

        private IEnumerator MarkReady()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            _ready = true;
        }

        // ----------------------------------------------------------- event system
        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            DontDestroyOnLoad(go);

            // Project uses the Input System package only, so the legacy
            // StandaloneInputModule won't deliver clicks. Add the Input System UI
            // module via reflection to avoid a hard assembly dependency.
            var moduleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                var module = go.AddComponent(moduleType);
                moduleType.GetMethod("AssignDefaultActions")?.Invoke(module, null);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();   // fallback (old input)
            }
        }

        // ----------------------------------------------------------------- canvas
        private void BuildCanvas()
        {
            var go = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<ResponsiveScaler>().Init(scaler);
        }

        private void BuildPanelsLayer()
        {
            _panelsLayer = FullLayer("Panels");   // sits at the bottom, behind HUD/nav
        }

        // Modal + FX layers are created last so they render above the HUD and nav
        // (offline popup must cover everything; toasts/coins float on top).
        private void BuildTopLayers()
        {
            _modalLayer = FullLayer("Modals");
            _fxLayer = FullLayer("FX");
            var fxCg = _fxLayer.gameObject.AddComponent<CanvasGroup>();
            fxCg.blocksRaycasts = false;
            fxCg.interactable = false;

            FloatingText.SetLayer(_fxLayer);
            Toast.SetLayer(_fxLayer);
        }

        private RectTransform FullLayer(string name)
        {
            var rt = UIFactory.Rect(name, _canvas.transform);
            UIFactory.FillParent(rt);
            return rt;
        }

        // ------------------------------------------------------------------- HUD
        private void BuildHud()
        {
            var hudRoot = UIFactory.Rect("HUD", _canvas.transform);
            UIFactory.FillParent(hudRoot);
            _hudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();
            _hud = hudRoot.gameObject.AddComponent<HudBar>();
            _hud.Build(hudRoot);
        }

        private void BuildPanels()
        {
            _panels[GameTab.Home] = AddPanel<HomePanel>("HomePanel");
            _panels[GameTab.Hunt] = AddPanel<HuntMiniGame>("HuntPanel");
            _panels[GameTab.Upgrade] = AddPanel<UpgradePanel>("UpgradePanel");
            _panels[GameTab.Travel] = AddPanel<TravelPanel>("TravelPanel");
            _panels[GameTab.Staff] = AddPanel<StaffPanel>("StaffPanel");

            foreach (var panel in _panels.Values)
            {
                panel.Build(_panelsLayer, this);
                panel.Root.gameObject.SetActive(false);
            }
        }

        private T AddPanel<T>(string name) where T : MonoBehaviour, IGamePanel
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        // ------------------------------------------------------------------- nav
        private void BuildNav()
        {
            var navRoot = UIFactory.Panel("NavBar", _canvas.transform, new Color(0.30f, 0.19f, 0.11f, 0.96f), out _, shadow: true);
            UIFactory.AnchorBottomStretch(navRoot, BottomSafe);
            _navGroup = navRoot.gameObject.AddComponent<CanvasGroup>();

            (GameTab tab, string label, Color color)[] defs =
            {
                (GameTab.Home,    "식당",     new Color(0.90f, 0.55f, 0.55f)),
                (GameTab.Hunt,    "사냥",     UITheme.Leaf),
                (GameTab.Upgrade, "업그레이드", new Color(0.95f, 0.66f, 0.36f)),
                (GameTab.Travel,  "모험",     UITheme.Gem),
                (GameTab.Staff,   "직원",     new Color(0.74f, 0.55f, 0.90f)),
            };

            int n = defs.Length;
            for (int i = 0; i < n; i++)
            {
                var d = defs[i];
                var btn = new NavButton();
                btn.Build(navRoot, d.label, d.color, () => SelectTab(d.tab));
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                UIFactory.SetAnchors(btn.root, new Vector2(t0, 0), new Vector2(t1, 1), new Vector2(0.5f, 0.5f));
                UIFactory.Stretch(btn.root, 6, 6, 8, 6);
                _navButtons[d.tab] = btn;
            }
        }

        // --------------------------------------------------------------- tab flow
        public void SelectTab(GameTab tab, bool instant = false, bool silent = false)
        {
            if (_current == tab && _ready) { /* allow re-show refresh */ }

            if (_panels.TryGetValue(_current, out var prev) && prev.Root.gameObject.activeSelf && _current != tab)
                prev.OnHide();

            foreach (var kv in _panels)
            {
                bool active = kv.Key == tab;
                if (active)
                {
                    kv.Value.Root.gameObject.SetActive(true);
                    kv.Value.OnShow();
                    if (!instant)
                    {
                        var cg = GetCanvasGroup(kv.Value.Root);
                        cg.alpha = 0f;
                        UITween.Fade(cg, 1f, 0.18f);
                        UITween.PopIn(kv.Value.Root, 0.22f, 0.96f);
                    }
                }
                else if (kv.Key != _current)
                {
                    kv.Value.Root.gameObject.SetActive(false);
                }
            }

            // Hide the previous one after the new one is shown (avoids a blank frame).
            if (_current != tab && _panels.TryGetValue(_current, out var old))
                old.Root.gameObject.SetActive(false);

            _current = tab;
            foreach (var kv in _navButtons) kv.Value.SetSelected(kv.Key == tab);
            if (!silent) Audio.Sfx.Whoosh();
        }

        private static CanvasGroup GetCanvasGroup(RectTransform rt)
        {
            // NOTE: never use `??` with GetComponent — Unity's overloaded null check
            // is bypassed and a "fake null" slips through. TryGetComponent is safe.
            if (!rt.TryGetComponent(out CanvasGroup cg))
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Fade the HUD + nav in/out (used for the fullscreen hunt).</summary>
        public void SetChromeVisible(bool visible)
        {
            UITween.Fade(_hudGroup, visible ? 1f : 0f, 0.22f);
            UITween.Fade(_navGroup, visible ? 1f : 0f, 0.22f);
            _hudGroup.blocksRaycasts = visible;
            _navGroup.blocksRaycasts = visible;
        }

        public RectTransform ModalLayer => _modalLayer;

        public Vector2 WorldToScreen(Vector3 world)
        {
            var cam = Camera.main;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(world) : new Vector2(Screen.width * 0.5f, Screen.height * 0.4f);
        }

        // ------------------------------------------------------- event → feedback
        private void OnEnable()
        {
            GameEvents.CustomerServed += OnServed;
            GameEvents.CustomerLeftAngry += OnAngry;
            GameEvents.UpgradePurchased += OnUpgrade;
            GameEvents.WorldTreeChanged += OnTree;
            GameEvents.StaffRecruited += OnStaff;
            GameEvents.RegionUnlocked += OnRegionUnlocked;
            GameEvents.OfflineIncomeReady += OnOfflineReady;
        }

        private void OnDisable()
        {
            GameEvents.CustomerServed -= OnServed;
            GameEvents.CustomerLeftAngry -= OnAngry;
            GameEvents.UpgradePurchased -= OnUpgrade;
            GameEvents.WorldTreeChanged -= OnTree;
            GameEvents.StaffRecruited -= OnStaff;
            GameEvents.RegionUnlocked -= OnRegionUnlocked;
            GameEvents.OfflineIncomeReady -= OnOfflineReady;
        }

        private void OnServed(CustomerData c)
        {
            Audio.Sfx.Coin();
            float mult = CustomerManager.Instance != null ? CustomerManager.Instance.RevenueMultiplier : 1f;
            int paid = c != null && c.preferredMenu != null
                ? Mathf.RoundToInt(c.preferredMenu.sellPrice * c.payMultiplier * Mathf.Max(0f, mult))
                : 0;
            // Pop near the restaurant area (lower third of screen).
            Vector2 p = new Vector2(Screen.width * Random.Range(0.32f, 0.68f), Screen.height * Random.Range(0.30f, 0.42f));
            if (paid > 0) FloatingText.Gold(p, paid);
        }

        private void OnAngry(CustomerData c)
        {
            Audio.Sfx.Error();
            Vector2 p = new Vector2(Screen.width * Random.Range(0.32f, 0.68f), Screen.height * 0.34f);
            FloatingText.Spawn(p, "퇴장…", UITheme.Danger, 34);
        }

        private void OnUpgrade(string id, int level)
        {
            Audio.Sfx.Purchase();
        }

        private void OnTree(int level, int essence)
        {
            if (level > _lastTreeLevel)
            {
                _lastTreeLevel = level;
                Audio.Sfx.LevelUp();
                if (_ready) Toast.Show($"세계수가 자랐어요!  Lv.{level}", UITheme.Leaf);
            }
        }

        private void OnStaff(string id)
        {
            Audio.Sfx.Unlock();
            var staff = ProgressionManager.Instance?.GetStaff(id);
            if (_ready) Toast.Show($"새 직원 영입!  {staff?.displayName}", new Color(0.74f, 0.55f, 0.90f));
        }

        private void OnRegionUnlocked(RegionType r)
        {
            if (!_ready) return;   // skip the initial Forest unlock on load
            Audio.Sfx.Unlock();
            string name = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetRegionName(r) : r.ToString();
            Toast.Show($"새 지역 해금!  {name}", UITheme.Gem);
        }

        private void OnOfflineReady(double gold)
        {
            if (_built && _modalLayer != null) OfflinePopup.Present(_modalLayer, gold);
            else _pendingOffline += gold;   // arrived before the UI finished building
        }
    }

    /// <summary>Keeps the canvas readable in both portrait and landscape.</summary>
    public class ResponsiveScaler : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private float _lastAspect = -1f;

        public void Init(CanvasScaler scaler) => _scaler = scaler;

        private void Update()
        {
            if (_scaler == null) return;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (Mathf.Abs(aspect - _lastAspect) < 0.01f) return;
            _lastAspect = aspect;
            // Portrait → match width; landscape → match height.
            _scaler.matchWidthOrHeight = aspect <= 1f ? 0f : 1f;
        }
    }

    /// <summary>A bottom-bar tab: stacked icon dot + label with a selected highlight.</summary>
    public class NavButton
    {
        public RectTransform root;
        private Image _icon, _highlight;
        private Text _label;
        private Color _color;

        public void Build(Transform parent, string label, Color color, System.Action onClick)
        {
            _color = color;
            root = UIFactory.Rect("Nav_" + label, parent);

            _highlight = UIFactory.Image("Highlight", root, UITheme.Rounded, new Color(1, 1, 1, 0f), Image.Type.Sliced);
            UIFactory.Stretch(_highlight.rectTransform, 0, 0, 0, 0);

            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var fx = root.gameObject.AddComponent<ButtonFx>();
            fx.Init(root);

            _icon = UIFactory.Image("Icon", root, UITheme.Circle, color);
            UIFactory.SetAnchors(_icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(_icon.rectTransform, 46, 46);
            _icon.rectTransform.anchoredPosition = new Vector2(0, -22);
            _icon.raycastTarget = false;

            _label = UIFactory.Text("Label", root, label, 24, UITheme.Cream, TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.SetAnchors(_label.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            UIFactory.Size(_label.rectTransform, 0, 40);
            _label.rectTransform.anchoredPosition = new Vector2(0, 12);
        }

        public void SetSelected(bool selected)
        {
            _highlight.color = selected ? new Color(_color.r, _color.g, _color.b, 0.28f) : new Color(1, 1, 1, 0f);
            _label.color = selected ? Color.white : UITheme.Parchment;
            _icon.transform.localScale = Vector3.one * (selected ? 1.18f : 1f);
            if (selected) UITween.PunchScale(_icon.transform, Vector3.one * 1.18f, 1.15f, 0.2f);
        }
    }

    /// <summary>
    /// The home screen overlay: a live stats ribbon (GPS / customers / cook time)
    /// over the world view, plus a friendly call-to-action to go hunt. Mostly
    /// transparent so the restaurant remains visible behind it.
    /// </summary>
    public class HomePanel : MonoBehaviour, IGamePanel
    {
        private GameUIController _controller;
        private RectTransform _root;
        private Text _stats, _tip;
        private float _timer;

        public RectTransform Root => _root;

        public void Build(RectTransform layer, GameUIController controller)
        {
            _controller = controller;
            _root = UIFactory.Rect("HomeRoot", layer);
            UIFactory.FillParent(_root);

            // Stats ribbon just below the HUD.
            var ribbon = UIFactory.Panel("Ribbon", _root, new Color(0f, 0f, 0f, 0.32f), out _, shadow: false);
            UIFactory.SetAnchors(ribbon, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(ribbon, 660, 66);
            ribbon.anchoredPosition = new Vector2(0, -(controller.TopSafe + 8));
            _stats = UIFactory.Text("Stats", ribbon, "", 26, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_stats.rectTransform, 16, 16, 4, 4);

            // Big CTA above the nav bar.
            var cta = UIFactory.Button("HuntCTA", _root, "사냥하러 가기", UITheme.Leaf,
                () => _controller.SelectTab(GameTab.Hunt), 34, Color.white);
            UIFactory.SetAnchors(cta.rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(cta.rect, 520, 96);
            cta.rect.anchoredPosition = new Vector2(0, controller.BottomSafe + 30);

            _tip = UIFactory.Text("Tip", _root, "재료를 사냥해 오면 손님에게 자동으로 요리를 내줘요.", 22,
                UITheme.Cream, TextAnchor.MiddleCenter);
            var to = _tip.gameObject.AddComponent<Outline>();
            to.effectColor = new Color(0, 0, 0, 0.5f); to.effectDistance = new Vector2(1.5f, -1.5f);
            UIFactory.SetAnchors(_tip.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(_tip.rectTransform, 760, 40);
            _tip.rectTransform.anchoredPosition = new Vector2(0, controller.BottomSafe + 96);
        }

        public void OnShow() { _timer = 0f; Refresh(); }
        public void OnHide() { }

        private void Update()
        {
            if (!_root.gameObject.activeInHierarchy) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f) { _timer = 0.4f; Refresh(); }
        }

        private void Refresh()
        {
            if (_stats == null) return;
            var idle = IdleIncomeManager.Instance;
            var cust = CustomerManager.Instance;
            double gps = idle != null ? idle.GoldPerSecond : 0;
            int active = cust != null ? cust.Active.Count : 0;
            int seats = cust != null ? cust.SeatCount : 0;
            float cook = cust != null ? cust.CurrentCookSeconds : 0f;
            _stats.text = $"<color=#FFD23F>{Num.Short(gps)} G/s</color>   ·   손님 {active}/{seats}   ·   조리 {cook:F1}s";
        }
    }
}
