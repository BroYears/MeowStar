using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Economy;
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
        private Text _txtGold;
        private Text _txtStack;
        private Image _stackIcon;
        private Text _txtGoalTitle;
        private Text _txtGoalHint;
        private Image _goalBar;
        private RectTransform _goalRect;
        private ButtonRef _btnPause;
        private bool _paused;

        // Change-detection caches so Update() never composes strings needlessly.
        private double _lastGold = double.MinValue;
        private ArcadeItemType _lastStackType = (ArcadeItemType)(-1);
        private int _lastStackCount = -1;
        private string _lastGoalName = "?";
        private double _lastGoalRemaining = -1;
        private bool _lastGoalLocked;
        private bool _lastGoalAffordable;
        private Rect _lastSafeArea;

        /// <summary>Layer above the HUD widgets, used by the joystick visuals.</summary>
        public RectTransform OverlayLayer => _overlayLayer;

        public void Configure(StackHolder stack)
        {
            playerStack = stack;
        }

        private void Awake()
        {
            BuildCanvas();
            BuildWidgets();
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

            if (playerStack != null &&
                (playerStack.CurrentType != _lastStackType || playerStack.Count != _lastStackCount))
            {
                _lastStackType = playerStack.CurrentType;
                _lastStackCount = playerStack.Count;
                _txtStack.text = $"{ItemName(_lastStackType)} {_lastStackCount}/{playerStack.Capacity}";
                if (_stackIcon != null) _stackIcon.color = ItemColor(_lastStackType);
            }

            UpdateLoopGoal(gold);
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

            // Pause toggle (top-right; left half is the joystick's, right half is free).
            _btnPause = UIFactory.Button("Btn_Pause", _safeRoot, "II", UITheme.Wood, TogglePause, 34, UITheme.Cream);
            UIFactory.SetAnchors(_btnPause.rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            _btnPause.rect.anchoredPosition = new Vector2(-28, -24);
            UIFactory.Size(_btnPause.rect, 84, 84);

            // Joystick (and future world bubbles) draw above the widgets.
            _overlayLayer = UIFactory.Rect("Overlay", _canvas.transform);
            UIFactory.FillParent(_overlayLayer);
        }

        private static string ItemName(ArcadeItemType type) => type switch
        {
            ArcadeItemType.Fish => "생선",
            ArcadeItemType.GrilledFish => "생선구이",
            ArcadeItemType.Berry => "베리",
            ArcadeItemType.BerryJuice => "베리주스",
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

        // -------------------------------------------------------------- loop goal
        private void UpdateLoopGoal(double gold)
        {
            string next = FindNextUnlock(out double remaining, out bool locked);
            bool hasGoal = !string.IsNullOrEmpty(next);
            bool affordable = gold >= remaining;

            // Wallet-vs-remaining-cost fill; set every frame (just a float, no alloc)
            // so it tracks gold continuously rather than only on text changes.
            if (_goalBar != null)
                _goalBar.fillAmount = !hasGoal ? 1f
                    : remaining > 0 ? Mathf.Clamp01((float)(gold / remaining)) : 1f;

            if (next == _lastGoalName && remaining == _lastGoalRemaining &&
                locked == _lastGoalLocked && affordable == _lastGoalAffordable) return;

            bool becameAffordable = hasGoal && !locked && affordable && !_lastGoalAffordable;
            _lastGoalName = next;
            _lastGoalRemaining = remaining;
            _lastGoalLocked = locked;
            _lastGoalAffordable = affordable;

            _txtGoalTitle.text = !hasGoal
                ? "Loop1 완료: 테이블/베리/직원 해금 완료"
                : locked
                    ? $"다음 목표: {next} - 선행 시설 필요"
                    : $"다음 목표: {next} - {Num.Short(remaining)}G 남음";

            _txtGoalHint.text = !hasGoal
                ? "이제 자원 종류, 업그레이드, 퀘스트를 확장할 차례"
                : locked
                    ? "선행 시설을 먼저 지어야 해금할 수 있어요"
                    : affordable
                        ? "노란/파란 패드 위에 서서 바로 해금"
                        : "손님에게 팔고 돈 더미를 주워 해금 비용 모으기";

            // Affordable cue: bar turns gold, and the panel pops once on the rising edge.
            if (_goalBar != null)
                _goalBar.color = (hasGoal && !locked && affordable) ? UITheme.Gold : UITheme.Leaf;
            if (becameAffordable && _goalRect != null)
                UITween.PunchScale(_goalRect, Vector3.one, 1.04f, 0.16f);
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
    }
}
