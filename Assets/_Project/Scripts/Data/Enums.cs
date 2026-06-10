namespace Nyangsta.Data
{
    public enum Rarity { Common, Rare, Epic, Legendary }

    public enum RegionType { Forest, River, Cave, Snow }

    public enum UpgradeCategory { Hunting, Cooking, Restaurant, Idle }

    public enum CustomerState { Entering, Waiting, Ordering, Eating, Paying, Leaving }

    public enum StaffEffectType
    {
        CookSpeed,
        ServiceSlots,
        HuntDuration,
        IngredientBonus,
        IdleGoldPerSecond,
        RevenueMultiplier
    }
}
