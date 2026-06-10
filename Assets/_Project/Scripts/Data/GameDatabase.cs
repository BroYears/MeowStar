using System.Collections.Generic;
using UnityEngine;

namespace Nyangsta.Data
{
    /// <summary>
    /// Single source of truth for all content SO assets. Lookup by id.
    /// Created via Create > Nyangsta > GameDatabase and referenced by GameManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Nyangsta/GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<IngredientData> ingredients = new();
        public List<MenuData> menus = new();
        public List<CustomerData> customers = new();
        public List<UpgradeData> upgrades = new();

        private Dictionary<string, IngredientData> _ingredientMap;
        private Dictionary<string, MenuData> _menuMap;
        private Dictionary<string, CustomerData> _customerMap;
        private Dictionary<string, UpgradeData> _upgradeMap;

        public void BuildLookup()
        {
            _ingredientMap = BuildMap(ingredients, i => i.id);
            _menuMap = BuildMap(menus, m => m.id);
            _customerMap = BuildMap(customers, c => c.id);
            _upgradeMap = BuildMap(upgrades, u => u.id);
        }

        public IngredientData GetIngredient(string id) => Get(_ingredientMap, id);
        public MenuData GetMenu(string id) => Get(_menuMap, id);
        public CustomerData GetCustomer(string id) => Get(_customerMap, id);
        public UpgradeData GetUpgrade(string id) => Get(_upgradeMap, id);

        private static Dictionary<string, T> BuildMap<T>(List<T> list, System.Func<T, string> keySelector)
        {
            var map = new Dictionary<string, T>();
            foreach (var item in list)
            {
                if (item == null) continue;
                var key = keySelector(item);
                if (!string.IsNullOrEmpty(key)) map[key] = item;
            }
            return map;
        }

        private static T Get<T>(Dictionary<string, T> map, string id) where T : class
        {
            if (map == null)
            {
                Debug.LogError("[GameDatabase] BuildLookup() not called before Get().");
                return null;
            }
            return map.TryGetValue(id, out var value) ? value : null;
        }
    }
}
