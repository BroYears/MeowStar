using UnityEngine;

namespace Nyangsta.Customers
{
    /// <summary>일정 간격으로 손님을 스폰해 빈 테이블에 배정 (GDD 4.4 스케줄러의 M1 버전).</summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private Customer customerPrefab;
        [SerializeField] private TableZone[] tables;
        [SerializeField] private Transform spawnPoint;   // 입구
        [SerializeField] private Transform exitPoint;    // 출구 (보통 입구와 동일)
        [SerializeField] private float spawnInterval = 5f;

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;

            TableZone freeTable = FindFreeTable();
            if (freeTable == null) return; // 자리 없으면 대기 (M2: 줄서기 추가)

            _timer = 0f;
            Customer c = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            c.Init(freeTable, exitPoint.position);
            freeTable.Seat(c);
        }

        private TableZone FindFreeTable()
        {
            foreach (var t in tables)
                if (!t.IsOccupied) return t;
            return null;
        }
    }
}
