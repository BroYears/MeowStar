using System;
using System.Collections.Generic;

namespace Nyangsta.Save
{
    [Serializable]
    public class SettingsData
    {
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
        public string language = "ko";
    }

    [Serializable]
    public class IngredientStack
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class ArcadeZonePaymentProgress
    {
        public string id;
        public double paid;
    }

    [Serializable]
    public class ArcadeUpgradePaymentProgress
    {
        public string id;
        public int level;
        public double paid;
    }

    /// <summary>
    /// Root save object. JsonUtility doesn't serialize Dictionary, so collections
    /// are stored as lists and rebuilt into dictionaries at runtime by the managers.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public double gold;
        public int gems;
        public int restaurantLevel = 1;
        public int worldTreeLevel = 1;
        public int worldTreeEssence;
        public string currentRegion = "Forest";

        public List<IngredientStack> ingredients = new();
        public List<UpgradeLevel> upgradeLevels = new();
        public List<string> unlockedRegions = new() { "Forest" };
        public List<string> recruitedStaff = new();
        public List<string> arcadeCompletedZones = new();
        public List<UpgradeLevel> arcadeUpgradeLevels = new();
        public List<ArcadeZonePaymentProgress> arcadeZonePaymentProgress = new();
        public List<ArcadeUpgradePaymentProgress> arcadeUpgradePaymentProgress = new();

        public long lastQuitUnixTime;
        public bool adsRemoved;
        public bool arcadeTutorialDone;
        public SettingsData settings = new();
    }
}
