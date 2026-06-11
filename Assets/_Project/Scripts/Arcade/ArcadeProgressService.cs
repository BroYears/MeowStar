using System;
using System.Collections.Generic;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Pure C# progress tracker for arcade unlock zones (build/hire pads).
    /// Wraps the save-backed id list so zones stay decoupled from SaveManager
    /// and the logic is testable without the Unity engine.
    /// </summary>
    public class ArcadeProgressService
    {
        private readonly List<string> _completedZoneIds;

        /// <summary>Raised once per zone, the first time it is marked complete.</summary>
        public event Action<string> ZoneCompleted;

        public ArcadeProgressService(List<string> completedZoneIds)
        {
            _completedZoneIds = completedZoneIds ?? throw new ArgumentNullException(nameof(completedZoneIds));
        }

        public bool IsComplete(string zoneId)
        {
            return !string.IsNullOrWhiteSpace(zoneId) && _completedZoneIds.Contains(zoneId);
        }

        /// <summary>Records a zone as completed. Returns false when the id is invalid or already recorded.</summary>
        public bool MarkComplete(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || _completedZoneIds.Contains(zoneId)) return false;
            _completedZoneIds.Add(zoneId);
            ZoneCompleted?.Invoke(zoneId);
            return true;
        }
    }
}
