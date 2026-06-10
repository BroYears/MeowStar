using Nyangsta.Core;
using Nyangsta.Stacking;
using UnityEngine;

namespace Nyangsta.Customers
{
    /// <summary>
    /// 손님이 떨군 지폐 더미 (GDD 1.3 #3 — 돈을 쓸어 담는 도파민).
    /// 플레이어가 지나가면 흡수: 살짝 떠올라 플레이어에게 날아간 뒤 골드 가산.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MoneyPile : MonoBehaviour
    {
        public long amount = 10;

        [SerializeField] private float flyDuration = 0.25f;

        private Transform _target;
        private float _t;
        private Vector3 _startPos;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_target != null) return;
            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;
            _target = holder.transform;
            _startPos = transform.position;
            GetComponent<Collider>().enabled = false;
        }

        private void Update()
        {
            if (_target == null) return;

            _t += Time.deltaTime / flyDuration;
            Vector3 targetPos = _target.position + Vector3.up * 1f;
            // 포물선으로 빨려 들어가는 연출
            Vector3 pos = Vector3.Lerp(_startPos, targetPos, _t);
            pos.y += Mathf.Sin(_t * Mathf.PI) * 0.8f;
            transform.position = pos;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, 1f - _t * 0.5f);

            if (_t >= 1f)
            {
                EconomyManager.Instance.AddGold(amount);
                // TODO(B): 코인 사운드(연속 흡수 시 피치 상승) + 숫자 팝업
                Destroy(gameObject);
            }
        }
    }
}
