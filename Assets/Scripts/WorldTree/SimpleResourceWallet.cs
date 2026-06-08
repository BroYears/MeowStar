using System.Collections.Generic;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 메모리 기반 <see cref="IResourceWallet"/> 기본 구현 — 단독 테스트/프로토타입용.
    /// 실제 경제·세이브 시스템 완성 시 그쪽 구현체로 교체한다(세이브 영속화 포함).
    /// </summary>
    public class SimpleResourceWallet : IResourceWallet
    {
        private readonly Dictionary<ResourceType, int> _amounts = new Dictionary<ResourceType, int>();

        public int GetAmount(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

        public bool CanAfford(ResourceType type, int amount) => GetAmount(type) >= amount;

        public void Spend(ResourceType type, int amount)
        {
            if (!CanAfford(type, amount))
                throw new System.InvalidOperationException($"{type} 재화가 부족합니다. (보유 {GetAmount(type)}, 필요 {amount})");
            _amounts[type] = GetAmount(type) - amount;
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount < 0) return;
            _amounts[type] = GetAmount(type) + amount;
        }
    }
}
