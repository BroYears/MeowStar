using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Progression;

namespace Nyangsta.UI
{
    /// <summary>
    /// Adventure screen: grow the World Tree with essence to unlock new regions,
    /// then move the restaurant between them. Each region changes the hunt stage,
    /// customers and ambience, so travelling is the long-term progression hook.
    /// </summary>
    public class TravelPanel : MonoBehaviour, IGamePanel
    {
        private GameUIController _controller;
        private RectTransform _root;

        private Image _treeCanopy;
        private Text _treeTitle, _essenceLabel;
        private Image _essenceFill;
        private ButtonRef _growBtn;
        private readonly List<RegionCard> _cards = new();

        public RectTransform Root => _root;

        public void Build(RectTransform layer, GameUIController controller)
        {
            _controller = controller;
            _root = UIFactory.Rect("TravelRoot", layer);
            UIFactory.FillParent(_root);
            var bg = UIFactory.Image("Bg", _root, UITheme.Square, new Color(0.86f, 0.92f, 0.84f, 0.97f));
            UIFactory.FillParent(bg.rectTransform);

            UIPanels.Header(_root, "모험 · 세계수", controller.TopSafe);

            var content = UIFactory.ScrollList("List", _root, out _, 16f, new RectOffset(20, 20, 12, 40));
            var viewport = (RectTransform)content.parent;
            UIFactory.Stretch(viewport, 16, 16, controller.TopSafe + 96, controller.BottomSafe + 12);

            BuildTreeCard(content);

            var p = ProgressionManager.Instance;
            if (p != null)
            {
                UIPanels.SectionLabel(content, "지역 이동", UITheme.Gem);
                foreach (var rule in p.Regions)
                {
                    var card = new RegionCard();
                    card.Build(content, rule, OnTravel);
                    _cards.Add(card);
                }
            }
        }

