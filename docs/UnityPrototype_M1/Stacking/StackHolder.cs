using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Stacking
{
    /// <summary>
    /// 머리 위 스태킹 (GDD 4.2). 플레이어·직원 공용.
    /// - 한 번에 한 종류만 운반
    /// - 체인 팔로우(아래 아이템을 위 아이템이 지연 추적)로 출렁임 연출 → 이게 손맛의 핵심
    /// </summary>
    public class StackHolder : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private int capacity = 3;            // 캐릭터 업그레이드 대상 스탯

        [Header("Stack Visual")]
        [SerializeField] private Transform stackAnchor;        // 머리 위 빈 오브젝트
        [SerializeField] private float itemHeight = 0.45f;
        [SerializeField, Range(0.01f, 0.3f)]
        private float followSmoothTime = 0.06f;                // 클수록 더 출렁임. 0.05~0.1 사이 튜닝

        private readonly List<StackItem> _items = new();

        public int Capacity { get => capacity; set => capacity = value; }
        public int Count => _items.Count;
        public bool IsFull => _items.Count >= capacity;
        public bool IsEmpty => _items.Count == 0;
        public ItemType CurrentType => IsEmpty ? ItemType.None : _items[0].type;

        /// <summary>이 종류를 더 집을 수 있나? (빈손이거나 같은 종류 + 여유 있음)</summary>
        public bool CanAccept(ItemType type)
            => !IsFull && (IsEmpty || CurrentType == type);

        public bool Push(StackItem item)
        {
            if (item == null || !CanAccept(item.type)) return false;
            item.transform.SetParent(null);
            var col = item.GetComponent<Collider>();
            if (col) col.enabled = false;
            _items.Add(item);
            return true;
        }

        public StackItem Pop()
        {
            if (IsEmpty) return null;
            int last = _items.Count - 1;
            StackItem item = _items[last];
            _items.RemoveAt(last);
            return item;
        }

        /// <summary>체인 팔로우: 0번은 앵커를, n번은 n-1번 위를 지연 추적 → 자연스러운 출렁임.</summary>
        private void LateUpdate()
        {
            if (stackAnchor == null) return;

            Vector3 targetBelow = stackAnchor.position;
            Quaternion rot = stackAnchor.rotation;

            for (int i = 0; i < _items.Count; i++)
            {
                StackItem item = _items[i];
                Vector3 target = targetBelow + Vector3.up * (i == 0 ? 0f : itemHeight);

                // 위로 갈수록 살짝 더 느리게 따라와 채찍처럼 출렁임
                float smooth = followSmoothTime * (1f + i * 0.15f);
                item.transform.position = Vector3.SmoothDamp(
                    item.transform.position, target, ref item.followVelocity, smooth);
                item.transform.rotation = Quaternion.Slerp(item.transform.rotation, rot, 0.2f);

                targetBelow = item.transform.position;
            }
        }
    }
}
