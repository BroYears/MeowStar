using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;

namespace Nyangsta.UI
{
    /// <summary>
    /// Upgrade store: every <see cref="UpgradeData"/> grouped by category as a card
    /// with its level, effect and exponential cost. Buying updates economy state and
    /// the card live; affordability refreshes on gold changes.
    /// </summary>
    public class UpgradePanel : MonoBehaviour, IGamePanel
    {
        private static readonly UpgradeCategory[] Order =
            { UpgradeCategory.Hunting, UpgradeCategory.Cooking, UpgradeCategory.Restaurant, UpgradeCategory.Idle };

        private GameUIController _controller;
        private RectTransform _root;
        private readonly List<UpgradeCard> _cards = new();

        public RectTransform Root => _root;

        public void Build(RectTransform layer, GameUIController controller)
        {
            _controller = controller;
            _root = UIFactory.Rect("UpgradeRoot", layer);
            UIFactory.FillParent(_root);
            var bg = UIFactory.Image("Bg", _root, UITheme.Square, new Color(0.96f, 0.91f, 0.80f, 0.97f));
            UIFactory.FillParent(bg.rectTransform);

            UIPanels.Header(_root, "주방 업그레이드", controller.TopSafe);

            var content = UIFactory.ScrollList("List", _root, out _, 14f, new RectOffset(20, 20, 12, 40));
            var viewport = (RectTransform)content.parent;
            UIFactory.Stretch(viewport, 16, 16, controller.TopSafe + 96, controller.BottomSafe + 12);

            var economy = EconomyManager.Instance;
            var upgrades = economy != null ? economy.Upgrades : null;
            if (upgrades == null) return;

            foreach (var cat in Order)
            {
                bool headerAdded = false;
                foreach (var up in upgrades)
                {
                    if (up == null || up.category != cat) continue;
                    if (!headerAdded) { UIPanels.SectionLabel(content, CategoryName(cat), UITheme.CategoryColor(cat)); headerAdded = true; }
                    var card = new UpgradeCard();
                    card.Build(content, up, OnBuy);
                    _cards.Add(card);
                }
            }
        }

        public void OnShow() { RefreshAll(); }
        public void OnHide() { }

        private void OnEnable()
        {
            GameEvents.GoldChanged += OnGold;
            GameEvents.UpgradePurchased += OnPurchased;
        }

        private void OnDisable()
        {
            GameEvents.GoldChanged -= OnGold;
            GameEvents.UpgradePurchased -= OnPurchased;
        }

        private void OnGold(double g) { RefreshAll(); }
        private void OnPurchased(string id, int level) { RefreshAll(); }

        private void RefreshAll()
        {
            var economy = EconomyManager.Instance;
            if (economy == null) return;
            foreach (var c in _cards) c.Refresh(economy);
        }

        private void OnBuy(UpgradeCard card)
        {
            var economy = EconomyManager.Instance;
            if (economy == null) return;
            double costBefore = economy.GetUpgradeCost(card.Data);
            bool ok = economy.TryPurchaseUpgrade(card.Data);
            if (ok)
            {
                UITween.PunchScale(card.Root, Vector3.one, 1.04f, 0.2f);
                Vector2 p = RectTransformUtility.WorldToScreenPoint(null, card.Root.position);
                FloatingText.Spawn(p + new Vector2(0, 30), $"Lv.{economy.GetUpgradeLevel(card.Data.id)}", UITheme.LeafDark, 38);
            }
            else
            {
                Audio.Sfx.Error();
                UITween.PunchScale(card.Root, Vector3.one, 1.03f, 0.16f);
            }
        }

        private static string CategoryName(UpgradeCategory c) => c switch
        {
            UpgradeCategory.Hunting => "사냥",
            UpgradeCategory.Cooking => "요리",
            UpgradeCategory.Restaurant => "식당",
            UpgradeCategory.Idle => "방치 · 자동",
            _ => c.ToString(),
        };

        /// <summary>One upgrade row with live level / cost / buy state.</summary>
        private class UpgradeCard
        {
            public RectTransform Root;
            public UpgradeData Data;
            private Text _name, _effect, _level;
            private ButtonRef _buy;

            public void Build(Transform parent, UpgradeData data, System.Action<UpgradeCard> onBuy)
            {
                Data = data;
                Root = UIFactory.Panel("Card_" + data.id, parent, UITheme.Panel, out _, shadow: true);
                UIFactory.PreferredHeight(Root.gameObject, 156);

                var stripe = UIFactory.Image("Stripe", Root, UITheme.Rounded, UITheme.CategoryColor(data.category), Image.Type.Sliced);
                UIFactory.SetAnchors(stripe.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f));
                stripe.rectTransform.sizeDelta = new Vector2(14, -24);
                stripe.rectTransform.anchoredPosition = new Vector2(16, 0);

                var badge = UIFactory.Image("Badge", Root, UITheme.Circle, UITheme.CategoryColor(data.category));
                UIFactory.SetAnchors(badge.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Size(badge.rectTransform, 84, 84);
                badge.rectTransform.anchoredPosition = new Vector2(78, 0);
                _level = UIFactory.Text("Lv", badge.transform, "Lv.0", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.FillParent(_level.rectTransform);

                _name = UIFactory.Text("Name", Root, data.displayName, 32, UITheme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                UIFactory.SetAnchors(_name.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 0.5f));
                UIFactory.Stretch(_name.rectTransform, 140, 250, 24, 6);

                _effect = UIFactory.Text("Effect", Root, "", 24, UITheme.Muted, TextAnchor.UpperLeft);
                UIFactory.SetAnchors(_effect.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Stretch(_effect.rectTransform, 140, 250, 6, 24);

                _buy = UIFactory.Button("Buy", Root, "구매", UITheme.GoldDeep, () => onBuy(this), 28);
                UIFactory.SetAnchors(_buy.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                UIFactory.Size(_buy.rect, 210, 100);
                _buy.rect.anchoredPosition = new Vector2(-22, 0);
            }

            public void Refresh(EconomyManager economy)
            {
                int level = economy.GetUpgradeLevel(Data.id);
                _level.text = $"Lv.{level}";
                _effect.text = EffectText(Data, level);

                bool maxed = Data.maxLevel > 0 && level >= Data.maxLevel;
                if (maxed)
                {
                    _buy.SetText("MAX");
                    _buy.SetInteractable(false);
                    return;
                }
                double cost = economy.GetUpgradeCost(Data);
                _buy.SetText($"{Num.Short(cost)} G");
                bool canAfford = economy.Gold >= cost;
                _buy.SetColor(canAfford ? UITheme.GoldDeep : UITheme.Disabled);
                _buy.button.interactable = true;   // keep tappable to give error feedback
                _buy.label.color = Color.white;
            }

            private static string EffectText(UpgradeData u, int level) => u.id switch
            {
                "UPG_HUNT_TOOL"   => $"재료 획득량  +{level}",
                "UPG_STAMINA"     => $"사냥 시간  +{level * 1.5f:0.#}s",
                "UPG_COOK_SPEED"  => $"조리 속도  +{level * 5}%",
                "UPG_COOK_STATION"=> $"동시 조리대  +{level}",
                "UPG_SEATS"       => $"좌석 수  +{level}",
                "UPG_REPUTATION"  => $"평판  +{level}  (손님·매출 ↑)",
                "UPG_STAFF"       => $"자동 수익  +{level}",
                "UPG_OFFLINE_CAP" => $"오프라인 상한  +{level}h",
                _ => $"효과 Lv.{level}",
            };
        }
    }
}
