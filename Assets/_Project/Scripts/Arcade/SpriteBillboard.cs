using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Makes a sprite stand upright and face the camera in the angled 2.5D world
    /// (think paper cutouts in My Perfect Hotel). Keeps the existing 3D simulation —
    /// movement, triggers, staff AI all run in 3D — and only changes how things look.
    ///
    /// Depth sorting: sortingOrder is derived from world Z (and a bias) so nearer
    /// actors draw in front. Tiles/ground pass a large negative bias to stay behind.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBillboard : MonoBehaviour
    {
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool sortByDepth = true;
        [SerializeField] private int orderBias;

        private SpriteRenderer _sr;
        private Transform _cam;

        public void Init(bool billboard, bool sort, int bias)
        {
            faceCamera = billboard;
            sortByDepth = sort;
            orderBias = bias;
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (Camera.main != null) _cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (faceCamera)
            {
                if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
                if (_cam != null)
                    // Tilt the sprite back to match the camera pitch so it reads as
                    // "standing up" rather than lying flat on the ground.
                    transform.rotation = Quaternion.Euler(_cam.eulerAngles.x, 0f, 0f);
            }

            if (sortByDepth && _sr != null)
            {
                // Lower (further) Z and higher Y → smaller order (drawn behind).
                // Scale by 100 for sub-unit precision; negate Z so near = larger.
                float depth = -transform.position.z * 100f - transform.position.y * 10f;
                _sr.sortingOrder = orderBias + Mathf.RoundToInt(depth);
            }
        }
    }
}
