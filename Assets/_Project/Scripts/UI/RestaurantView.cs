using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Customer;
using Nyangsta.Data;
using Nyangsta.Progression;

namespace Nyangsta.UI
{
    /// <summary>
    /// Visual layer for the restaurant. Draws the background, the chef, and one
    /// sprite (or coloured block fallback) per seated customer, reflecting their
    /// FSM state. Logic in CustomerManager is untouched.
    /// </summary>
    public class RestaurantView : MonoBehaviour
    {
        [Header("Art (assigned by ProjectSetup; optional)")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite chefSprite;

        [Header("Layout")]
        [SerializeField] private int seatCount = 3;
        [SerializeField] private float seatSpacing = 2.4f;
        [SerializeField] private float seatY = -0.8f;
        [SerializeField] private float customerScale = 1.6f;

        private static Sprite _block;
        private readonly Dictionary<CustomerInstance, CustomerView> _views = new();
        private readonly List<GameObject> _seatMarkers = new();
        private SpriteRenderer _regionTint;
        private SpriteRenderer _treeCanopy;
        private TextMesh _regionLabel;
        private TextMesh _treeLabel;
        private Transform _chef;
        private Vector3 _chefBase;
        private float _bobTime;

        private void Awake()
        {
            EnsureBlockSprite();
            BuildStaticScenery();
            BuildProgressionScenery();
            RefreshProgressionScenery();
        }

        private void OnEnable()
        {
            GameEvents.CurrentRegionChanged += OnRegionChanged;
            GameEvents.WorldTreeChanged += OnWorldTreeChanged;
        }

        private void OnDisable()
        {
            GameEvents.CurrentRegionChanged -= OnRegionChanged;
            GameEvents.WorldTreeChanged -= OnWorldTreeChanged;
        }

        private void Update()
        {
            // Gentle chef idle bob (runs even before customers exist).
            if (_chef != null)
            {
                _bobTime += Time.deltaTime;
                var p = _chefBase;
                p.y = _chefBase.y + Mathf.Sin(_bobTime * 3f) * 0.06f;
                _chef.localPosition = p;
            }

            var mgr = CustomerManager.Instance;
            if (mgr == null) return;
            EnsureSeatMarkers(mgr.SeatCount);
            var active = mgr.Active;

            foreach (var c in active)
                if (!_views.ContainsKey(c))
                    _views[c] = SpawnCustomerView(c);

            var toRemove = new List<CustomerInstance>();
            foreach (var kv in _views)
            {
                if (!IsActive(active, kv.Key))
                {
                    Destroy(kv.Value.root);
                    toRemove.Add(kv.Key);
                    continue;
                }
                kv.Value.Refresh(kv.Key);
            }
            foreach (var c in toRemove) _views.Remove(c);
        }

        private static bool IsActive(IReadOnlyList<CustomerInstance> list, CustomerInstance c)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], c)) return true;
            return false;
        }

        // ---- static scenery ----
        private void BuildStaticScenery()
        {
            // Background: real illustration if provided, else a soft block.
            if (backgroundSprite != null)
            {
                var bg = MakeSpriteObject("Background", backgroundSprite, Vector2.zero, order: -50, parent: transform);
                FitToCamera(bg);
            }
            else
            {
                MakeBlock("Wall", new Vector2(0, 1f), new Vector2(20, 8), new Color(0.96f, 0.91f, 0.82f), -50);
                MakeBlock("Floor", new Vector2(0, -2.5f), new Vector2(20, 4), new Color(0.83f, 0.74f, 0.61f), -49);
            }

            // Chef (냥스타) standing behind the counter, left side.
            if (chefSprite != null)
            {
                var chef = MakeSpriteObject("Chef", chefSprite, new Vector2(-3.2f, -0.2f), order: 5, parent: transform);
                ScaleSpriteToHeight(chef, 2.4f);
                _chef = chef.transform;
            }
            else
            {
                var chef = MakeBlock("Chef", new Vector2(-3.2f, 0f), new Vector2(0.8f, 1.2f), new Color(1f, 0.55f, 0.2f), 5);
                _chef = chef.transform;
            }
            _chefBase = _chef.localPosition;
        }

        private void BuildProgressionScenery()
        {
            _regionTint = MakeBlock("RegionTint", Vector2.zero, new Vector2(20, 10),
                new Color(1f, 1f, 1f, 0.1f), -48);

            MakeBlock("WorldTreeTrunk", new Vector2(3.85f, 0.1f), new Vector2(0.45f, 2.3f),
                new Color(0.37f, 0.22f, 0.12f), -2);
            _treeCanopy = MakeBlock("WorldTreeCanopy", new Vector2(3.85f, 1.5f), new Vector2(1.7f, 1.4f),
                new Color(0.28f, 0.72f, 0.4f), -1);
            MakeBlock("WorldTreeGlow", new Vector2(3.85f, 1.5f), new Vector2(2.2f, 1.8f),
                new Color(1f, 0.9f, 0.35f, 0.18f), -3);

            _regionLabel = MakeText("RegionLabel", "", new Vector2(0f, 3.8f), 9, transform);
            _treeLabel = MakeText("WorldTreeLabel", "", new Vector2(3.85f, 2.7f), 9, transform);
        }

        private void RefreshProgressionScenery()
        {
            var progression = ProgressionManager.Instance;
            RegionType region = progression != null ? progression.CurrentRegion : RegionType.Forest;
            int level = progression != null ? progression.WorldTreeLevel : 1;

            if (_regionTint != null) _regionTint.color = RegionTint(region);
            if (_regionLabel != null)
            {
                string name = progression != null ? progression.GetRegionName(region) : "요정의 숲";
                _regionLabel.text = $"MeowStar Kitchen - {name}";
            }

            if (_treeLabel != null) _treeLabel.text = $"세계수 Lv.{level}";
            if (_treeCanopy != null)
            {
                float s = 1f + Mathf.Clamp(level - 1, 0, 3) * 0.18f;
                _treeCanopy.transform.localScale = new Vector3(1.7f * s, 1.4f * s, 1f);
                _treeCanopy.color = Color.Lerp(new Color(0.28f, 0.72f, 0.4f), new Color(0.65f, 0.9f, 1f), (level - 1) / 3f);
            }
        }

        private void OnRegionChanged(RegionType region)
        {
            RefreshProgressionScenery();
        }

        private void OnWorldTreeChanged(int level, int essence)
        {
            RefreshProgressionScenery();
        }

        private CustomerView SpawnCustomerView(CustomerInstance c)
        {
            var root = new GameObject($"Customer_{c.Data.displayName}");
            root.transform.SetParent(transform, false);

            SpriteRenderer body;
            if (c.Data.icon != null)
            {
                body = MakeSpriteObject("Body", c.Data.icon, Vector2.zero, order: 1, parent: root.transform);
                ScaleSpriteToHeight(body, customerScale);
            }
            else
            {
                body = MakeBlock("Body", Vector2.zero, new Vector2(0.9f, 1.0f), Color.white, 1, root.transform);
            }

            var bar = MakeBlock("PatienceBar", new Vector2(0, customerScale * 0.65f), new Vector2(1.0f, 0.14f),
                Color.green, 6, root.transform);

            var label = MakeText("NameLabel", c.Data.displayName, new Vector2(0, customerScale * 0.9f), 7, root.transform);

            // Thought bubble: what this customer wants to order.
            var bubble = MakeSpriteObject("Bubble", UITheme.Bubble, new Vector2(0.85f, customerScale * 1.0f), 8, root.transform);
            bubble.color = new Color(1f, 1f, 1f, 0.96f);
            bubble.transform.localScale = new Vector3(1.5f, 1.2f, 1f);
            string want = c.Data.preferredMenu != null ? c.Data.preferredMenu.displayName : "?";
            var wantLabel = MakeText("Want", want, new Vector2(0.85f, customerScale * 1.06f), 9, root.transform);
            wantLabel.characterSize = 0.14f;
            wantLabel.color = new Color(0.5f, 0.3f, 0.15f);

            return new CustomerView
            {
                root = root,
                body = body,
                bar = bar,
                label = label,
                bubble = bubble.gameObject,
                wantLabel = wantLabel,
                seatPos = SeatPos(c.SeatIndex),
                hasSprite = c.Data.icon != null
            };
        }

        private Vector2 SeatPos(int seat)
        {
            int count = CustomerManager.Instance != null ? CustomerManager.Instance.SeatCount : seatCount;
            float startX = -(count - 1) * 0.5f * seatSpacing + 1.2f; // shift right of chef
            return new Vector2(startX + seat * seatSpacing, seatY);
        }

        private void EnsureSeatMarkers(int count)
        {
            while (_seatMarkers.Count < count)
            {
                int idx = _seatMarkers.Count;
                var root = new GameObject($"Seat_{idx + 1}");
                root.transform.SetParent(transform, false);
                MakeBlock("Table", new Vector2(0, -0.35f), new Vector2(1.3f, 0.35f),
                    new Color(0.47f, 0.28f, 0.16f), 0, root.transform);
                MakeBlock("Chair", new Vector2(0, -0.8f), new Vector2(0.8f, 0.45f),
                    new Color(0.62f, 0.38f, 0.22f), -1, root.transform);
                _seatMarkers.Add(root);
            }

            for (int i = 0; i < _seatMarkers.Count; i++)
            {
                bool visible = i < count;
                _seatMarkers[i].SetActive(visible);
                if (visible) _seatMarkers[i].transform.localPosition = SeatPos(i);
            }
        }

        // ---- sprite/block helpers ----
        private static void EnsureBlockSprite()
        {
            if (_block != null) return;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _block = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private SpriteRenderer MakeSpriteObject(string name, Sprite sprite, Vector2 pos, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private SpriteRenderer MakeBlock(string name, Vector2 pos, Vector2 size, Color color, int order, Transform parent = null)
        {
            EnsureBlockSprite();
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _block;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static TextMesh MakeText(string name, string value, Vector2 pos, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var text = go.AddComponent<TextMesh>();
            // Use the shared dynamic font so Korean glyphs render in world space.
            if (UITheme.Font != null)
            {
                text.font = UITheme.Font;
                text.fontStyle = FontStyle.Bold;
            }
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.22f;
            text.fontSize = 48;
            text.color = new Color(0.12f, 0.08f, 0.05f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (UITheme.Font != null && UITheme.Font.material != null)
                renderer.sharedMaterial = UITheme.Font.material;
            renderer.sortingOrder = order;
            return text;
        }

        private static Color RegionTint(RegionType region)
        {
            return region switch
            {
                RegionType.River => new Color(0.34f, 0.72f, 0.95f, 0.18f),
                RegionType.Cave => new Color(0.72f, 0.52f, 0.25f, 0.2f),
                RegionType.Snow => new Color(0.72f, 0.9f, 1f, 0.24f),
                _ => new Color(0.47f, 0.84f, 0.48f, 0.14f),
            };
        }

        /// <summary>Scale a sprite renderer so its world height equals targetHeight.</summary>
        private static void ScaleSpriteToHeight(SpriteRenderer sr, float targetHeight)
        {
            float h = sr.sprite.bounds.size.y;
            if (h <= 0f) return;
            float s = targetHeight / h;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        private void FitToCamera(SpriteRenderer sr)
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            var size = sr.sprite.bounds.size;
            float sx = worldW / size.x, sy = worldH / size.y;
            float s = Mathf.Max(sx, sy);   // cover
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Per-customer view widgets + animation.</summary>
        private class CustomerView
        {
            public GameObject root;
            public SpriteRenderer body;
            public SpriteRenderer bar;
            public TextMesh label;
            public GameObject bubble;
            public TextMesh wantLabel;
            public Vector2 seatPos;
            public bool hasSprite;

            public void Refresh(CustomerInstance c)
            {
                Vector2 target = c.State switch
                {
                    CustomerState.Entering => seatPos + Vector2.right * 5f,
                    CustomerState.Leaving => seatPos + Vector2.right * 6f,
                    _ => seatPos,
                };
                root.transform.localPosition = Vector2.Lerp(
                    root.transform.localPosition, target, Time.deltaTime * 5f);

                // Tint: real sprites get a subtle state tint; blocks get full colour.
                Color tint = c.State switch
                {
                    CustomerState.Eating => new Color(0.7f, 1f, 0.7f),
                    CustomerState.Paying => new Color(0.7f, 0.85f, 1f),
                    CustomerState.Leaving => c.LeftAngry ? new Color(1f, 0.6f, 0.6f) : Color.white,
                    _ => Color.white,
                };
                body.color = hasSprite ? Color.Lerp(Color.white, tint, 0.5f) : tint;
                if (label != null) label.text = c.Data.displayName;

                bool showBar = c.State == CustomerState.Waiting || c.State == CustomerState.Ordering;
                bar.enabled = showBar;
                if (showBar)
                {
                    float t = c.PatienceNormalized;
                    bar.transform.localScale = new Vector3(1.0f * t, 0.14f, 1f);
                    bar.color = Color.Lerp(Color.red, Color.green, t);
                }

                // Show the "what I want" bubble only while waiting to order.
                if (bubble != null) bubble.SetActive(showBar);
                if (wantLabel != null) wantLabel.gameObject.SetActive(showBar);
            }
        }
    }
}
