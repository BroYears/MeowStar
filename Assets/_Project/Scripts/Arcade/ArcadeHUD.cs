using UnityEngine;
using UnityEngine.UI;
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
        private Text _txtGoalTitle;
        private Text _txtGoalHint;

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
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
            goldHolder.anchoredPosition = new Vector2(24, -24);
            UIFactory.Size(goldHolder, 260, 64);

            // Carried-stack pill, right under the gold pill.
            _txtStack = UIFactory.Pill("Pill_Stack", _safeRoot, UITheme.Leaf, "■", out _, out var stackHolder);
            UIFactory.SetAnchors(stackHolder, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            stackHolder.anchoredPosition = new Vector2(24, -98);
            UIFactory.Size(stackHolder, 260, 52);
            _txtStack.text = "- 0/0";

            // Loop-goal panel below the pills.
            var goal = UIFactory.Panel("Panel_Goal", _safeRoot, UITheme.Panel, outline: true);
            UIFactory.SetAnchors(goal, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            goal.anchoredPosition = new Vector2(24, -164);
            UIFactory.Size(goal, 520, 128);

            _txtGoalTitle = UIFactory.Text("Txt_GoalTitle", goal, "", 30, UITheme.Ink,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Stretch(_txtGoalTitle.rectTransform, 22, 22, 16, 64);

            _txtGoalHint = UIFactory.Text("Txt_GoalHint", goal, "", 24, UITheme.InkSoft,
                TextAnchor.UpperLeft);
            UIFactory.Stretch(_txtGoalHint.rectTransform, 22, 22, 64, 14);

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

        // -------------------------------------------------------------- loop goal
        private void UpdateLoopGoal(double gold)
        {
            string next = FindNextUnlock(out double remaining, out bool locked);
            bool affordable = gold >= remaining;
            if (next == _lastGoalName && remaining == _lastGoalRemaining &&
                locked == _lastGoalLocked && affordable == _lastGoalAffordable) return;
            _lastGoalName = next;
            _lastGoalRemaining = remaining;
            _lastGoalLocked = locked;
            _lastGoalAffordable = affordable;

            _txtGoalTitle.text = string.IsNullOrEmpty(next)
                ? "Loop1 완료: 테이블/베리/직원 해금 완료"
                : locked
                    ? $"다음 목표: {next} - 선행 시설 필요"
                    : $"다음 목표: {next} - {remaining:N0}G 남음";

            _txtGoalHint.text = string.IsNullOrEmpty(next)
                ? "이제 자원 종류, 업그레이드, 퀘스트를 확장할 차례"
                : affordable
                    ? "노란/파란 패드 위에 서서 바로 해금"
                    : "손님에게 팔고 돈 더미를 주워 해금 비용 모으기";
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
