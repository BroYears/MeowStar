using System;
using System.Collections.Generic;
using Nyangsta.Save;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Pure C# tracker for arcade facility upgrade levels (grill/juicer speed, etc.).
    /// Wraps the save-backed level list so upgrade zones stay decoupled from SaveManager
    /// and the logic is testable without the Unity engine. Mirrors
    /// <see cref="ArcadeProgressService"/> but stores a per-id level instead of a flag.
    /// </summary>
    public class ArcadeUpgradeService
    {
        private readonly List<UpgradeLevel> _levels;

        public ArcadeUpgradeService(List<UpgradeLevel> levels)
        {
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }

        public int GetLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            foreach (var entry in _levels)
                if (entry != null && entry.id == id) return entry.level;
            return 0;
        }

        /// <summary>Stores the level for an id (adds the entry on first use).</summary>
        public void SetLevel(string id, int level)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            foreach (var entry in _levels)
                if (entry != null && entry.id == id) { entry.level = level; return; }
            _levels.Add(new UpgradeLevel { id = id, level = level });
        }
    }
}
