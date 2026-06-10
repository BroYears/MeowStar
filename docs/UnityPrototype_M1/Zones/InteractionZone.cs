using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Zones
{
    /// <summary>
    /// 모든 인터랙션의 베이스 (GDD 4.1).
    /// "존에 서 있으면 행동이 수행된다" — 트리거 콜라이더(isTrigger) 필수.
    /// 게이지(progress)는 존을 벗어나도 리셋되지 않는다(관대한 설계).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class InteractionZone : MonoBehaviour
    {
        [Header("Zone Visual (optional)")]
        [SerializeField] private Renderer zoneRenderer;       // 진입 시 밝아지는 바닥 표시
        [SerializeField] private Color idleColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color activeColor = new(0.4f, 1f, 0.4f, 0.7f);

        protected StackHolder Occupant { get; private set; }

        protected virtual void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;
            Occupant = holder;
            SetZoneColor(activeColor);
            OnAgentEnter(holder);
        }

        private void OnTriggerStay(Collider other)
        {
            if (Occupant == null) return;
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder != Occupant) return;
            OnAgentStay(Occupant, Time.deltaTime);
        }

        private void OnTriggerExit(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null || holder != Occupant) return;
            OnAgentExit(Occupant);
            Occupant = null;
            SetZoneColor(idleColor);
        }

        private void SetZoneColor(Color c)
        {
            if (zoneRenderer != null) zoneRenderer.material.color = c;
        }

        protected virtual void OnAgentEnter(StackHolder agent) { }
        protected virtual void OnAgentStay(StackHolder agent, float dt) { }
        protected virtual void OnAgentExit(StackHolder agent) { }
    }
}
