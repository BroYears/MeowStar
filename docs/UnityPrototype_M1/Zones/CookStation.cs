using System.Collections.Generic;
using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Zones
{
    /// <summary>
    /// 조리 스테이션 (GDD 4.4 / 5.2) — 그릴 등.
    /// 존에 서 있으면: 재료 운반 중 → 자동 투입 / 빈손·완성품 운반 중 → 완성품 자동 픽업.
    /// 조리는 플레이어가 없어도 독립적으로 진행 (버퍼 → 시간 → 출력 슬롯).
    /// </summary>
    public class CookStation : InteractionZone
    {
        [Header("Recipe (M1: 재료 1종 → 완성품 1개)")]
        [SerializeField] private ItemType inputType = ItemType.Fish;
        [SerializeField] private StackItem outputPrefab;      // GrilledFish 프리팹
        [SerializeField] private float cookTime = 2f;          // 조리 속도 업그레이드 대상

        [Header("Capacity")]
        [SerializeField] private int inputBufferMax = 5;
        [SerializeField] private Transform[] outputSlots;      // 완성품이 놓이는 카운터 위치들 = 대기열 용량

        [Header("Transfer")]
        [SerializeField] private float transferInterval = 0.2f; // 슉슉 넘어가는 속도

        private int _inputBuffer;
        private float _cookTimer;
        private float _transferTimer;
        private readonly List<StackItem> _outputs = new();

        public bool HasOutput => _outputs.Count > 0;

        private void Update()
        {
            // 조리: 버퍼에 재료가 있고 출력 슬롯이 비어 있으면 진행
            if (_inputBuffer <= 0 || _outputs.Count >= outputSlots.Length) return;

            _cookTimer += Time.deltaTime;
            if (_cookTimer < cookTime) return;
            _cookTimer = 0f;
            _inputBuffer--;

            Transform slot = outputSlots[_outputs.Count];
            StackItem dish = Instantiate(outputPrefab, slot.position, slot.rotation);
            var col = dish.GetComponent<Collider>();
            if (col) col.enabled = false;
            _outputs.Add(dish);
            // TODO(B): 조리 완료 "띵" + 김 파티클
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            _transferTimer += dt;
            if (_transferTimer < transferInterval) return;

            // 1) 재료 투입 (재료를 들고 있을 때)
            if (agent.CurrentType == inputType && _inputBuffer < inputBufferMax)
            {
                StackItem item = agent.Pop();
                if (item != null) { Destroy(item.gameObject); _inputBuffer++; _transferTimer = 0f; }
                return;
            }

            // 2) 완성품 픽업 (빈손이거나 완성품 운반 중일 때)
            if (HasOutput && agent.CanAccept(outputPrefab.type))
            {
                StackItem dish = _outputs[_outputs.Count - 1];
                _outputs.RemoveAt(_outputs.Count - 1);
                if (agent.Push(dish)) _transferTimer = 0f;
                else _outputs.Add(dish); // 실패 시 복구
            }
        }
    }
}
