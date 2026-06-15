using System.Collections.Generic;
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

        private readonly Dictionary<StackHolder, int> _occupants = new Dictionary<StackHolder, int>();
        private readonly HashSet<StackHolder> _stayedThisFrame = new HashSet<StackHolder>();
        private int _stayFrame = -1;

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

        protected virtual void OnDisable()
        {
            _occupants.Clear();
            _stayedThisFrame.Clear();
            Occupant = null;
            SetZoneColor(idleColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;

            AddOccupant(holder);
        }

        private void OnTriggerStay(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;

            if (!_occupants.ContainsKey(holder))
                AddOccupant(holder);

            if (_stayFrame != Time.frameCount)
            {
                _stayFrame = Time.frameCount;
                _stayedThisFrame.Clear();
            }

            if (!_stayedThisFrame.Add(holder)) return;
            OnAgentStay(holder, Time.deltaTime);
        }

        private void OnTriggerExit(Collider other)
        {
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null || !_occupants.TryGetValue(holder, out int count)) return;

            count--;
            if (count > 0)
            {
                _occupants[holder] = count;
                return;
            }

            _occupants.Remove(holder);
            _stayedThisFrame.Remove(holder);
            OnAgentExit(holder);

            if (Occupant == holder)
                Occupant = GetAnyOccupant();

            SetZoneColor(_occupants.Count > 0 ? activeColor : idleColor);
        }

        private void AddOccupant(StackHolder holder)
        {
            if (_occupants.TryGetValue(holder, out int count))
            {
                _occupants[holder] = count + 1;
                return;
            }

            _occupants.Add(holder, 1);
            if (Occupant == null)
                Occupant = holder;
            SetZoneColor(activeColor);
            OnAgentEnter(holder);
        }

        private StackHolder GetAnyOccupant()
        {
            foreach (var occupant in _occupants.Keys)
                return occupant;
            return null;
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
