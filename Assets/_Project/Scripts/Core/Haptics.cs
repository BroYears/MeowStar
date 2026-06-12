using UnityEngine;

namespace Nyangsta.Core
{
    /// <summary>
    /// Static mobile vibration helper. Calls Handheld.Vibrate() on Android/iOS builds
    /// only (no-op in editor/standalone) and throttles to one pulse per 0.1s.
    /// </summary>
    public static class Haptics
    {
        private const float COOLDOWN = 0.1f;
        private static float _lastTime = -1f;

        public static void Light()  => Vibrate();
        public static void Medium() => Vibrate();

        private static void Vibrate()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastTime < COOLDOWN) return;
            _lastTime = now;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
