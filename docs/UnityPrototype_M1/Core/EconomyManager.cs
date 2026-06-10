using System;
using UnityEngine;

namespace Nyangsta.Core
{
    /// <summary>골드 잔액 관리 싱글톤. M1 그레이박스용 최소 구현.</summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private long startingGold = 0;

        public long Gold { get; private set; }
        public event Action<long> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Gold = startingGold;
        }

        public void AddGold(long amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>잔액이 충분하면 차감하고 true. BuildZone의 초당 드레인에 사용.</summary>
        public bool TrySpend(long amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }
    }
}
