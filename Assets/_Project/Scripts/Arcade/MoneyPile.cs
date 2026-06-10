using UnityEngine;
using Nyangsta.Economy;

namespace Nyangsta.Arcade
{
    [RequireComponent(typeof(Collider))]
    public class MoneyPile : MonoBehaviour
    {
        [SerializeField] private double amount = 10;
        [SerializeField] private float flyDuration = 0.25f;

        private Transform _target;
        private Vector3 _startPos;
        private float _t;

        public double Amount { get => amount; set => amount = value; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_target != null) return;

            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;

            _target = holder.transform;
            _startPos = transform.position;
            _t = 0f;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void Update()
        {
            if (_target == null) return;

            _t += Time.deltaTime / Mathf.Max(0.05f, flyDuration);
            Vector3 targetPos = _target.position + Vector3.up * 1.15f;
            Vector3 pos = Vector3.Lerp(_startPos, targetPos, _t);
            pos.y += Mathf.Sin(_t * Mathf.PI) * 0.8f;
            transform.position = pos;
            transform.localScale = Vector3.one * Mathf.Max(0.08f, 1f - _t * 0.55f);

            if (_t >= 1f)
            {
                EconomyManager.Instance?.AddGold(amount);
                Destroy(gameObject);
            }
        }
    }
}
