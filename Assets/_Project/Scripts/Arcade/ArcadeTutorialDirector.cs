using UnityEngine;
using Nyangsta.Economy;
using Nyangsta.Save;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// First-session FTUE: a single bobbing arrow guides the player through the
    /// core loop (gather fish -> grill -> serve -> collect cash -> build pad).
    /// Step completion is detected by lightweight polling against existing public
    /// state (stack contents, gold, build progress) — no hooks into game code.
    /// </summary>
    public class ArcadeTutorialDirector : MonoBehaviour
    {
        private const float POLL_INTERVAL = 0.2f;
        private const float ARROW_HEIGHT = 2.4f;
        private const float BOB_AMPLITUDE = 0.22f;
        private const float BOB_SPEED = 4.5f;
        private const float REACH_DISTANCE = 2.0f;

        private enum Step
        {
            GoToFishGather,
            PickUpFish,
            GoToGrill,
            ServeTable,
            CollectMoney,
            GoToBuildPad,
            Done,
        }

        private StackHolder _playerStack;
        private Transform _player;
        private Transform _fishGather;
        private Transform _grillZone;
        private Transform _serveZone;

        private Step _step = Step.GoToFishGather;
        private float _pollTimer;
        private double _goldBaseline;
        private BuildZone _targetBuild;
        private double _buildCostBaseline;
        private MoneyPile _targetPile;

        private Transform _arrow;
        private Vector3 _arrowBase;
        private Transform _hintRoot;
        private WorldBubble _hintBubble;
        private Step _lastHintStep = (Step)(-1);

        public void Configure(StackHolder playerStack, Transform fishGather, Transform grillZone, Transform serveZone)
        {
            _playerStack = playerStack;
            _player = playerStack != null ? playerStack.transform : null;
            _fishGather = fishGather;
            _grillZone = grillZone;
            _serveZone = serveZone;

            _arrow = CreateArrow();
            _hintRoot = CreateHintBubble();
            PointAt(_fishGather != null ? _fishGather.position : Vector3.zero);
            SetHint(Step.GoToFishGather);
        }

        private void Update()
        {
            if (_step == Step.Done || _playerStack == null) return;

            // Bob the arrow every frame; cheap transform write only.
            if (_arrow != null)
            {
                Vector3 pos = _arrowBase;
                pos.y += Mathf.Sin(Time.time * BOB_SPEED) * BOB_AMPLITUDE;
                _arrow.position = pos;
                if (_hintRoot != null)
                {
                    var hintPos = _arrowBase;
                    hintPos.y -= 0.9f;
                    _hintRoot.position = hintPos;
                }
            }

            _pollTimer += Time.deltaTime;
            if (_pollTimer < POLL_INTERVAL) return;
            _pollTimer = 0f;
            Poll();
        }

        private void Poll()
        {
            switch (_step)
            {
                case Step.GoToFishGather:
                    if (IsNear(_fishGather) || HasItem(ArcadeItemType.Fish))
                        Advance(Step.PickUpFish, _fishGather);
                    break;

                case Step.PickUpFish:
                    if (HasItem(ArcadeItemType.Fish))
                        Advance(Step.GoToGrill, _grillZone);
                    break;

                case Step.GoToGrill:
                    if (HasItem(ArcadeItemType.GrilledFish))
                        Advance(Step.ServeTable, _serveZone);
                    break;

                case Step.ServeTable:
                    // A money pile appearing means a dish was served and paid for.
                    _targetPile = FindAnyObjectByType<MoneyPile>();
                    if (_targetPile != null)
                    {
                        _goldBaseline = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
                        Advance(Step.CollectMoney, _targetPile.transform);
                    }
                    break;

                case Step.CollectMoney:
                    if (_targetPile != null) PointAt(_targetPile.transform.position);
                    if (EconomyManager.Instance != null && EconomyManager.Instance.Gold > _goldBaseline)
                    {
                        _targetBuild = FindNearestBuildZone();
                        if (_targetBuild != null)
                        {
                            _buildCostBaseline = _targetBuild.RemainingCost;
                            Advance(Step.GoToBuildPad, _targetBuild.transform);
                        }
                        else
                        {
                            Finish();
                        }
                    }
                    break;

                case Step.GoToBuildPad:
                    // Done once the player has started (or finished) paying on the pad.
                    if (_targetBuild == null || !_targetBuild.gameObject.activeInHierarchy
                        || _targetBuild.IsComplete || _targetBuild.RemainingCost < _buildCostBaseline)
                        Finish();
                    break;
            }
        }

        private void Advance(Step next, Transform target)
        {
            _step = next;
            if (target != null) PointAt(target.position);
            SetHint(next);
        }

        private void Finish()
        {
            _step = Step.Done;
            var save = SaveManager.Instance;
            if (save != null)
            {
                save.Data.arcadeTutorialDone = true;
                save.Save();
            }
            if (_arrow != null) Destroy(_arrow.gameObject);
            if (_hintRoot != null) Destroy(_hintRoot.gameObject);
            Destroy(gameObject);
        }

        private bool HasItem(ArcadeItemType type)
            => !_playerStack.IsEmpty && _playerStack.CurrentType == type;

        private bool IsNear(Transform target)
        {
            if (target == null || _player == null) return false;
            Vector3 d = target.position - _player.position;
            d.y = 0f;
            return d.sqrMagnitude <= REACH_DISTANCE * REACH_DISTANCE;
        }

        private static BuildZone FindNearestBuildZone()
        {
            var zones = BuildZone.ActiveZones;
            for (int i = 0; i < zones.Count; i++)
                if (zones[i] != null && !zones[i].IsComplete && zones[i].gameObject.activeInHierarchy)
                    return zones[i];
            return null;
        }

        private void PointAt(Vector3 groundPos)
        {
            _arrowBase = new Vector3(groundPos.x, groundPos.y + ARROW_HEIGHT, groundPos.z);
            if (_arrow != null) _arrow.position = _arrowBase;
            if (_hintRoot != null) _hintRoot.position = _arrowBase + Vector3.down * 0.9f;
        }

        // ---- Arrow visual (procedural downward triangle, billboarded) ----

        private static Sprite _arrowSprite;

        private Transform CreateArrow()
        {
            var go = new GameObject("Img_TutorialArrow");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetArrowSprite();
            sr.color = UI.UITheme.Gold;
            go.AddComponent<SpriteBillboard>().Init(true, false, 0);
            sr.sortingOrder = 500; // always in front of world sprites
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
            float s = h > 0f ? 0.8f / h : 1f;
            go.transform.localScale = new Vector3(s, s, s);
            return go.transform;
        }

        private Transform CreateHintBubble()
        {
            var go = new GameObject("Bubble_TutorialHint");
            var bubble = WorldBubble.Create(go.transform, Vector3.zero, 2.45f, 0.72f);
            bubble.SetTitle("", 32, new Vector2(0f, 0.05f), UITheme.Ink);
            _hintBubble = bubble;
            return go.transform;
        }

        private void SetHint(Step step)
        {
            if (_hintBubble == null || step == _lastHintStep) return;
            _lastHintStep = step;

            string text = step switch
            {
                Step.GoToFishGather => "강가에서 생선을 모으세요",
                Step.PickUpFish => "잠깐 서 있으면 생선을 줍습니다",
                Step.GoToGrill => "그릴에 생선을 올리세요",
                Step.ServeTable => "요리를 테이블에 서빙하세요",
                Step.CollectMoney => "돈 더미를 지나가세요",
                Step.GoToBuildPad => "노란 패드에서 새 테이블 해금",
                _ => "",
            };

            _hintBubble.SetTitle(text, 32, new Vector2(0f, 0.05f), UITheme.Ink);
        }

        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var fill = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                // Downward triangle: full width at the top, apex at the bottom center.
                float t = y / (float)(size - 1);          // 0 = bottom, 1 = top
                float halfWidth = t * (size * 0.5f - 2f);
                float center = size * 0.5f;
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Mathf.Abs(x - center) <= halfWidth ? fill : clear;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 64f);
            return _arrowSprite;
        }
    }

    /// <summary>
    /// Persistent post-FTUE destination marker. It keeps the arcade loop readable after
    /// the one-shot tutorial by pointing at the next useful world object: loose money,
    /// a gather zone, cook station, serving table, or unlock pad.
    /// </summary>
    public class ArcadeObjectiveMarker : MonoBehaviour
    {
        private const float REFRESH_INTERVAL = 0.2f;
        private const float MARKER_HEIGHT = 2.15f;
        private const float BOB_AMPLITUDE = 0.14f;
        private const float BOB_SPEED = 4.0f;

        private StackHolder _playerStack;
        private Transform _arrow;
        private Transform _hintRoot;
        private WorldBubble _hintBubble;
        private Transform _target;
        private string _lastLabel;
        private float _refreshTimer;

        private static Sprite _markerSprite;

        public void Configure(StackHolder playerStack)
        {
            _playerStack = playerStack;
            _arrow = CreateArrow();
            _hintRoot = CreateHintBubble();
            SetVisible(false);
        }

        private void Update()
        {
            bool tutorialActive = FindAnyObjectByType<ArcadeTutorialDirector>() != null;
            if (tutorialActive)
            {
                SetVisible(false);
                return;
            }

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                RefreshTarget();
            }

            if (_target == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            Vector3 pos = _target.position + Vector3.up * MARKER_HEIGHT;
            pos.y += Mathf.Sin(Time.time * BOB_SPEED) * BOB_AMPLITUDE;
            if (_arrow != null) _arrow.position = pos;
            if (_hintRoot != null) _hintRoot.position = pos + Vector3.down * 0.72f;
        }

        private void RefreshTarget()
        {
            if (!TrySelectTarget(out var nextTarget, out string label))
            {
                _target = null;
                _lastLabel = null;
                return;
            }

            _target = nextTarget;
            if (label == _lastLabel || _hintBubble == null) return;
            _lastLabel = label;
            _hintBubble.SetTitle(label, 28, new Vector2(0f, 0.04f), UITheme.Ink);
        }

        private bool TrySelectTarget(out Transform target, out string label)
        {
            if (TryFindLooseMoney(out var pile))
            {
                target = pile.transform;
                label = "돈 회수";
                return true;
            }

            if (_playerStack != null && !_playerStack.IsEmpty)
            {
                var carried = _playerStack.CurrentType;
                if (carried == ArcadeItemType.Wood && TryFindBuildZone(out var build))
                {
                    target = build.transform;
                    label = "나무 투입";
                    return true;
                }

                if (IsRawIngredient(carried) && TryFindCookStation(carried, out var station))
                {
                    target = station.transform;
                    label = $"{ShortItemName(carried)} 조리";
                    return true;
                }

                if (TryFindServingTable(carried, out var table))
                {
                    target = table.transform;
                    label = "테이블 서빙";
                    return true;
                }
            }

            if (TryFindWaitingOrder(out var wanted))
            {
                var raw = RawFor(wanted);
                if (TryFindGatherZone(raw, out var gather))
                {
                    target = gather.transform;
                    label = $"{ShortItemName(raw)} 모으기";
                    return true;
                }
            }

            if (TryFindBuildZone(out var nextBuild))
            {
                target = nextBuild.transform;
                label = $"{nextBuild.DisplayName} 해금";
                return true;
            }

            if (TryFindHireZone(out var hire))
            {
                target = hire.transform;
                label = $"{hire.DisplayName} 고용";
                return true;
            }

            if (TryFindGatherZone(ArcadeItemType.Fish, out var fishGather))
            {
                target = fishGather.transform;
                label = "생선 모으기";
                return true;
            }

            target = null;
            label = null;
            return false;
        }

        private void SetVisible(bool visible)
        {
            if (_arrow != null && _arrow.gameObject.activeSelf != visible)
                _arrow.gameObject.SetActive(visible);
            if (_hintRoot != null && _hintRoot.gameObject.activeSelf != visible)
                _hintRoot.gameObject.SetActive(visible);
        }

        private Transform CreateArrow()
        {
            var go = new GameObject("Img_ObjectiveArrow");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetMarkerSprite();
            sr.color = UITheme.Gem;
            sr.sortingOrder = 490;
            go.AddComponent<SpriteBillboard>().Init(true, false, 0);
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
            float s = h > 0f ? 0.55f / h : 1f;
            go.transform.localScale = new Vector3(s, s, s);
            return go.transform;
        }

        private Transform CreateHintBubble()
        {
            var go = new GameObject("Bubble_ObjectiveHint");
            _hintBubble = WorldBubble.Create(go.transform, Vector3.zero, 1.85f, 0.58f);
            _hintBubble.SetTitle("", 28, new Vector2(0f, 0.04f), UITheme.Ink);
            return go.transform;
        }

        private static bool TryFindLooseMoney(out MoneyPile result)
        {
            result = null;
            double best = -1;
            foreach (var pile in MoneyPile.ActivePiles)
            {
                if (pile == null || pile.IsCollecting || !pile.gameObject.activeInHierarchy) continue;
                if (pile.Amount <= best) continue;
                best = pile.Amount;
                result = pile;
            }
            return result != null;
        }

        private static bool TryFindGatherZone(ArcadeItemType type, out GatherZone result)
        {
            result = null;
            foreach (var zone in FindObjectsByType<GatherZone>(FindObjectsInactive.Exclude))
            {
                if (zone == null || !zone.gameObject.activeInHierarchy || zone.ProducedType != type) continue;
                result = zone;
                return true;
            }
            return false;
        }

        private static bool TryFindCookStation(ArcadeItemType input, out CookStation result)
        {
            result = null;
            foreach (var station in FindObjectsByType<CookStation>(FindObjectsInactive.Exclude))
            {
                if (station == null || !station.gameObject.activeInHierarchy || station.InputType != input) continue;
                result = station;
                return true;
            }
            return false;
        }

        private static bool TryFindServingTable(ArcadeItemType item, out TableZone result)
        {
            result = null;
            foreach (var table in TableZone.ActiveZones)
            {
                if (table == null || !table.gameObject.activeInHierarchy || !table.HasWaitingOrderFor(item)) continue;
                result = table;
                return true;
            }
            return false;
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

        private static bool TryFindBuildZone(out BuildZone result)
        {
            result = null;
            double best = double.MaxValue;
            foreach (var zone in BuildZone.ActiveZones)
            {
                if (zone == null || !zone.gameObject.activeInHierarchy || zone.IsComplete) continue;
                if (zone.RemainingCost >= best) continue;
                best = zone.RemainingCost;
                result = zone;
            }
            return result != null;
        }

        private static bool TryFindHireZone(out HireZone result)
        {
            result = null;
            double best = double.MaxValue;
            foreach (var zone in HireZone.ActiveZones)
            {
                if (zone == null || !zone.gameObject.activeInHierarchy || zone.IsComplete || zone.IsLockedByFacility) continue;
                if (zone.RemainingCost >= best) continue;
                best = zone.RemainingCost;
                result = zone;
            }
            return result != null;
        }

        private static bool IsRawIngredient(ArcadeItemType item) => item switch
        {
            ArcadeItemType.Fish or ArcadeItemType.Berry or ArcadeItemType.Mushroom
                or ArcadeItemType.Salmon or ArcadeItemType.Honey => true,
            _ => false,
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

        private static string ShortItemName(ArcadeItemType type) => type switch
        {
            ArcadeItemType.Fish => "생선",
            ArcadeItemType.Berry => "베리",
            ArcadeItemType.Wood => "나무",
            ArcadeItemType.Mushroom => "버섯",
            ArcadeItemType.Salmon => "연어",
            ArcadeItemType.Honey => "꿀",
            _ => "자원",
        };

        private static Sprite GetMarkerSprite()
        {
            if (_markerSprite != null) return _markerSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var fill = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                float halfWidth = t * (size * 0.45f - 2f);
                float center = size * 0.5f;
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Mathf.Abs(x - center) <= halfWidth ? fill : clear;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _markerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 64f);
            return _markerSprite;
        }
    }
}
