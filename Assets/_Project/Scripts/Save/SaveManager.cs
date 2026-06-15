using System;
using System.IO;
using UnityEngine;
using Nyangsta.Core;

namespace Nyangsta.Save
{
    /// <summary>
    /// Handles JSON persistence, autosave, and offline-time calculation.
    /// Other managers read/write through the shared <see cref="Data"/> instance.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        public SaveData Data { get; private set; } = new SaveData();

        private string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        protected override void OnAwake()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                    Migrate(Data);
                }
                else
                {
                    Data = new SaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e}. Starting fresh.");
                Data = new SaveData();
            }
        }

        public void Save()
        {
            try
            {
                Data.lastQuitUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
            }
        }

        /// <summary>Seconds elapsed since last quit, clamped to [0, capSeconds].</summary>
        public double GetOfflineSeconds(double capSeconds)
        {
            if (Data.lastQuitUnixTime <= 0) return 0;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var elapsed = now - Data.lastQuitUnixTime;
            return Math.Clamp(elapsed, 0, capSeconds);
        }

        private void Migrate(SaveData data)
        {
            if (data.ingredients == null) data.ingredients = new();
            if (data.upgradeLevels == null) data.upgradeLevels = new();
            if (data.unlockedRegions == null) data.unlockedRegions = new();
            if (data.recruitedStaff == null) data.recruitedStaff = new();
            if (data.arcadeCompletedZones == null) data.arcadeCompletedZones = new();
            if (data.arcadeUpgradeLevels == null) data.arcadeUpgradeLevels = new();
            if (data.arcadeZonePaymentProgress == null) data.arcadeZonePaymentProgress = new();
            if (data.arcadeUpgradePaymentProgress == null) data.arcadeUpgradePaymentProgress = new();
            if (data.settings == null) data.settings = new SettingsData();

            if (data.restaurantLevel < 1) data.restaurantLevel = 1;
            if (data.worldTreeLevel < 1) data.worldTreeLevel = 1;
            if (string.IsNullOrEmpty(data.currentRegion)) data.currentRegion = "Forest";
            if (!data.unlockedRegions.Contains("Forest")) data.unlockedRegions.Add("Forest");

            data.version = 2;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
