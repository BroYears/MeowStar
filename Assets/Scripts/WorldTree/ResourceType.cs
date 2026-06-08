namespace Nyastar.WorldTree
{
    /// <summary>
    /// 세계수 성장에 사용되는 재화 종류. (GDD 3.3 — 세계수의 이슬, 정수 등)
    /// 추후 일반 경제 시스템으로 승격될 수 있으나, 현재는 세계수 프레임 범위에 둔다.
    /// </summary>
    public enum ResourceType
    {
        Dew,     // 세계수의 이슬
        Essence, // 세계수의 정수
        Gold,    // 골드 (소프트 재화)
        Gem      // 보석 (하드 재화)
    }
}
