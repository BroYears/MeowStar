using UnityEngine;

namespace Nyangsta.Stacking
{
    /// <summary>스택으로 운반 가능한 아이템 종류. M1은 enum, M2에서 ScriptableObject(IngredientData/MenuData)로 교체 예정.</summary>
    public enum ItemType
    {
        None = 0,
        Fish = 1,        // 강에서 채집
        Berry = 2,       // 숲에서 채집
        GrilledFish = 10, // 그릴 완성품
        BerryJuice = 11,
    }

    /// <summary>머리 위에 쌓이는 아이템 1개. 위치는 StackHolder가 체인 팔로우로 제어.</summary>
    public class StackItem : MonoBehaviour
    {
        public ItemType type = ItemType.Fish;
        [HideInInspector] public Vector3 followVelocity; // SmoothDamp 상태값 (StackHolder용)
    }
}
