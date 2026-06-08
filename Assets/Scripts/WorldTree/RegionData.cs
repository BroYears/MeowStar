using UnityEngine;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 지역 정적 데이터(ScriptableObject). 기획자가 인스펙터에서 편집한다. (GDD 3.5)
    ///
    /// <para>지역 이동 시 이 데이터가 가리키는 배경 프리팹 / 손님 테이블 / 사냥 스폰 테이블을
    /// Scene_Lobby · Scene_Hunt 가 교체 로드한다(씬 충돌 방지를 위한 동적 로드 방식 — GDD 5.1).</para>
    /// </summary>
    [CreateAssetMenu(fileName = "RegionData", menuName = "Nyastar/World Tree/Region Data")]
    public class RegionData : ScriptableObject
    {
        [Tooltip("고유 지역 ID — 세이브/스폰테이블 매핑 키. 예: REGION_FAIRY_FOREST")]
        public string regionId;

        [Tooltip("표시 이름. 예: 요정의 숲")]
        public string displayName;

        [Tooltip("지역 테마 — 배경/손님/재료 세트 구분")]
        public RegionTheme theme = RegionTheme.FairyForest;

        [Tooltip("이 지역 해금에 필요한 세계수 레벨 (이 레벨 이상이면 해금)")]
        [Min(1)] public int requiredWorldTreeLevel = 1;

        // ── 후속 연결 지점(다른 파트 담당자와 협의 후 채움) ──
        // [Tooltip("Scene_Lobby 배경 테마 프리팹")]            public GameObject lobbyThemePrefab;
        // [Tooltip("Scene_Hunt 사냥터 맵/스폰 테이블")]         public ScriptableObject huntSpawnTable;
        // [Tooltip("이 지역에서 등장 가능한 손님 데이터 목록")]  public ScriptableObject[] customers;
    }
}
