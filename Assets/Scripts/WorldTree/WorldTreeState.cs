using System;
using System.Collections.Generic;

namespace Nyastar.WorldTree
{
    /// <summary>
    /// 세계수 동적 세이브 데이터 — JSON 직렬화 대상. (GDD 3.5 Save/Load)
    ///
    /// <para>Unity <c>JsonUtility</c> 직렬화를 위해 필드는 public, 컬렉션은 List 로 둔다
    /// (Dictionary 는 JsonUtility 미지원). 전체 세이브 루트가 이 객체를 멤버로 포함하게 된다.</para>
    /// </summary>
    [Serializable]
    public class WorldTreeState
    {
        /// <summary>현재 세계수 레벨(1부터 시작).</summary>
        public int level = 1;

        /// <summary>현재 식당이 위치한 지역 ID.</summary>
        public string currentRegionId;

        /// <summary>해금된 지역 ID 목록.</summary>
        public List<string> unlockedRegionIds = new List<string>();

        public bool IsRegionUnlocked(string regionId) => unlockedRegionIds.Contains(regionId);
    }
}
