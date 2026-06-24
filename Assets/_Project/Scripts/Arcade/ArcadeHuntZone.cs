using UnityEngine;
using Nyangsta.Progression;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Lightweight arcade-side exploration pad. The legacy hunt mini-game still owns the
    /// full tap gameplay, but the runtime arcade map needs a world interaction that can
    /// feed world-tree essence without reopening the disabled tab UI.
    /// </summary>
    public class ArcadeHuntZone : InteractionZone
    {
        [SerializeField] private int essenceReward = 3;
        [SerializeField] private float channelSeconds = 2.2f;
        [SerializeField] private float cooldownSeconds = 6f;
        [SerializeField] private WorldBubble bubble;
        [SerializeField] private string displayName = "탐험";

        private float _channel;
        private float _cooldown;
        private float _labelTimer;
        private string _lastLabel;

        private const float LabelRefreshInterval = 0.2f;

        public void Configure(int reward, float channelTime, float cooldown, WorldBubble statusBubble, string name)
        {
            essenceReward = Mathf.Max(1, reward);
            channelSeconds = Mathf.Max(0.2f, channelTime);
            cooldownSeconds = Mathf.Max(0.1f, cooldown);
            bubble = statusBubble;
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            UpdateLabel(true);
        }

        private void Update()
        {
            if (_cooldown > 0f)
                _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);

            _labelTimer += Time.deltaTime;
            if (_labelTimer < LabelRefreshInterval) return;
            _labelTimer = 0f;
            UpdateLabel();
        }

        protected override void OnAgentEnter(StackHolder agent)
        {
            if (IsPlayer(agent)) UpdateLabel(true);
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (!IsPlayer(agent))
                return;

            var progression = ProgressionManager.Instance;
            if (progression == null || progression.WorldTreeLevel >= progression.MaxWorldTreeLevel)
            {
                _channel = 0f;
                UpdateLabel();
                return;
            }

            if (_cooldown > 0f)
            {
                _channel = 0f;
                UpdateLabel();
                return;
            }

            _channel += dt;
            UpdateLabel();

            if (_channel < channelSeconds)
                return;

            _channel = 0f;
            _cooldown = cooldownSeconds;
            progression.AddWorldTreeEssence(essenceReward);
            Nyangsta.Save.SaveManager.Instance?.Save();
            Nyangsta.Audio.Sfx.Fanfare();
            Nyangsta.Core.Haptics.Light();
            UpdateLabel(true);
        }

        protected override void OnAgentExit(StackHolder agent)
        {
            if (!IsPlayer(agent)) return;
            _channel = 0f;
            UpdateLabel(true);
        }

        private void UpdateLabel(bool force = false)
        {
            if (bubble == null) return;

            var progression = ProgressionManager.Instance;
            string label;
            if (progression == null)
                label = "대기";
            else if (progression.WorldTreeLevel >= progression.MaxWorldTreeLevel)
                label = "완료";
            else if (_cooldown > 0f)
                label = $"휴식 {Mathf.CeilToInt(_cooldown)}초";
            else if (_channel > 0f)
                label = $"{Mathf.Clamp01(_channel / channelSeconds) * 100f:0}%";
            else
                label = $"+{essenceReward} 정수";

            if (!force && label == _lastLabel) return;
            _lastLabel = label;
            bubble.SetTitle(displayName, 30, new Vector2(0f, 0.24f));
            bubble.SetValue(label, 28, new Vector2(0f, -0.12f), UITheme.Ink);
        }

        private static bool IsPlayer(StackHolder agent)
        {
            return agent != null && agent.GetComponentInParent<ArcadePlayerController>() != null;
        }
    }
}
