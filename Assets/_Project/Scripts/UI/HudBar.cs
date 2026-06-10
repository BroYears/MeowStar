using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Progression;

namespace Nyangsta.UI
{
    /// <summary>
    /// Top status bar: gold / gems / essence pills with rolling counters, the
    /// current region + world-tree level, and a sound toggle. Subscribes to the
    /// event bus so it always reflects live game state.
    /// </summary>
    public class HudBar : MonoBehaviour
    {
        private Text _goldLabel, _gemLabel, _essenceLabel, _regionLabel;
        private RectTransform _goldPill, _gemPill, _essencePill;
        private ButtonRef _muteBtn;

        private double _goldShown;
        private double _goldTarget;

        public void Build(RectTransform parent)
        {
            var holder = UIFactory.Panel("HudBar", parent, new Color(0.30f, 0.19f, 0.11f, 0.92f), out _, shadow: true);
            UIFactory.AnchorTopStretch(holder, 132);

            // ---- left cluster: gold / gems / essence ----
            float x = 22f;
            _goldLabel = MakePill(holder, "Gold", UITheme.Gold, "G", 190, ref x, out _goldPill);
            _gemLabel = MakePill(holder, "Gems", UITheme.Gem, "◆", 140, ref x, out _gemPill);
            _essenceLabel = MakePill(holder, "Essence", UITheme.Essence, "✦", 140, ref x, out _essencePill);

            // ---- right cluster: mute + region/tree chip ----
            _muteBtn = UIFactory.Button("Mute", holder, "♪", UITheme.Gold,
                () => { bool m = Audio.Sfx.ToggleMute(); RefreshMute(m); }, 30, UITheme.Ink);
            UIFactory.SetAnchors(_muteBtn.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            UIFactory.Size(_muteBtn.rect, 70, 70);
            _muteBtn.rect.anchoredPosition = new Vector2(-22, 4);

            var chip = UIFactory.Panel("RegionChip", holder, new Color(0f, 0f, 0f, 0.28f), out _, shadow: false);
            UIFactory.SetAnchors(chip, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            UIFactory.Size(chip, 300, 64);
            chip.anchoredPosition = new Vector2(-104, 4);
            var leaf = UIFactory.Image("Dot", chip, UITheme.Circle, UITheme.Leaf);
            UIFactory.SetAnchors(leaf.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            leaf.rectTransform.sizeDelta = new Vector2(22, 22);
            leaf.rectTransform.anchoredPosition = new Vector2(22, 0);
            _regionLabel = UIFactory.Text("Region", chip, "요정의 숲", 24, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Stretch(_regionLabel.rectTransform, 44, 12, 4, 4);

            // initial values
            if (EconomyManager.Instance != null)
            {
                _goldShown = _goldTarget = EconomyManager.Instance.Gold;
                _goldLabel.text = Num.Short(_goldShown);
                _gemLabel.text = Num.Short(EconomyManager.Instance.Gems);
            }
            RefreshEssence();
            RefreshRegion();
            RefreshMute(Audio.Sfx.IsMuted);
        }

        private Text MakePill(RectTransform parent, string name, Color iconColor, string glyph,
            float width, ref float x, out RectTransform pill)
        {
            var label = UIFactory.Pill(name, parent, iconColor, glyph, out _, out pill);
            UIFactory.SetAnchors(pill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            UIFactory.Size(pill, width, 64);
            pill.anchoredPosition = new Vector2(x + width * 0.5f, 4);
            x += width + 12f;
            return label;
        }

        private void OnEnable()
        {
            GameEvents.GoldChanged += OnGold;
            GameEvents.GemsChanged += OnGems;
            GameEvents.WorldTreeChanged += OnTree;
            GameEvents.CurrentRegionChanged += OnRegion;
            GameEvents.RegionUnlocked += OnRegion;
        }

        private void OnDisable()
        {
            GameEvents.GoldChanged -= OnGold;
            GameEvents.GemsChanged -= OnGems;
            GameEvents.WorldTreeChanged -= OnTree;
            GameEvents.CurrentRegionChanged -= OnRegion;
            GameEvents.RegionUnlocked -= OnRegion;
        }

        private void Update()
        {
            // Roll the gold counter smoothly toward its target.
            if (_goldLabel == null) return;
            if (Mathf.Abs((float)(_goldTarget - _goldShown)) > 0.5f)
            {
                _goldShown = Mathf.Lerp((float)_goldShown, (float)_goldTarget, Time.unscaledDeltaTime * 9f);
                if (System.Math.Abs(_goldTarget - _goldShown) < 1.0) _goldShown = _goldTarget;
                _goldLabel.text = Num.Short(_goldShown);
            }
        }

        private void OnGold(double g)
        {
            bool up = g > _goldTarget;
            _goldTarget = g;
            if (up && _goldPill != null) UITween.PunchScale(_goldPill, Vector3.one, 1.08f, 0.18f);
        }

        private void OnGems(int v)
        {
            if (_gemLabel != null) _gemLabel.text = Num.Short(v);
            if (_gemPill != null) UITween.PunchScale(_gemPill, Vector3.one, 1.1f, 0.18f);
        }

        private void OnTree(int level, int essence)
        {
            RefreshEssence();
            if (_essencePill != null) UITween.PunchScale(_essencePill, Vector3.one, 1.1f, 0.18f);
        }

        private void OnRegion(RegionType r) => RefreshRegion();

        private void RefreshEssence()
        {
            if (_essenceLabel == null) return;
            var p = ProgressionManager.Instance;
            _essenceLabel.text = p != null ? Num.Short(p.WorldTreeEssence) : "0";
        }

        private void RefreshRegion()
        {
            if (_regionLabel == null) return;
            var p = ProgressionManager.Instance;
            if (p == null) return;
            _regionLabel.text = $"{p.GetRegionName(p.CurrentRegion)}  ·  세계수 Lv.{p.WorldTreeLevel}";
        }

        private void RefreshMute(bool muted)
        {
            if (_muteBtn == null) return;
            _muteBtn.SetText("♪");
            _muteBtn.SetColor(muted ? UITheme.Disabled : UITheme.Gold);
            if (_muteBtn.label != null)
                _muteBtn.label.color = muted ? new Color(0.45f, 0.4f, 0.35f) : UITheme.Ink;
        }
    }
}
