using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Data;
using Nyangsta.Economy;
using Nyangsta.Hunting;
using Nyangsta.Progression;
using Nyangsta.Ads;

namespace Nyangsta.UI
{
    /// <summary>
    /// The active-play heart of the game: a tap mini-game. Region-flavoured targets
    /// pop in across the stage; tapping one calls <see cref="HuntingManager.RegisterHit"/>
    /// with full juice (squash, burst, floating number, combo). A live timer/combo
    /// HUD runs the round, then a result card summarises the haul with an optional
    /// rewarded-ad double. Ready → Playing → Result.
    /// </summary>
    public class HuntMiniGame : MonoBehaviour, IGamePanel
    {
        private const int MaxConcurrent = 5;
        private const float SpawnInterval = 0.42f;
        private const float MinLife = 1.0f, MaxLife = 1.55f;

        private GameUIController _controller;
        private RectTransform _root, _playArea;
        private Image _bg, _vignette;

        private RectTransform _readyView, _playView, _resultView;
        private Text _readyTitle, _readyInfo;
        private Image _timerFill;
        private Text _timerText, _comboText, _countText;
        private Text _resultTitle, _resultEssence;
        private RectTransform _resultList;
        private ButtonRef _doubleBtn;

        private readonly List<Target> _targets = new();
        private float _spawnTimer;
        private float _huntMax = 15f;
        private int _caught, _lastCombo;
        private int _essenceBefore, _essenceGained;
        private readonly Dictionary<string, int> _drops = new();
        private bool _claimedDouble;

        private enum State { Ready, Playing, Result }
        private State _state = State.Ready;

        public RectTransform Root => _root;

        // ----------------------------------------------------------------- build
        public void Build(RectTransform layer, GameUIController controller)
        {
            _controller = controller;
            _root = UIFactory.Rect("HuntRoot", layer);
            UIFactory.FillParent(_root);

            _bg = UIFactory.Image("Bg", _root, UITheme.Square, UITheme.RegionColor(RegionType.Forest));
            UIFactory.FillParent(_bg.rectTransform);
            _bg.raycastTarget = false;
            _bg.preserveAspect = false;
            _vignette = UIFactory.Image("Vignette", _root, UITheme.Glow, new Color(0f, 0f, 0f, 0.0f));
            UIFactory.FillParent(_vignette.rectTransform);
            _vignette.raycastTarget = false;

            _playArea = UIFactory.Rect("PlayArea", _root);
            UIFactory.Stretch(_playArea, 36, 36, _controller.TopSafe + 130, 150);

            BuildReadyView();
            BuildPlayView();
            BuildResultView();
        }

