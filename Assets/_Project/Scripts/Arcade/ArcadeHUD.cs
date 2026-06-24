using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Nyangsta.Core;
using Nyangsta.Economy;
using Nyangsta.Progression;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Canvas-based mobile HUD for the arcade world: gold, carried stack and the
    /// current loop goal. Built entirely in code via <see cref="UIFactory"/> so it
    /// needs no prefab. Texts are only rebuilt when the underlying values change,
    /// keeping per-frame GC allocation at zero.
    /// </summary>
    public class ArcadeHUD : MonoBehaviour
    {
        [SerializeField] private StackHolder playerStack;

        private Canvas _canvas;
        private RectTransform _safeRoot;
        private RectTransform _overlayLayer;
        private RectTransform _modalLayer;
        private Text _txtGold;
        private Text _txtEssence;
        private RectTransform _essenceHolder;
        private Text _txtStack;
        private Image _stackIcon;
        private Text _txtGoalTitle;
        private Text _txtGoalHint;
        private Image _goalBar;
        private RectTransform _goalRect;
        private Text _txtActionTitle;
        private Text _txtActionHint;
        private RectTransform _actionRect;
        private ButtonRef _btnPause;
        private bool _paused;

        // Change-detection caches so Update() never composes strings needlessly.
        private double _lastGold = double.MinValue;
        private int _lastEssence = int.MinValue;
        private bool _lastEssenceShown;
        private ArcadeItemType _lastStackType = (ArcadeItemType)(-1);
        private int _lastStackCount = -1;
        private string _lastGoalName = "?";
        private double _lastGoalRemaining = -1;
        private bool _lastGoalLocked;
        private bool _lastGoalAffordable;
        private string _lastActionKey;
        private float _actionTimer;
        private Rect _lastSafeArea;

        private const float ActionRefreshInterval = 0.18f;

        /// <summary>Layer above the HUD widgets, used by the joystick visuals.</summary>
        public RectTransform OverlayLayer => _overlayLayer;

        /// <summary>Top-most layer for modal popups (offline income, etc.).</summary>
        public RectTransform ModalLayer => _modalLayer;

        public void Configure(StackHolder stack)
        {
            playerStack = stack;
        }

        private void Awake()
        {
            EnsureEventSystem();   // arcade mode disables GameUIController, which used to own it
            BuildCanvas();
            BuildWidgets();
        }

        // The project ships only the Input System package, so clicks need its UI module.
        // GameUIController creates this for the legacy UI, but that controller is disabled
        // in arcade mode — without this the pause button and popups wouldn't take input.
        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            DontDestroyOnLoad(go);

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

        private void Update()
        {
            ApplySafeArea();

            double gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            if (gold != _lastGold)
            {
                _lastGold = gold;
                _txtGold.text = Num.Short(gold);
            }

            UpdateEssence();

            if (playerStack != null &&
                (playerStack.CurrentType != _lastStackType || playerStack.Count != _lastStackCount))
            {
                _lastStackType = playerStack.CurrentType;
                _lastStackCount = playerStack.Count;
                _txtStack.text = $"{ItemName(_lastStackType)} {_lastStackCount}/{playerStack.Capacity}";
                if (_stackIcon != null) _stackIcon.color = ItemColor(_lastStackType);
            }

            UpdateLoopGoal(gold);
            UpdateActionGuide(gold);
        }

        // ----------------------------------------------------------------- canvas
        private void BuildCanvas()
        {
            var go = new GameObject("ArcadeHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;   // below GameUIController's canvas (100)

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);   // landscape
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;   // match height: the limiting axis in landscape

            _safeRoot = UIFactory.Rect("Hud_SafeArea", _canvas.transform);
            UIFactory.FillParent(_safeRoot);
        }

        // Inset the HUD root to the device safe area (notch / home indicator).
        private void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            if (area == _lastSafeArea) return;
            _lastSafeArea = area;

            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            _safeRoot.anchorMin = min;
            _safeRoot.anchorMax = max;
            _safeRoot.offsetMin = Vector2.zero;
            _safeRoot.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- widgets
        private void BuildWidgets()
        {
            // Gold pill (top-left).
            _txtGold = UIFactory.Pill("Pill_Gold", _safeRoot, UITheme.Gold, "G", out _, out var goldHolder);
            UIFactory.SetAnchors(goldHolder, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            goldHolder.anchoredPosition = new Vector2(28, -24);
            UIFactory.Size(goldHolder, 300, 60);
            AddOutline(_txtGold);

            // Essence pill, right of the gold pill. Hidden until the player banks
            // essence (earned from hunting) so it doesn't clutter the early loop.
            _txtEssence = UIFactory.Pill("Pill_Essence", _safeRoot, UITheme.Leaf, "✦", out _, out _essenceHolder);
            UIFactory.SetAnchors(_essenceHolder, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            _essenceHolder.anchoredPosition = new Vector2(340, -24);
            UIFactory.Size(_essenceHolder, 240, 60);
            AddOutline(_txtEssence);
            _essenceHolder.gameObject.SetActive(false);

            // Carried-stack pill, right under the gold pill. The icon is recoloured to
            // match the carried item type in Update (see ItemColor); start neutral.
            _txtStack = UIFactory.Pill("Pill_Stack", _safeRoot, UITheme.Muted, "■", out _stackIcon, out var stackHolder);
            UIFactory.SetAnchors(stackHolder, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            stackHolder.anchoredPosition = new Vector2(28, -90);
            UIFactory.Size(stackHolder, 300, 52);
            _txtStack.text = "- 0/0";
            AddOutline(_txtStack);

            // Loop-goal panel below the pills. Tall enough to host a progress bar.
            _goalRect = UIFactory.Panel("Panel_Goal", _safeRoot, UITheme.Panel, outline: true);
            UIFactory.SetAnchors(_goalRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            _goalRect.anchoredPosition = new Vector2(28, -150);
            UIFactory.Size(_goalRect, 500, 140);

            _txtGoalTitle = UIFactory.Text("Txt_GoalTitle", _goalRect, "", 30, UITheme.Ink,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Stretch(_txtGoalTitle.rectTransform, 22, 22, 14, 86);
            // Shrink-to-fit (with wrapping) so long Korean strings stay inside the
            // panel instead of overflowing past its right edge.
            FitText(_txtGoalTitle, 18, 30);

            _txtGoalHint = UIFactory.Text("Txt_GoalHint", _goalRect, "", 24, UITheme.InkSoft,
                TextAnchor.UpperLeft);
            UIFactory.Stretch(_txtGoalHint.rectTransform, 22, 22, 56, 52);
            FitText(_txtGoalHint, 16, 24);

            // Wallet-vs-cost progress toward the next unlock (fill set each frame in
            // UpdateLoopGoal; turns gold and pops when the goal becomes affordable).
            _goalBar = UIFactory.ProgressBar("Bar_Goal", _goalRect, new Color(0f, 0f, 0f, 0.16f),
                UITheme.Leaf, out var goalBarHolder);
            UIFactory.Stretch(goalBarHolder, 22, 22, 108, 16);

            // Contextual action ribbon: the loop goal tells "what to unlock next";
            // this tells the player what to physically do right now.
            _actionRect = UIFactory.Panel("Panel_ActionGuide", _safeRoot, new Color(0.13f, 0.09f, 0.06f, 0.78f), outline: true);
            UIFactory.SetAnchors(_actionRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            _actionRect.anchoredPosition = new Vector2(28, -304);
            UIFactory.Size(_actionRect, 500, 104);

            _txtActionTitle = UIFactory.Text("Txt_ActionTitle", _actionRect, "", 26, UITheme.Gold,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Stretch(_txtActionTitle.rectTransform, 22, 22, 16, 56);
            FitText(_txtActionTitle, 17, 26);

            _txtActionHint = UIFactory.Text("Txt_ActionHint", _actionRect, "", 22, UITheme.Cream,
                TextAnchor.UpperLeft);
            UIFactory.Stretch(_txtActionHint.rectTransform, 22, 22, 50, 16);
            FitText(_txtActionHint, 15, 22);

            // Pause toggle (top-right; left half is the joystick's, right half is free).
            _btnPause = UIFactory.Button("Btn_Pause", _safeRoot, "II", UITheme.Wood, TogglePause, 34, UITheme.Cream);
            UIFactory.SetAnchors(_btnPause.rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            _btnPause.rect.anchoredPosition = new Vector2(-28, -24);
            UIFactory.Size(_btnPause.rect, 84, 84);

            // Joystick (and future world bubbles) draw above the widgets.
            _overlayLayer = UIFactory.Rect("Overlay", _canvas.transform);
            UIFactory.FillParent(_overlayLayer);

            // Modal layer sits last (top-most) so popups cover the HUD and joystick.
            _modalLayer = UIFactory.Rect("Modal", _canvas.transform);
            UIFactory.FillParent(_modalLayer);
        }

        private static string ItemName(ArcadeItemType type) => type switch
        {
            ArcadeItemType.Fish => "생선",
            ArcadeItemType.GrilledFish => "생선구이",
            ArcadeItemType.Berry => "베리",
            ArcadeItemType.BerryJuice => "베리주스",
            ArcadeItemType.Wood => "나무",
            ArcadeItemType.Mushroom => "버섯",
            ArcadeItemType.MushroomSkewer => "버섯꼬치",
            ArcadeItemType.Salmon => "연어",
            ArcadeItemType.SalmonSteak => "연어스테이크",
            ArcadeItemType.Honey => "꿀",
            ArcadeItemType.HoneyDessert => "허니디저트",
            ArcadeItemType.None => "-",
            _ => type.ToString(),
        };

        // Pill icon tint per carried item, matching the world prefab/material tones.
        private static Color ItemColor(ArcadeItemType type) => type switch
        {
            ArcadeItemType.Fish        => new Color(0.30f, 0.62f, 1.00f),
            ArcadeItemType.GrilledFish => new Color(1.00f, 0.62f, 0.16f),
            ArcadeItemType.Berry       => new Color(0.82f, 0.28f, 0.50f),
            ArcadeItemType.BerryJuice  => new Color(0.95f, 0.42f, 0.66f),
            ArcadeItemType.Wood        => new Color(0.60f, 0.38f, 0.18f),
            ArcadeItemType.Mushroom    => new Color(0.82f, 0.36f, 0.42f),
            ArcadeItemType.MushroomSkewer => new Color(0.95f, 0.70f, 0.32f),
            ArcadeItemType.Salmon       => new Color(1.00f, 0.50f, 0.42f),
            ArcadeItemType.SalmonSteak  => new Color(0.90f, 0.45f, 0.30f),
            ArcadeItemType.Honey        => new Color(1.00f, 0.78f, 0.25f),
            ArcadeItemType.HoneyDessert => new Color(1.00f, 0.85f, 0.55f),
            _ => UITheme.Muted,
        };

        // Auto-shrink a HUD label (with wrapping) so it always fits its rect instead
        // of spilling outside the panel; uGUI best-fit needs Wrap to honour width.
        private static void FitText(Text t, int min, int max)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
        }

        // Soft dark outline so white pill values stay legible over any background.
        private static void AddOutline(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.55f);
            o.effectDistance = new Vector2(1.4f, -1.4f);
        }

        // Pause/resume via the shared GameManager flag (falls back to Time.timeScale
        // when no GameManager is present, e.g. a standalone arcade scene).
        private void TogglePause()
        {
            var gm = GameManager.Instance;
            bool next = !(gm != null ? gm.IsPaused : _paused);
            if (gm != null) gm.SetPaused(next);
            else Time.timeScale = next ? 0f : 1f;
            _paused = next;
            _btnPause?.SetText(next ? "▶" : "II");
        }

        // World-tree essence (hunt currency). Shown only once an essence-gated pad is
        // live or the player has banked some, so the early gold-only loop stays clean.
        private void UpdateEssence()
        {
            var p = ProgressionManager.Instance;
            int essence = p != null ? p.WorldTreeEssence : 0;
            bool gateActive = WorldTreeGateZone.ActiveZones.Count > 0;
            bool show = gateActive || essence > 0;

            if (show != _lastEssenceShown)
            {
                _lastEssenceShown = show;
                if (_essenceHolder != null) _essenceHolder.gameObject.SetActive(show);
            }
            if (show && essence != _lastEssence)
            {
                _lastEssence = essence;
                _txtEssence.text = essence.ToString();
            }
        }

        // -------------------------------------------------------------- loop goal
        private void UpdateLoopGoal(double gold)
        {
            string next = FindNextUnlock(out double remaining, out bool locked);

            // Gold build/hire goals take priority; once they're all done, steer the
            // player toward the essence-gated world-tree expansion instead of dead-ending.
            if (string.IsNullOrEmpty(next))
            {
                UpdateEssenceGoal();
                return;
            }

            bool hasGoal = true;
            bool affordable = gold >= remaining;

            // Wallet-vs-remaining-cost fill; set every frame (just a float, no alloc)
            // so it tracks gold continuously rather than only on text changes.
            if (_goalBar != null)
                _goalBar.fillAmount = remaining > 0 ? Mathf.Clamp01((float)(gold / remaining)) : 1f;

            if (next == _lastGoalName && remaining == _lastGoalRemaining &&
                locked == _lastGoalLocked && affordable == _lastGoalAffordable) return;

            bool becameAffordable = hasGoal && !locked && affordable && !_lastGoalAffordable;
            _lastGoalName = next;
            _lastGoalRemaining = remaining;
            _lastGoalLocked = locked;
            _lastGoalAffordable = affordable;

            _txtGoalTitle.text = locked
                    ? $"다음 목표: {next} - 선행 시설 필요"
                    : affordable
                        ? $"해금 가능: {next}"
                        : $"다음 목표: {next} - {Num.Short(System.Math.Max(0, remaining - gold))}G 더 필요";

            _txtGoalHint.text = locked
                    ? "선행 시설을 먼저 지어야 해금할 수 있어요"
                    : affordable
                        ? "노란/파란 패드 위에 서서 바로 해금"
                        : "돈을 모으거나 나무를 들고 건설 패드에 투입하기";

            // Affordable cue: bar turns gold, and the panel pops once on the rising edge.
            if (_goalBar != null)
                _goalBar.color = (!locked && affordable) ? UITheme.Gold : UITheme.Leaf;
            if (becameAffordable && _goalRect != null)
                UITween.PunchScale(_goalRect, Vector3.one, 1.04f, 0.16f);
        }

        // Essence-gated goal shown after all gold unlocks are done: grow the world tree
        // by banking hunt essence. Progress is essence-vs-cost, bar tinted leaf-green.
        private void UpdateEssenceGoal()
        {
            var gate = FindNextGate(out int needEssence, out int haveEssence, out string gateName);
            bool hasGate = gate != null;

            if (_goalBar != null)
            {
                _goalBar.fillAmount = !hasGate ? 1f
                    : needEssence > 0 ? Mathf.Clamp01((float)haveEssence / needEssence) : 1f;
                _goalBar.color = UITheme.Leaf;
            }

            string name = hasGate ? gateName : "ALLDONE";
            double remaining = hasGate ? needEssence - haveEssence : 0;
            if (name == _lastGoalName && remaining == _lastGoalRemaining) return;
            _lastGoalName = name;
            _lastGoalRemaining = remaining;
            _lastGoalAffordable = false;
            _lastGoalLocked = false;

            _txtGoalTitle.text = hasGate
                ? $"세계수 성장: {gateName} - 정수 {Mathf.Max(0, needEssence - haveEssence)}개 남음"
                : "모든 구역 해금 완료! 식당이 만석이에요";
            _txtGoalHint.text = hasGate
                ? "사냥으로 세계수 정수를 모아 초록 패드에서 성장시키세요"
                : "직원과 업그레이드로 수익을 극대화해 보세요";
        }

        // Nearest incomplete essence gate (lowest remaining essence first).
        private static WorldTreeGateZone FindNextGate(out int need, out int have, out string name)
        {
            need = 0; have = 0; name = null;
            var p = ProgressionManager.Instance;
            if (p == null) return null;

            int remainingBest = int.MaxValue;
            WorldTreeGateZone best = null;
            foreach (var zone in WorldTreeGateZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.IsComplete) continue;
                if (zone.RemainingEssence >= remainingBest) continue;
                remainingBest = zone.RemainingEssence;
                best = zone;
            }

            if (best == null) return null;
            need = p.NextWorldTreeCost;
            have = p.WorldTreeEssence;
            name = best.DisplayName;
            return best;
        }

        private static string FindNextUnlock(out double remainingCost, out bool locked)
        {
            remainingCost = double.MaxValue;
            locked = false;
            string best = null;

            foreach (var zone in BuildZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.IsComplete) continue;
                if (zone.RemainingCost >= remainingCost) continue;

                remainingCost = zone.RemainingCost;
                best = zone.DisplayName;
                locked = false;
            }

            foreach (var zone in HireZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.IsComplete) continue;
                if (zone.IsLockedByFacility && best != null) continue;
                if (zone.RemainingCost >= remainingCost && !zone.IsLockedByFacility) continue;

                remainingCost = zone.RemainingCost;
                best = zone.DisplayName;
                locked = zone.IsLockedByFacility;
            }

            if (best == null) remainingCost = 0;
            return best;
        }

        // ------------------------------------------------------------ action guide
        private void UpdateActionGuide(double gold)
        {
            _actionTimer += Time.deltaTime;
            if (_actionTimer < ActionRefreshInterval) return;
            _actionTimer = 0f;

            string title;
            string hint;
            string key;

            if (TryFindLooseMoney(out double money))
            {
                title = $"돈 회수: +{Num.Short(money)}G";
                hint = "테이블 옆 돈 더미를 지나가면 자동으로 회수됩니다";
                key = $"money:{money:0}";
            }
            else if (playerStack != null && !playerStack.IsEmpty)
            {
                var carried = playerStack.CurrentType;
                if (carried == ArcadeItemType.Wood)
                {
                    title = "건설 지원";
                    hint = "나무를 노란 건설 패드에 넣으면 비용이 크게 줄어듭니다";
                    key = "carry:wood";
                }
                else if (IsRawIngredient(carried))
                {
                    title = $"{ItemName(carried)} 조리";
                    hint = $"{StationNameFor(carried)}에 가져가 {ItemName(CookedFor(carried))}로 만드세요";
                    key = $"carry:raw:{carried}";
                }
                else if (HasWaitingOrderFor(carried))
                {
                    title = $"{ItemName(carried)} 서빙";
                    hint = "주문 말풍선이 떠 있는 테이블 앞 초록 패드에 서세요";
                    key = $"carry:serve:{carried}";
                }
                else
                {
                    title = $"{ItemName(carried)} 보관 중";
                    hint = "곧 같은 주문이 오면 테이블에 서빙하거나, 다른 라인을 자동화하세요";
                    key = $"carry:wait:{carried}:{playerStack.Count}";
                }
            }
            else if (TryFindWaitingOrder(out var wanted))
            {
                var raw = RawFor(wanted);
                title = $"주문 준비: {ItemName(wanted)}";
                hint = $"{GatherPlaceFor(raw)}에서 {ItemName(raw)}를 모아 {StationNameFor(raw)}로 가져가세요";
                key = $"order:{wanted}";
            }
            else
            {
                string unlock = FindNextUnlock(out double remaining, out bool locked);
                bool affordable = !locked && !string.IsNullOrEmpty(unlock) && gold >= remaining;
                if (affordable)
                {
                    title = $"해금 가능: {unlock}";
                    hint = "해당 건설/고용 패드 위에 서면 바로 진행됩니다";
                    key = $"unlock:{unlock}:ready";
                }
                else if (!string.IsNullOrEmpty(unlock))
                {
                    title = $"목표 자금 모으기";
                    hint = locked ? "먼저 연결된 생산 라인을 건설해야 고용할 수 있습니다" : $"{unlock}까지 {Num.Short(System.Math.Max(0, remaining - gold))}G 더 필요합니다";
                    key = $"unlock:{unlock}:{locked}:{remaining - gold:0}";
                }
                else
                {
                    title = "식당 운영";
                    hint = "주문이 오면 재료를 모아 조리하고 테이블에 서빙하세요";
                    key = "idle";
                }
            }

            SetActionGuide(title, hint, key);
        }

        private void SetActionGuide(string title, string hint, string key)
        {
            if (key == _lastActionKey) return;
            _lastActionKey = key;
            if (_txtActionTitle != null) _txtActionTitle.text = title;
            if (_txtActionHint != null) _txtActionHint.text = hint;
            if (_actionRect != null) UITween.PunchScale(_actionRect, Vector3.one, 1.025f, 0.12f);
        }

        private static bool TryFindLooseMoney(out double amount)
        {
            amount = 0;
            foreach (var pile in MoneyPile.ActivePiles)
            {
                if (pile == null || pile.IsCollecting || !pile.gameObject.activeInHierarchy) continue;
                amount = System.Math.Max(amount, pile.Amount);
            }
            return amount > 0;
        }

        private static bool TryFindWaitingOrder(out ArcadeItemType item)
        {
            foreach (var table in TableZone.ActiveZones)
            {
                if (table == null || !table.gameObject.activeInHierarchy || !table.HasWaitingOrder) continue;
                item = table.WaitingOrderItem;
                return true;
            }
            item = ArcadeItemType.None;
            return false;
        }

        private static bool HasWaitingOrderFor(ArcadeItemType item)
        {
            foreach (var table in TableZone.ActiveZones)
                if (table != null && table.gameObject.activeInHierarchy && table.HasWaitingOrderFor(item))
                    return true;
            return false;
        }

        private static bool IsRawIngredient(ArcadeItemType item) => item switch
        {
            ArcadeItemType.Fish or ArcadeItemType.Berry or ArcadeItemType.Mushroom
                or ArcadeItemType.Salmon or ArcadeItemType.Honey => true,
            _ => false,
        };

        private static ArcadeItemType CookedFor(ArcadeItemType raw) => raw switch
        {
            ArcadeItemType.Fish => ArcadeItemType.GrilledFish,
            ArcadeItemType.Berry => ArcadeItemType.BerryJuice,
            ArcadeItemType.Mushroom => ArcadeItemType.MushroomSkewer,
            ArcadeItemType.Salmon => ArcadeItemType.SalmonSteak,
            ArcadeItemType.Honey => ArcadeItemType.HoneyDessert,
            _ => ArcadeItemType.None,
        };

        private static ArcadeItemType RawFor(ArcadeItemType cooked) => cooked switch
        {
            ArcadeItemType.GrilledFish => ArcadeItemType.Fish,
            ArcadeItemType.BerryJuice => ArcadeItemType.Berry,
            ArcadeItemType.MushroomSkewer => ArcadeItemType.Mushroom,
            ArcadeItemType.SalmonSteak => ArcadeItemType.Salmon,
            ArcadeItemType.HoneyDessert => ArcadeItemType.Honey,
            _ => cooked,
        };

        private static string StationNameFor(ArcadeItemType raw) => raw switch
        {
            ArcadeItemType.Fish => "그릴",
            ArcadeItemType.Berry => "주스기",
            ArcadeItemType.Mushroom => "버섯 솥",
            ArcadeItemType.Salmon => "화덕",
            ArcadeItemType.Honey => "디저트바",
            _ => "조리대",
        };

        private static string GatherPlaceFor(ArcadeItemType raw) => raw switch
        {
            ArcadeItemType.Fish or ArcadeItemType.Salmon => "강가",
            ArcadeItemType.Berry => "베리밭",
            ArcadeItemType.Mushroom => "버섯밭",
            ArcadeItemType.Honey => "숲",
            ArcadeItemType.Wood => "나무숲",
            _ => "자원 구역",
        };
    }
}
