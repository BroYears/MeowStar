namespace Nyastar.WorldTree
{
    /// <summary>
    /// 지역(대륙) 테마. 지역 이동 시 배경/손님/사냥 재료가 통째로 교체된다. (GDD 3.3)
    /// 신규 지역은 여기에 추가.
    /// </summary>
    public enum RegionTheme
    {
        FairyForest,  // 요정의 숲 (기본)
        IceValley,    // 얼음 계곡
        GoldenDesert, // 황금 사막
        River,        // 강가 (냥스타 키친)
        Cave,         // 동굴 (냥스타 키친)
        Snow,         // 설산 (냥스타 키친)
    }
}
