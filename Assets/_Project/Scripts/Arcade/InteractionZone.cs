using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>Base class for "stand in the zone to act" interactions.</summary>
    [RequireComponent(typeof(Collider))]
    public abstract class InteractionZone : MonoBehaviour
    {
        [SerializeField] private Renderer zoneRenderer;
        [SerializeField] private Color idleColor = new(1f, 1f, 1f, 0.42f);
        [SerializeField] private Color activeColor = new(0.45f, 1f, 0.55f, 0.72f);

        protected StackHolder Occupant { get; private set; }

        public void ConfigureVisual(Renderer renderer, Color idle, Color active)
        {
            zoneRenderer = renderer;
            idleColor = idle;
            activeColor = active;
            SetZoneColor(idleColor);
        }

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
            OnAgentStay(holder, Time.deltaTime);
        }

        private void OnTriggerExit(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null || holder != Occupant) return;

            OnAgentExit(holder);
            Occupant = null;
            SetZoneColor(idleColor);
        }

        private void SetZoneColor(Color color)
        {
            if (zoneRenderer != null)
                zoneRenderer.material.color = color;
        }

        protected virtual void OnAgentEnter(StackHolder agent) { }
        protected virtual void OnAgentStay(StackHolder agent, float dt) { }
        protected virtual void OnAgentExit(StackHolder agent) { }
    }
}
