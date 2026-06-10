using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Progression;

namespace Nyangsta.UI
{
    /// <summary>
    /// Staff recruitment: region animals hired with gold + essence grant passive
    /// buffs (cook speed, serving slots, hunt yield, idle income). Cards reflect
    /// lock state, affordability and recruited status live.
    /// </summary>
    public class StaffPanel : MonoBehaviour, IGamePanel
    {
        private GameUIController _controller;
        private RectTransform _root;
        private readonly List<StaffCard> _cards = new();

        public RectTransform Root => _root;

        public void Build(RectTransform layer, GameUIController controller)
        {
            _controller = controller;
            _root = UIFactory.Rect("StaffRoot", layer);
            UIFactory.FillParent(_root);
            var bg = UIFactory.Image("Bg", _root, UITheme.Square, new Color(0.92f, 0.88f, 0.95f, 0.97f));
            UIFactory.FillParent(bg.rectTransform);

            UIPanels.Header(_root, "직원 영입", controller.TopSafe);

            var content = UIFactory.ScrollList("List", _root, out _, 16f, new RectOffset(20, 20, 12, 40));
            var viewport = (RectTransform)content.parent;
            UIFactory.Stretch(viewport, 16, 16, controller.TopSafe + 96, controller.BottomSafe + 12);

            var p = ProgressionManager.Instance;
            if (p == null) return;
            foreach (var staff in p.Staff)
            {
                var card = new StaffCard();
                card.Build(content, staff, OnRecruit);
                _cards.Add(card);
            }
        }

        public void OnShow() { Refresh(); }
        public void OnHide() { }

        private void OnEnable()
        {
            GameEvents.StaffRecruited += OnStaff;
            GameEvents.GoldChanged += OnGold;
            GameEvents.WorldTreeChanged += OnTree;
            GameEvents.RegionUnlocked += OnRegion;
        }

        private void OnDisable()
        {
            GameEvents.StaffRecruited -= OnStaff;
            GameEvents.GoldChanged -= OnGold;
            GameEvents.WorldTreeChanged -= OnTree;
            GameEvents.RegionUnlocked -= OnRegion;
        }

        private void OnStaff(string id) => Refresh();
        private void OnGold(double g) => Refresh();
        private void OnTree(int l, int e) => Refresh();
        private void OnRegion(RegionType r) => Refresh();

        private void Refresh()
        {
            var p = ProgressionManager.Instance;
            if (p == null) return;
            foreach (var c in _cards) c.Refresh(p);
        }

        private void OnRecruit(StaffCard card)
        {
            var p = ProgressionManager.Instance;
            if (p == null) return;
            if (p.TryRecruitStaff(card.Id))
            {
                UITween.PunchScale(card.Root, Vector3.one, 1.05f, 0.22f);
            }
            else Audio.Sfx.Error();
        }

        private class StaffCard
        {
            public string Id;
            public RectTransform Root;
            private Text _name, _desc;
            private ButtonRef _btn;
            private Image _badge;

            public void Build(Transform parent, StaffDefinition staff, System.Action<StaffCard> onRecruit)
            {
                Id = staff.id;
                Root = UIFactory.Panel("Staff_" + staff.id, parent, UITheme.Panel, out _, shadow: true);
                UIFactory.PreferredHeight(Root.gameObject, 156);

                _badge = UIFactory.Image("Badge", Root, UITheme.Circle, UITheme.RegionColor(staff.homeRegion));
                UIFactory.SetAnchors(_badge.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Size(_badge.rectTransform, 96, 96);
                _badge.rectTransform.anchoredPosition = new Vector2(74, 0);
                var glyph = UIFactory.Text("G", _badge.transform, staff.displayName != null && staff.displayName.Length > 0
                    ? staff.displayName.Substring(0, 1) : "냥", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.FillParent(glyph.rectTransform);

                _name = UIFactory.Text("Name", Root, staff.displayName, 30, UITheme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                UIFactory.SetAnchors(_name.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 0.5f));
                UIFactory.Stretch(_name.rectTransform, 150, 250, 22, 6);

                _desc = UIFactory.Text("Desc", Root, EffectLine(staff), 23, UITheme.Muted, TextAnchor.UpperLeft);
                UIFactory.SetAnchors(_desc.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Stretch(_desc.rectTransform, 150, 250, 6, 22);

                _btn = UIFactory.Button("Hire", Root, "영입", new Color(0.66f, 0.5f, 0.86f), () => onRecruit(this), 26);
                UIFactory.SetAnchors(_btn.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                UIFactory.Size(_btn.rect, 220, 104);
                _btn.rect.anchoredPosition = new Vector2(-22, 0);
            }

            public void Refresh(ProgressionManager p)
            {
                var staff = p.GetStaff(Id);
                if (staff == null) return;

                if (p.IsStaffRecruited(Id))
                {
                    _desc.text = EffectLine(staff) + "   ·   영입 완료";
                    _btn.SetText("영입됨");
                    _btn.SetInteractable(false);
                    _badge.color = UITheme.Leaf;
                    return;
                }

                _badge.color = UITheme.RegionColor(staff.homeRegion);
                string reason = LockReason(p, staff);
                if (reason != null)
                {
                    _btn.SetText(reason);
                    _btn.SetInteractable(false);
                }
                else
                {
                    _btn.SetText($"{Num.Short(staff.goldCost)}G\n{staff.essenceCost} 정수");
                    _btn.SetColor(new Color(0.66f, 0.5f, 0.86f));
                    _btn.button.interactable = true;
                    _btn.label.color = Color.white;
                }
            }

            private static string LockReason(ProgressionManager p, StaffDefinition staff)
            {
                if (p.WorldTreeLevel < staff.requiredWorldTreeLevel) return $"세계수 Lv.{staff.requiredWorldTreeLevel}";
                if (!p.IsRegionUnlocked(staff.homeRegion)) return $"{p.GetRegionName(staff.homeRegion)} 필요";
                if (p.WorldTreeEssence < staff.essenceCost) return "정수 부족";
                var eco = EconomyManager.Instance;
                if (eco != null && eco.Gold < staff.goldCost) return "골드 부족";
                return null;
            }

            private static string EffectLine(StaffDefinition staff)
            {
                string region = staff.homeRegion switch
                {
                    RegionType.River => "달빛 강가",
                    RegionType.Cave => "꿀빛 동굴",
                    RegionType.Snow => "별눈 설산",
                    _ => "요정의 숲",
                };
                return $"{staff.description}  ·  {region}";
            }
        }
    }
}
