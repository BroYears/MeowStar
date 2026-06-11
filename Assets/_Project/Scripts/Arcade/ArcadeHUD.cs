using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    public class ArcadeHUD : MonoBehaviour
    {
        [SerializeField] private StackHolder playerStack;

        private GUIStyle _large;
        private GUIStyle _small;
        private GUIStyle _goal;
        private GUIStyle _goalDim;

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
            _goal ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(18, 18, 12, 12),
                normal = { textColor = Color.white }
            };
            _goalDim ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.94f, 0.68f) }
            };

            double gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            GUI.Label(new Rect(24, 20, 420, 54), $"GOLD {gold:N0}", _large);

            if (playerStack != null)
            {
                string item = playerStack.CurrentType == ArcadeItemType.None ? "-" : playerStack.CurrentType.ToString();
                GUI.Label(new Rect(26, 72, 420, 36), $"STACK {item} {playerStack.Count}/{playerStack.Capacity}", _small);
            }

            DrawLoopGoal(gold);
        }

        private void DrawLoopGoal(double gold)
        {
            string next = FindNextUnlock(out double remaining, out bool locked);
            string line = string.IsNullOrEmpty(next)
                ? "Loop1 완료: 테이블/베리/직원 해금 완료"
                : locked
                    ? $"다음 목표: {next} - 선행 시설 필요"
                    : $"다음 목표: {next} - {remaining:N0}G 남음";

            string hint = string.IsNullOrEmpty(next)
                ? "이제 자원 종류, 업그레이드, 퀘스트를 확장할 차례"
                : gold >= remaining
                    ? "노란/파란 패드 위에 서서 바로 해금"
                    : "손님에게 팔고 돈 더미를 주워 해금 비용 모으기";

            GUI.Box(new Rect(24, 116, 430, 92), line, _goal);
            GUI.Label(new Rect(42, 164, 390, 32), hint, _goalDim);
        }

        private static string FindNextUnlock(out double remainingCost, out bool locked)
        {
            remainingCost = double.MaxValue;
            locked = false;
            string best = null;

            foreach (var zone in BuildZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.IsComplete) continue;
                if (zone.RemainingCost >= remainingCost) continue;

                remainingCost = zone.RemainingCost;
                best = zone.DisplayName;
                locked = false;
            }

            foreach (var zone in HireZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.IsComplete) continue;
                if (zone.IsLockedByFacility && best != null) continue;
                if (zone.RemainingCost >= remainingCost && !zone.IsLockedByFacility) continue;

                remainingCost = zone.RemainingCost;
                best = zone.DisplayName;
                locked = zone.IsLockedByFacility;
            }

            if (best == null) remainingCost = 0;
            return best;
        }
    }
}
