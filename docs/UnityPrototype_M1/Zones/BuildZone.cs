using Nyangsta.Core;
using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Zones
{
    /// <summary>
    /// 건설/업그레이드 존 (GDD 4.6). 돈을 들고(잔액 보유) 존에 서 있으면 골드가 초당 빠져나가며 건설 진행.
    /// 완료 시 연결된 시설 GameObject 활성화. 진행도는 벗어나도 유지.
    /// </summary>
    public class BuildZone : InteractionZone
    {
        [Header("Build")]
        [SerializeField] private long totalCost = 100;
        [SerializeField] private long drainPerTick = 5;       // 1틱당 차감 골드
        [SerializeField] private float tickInterval = 0.1f;   // 빠르게 차감될수록 손맛 좋음
        [SerializeField] private GameObject targetToActivate; // 완성될 시설 (비활성 상태로 배치)

        [Header("Visual (optional)")]
        [SerializeField] private TextMesh costLabel;           // 남은 비용 표시

        private long _paid;

        private void Start()
        {
            if (targetToActivate != null) targetToActivate.SetActive(false);
            UpdateLabel();
        }

        private float _timer;

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (_paid >= totalCost) return;

            _timer += dt;
            if (_timer < tickInterval) return;
            _timer = 0f;

            long remaining = totalCost - _paid;
            long tick = (long)Mathf.Min(drainPerTick, remaining);
            if (!EconomyManager.Instance.TrySpend(tick)) return; // 잔액 부족 → 정지

            _paid += tick;
            UpdateLabel();
            // TODO(B): 지폐가 존으로 날아가는 연출 + 카운트 사운드

            if (_paid >= totalCost) Complete();
        }

        private void Complete()
        {
            if (targetToActivate != null) targetToActivate.SetActive(true);
            // TODO(B): "펑" 파티클 + 건설 팡파르 + 카메라 살짝 셰이크
            gameObject.SetActive(false);
        }

        private void UpdateLabel()
        {
            if (costLabel != null) costLabel.text = $"{totalCost - _paid:N0}G";
        }
    }
}
