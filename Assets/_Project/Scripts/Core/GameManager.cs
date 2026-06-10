using UnityEngine;
using Nyangsta.Data;
using Nyangsta.Progression;
using Nyangsta.Save;

namespace Nyangsta.Core
{
    /// <summary>
    /// Top-level coordinator: holds the content database, bootstraps managers,
    /// and exposes global progression actions (region unlock, restaurant level).
    /// Place this on a Bootstrap GameObject; other managers live as children or
    /// are created here.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameDatabase database;
        public GameDatabase Database => database;

        public bool IsPaused { get; private set; }

        protected override void OnAwake()
        {
            if (database != null) database.BuildLookup();
            // SaveManager loads in its own Awake; ensure it exists before others read it.
            Application.targetFrameRate = 60;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        // ---- Progression ----
        public bool IsRegionUnlocked(RegionType region)
        {
            if (ProgressionManager.Instance != null)
                return ProgressionManager.Instance.IsRegionUnlocked(region);
            return SaveManager.Instance.Data.unlockedRegions.Contains(region.ToString());
        }

        public void UnlockRegion(RegionType region)
        {
            var list = SaveManager.Instance.Data.unlockedRegions;
            string key = region.ToString();
            if (!list.Contains(key))
            {
                list.Add(key);
                GameEvents.RaiseRegionUnlocked(region);
                SaveManager.Instance.Save();
            }
        }

        public RegionType CurrentRegion
        {
            get
            {
                if (ProgressionManager.Instance != null)
                    return ProgressionManager.Instance.CurrentRegion;
                return System.Enum.TryParse(SaveManager.Instance.Data.currentRegion, out RegionType region)
                    ? region
                    : RegionType.Forest;
            }
        }

        public void SetRestaurantLevel(int level)
        {
            var data = SaveManager.Instance.Data;
            if (level == data.restaurantLevel) return;
            data.restaurantLevel = Mathf.Max(1, level);
            GameEvents.RaiseRestaurantLevelChanged(data.restaurantLevel);
        }
    }
}
