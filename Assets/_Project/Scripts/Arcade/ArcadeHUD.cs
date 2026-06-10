using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    public class ArcadeHUD : MonoBehaviour
    {
        [SerializeField] private StackHolder playerStack;

        private GUIStyle _large;
        private GUIStyle _small;

        public void Configure(StackHolder stack)
        {
            playerStack = stack;
        }

        private void OnGUI()
        {
            _large ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.82f, 0.2f) }
            };
            _small ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            double gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            GUI.Label(new Rect(24, 20, 420, 54), $"GOLD {gold:N0}", _large);

            if (playerStack != null)
            {
                string item = playerStack.CurrentType == ArcadeItemType.None ? "-" : playerStack.CurrentType.ToString();
                GUI.Label(new Rect(26, 72, 420, 36), $"STACK {item} {playerStack.Count}/{playerStack.Capacity}", _small);
            }
        }
    }
}
