using UnityEngine;

namespace Nyangsta.Arcade
{
    public enum ArcadeItemType
    {
        None = 0,

        // Raw resources gathered from world nodes.
        Fish = 1,
        Berry = 2,
        Wood = 3,
        Mushroom = 4,
        Salmon = 5,
        Honey = 6,

        // Cooked / processed goods sold to customers.
        GrilledFish = 10,
        BerryJuice = 11,
        MushroomSkewer = 12,
        SalmonSteak = 13,
        HoneyDessert = 14,
    }

    /// <summary>One physical item carried in a stack above the actor.</summary>
    public class ArcadeStackItem : MonoBehaviour
    {
        public ArcadeItemType type = ArcadeItemType.Fish;
        [HideInInspector] public Vector3 followVelocity;
        [HideInInspector] public float pickupTimer; // reset on Push; drives the pickup arc
    }
}
