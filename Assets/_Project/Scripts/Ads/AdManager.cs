using System;
using UnityEngine;
using Nyangsta.Core;

namespace Nyangsta.Ads
{
    /// <summary>
    /// Centralizes ad calls so game logic stays decoupled from the SDK.
    /// This is a STUB: replace the bodies with Google Mobile Ads (AdMob) plugin
    /// calls. During development use test ad unit ids. See GDD §11.6.
    /// </summary>
    public class AdManager : Singleton<AdManager>
    {
        [SerializeField] private bool simulateInEditor = true;

        /// <summary>Show a rewarded ad. Invokes onReward(true) on completion.</summary>
        public void ShowRewarded(Action<bool> onReward)
        {
#if UNITY_EDITOR
            if (simulateInEditor)
            {
                Debug.Log("[AdManager] (sim) rewarded ad completed.");
                onReward?.Invoke(true);
                return;
            }
#endif
            // TODO: integrate RewardedAd.Show() and forward the reward callback.
            Debug.LogWarning("[AdManager] Rewarded ad not integrated yet.");
            onReward?.Invoke(false);
        }

        /// <summary>Show an interstitial at a natural transition (frequency-capped by caller).</summary>
        public void ShowInterstitial(Action onClosed = null)
        {
#if UNITY_EDITOR
            if (simulateInEditor)
            {
                Debug.Log("[AdManager] (sim) interstitial closed.");
                onClosed?.Invoke();
                return;
            }
#endif
            // TODO: integrate InterstitialAd.Show().
            onClosed?.Invoke();
        }
    }
}
