using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nyangsta.UI
{
    /// <summary>
    /// Tiny dependency-free tween helper (no external libraries). Drives short
    /// coroutines through a hidden persistent runner so any static call animates
    /// scale, fade and motion — the "juice" that makes the UI feel alive.
    /// </summary>
    public static class UITween
    {
        private static UITweenRunner _runner;

        private static UITweenRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("~UITweenRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _runner = go.AddComponent<UITweenRunner>();
                }
                return _runner;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Runner.StartCoroutine(routine);

        /// <summary>Generic timed driver: calls step(t in 0..1) each frame (unscaled time).</summary>
        public static Coroutine Do(float duration, Action<float> step, Action onDone = null)
            => Runner.StartCoroutine(DoRoutine(duration, step, onDone));

        private static IEnumerator DoRoutine(float duration, Action<float> step, Action onDone)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step?.Invoke(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step?.Invoke(1f);
            onDone?.Invoke();
        }

        // ----------------------------------------------------------------- scale
        public static void PunchScale(Transform target, Vector3 baseScale, float peak, float duration)
        {
            if (target == null) return;
            Do(duration, t =>
            {
                if (target == null) return;
                // up to peak in first 40%, back to base after, with overshoot.
                float s = t < 0.4f
                    ? Mathf.Lerp(1f, peak, EaseOut(t / 0.4f))
                    : Mathf.Lerp(peak, 1f, EaseOutBack((t - 0.4f) / 0.6f));
                target.localScale = baseScale * s;
            }, () => { if (target != null) target.localScale = baseScale; });
        }

        public static void PopIn(Transform target, float duration = 0.28f, float startScale = 0.2f)
        {
            if (target == null) return;
            Vector3 baseScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;
            target.localScale = baseScale * startScale;
            Do(duration, t =>
            {
                if (target == null) return;
                target.localScale = baseScale * Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(t));
            }, () => { if (target != null) target.localScale = baseScale; });
        }

        public static void ScaleTo(Transform target, Vector3 to, float duration, Action onDone = null)
        {
            if (target == null) return;
            Vector3 from = target.localScale;
            Do(duration, t =>
            {
                if (target != null) target.localScale = Vector3.LerpUnclamped(from, to, EaseOut(t));
            }, onDone);
        }

        // ------------------------------------------------------------------ fade
        public static void Fade(CanvasGroup cg, float to, float duration, Action onDone = null)
        {
            if (cg == null) return;
            float from = cg.alpha;
            Do(duration, t => { if (cg != null) cg.alpha = Mathf.Lerp(from, to, t); }, onDone);
        }

        public static void FadeOutDestroy(GameObject go, float duration = 0.25f)
        {
            if (go == null) return;
            if (!go.TryGetComponent(out CanvasGroup cg)) cg = go.AddComponent<CanvasGroup>();
            Fade(cg, 0f, duration, () => { if (go != null) UnityEngine.Object.Destroy(go); });
        }

        public static void FadeGraphic(Graphic g, float to, float duration, Action onDone = null)
        {
            if (g == null) return;
            float from = g.color.a;
            Do(duration, t =>
            {
                if (g == null) return;
                var c = g.color; c.a = Mathf.Lerp(from, to, t); g.color = c;
            }, onDone);
        }

        // ------------------------------------------------------------------ move
        public static void MoveAnchored(RectTransform rt, Vector2 to, float duration, Action onDone = null)
        {
            if (rt == null) return;
            Vector2 from = rt.anchoredPosition;
            Do(duration, t =>
            {
                if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOut(t));
            }, onDone);
        }

        // --------------------------------------------------------------- easings
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = 1.70158f + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    /// <summary>Hidden MonoBehaviour that hosts tween coroutines.</summary>
    public class UITweenRunner : MonoBehaviour { }
}
