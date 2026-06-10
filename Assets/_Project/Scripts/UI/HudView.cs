using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Core;
using Nyangsta.Economy;

namespace Nyangsta.UI
{
    /// <summary>
    /// Example HUD that listens to the event bus for gold/gem changes.
    /// Demonstrates the subscribe-in-OnEnable / unsubscribe-in-OnDisable pattern.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Text goldLabel;
        [SerializeField] private Text gemLabel;

        private void OnEnable()
        {
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.GemsChanged += OnGemsChanged;
            // Initialise with current values.
            if (EconomyManager.Instance != null)
            {
                OnGoldChanged(EconomyManager.Instance.Gold);
                OnGemsChanged(EconomyManager.Instance.Gems);
            }
        }

        private void OnDisable()
        {
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.GemsChanged -= OnGemsChanged;
        }

        private void OnGoldChanged(double gold)
        {
            if (goldLabel != null) goldLabel.text = Mathf.FloorToInt((float)gold).ToString("N0");
        }

        private void OnGemsChanged(int gems)
        {
            if (gemLabel != null) gemLabel.text = gems.ToString();
        }
    }
}
