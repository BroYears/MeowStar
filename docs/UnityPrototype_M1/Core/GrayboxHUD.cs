using UnityEngine;

namespace Nyangsta.Core
{
    /// <summary>그레이박스용 임시 HUD (OnGUI — Canvas 세팅 불필요). M4에서 정식 UI로 교체.</summary>
    public class GrayboxHUD : MonoBehaviour
    {
        private GUIStyle _style;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
            long gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            GUI.Label(new Rect(30, 30, 600, 80), $"GOLD  {gold:N0}", _style);
        }
    }
}
