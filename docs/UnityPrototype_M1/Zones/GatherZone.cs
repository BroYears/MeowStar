using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Zones
{
    /// <summary>
    /// 채집 존 (GDD 4.3) — 숲(산딸기)·강(생선).
    /// 존에 서 있으면 일정 간격으로 아이템이 머리 위로 쌓인다.
    /// </summary>
    public class GatherZone : InteractionZone
    {
        [Header("Gather")]
        [SerializeField] private StackItem itemPrefab;        // Fish 또는 Berry 프리팹
        [SerializeField] private float gatherInterval = 0.5f; // 채집 속도 업그레이드 대상
        [SerializeField] private Transform spawnPoint;        // 아이템이 튀어나오는 위치(덤불/강)

        private float _timer;

        protected override void OnAgentEnter(StackHolder agent) => _timer = 0f;

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (!agent.CanAccept(itemPrefab.type)) return;    // 가득 찼거나 다른 종류 운반 중

            _timer += dt;
            if (_timer < gatherInterval) return;
            _timer = 0f;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            StackItem item = Instantiate(itemPrefab, pos, Quaternion.identity);
            agent.Push(item);
            // TODO(B): 채집 파티클 + "통" 사운드 + 햅틱
        }
    }
}