        private void BuildTreeCard(Transform content)
        {
            var card = UIFactory.Panel("TreeCard", content, UITheme.Panel, out _, shadow: true, outline: true);
            UIFactory.PreferredHeight(card.gameObject, 250);

            // stylised tree
            var trunk = UIFactory.Image("Trunk", card, UITheme.Rounded, UITheme.Wood, Image.Type.Sliced);
            UIFactory.SetAnchors(trunk.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            UIFactory.Size(trunk.rectTransform, 26, 90);
            trunk.rectTransform.anchoredPosition = new Vector2(90, -36);
            _treeCanopy = UIFactory.Image("Canopy", card, UITheme.Circle, UITheme.Leaf);
            UIFactory.SetAnchors(_treeCanopy.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            UIFactory.Size(_treeCanopy.rectTransform, 130, 130);
            _treeCanopy.rectTransform.anchoredPosition = new Vector2(90, 22);

            _treeTitle = UIFactory.Text("Title", card, "세계수  Lv.1", 36, UITheme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetAnchors(_treeTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
            UIFactory.Size(_treeTitle.rectTransform, -210, 60);
            _treeTitle.rectTransform.anchoredPosition = new Vector2(190, -28);

            var barHolder = UIFactory.Rect("Bar", card);
            UIFactory.SetAnchors(barHolder, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f));
            barHolder.sizeDelta = new Vector2(-210, 34);
            barHolder.anchoredPosition = new Vector2(90, 6);
            _essenceFill = UIFactory.ProgressBar("Fill", barHolder, new Color(0, 0, 0, 0.15f), UITheme.Essence, out _);
            UIFactory.FillParent((RectTransform)_essenceFill.transform.parent);
            _essenceLabel = UIFactory.Text("Ess", barHolder, "", 22, UITheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.FillParent(_essenceLabel.rectTransform);

            _growBtn = UIFactory.Button("Grow", card, "세계수 키우기", UITheme.LeafDark, OnGrow, 28);
            UIFactory.SetAnchors(_growBtn.rect, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            _growBtn.rect.sizeDelta = new Vector2(-210, 78);
            _growBtn.rect.anchoredPosition = new Vector2(90, 28);
        }

        public void OnShow() { Refresh(); }
        public void OnHide() { }

        private void OnEnable()
        {
            GameEvents.WorldTreeChanged += OnTree;
            GameEvents.CurrentRegionChanged += OnRegion;
            GameEvents.RegionUnlocked += OnRegion;
        }

        private void OnDisable()
        {
            GameEvents.WorldTreeChanged -= OnTree;
            GameEvents.CurrentRegionChanged -= OnRegion;
            GameEvents.RegionUnlocked -= OnRegion;
        }

        private void OnTree(int level, int essence) => Refresh();
        private void OnRegion(RegionType r) => Refresh();

        private void Refresh()
        {
            var p = ProgressionManager.Instance;
            if (p == null) return;

            int level = p.WorldTreeLevel;
            int essence = p.WorldTreeEssence;
            int next = p.NextWorldTreeCost;
            bool maxed = next <= 0;

            _treeTitle.text = $"세계수  Lv.{level}";
            float canopy = 110f + Mathf.Clamp(level - 1, 0, 4) * 22f;
            _treeCanopy.rectTransform.sizeDelta = new Vector2(canopy, canopy);
            _treeCanopy.color = Color.Lerp(UITheme.Leaf, new Color(0.6f, 0.9f, 1f), Mathf.Clamp01((level - 1) / 4f));

            if (maxed)
            {
                _essenceFill.fillAmount = 1f;
                _essenceLabel.text = "최고 레벨";
                _growBtn.SetText("완성");
                _growBtn.SetInteractable(false);
            }
            else
            {
                _essenceFill.fillAmount = Mathf.Clamp01(next > 0 ? (float)essence / next : 0f);
                _essenceLabel.text = $"정수 {essence} / {next}";
                _growBtn.SetText($"세계수 키우기  ({next} 정수)");
                _growBtn.SetColor(essence >= next ? UITheme.LeafDark : UITheme.Disabled);
                _growBtn.button.interactable = true;
                _growBtn.label.color = Color.white;
            }

            foreach (var c in _cards) c.Refresh(p);
        }

        private void OnGrow()
        {
            var p = ProgressionManager.Instance;
            if (p == null) return;
            if (p.TryGrowWorldTree())
            {
                UITween.PunchScale(_treeCanopy.transform, _treeCanopy.transform.localScale, 1.25f, 0.3f);
            }
            else Audio.Sfx.Error();
        }

        private void OnTravel(RegionCard card)
        {
            var p = ProgressionManager.Instance;
            if (p == null) return;
            if (p.TryTravelTo(card.Region))
            {
                Audio.Sfx.Whoosh();
                Toast.Show($"{p.GetRegionName(card.Region)}(으)로 이동!", UITheme.Gem);
            }
            else Audio.Sfx.Error();
        }

        private class RegionCard
        {
            public RegionType Region;
            private Text _name, _sub;
            private ButtonRef _btn;
            private Image _tint;

            public void Build(Transform parent, RegionRule rule, System.Action<RegionCard> onTravel)
            {
                Region = rule.region;
                var root = UIFactory.Panel("Region_" + rule.region, parent, UITheme.Panel, out _, shadow: true);
                UIFactory.PreferredHeight(root.gameObject, 132);

                _tint = UIFactory.Image("Tint", root, UITheme.Circle, UITheme.RegionColor(rule.region));
                UIFactory.SetAnchors(_tint.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Size(_tint.rectTransform, 84, 84);
                _tint.rectTransform.anchoredPosition = new Vector2(70, 0);

                _name = UIFactory.Text("Name", root, rule.displayName, 32, UITheme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                UIFactory.SetAnchors(_name.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 0.5f));
                UIFactory.Stretch(_name.rectTransform, 130, 250, 20, 6);

                _sub = UIFactory.Text("Sub", root, "", 24, UITheme.Muted, TextAnchor.UpperLeft);
                UIFactory.SetAnchors(_sub.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Stretch(_sub.rectTransform, 130, 250, 6, 20);

                _btn = UIFactory.Button("Go", root, "이동", UITheme.Gem, () => onTravel(this), 28, UITheme.Ink);
                UIFactory.SetAnchors(_btn.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                UIFactory.Size(_btn.rect, 200, 88);
                _btn.rect.anchoredPosition = new Vector2(-22, 0);
            }

            public void Refresh(ProgressionManager p)
            {
                bool unlocked = p.IsRegionUnlocked(Region);
                bool current = p.CurrentRegion == Region;
                var rule = p.GetRegionRule(Region);

                if (current)
                {
                    _sub.text = "현재 위치";
                    _btn.SetText("머무는 중");
                    _btn.SetInteractable(false);
                }
                else if (unlocked)
                {
                    _sub.text = "이동 가능";
                    _btn.SetText("이동");
                    _btn.SetColor(UITheme.Gem);
                    _btn.button.interactable = true;
                    _btn.label.color = UITheme.Ink;
                }
                else
                {
                    _sub.text = rule != null ? $"세계수 Lv.{rule.requiredWorldTreeLevel} 필요" : "잠김";
                    _btn.SetText("잠김");
                    _btn.SetInteractable(false);
                }
                _name.color = unlocked ? UITheme.Ink : UITheme.Muted;
            }
        }
    }
}
