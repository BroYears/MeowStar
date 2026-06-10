using System;
using Nyangsta.Data;

namespace Nyangsta.Core
{
    /// <summary>
    /// Lightweight static event bus to decouple systems (e.g. gold change -> HUD refresh).
    /// Subscribers MUST unsubscribe in OnDisable/OnDestroy to avoid leaks.
    /// </summary>
    public static class GameEvents
    {
        // Economy
        public static event Action<double> GoldChanged;
        public static event Action<int> GemsChanged;
        public static event Action<string, int> IngredientChanged; // id, newAmount

        // Upgrades
        public static event Action<string, int> UpgradePurchased;   // id, newLevel

        // Hunting
        public static event Action HuntStarted;
        public static event Action<int> HuntFinished;               // ingredients gained

        // Customers
        public static event Action<CustomerData> CustomerServed;    // satisfied & paid
        public static event Action<CustomerData> CustomerLeftAngry;

        // Progression
        public static event Action<RegionType> RegionUnlocked;
        public static event Action<RegionType> CurrentRegionChanged;
        public static event Action<int> RestaurantLevelChanged;
        public static event Action<int, int> WorldTreeChanged;       // level, essence
        public static event Action<string> StaffRecruited;

        // Offline
        public static event Action<double> OfflineIncomeReady;      // accumulated gold

        public static void RaiseGoldChanged(double gold) => GoldChanged?.Invoke(gold);
        public static void RaiseGemsChanged(int gems) => GemsChanged?.Invoke(gems);
        public static void RaiseIngredientChanged(string id, int amount) => IngredientChanged?.Invoke(id, amount);
        public static void RaiseUpgradePurchased(string id, int level) => UpgradePurchased?.Invoke(id, level);
        public static void RaiseHuntStarted() => HuntStarted?.Invoke();
        public static void RaiseHuntFinished(int gained) => HuntFinished?.Invoke(gained);
        public static void RaiseCustomerServed(CustomerData c) => CustomerServed?.Invoke(c);
        public static void RaiseCustomerLeftAngry(CustomerData c) => CustomerLeftAngry?.Invoke(c);
        public static void RaiseRegionUnlocked(RegionType r) => RegionUnlocked?.Invoke(r);
        public static void RaiseCurrentRegionChanged(RegionType r) => CurrentRegionChanged?.Invoke(r);
        public static void RaiseRestaurantLevelChanged(int lvl) => RestaurantLevelChanged?.Invoke(lvl);
        public static void RaiseWorldTreeChanged(int level, int essence) => WorldTreeChanged?.Invoke(level, essence);
        public static void RaiseStaffRecruited(string staffId) => StaffRecruited?.Invoke(staffId);
        public static void RaiseOfflineIncomeReady(double gold) => OfflineIncomeReady?.Invoke(gold);
    }
}
