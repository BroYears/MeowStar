using System.Collections;
using UnityEngine;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// "Map expands" reveal for a build target: when an area is unlocked the structure
    /// pops in with an overshoot bounce and a ring of dust puffs bursts at ground level,
    /// so the world reads as physically growing. Save-restored builds call
    /// <see cref="SnapInstant"/> instead, so the celebration only plays on a fresh unlock,
    /// not on every launch.
    /// </summary>
    public class MapReveal : MonoBehaviour
    {
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private float startScale = 0.12f;
        [SerializeField] private int dustCount = 8;

        private Vector3 _fullScale = Vector3.one;

        /// <summary>Capture the target's intended (level-0) scale; call while it's known.</summary>
        public void Init(Vector3 fullScale) => _fullScale = fullScale;

        public void SnapInstant() => transform.localScale = _fullScale;

        public void PlayAnimated()
        {
            if (!gameObject.activeInHierarchy) { SnapInstant(); return; }
            StopAllCoroutines();
            StartCoroutine(Animate());
            SpawnDust();
        }

        private IEnumerator Animate()
        {
            Vector3 from = _fullScale * startScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.LerpUnclamped(from, _fullScale, EaseOutBack(p));
                yield return null;
            }
            transform.localScale = _fullScale;
        }

        // A short-lived ring of soft puffs kicked outward from the target's base.
        private void SpawnDust()
        {
            var sprite = UITheme.Glow;
            if (sprite == null || dustCount <= 0) return;

            Vector3 center = transform.position;
            for (int i = 0; i < dustCount; i++)
            {
                float ang = (Mathf.PI * 2f / dustCount) * i;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));

                var puff = new GameObject("RevealDust");
                puff.transform.position = center + dir * 0.2f + Vector3.up * 0.15f;

                var sr = puff.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.86f, 0.79f, 0.62f, 0.9f);
                puff.AddComponent<SpriteBillboard>().Init(true, true, 220);
                puff.AddComponent<DustPuff>().Launch(dir);
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }
    }

    /// <summary>One reveal dust puff: drifts outward + up, swells, fades, self-destructs.</summary>
    public class DustPuff : MonoBehaviour
    {
        private const float LIFE = 0.5f;

        private SpriteRenderer _sr;
        private Vector3 _vel;
        private Vector3 _baseScale;
        private float _t;

        public void Launch(Vector3 dir)
        {
            _sr = GetComponent<SpriteRenderer>();
            _vel = dir * 1.6f + Vector3.up * 0.8f;
            _baseScale = Vector3.one * 0.35f;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float p = _t / LIFE;
            if (p >= 1f) { Destroy(gameObject); return; }

            transform.position += _vel * Time.deltaTime;
            _vel *= 0.9f;   // air drag so puffs settle
            transform.localScale = _baseScale * (1f + p * 1.5f);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 0.9f * (1f - p);
                _sr.color = c;
            }
        }
    }
}
