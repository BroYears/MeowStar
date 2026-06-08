namespace Nyastar.WorldTree
{
    /// <summary>
    /// 재화 보관/소비 추상화. 세계수 성장은 이 지갑에서 비용을 차감한다.
    ///
    /// <para>아직 만들지 않은 경제/인벤토리/세이브 시스템과 세계수 프레임을 분리하기 위한 seam.
    /// 실제 경제 시스템이 이 인터페이스를 구현해 주입하면 되고, 그 전까지는
    /// <see cref="SimpleResourceWallet"/> 로 단독 테스트가 가능하다.</para>
    /// </summary>
    public interface IResourceWallet
    {
        int GetAmount(ResourceType type);
        bool CanAfford(ResourceType type, int amount);
        void Spend(ResourceType type, int amount);
        void Add(ResourceType type, int amount);
    }
}
