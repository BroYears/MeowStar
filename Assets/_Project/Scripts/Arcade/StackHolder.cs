using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Shared stack carrier for the player and future staff agents.
    /// M1 keeps the genre rule simple: one item type at a time.
    /// </summary>
    public class StackHolder : MonoBehaviour
    {
        // Freshly picked items fly to the stack along a short sin arc instead of a straight line.
        private const float PICKUP_ARC_DURATION = 0.2f;
        private const float PICKUP_ARC_HEIGHT = 0.6f;

        [SerializeField] private int capacity = 3;
        [SerializeField] private Transform stackAnchor;
        [SerializeField] private float itemHeight = 0.42f;
        [SerializeField, Range(0.01f, 0.3f)] private float followSmoothTime = 0.06f;

        private readonly List<ArcadeStackItem> _items = new();

        public int Capacity { get => capacity; set => capacity = Mathf.Max(1, value); }
        public int Count => _items.Count;
        public bool IsFull => Count >= Capacity;
        public bool IsEmpty => Count == 0;
        public ArcadeItemType CurrentType => IsEmpty ? ArcadeItemType.None : _items[0].type;

        public void Configure(Transform anchor, int stackCapacity)
        {
            stackAnchor = anchor;
            Capacity = stackCapacity;
        }

        public bool CanAccept(ArcadeItemType type)
            => type != ArcadeItemType.None && !IsFull && (IsEmpty || CurrentType == type);

        public bool Push(ArcadeStackItem item)
        {
            if (item == null || !CanAccept(item.type)) return false;

            item.gameObject.SetActive(true);
            item.transform.SetParent(null, true);
            item.followVelocity = Vector3.zero;
            item.pickupTimer = 0f;

            var col = item.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            _items.Add(item);
            return true;
        }

        public ArcadeStackItem Pop()
        {
            if (IsEmpty) return null;

            int last = _items.Count - 1;
            var item = _items[last];
            _items.RemoveAt(last);
            return item;
        }

        private void LateUpdate()
        {
            if (stackAnchor == null) return;

            Vector3 below = stackAnchor.position;
            Quaternion rot = stackAnchor.rotation;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null) continue;

                Vector3 target = below + Vector3.up * (i == 0 ? 0f : itemHeight);
                if (item.pickupTimer < PICKUP_ARC_DURATION)
                {
                    item.pickupTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(item.pickupTimer / PICKUP_ARC_DURATION);
                    target.y += Mathf.Sin(t * Mathf.PI) * PICKUP_ARC_HEIGHT;
                }
                float smooth = followSmoothTime * (1f + i * 0.15f);
                item.transform.position = Vector3.SmoothDamp(
                    item.transform.position, target, ref item.followVelocity, smooth);
                item.transform.rotation = Quaternion.Slerp(item.transform.rotation, rot, 0.22f);
                below = item.transform.position;
            }
        }
    }
}
