using System.Collections.Generic;
using UnityEngine;
using Nyangsta.Core;

namespace Nyangsta.UI
{
    /// <summary>
    /// Minimal panel-stack manager. Panels are prefabs registered by key and
    /// shown/hidden by name. Developer B fleshes this out with transitions/HUD.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Transform panelRoot;

        private readonly Dictionary<string, GameObject> _panels = new();
        private readonly Stack<string> _stack = new();

        public void Register(string key, GameObject panel)
        {
            _panels[key] = panel;
            panel.SetActive(false);
        }

        public void Show(string key)
        {
            if (!_panels.TryGetValue(key, out var panel)) return;
            panel.SetActive(true);
            _stack.Push(key);
        }

        public void Back()
        {
            if (_stack.Count == 0) return;
            var key = _stack.Pop();
            if (_panels.TryGetValue(key, out var panel)) panel.SetActive(false);
        }
    }
}
