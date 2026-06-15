using UnityEngine;
using UnityEngine.SceneManagement;
using Nyangsta.Arcade;
using Nyangsta.Core;
using Nyangsta.Save;
using Nyangsta.Audio;

namespace Nyangsta.UI
{
    /// <summary>
    /// Safety net so the playable UI appears even if the Bootstrap scene was saved
    /// before this UI existed (i.e. without re-running Nyangsta &gt; Setup). After the
    /// scene loads, if the managers are present but no <see cref="GameUIController"/>
    /// is in the scene, one is created (plus audio), and any leftover debug overlay
    /// is hidden. Does nothing in non-game scenes.
    /// </summary>
    public static class GameUIBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureUI()
        {
            // Only act inside the actual game scene (its managers are present).
            if (Object.FindAnyObjectByType<SaveManager>() == null) return;
            if (ArcadePrototypeBootstrap.IsArcadeBootstrapScene(SceneManager.GetActiveScene())) return;
            if (Object.FindAnyObjectByType<ArcadeSceneManager>() != null) return;

            // Hide the old IMGUI debug overlay if it's still in the saved scene.
            var debug = Object.FindAnyObjectByType<DebugTester>();
            if (debug != null) debug.enabled = false;

            if (Object.FindAnyObjectByType<GameUIController>() != null) return;

            var go = new GameObject("_GameUI(Auto)");
            go.AddComponent<GameUIController>();
            if (Object.FindAnyObjectByType<SfxManager>() == null)
                go.AddComponent<SfxManager>();
        }
    }
}