        private void BuildReadyView()
        {
            _readyView = UIFactory.Rect("Ready", _root);
            UIFactory.FillParent(_readyView);

            var card = UIFactory.Panel("Card", _readyView, UITheme.Panel, out _, shadow: true, outline: true);
            UIFactory.SetAnchors(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(card, 660, 520);

            UIFactory.Text("Heading", card, "사냥터", 30, UITheme.Muted, TextAnchor.UpperCenter, FontStyle.Bold)
                .rectTransform.anchoredPosition = new Vector2(0, -34);
            _readyTitle = UIFactory.Text("Title", card, "요정의 숲", 56, UITheme.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            _readyTitle.rectTransform.anchoredPosition = new Vector2(0, -74);

            var icon = UIFactory.Image("BigIcon", card, UITheme.Circle, UITheme.Leaf);
            UIFactory.SetAnchors(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(icon.rectTransform, 170, 170);
            icon.rectTransform.anchoredPosition = new Vector2(0, 30);
            UIFactory.Text("Paw", icon.transform, "냥", 70, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            _readyInfo = UIFactory.Text("Info", card,
                "재료가 통통 튀어나오면 빠르게 탭하세요!\n연속으로 잡으면 콤보 보너스를 받아요.", 24,
                UITheme.InkSoft, TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(_readyInfo.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(_readyInfo.rectTransform, 600, 80);
            _readyInfo.rectTransform.anchoredPosition = new Vector2(0, 150);

            var start = UIFactory.Button("Start", card, "사냥 시작!", UITheme.LeafDark, BeginHunt, 38);
            UIFactory.SetAnchors(start.rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(start.rect, 460, 104);
            start.rect.anchoredPosition = new Vector2(0, 40);
        }

        private void BuildPlayView()
        {
            _playView = UIFactory.Rect("Play", _root);
            UIFactory.FillParent(_playView);

            // Timer bar near the top.
            var barHolder = UIFactory.Rect("TimerBar", _playView);
            UIFactory.SetAnchors(barHolder, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(barHolder, 720, 36);
            barHolder.anchoredPosition = new Vector2(0, -70);
            _timerFill = UIFactory.ProgressBar("Bar", barHolder, new Color(0, 0, 0, 0.35f), UITheme.Gold, out _);
            UIFactory.FillParent((RectTransform)_timerFill.transform.parent);
            _timerFill.fillAmount = 1f;
            _timerText = UIFactory.Text("Time", barHolder, "15s", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.FillParent(_timerText.rectTransform);

            _countText = UIFactory.Text("Count", _playView, "0", 30, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            var co = _countText.gameObject.AddComponent<Outline>(); co.effectColor = new Color(0,0,0,0.5f); co.effectDistance = new Vector2(2,-2);
            UIFactory.SetAnchors(_countText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            UIFactory.Size(_countText.rectTransform, 300, 44);
            _countText.rectTransform.anchoredPosition = new Vector2(36, -118);

            _comboText = UIFactory.Text("Combo", _playView, "", 64, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            var cco = _comboText.gameObject.AddComponent<Outline>(); cco.effectColor = new Color(0,0,0,0.55f); cco.effectDistance = new Vector2(3,-3);
            UIFactory.SetAnchors(_comboText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(_comboText.rectTransform, 600, 90);
            _comboText.rectTransform.anchoredPosition = new Vector2(0, -150);

            var stop = UIFactory.Button("Stop", _playView, "종료", new Color(0.55f, 0.4f, 0.34f, 0.9f), EndHuntEarly, 26);
            UIFactory.SetAnchors(stop.rect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            UIFactory.Size(stop.rect, 150, 78);
            stop.rect.anchoredPosition = new Vector2(-30, 30);
        }

        private void BuildResultView()
        {
            _resultView = UIFactory.Rect("Result", _root);
            UIFactory.FillParent(_resultView);
            var dim = UIFactory.Image("Dim", _resultView, UITheme.Square, new Color(0, 0, 0, 0.45f));
            UIFactory.FillParent(dim.rectTransform);

            var card = UIFactory.Panel("Card", _resultView, UITheme.Panel, out _, shadow: true, outline: true);
            UIFactory.SetAnchors(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(card, 700, 760);

            var banner = UIFactory.Image("Banner", card, UITheme.Rounded, UITheme.LeafDark, Image.Type.Sliced);
            UIFactory.SetAnchors(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(banner.rectTransform, 700, 110);
            _resultTitle = UIFactory.Text("Title", banner.transform, "사냥 완료!", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.FillParent(_resultTitle.rectTransform);

            var scroll = UIFactory.ScrollList("Haul", card, out _, 12f, new RectOffset(16, 16, 16, 16));
            UIFactory.SetAnchors(scroll.parent.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            var sv = (RectTransform)scroll.parent;
            UIFactory.Stretch(sv, 24, 24, 130, 290);
            _resultList = scroll;

            _resultEssence = UIFactory.Text("Essence", card, "", 28, UITheme.LeafDark, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetAnchors(_resultEssence.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(_resultEssence.rectTransform, 640, 50);
            _resultEssence.rectTransform.anchoredPosition = new Vector2(0, 250);

            _doubleBtn = UIFactory.Button("Double", card, "광고 보고 보상 2배", UITheme.GoldDeep, ClaimDouble, 30);
            UIFactory.SetAnchors(_doubleBtn.rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(_doubleBtn.rect, 600, 96);
            _doubleBtn.rect.anchoredPosition = new Vector2(0, 150);

            var again = UIFactory.Button("Again", card, "한 번 더", UITheme.LeafDark, () => { ShowReady(); BeginHunt(); }, 30);
            UIFactory.SetAnchors(again.rect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            UIFactory.Size(again.rect, 300, 92);
            again.rect.anchoredPosition = new Vector2(40, 40);

            var done = UIFactory.Button("Done", card, "완료", UITheme.Wood, () => _controller.SelectTab(GameTab.Home), 30);
            UIFactory.SetAnchors(done.rect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            UIFactory.Size(done.rect, 300, 92);
            done.rect.anchoredPosition = new Vector2(-40, 40);
        }

        // ----------------------------------------------------------- panel hooks
        public void OnShow()
        {
            if (HuntingManager.Instance != null && HuntingManager.Instance.IsHunting)
            {
                EnterPlaying();
            }
            else ShowReady();
        }

        public void OnHide()
        {
            ClearTargets();
            _controller.SetChromeVisible(true);
        }

        private void ShowReady()
        {
            _state = State.Ready;
            ClearTargets();
            _controller.SetChromeVisible(true);
            SetViews(ready: true, play: false, result: false);

            var p = ProgressionManager.Instance;
            RegionType region = p != null ? p.CurrentRegion : RegionType.Forest;
            if (_readyTitle != null) _readyTitle.text = p != null ? p.GetRegionName(region) : "요정의 숲";
            ApplyRegionColor(region);
            if (_readyView != null) _readyView.SetAsLastSibling();
        }

        // --------------------------------------------------------------- the loop
        private void BeginHunt()
        {
            var hm = HuntingManager.Instance;
            if (hm == null) return;
            _essenceBefore = ProgressionManager.Instance != null ? ProgressionManager.Instance.WorldTreeEssence : 0;
            hm.StartCurrentRegionHunt();
            EnterPlaying();
            Audio.Sfx.HuntStart();
        }

        private void EnterPlaying()
        {
            _state = State.Playing;
            _caught = 0;
            _lastCombo = 0;
            _spawnTimer = 0.15f;
            _huntMax = Mathf.Max(1f, HuntingManager.Instance.TimeRemaining);
            _controller.SetChromeVisible(false);
            SetViews(ready: false, play: true, result: false);
            ApplyRegionColor(HuntingManager.Instance.ActiveRegion);
            // Targets live in _playArea; keep it above the chrome so taps land.
            if (_playArea != null) _playArea.SetAsLastSibling();
            if (_playView != null) _playView.SetAsLastSibling();
            if (_comboText != null) _comboText.text = "";
            if (_countText != null) _countText.text = "잡은 재료 0";
        }

        private void Update()
        {
            if (_state != State.Playing) return;
            var hm = HuntingManager.Instance;
            if (hm == null) return;

            if (!hm.IsHunting) { GoToResult(); return; }

            float dt = Time.deltaTime;

            // Timer
            if (_timerFill != null) _timerFill.fillAmount = Mathf.Clamp01(hm.TimeRemaining / _huntMax);
            if (_timerText != null) _timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0, hm.TimeRemaining))}s";

            // Combo readout
            if (hm.Combo != _lastCombo)
            {
                bool up = hm.Combo > _lastCombo;
                _lastCombo = hm.Combo;
                if (_comboText != null)
                {
                    if (hm.Combo >= 2)
                    {
                        _comboText.text = $"콤보 x{hm.Combo}";
                        _comboText.color = Color.Lerp(UITheme.Gold, UITheme.Danger, Mathf.Clamp01((hm.Combo - 2) / 12f));
                        if (up) UITween.PunchScale(_comboText.transform, Vector3.one, 1.35f, 0.22f);
                    }
                    else _comboText.text = "";
                }
            }

            // Tick + expire targets
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                var t = _targets[i];
                t.life -= dt;
                if (t.root == null) { _targets.RemoveAt(i); continue; }
                // gentle bob
                t.root.anchoredPosition = t.home + new Vector2(0, Mathf.Sin(Time.time * 6f + t.phase) * 6f);
                if (t.life <= 0f) { Expire(t); _targets.RemoveAt(i); }
            }

            // Spawn
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f && _targets.Count < MaxConcurrent)
            {
                _spawnTimer = SpawnInterval;
                Spawn();
            }
        }

        private void GoToResult()
        {
            _state = State.Result;
            ClearTargets();
            _controller.SetChromeVisible(true);

            // Snapshot the haul.
            _drops.Clear();
            var hm = HuntingManager.Instance;
            if (hm != null)
                foreach (var kv in hm.SessionDrops) _drops[kv.Key] = kv.Value;
            int essNow = ProgressionManager.Instance != null ? ProgressionManager.Instance.WorldTreeEssence : 0;
            _essenceGained = Mathf.Max(0, essNow - _essenceBefore);
            _claimedDouble = false;

            BuildResultList();
            SetViews(ready: false, play: false, result: true);
            if (_resultView != null) _resultView.SetAsLastSibling();
            UITween.PopIn(_resultView.GetChild(_resultView.childCount - 1), 0.34f, 0.8f);
            Audio.Sfx.Ding();
        }

        private void BuildResultList()
        {
            for (int i = _resultList.childCount - 1; i >= 0; i--) Destroy(_resultList.GetChild(i).gameObject);

            var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
            int totalItems = 0;
            foreach (var kv in _drops)
            {
                totalItems += kv.Value;
                IngredientData ing = db != null ? db.GetIngredient(kv.Key) : null;
                var row = UIFactory.Panel("Row", _resultList, UITheme.PanelAlt, out _, shadow: false);
                UIFactory.PreferredHeight(row.gameObject, 92);

                var icon = UIFactory.Image("Icon", row, ing != null && ing.icon != null ? ing.icon : UITheme.Circle,
                    ing != null && ing.icon != null ? Color.white : RarityColor(ing != null ? ing.rarity : Rarity.Common));
                UIFactory.SetAnchors(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                UIFactory.Size(icon.rectTransform, 64, 64);
                icon.rectTransform.anchoredPosition = new Vector2(56, 0);
                icon.preserveAspect = true;

                var name = UIFactory.Text("Name", row, ing != null ? ing.displayName : kv.Key, 30,
                    UITheme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.SetAnchors(name.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f));
                UIFactory.Stretch(name.rectTransform, 100, 120, 0, 0);

                var qty = UIFactory.Text("Qty", row, $"x{kv.Value}", 32, UITheme.GoldDeep, TextAnchor.MiddleRight, FontStyle.Bold);
                UIFactory.SetAnchors(qty.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f));
                UIFactory.Stretch(qty.rectTransform, 0, 28, 0, 0);
            }

            if (totalItems == 0)
            {
                var empty = UIFactory.Text("Empty", _resultList, "아무것도 잡지 못했어요…\n다음엔 더 빠르게!", 28,
                    UITheme.Muted, TextAnchor.MiddleCenter);
                UIFactory.PreferredHeight(empty.gameObject, 120);
            }

            if (_resultTitle != null) _resultTitle.text = totalItems > 0 ? $"사냥 완료!  +{totalItems}" : "사냥 완료";
            if (_resultEssence != null)
                _resultEssence.text = _essenceGained > 0 ? $"✦ 세계수 정수  +{_essenceGained}" : "";
            if (_doubleBtn != null) _doubleBtn.SetInteractable(totalItems > 0 || _essenceGained > 0);
        }

        private void ClaimDouble()
        {
            if (_claimedDouble) return;
            var ad = AdManager.Instance;
            Action<bool> onReward = ok =>
            {
                if (!ok) return;
                _claimedDouble = true;
                var eco = EconomyManager.Instance;
                foreach (var kv in _drops) eco?.AddIngredient(kv.Key, kv.Value);
                if (_essenceGained > 0) ProgressionManager.Instance?.AddWorldTreeEssence(_essenceGained);
                _doubleBtn.SetText("보상 2배 완료!");
                _doubleBtn.SetInteractable(false);
                Audio.Sfx.LevelUp();
                Toast.Show("보상 2배 획득!", UITheme.Gold);
            };
            if (ad != null) ad.ShowRewarded(onReward); else onReward(true);
        }

        private void EndHuntEarly()
        {
            var hm = HuntingManager.Instance;
            if (hm != null && hm.IsHunting) hm.FinishHunt();
            GoToResult();
        }

        // --------------------------------------------------------------- targets
        private void Spawn()
        {
            var ing = RollDisplayIngredient(out Color color, out Sprite sprite, out string label);

            var root = UIFactory.Rect("Target", _playArea);
            UIFactory.SetAnchors(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            float size = ing != null && ing.rarity >= Rarity.Epic ? 150 : 128;

            // soft glow behind
            var glow = UIFactory.Image("Glow", root, UITheme.Glow, new Color(color.r, color.g, color.b, 0.5f));
            UIFactory.SetAnchors(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(glow.rectTransform, size * 1.5f, size * 1.5f);
            glow.raycastTarget = false;

            var disc = UIFactory.Image("Disc", root, sprite != null ? sprite : UITheme.Circle,
                sprite != null ? Color.white : color);
            UIFactory.FillParent(disc.rectTransform);
            disc.preserveAspect = true;
            if (sprite == null)
            {
                var t = UIFactory.Text("Lbl", disc.transform, label, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.FillParent(t.rectTransform);
            }

            UIFactory.Size(root, size, size);
            Vector2 home = RandomPlayPos(size);
            root.anchoredPosition = home;

            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var target = new Target { root = root, life = UnityEngine.Random.Range(MinLife, MaxLife),
                home = home, phase = UnityEngine.Random.value * 10f, color = color };
            btn.onClick.AddListener(() => Hit(target));
            _targets.Add(target);

            UITween.PopIn(root, 0.22f, 0.1f);
        }

        private void Hit(Target t)
        {
            if (_state != State.Playing || t.root == null || t.hit) return;
            t.hit = true;

            HuntingManager.Instance?.RegisterHit();
            _caught++;
            if (_countText != null) _countText.text = $"잡은 재료 {_caught}";

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, t.root.position);
            FloatingText.Spawn(screen, "+", t.color, 44, 70);
            Burst(t.root.anchoredPosition, t.color);
            Audio.Sfx.Tap();
            if (HuntingManager.Instance != null && HuntingManager.Instance.Combo > 0 &&
                HuntingManager.Instance.Combo % 5 == 0) Audio.Sfx.Coin();

            _targets.Remove(t);
            var go = t.root.gameObject;
            t.root = null;
            UITween.ScaleTo(go.transform, Vector3.one * 1.3f, 0.12f, () => { if (go != null) Destroy(go); });
        }

        private void Expire(Target t)
        {
            if (t.root != null) UITween.FadeOutDestroy(t.root.gameObject, 0.2f);
        }

        private void ClearTargets()
        {
            foreach (var t in _targets) if (t.root != null) Destroy(t.root.gameObject);
            _targets.Clear();
        }

        private Vector2 RandomPlayPos(float size)
        {
            float w = _playArea.rect.width, h = _playArea.rect.height;
            if (w < 50f) { w = Screen.width * 0.8f; h = Screen.height * 0.5f; }
            float hx = Mathf.Max(10f, w * 0.5f - size * 0.6f);
            float hy = Mathf.Max(10f, h * 0.5f - size * 0.6f);
            return new Vector2(UnityEngine.Random.Range(-hx, hx), UnityEngine.Random.Range(-hy, hy));
        }

        private IngredientData RollDisplayIngredient(out Color color, out Sprite sprite, out string label)
        {
            color = UITheme.Leaf; sprite = null; label = "재료";
            var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
            RegionType region = HuntingManager.Instance != null ? HuntingManager.Instance.ActiveRegion : RegionType.Forest;
            if (db == null) return null;

            var pool = new List<IngredientData>();
            float total = 0f;
            foreach (var ing in db.ingredients)
                if (ing != null && ing.region == region) { pool.Add(ing); total += Mathf.Max(0.01f, ing.spawnRate); }
            if (pool.Count == 0) return null;

            float roll = UnityEngine.Random.value * total;
            IngredientData chosen = pool[pool.Count - 1];
            foreach (var ing in pool)
            {
                roll -= Mathf.Max(0.01f, ing.spawnRate);
                if (roll <= 0f) { chosen = ing; break; }
            }
            color = RarityColor(chosen.rarity);
            sprite = chosen.icon;
            label = chosen.displayName != null && chosen.displayName.Length > 0 ? chosen.displayName.Substring(0, 1) : "냥";
            return chosen;
        }

        private void Burst(Vector2 localPos, Color color)
        {
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                var p = UIFactory.Image("Spark", _playArea, UITheme.Circle, color);
                p.raycastTarget = false;
                UIFactory.SetAnchors(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                UIFactory.Size(p.rectTransform, 26, 26);
                p.rectTransform.anchoredPosition = localPos;
                float ang = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.value;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(60f, 120f);
                UITween.MoveAnchored(p.rectTransform, localPos + dir, 0.4f);
                UITween.FadeGraphic(p, 0f, 0.4f, () => { if (p != null) Destroy(p.gameObject); });
            }
        }

        // --------------------------------------------------------------- helpers
        private void SetViews(bool ready, bool play, bool result)
        {
            if (_readyView != null) _readyView.gameObject.SetActive(ready);
            if (_playView != null) _playView.gameObject.SetActive(play);
            if (_resultView != null) _resultView.gameObject.SetActive(result);
        }

        private static readonly Dictionary<RegionType, Sprite> _bgCache = new();

        private void ApplyRegionColor(RegionType region)
        {
            if (_bg == null) return;
            var bg = RegionBackground(region);
            if (bg != null) { _bg.sprite = bg; _bg.color = Color.white; }
            else { _bg.sprite = null; _bg.color = UITheme.RegionColor(region); }
        }

        /// <summary>Loads the illustrated region background from Resources/Hunt (cached).</summary>
        private static Sprite RegionBackground(RegionType region)
        {
            if (_bgCache.TryGetValue(region, out var cached)) return cached;
            string key = region switch
            {
                RegionType.River => "river",
                RegionType.Cave => "cave",
                RegionType.Snow => "snow",
                _ => "forest",
            };
            var tex = Resources.Load<Texture2D>($"Hunt/{key}");
            Sprite sp = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
            _bgCache[region] = sp;
            return sp;
        }

        private static Color RarityColor(Rarity r) => r switch
        {
            Rarity.Rare => UITheme.Gem,
            Rarity.Epic => new Color(0.72f, 0.52f, 0.93f),
            Rarity.Legendary => UITheme.Gold,
            _ => new Color(0.52f, 0.76f, 0.46f),
        };

        private class Target
        {
            public RectTransform root;
            public float life;
            public Vector2 home;
            public float phase;
            public Color color;
            public bool hit;
        }
    }
}
