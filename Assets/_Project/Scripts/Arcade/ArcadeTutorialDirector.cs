using UnityEngine;
using Nyangsta.Economy;
using Nyangsta.Save;

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

        public void Configure(StackHolder playerStack, Transform fishGather, Transform grillZone, Transform serveZone)
        {
            _playerStack = playerStack;
            _player = playerStack != null ? playerStack.transform : null;
            _fishGather = fishGather;
            _grillZone = grillZone;
            _serveZone = serveZone;

            _arrow = CreateArrow();
            PointAt(_fishGather != null ? _fishGather.position : Vector3.zero);
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
}
