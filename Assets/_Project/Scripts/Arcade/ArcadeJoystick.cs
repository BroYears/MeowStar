using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nyangsta.UI;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Visible floating joystick for mobile. Anchors to wherever the player first
    /// touches the left half of the screen, then reports a normalized direction.
    /// Visuals are uGUI Images on the arcade HUD canvas (see <see cref="AttachVisual"/>);
    /// input logic stays self-contained so the joystick works even without visuals.
    /// </summary>
    public class ArcadeJoystick : MonoBehaviour
    {
        [SerializeField] private float radius = 130f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        // Top fraction of the screen reserved for the HUD; presses that start there
        // never grab the stick, so reading/tapping the gold/stack pills and the goal
        // panel can't accidentally drive the player.
        [SerializeField, Range(0f, 0.6f)] private float hudReserveTop = 0.35f;

        private bool _active;
        private int _pointerId = -1;
        private Vector2 _origin;     // screen-space, y-up
        private Vector2 _knob;       // screen-space, y-up

        private Canvas _canvas;
        private RectTransform _baseRect;
        private RectTransform _knobRect;

        /// <summary>Normalized move direction (x = right, y = forward). Zero when idle.</summary>
        public Vector2 Direction { get; private set; }
        public bool IsActive => _active;

        /// <summary>Create the base/knob images under a HUD canvas layer.</summary>
        public void AttachVisual(RectTransform layer)
        {
            if (layer == null) return;
            _canvas = layer.GetComponentInParent<Canvas>();

            var baseImg = UIFactory.Image("Img_JoystickBase", layer, UITheme.Circle,
                new Color(1f, 1f, 1f, 0.18f));
            baseImg.raycastTarget = false;
            _baseRect = baseImg.rectTransform;

            var knobImg = UIFactory.Image("Img_JoystickKnob", layer, UITheme.Circle,
                new Color(1f, 0.55f, 0.24f, 0.85f));
            knobImg.raycastTarget = false;
            _knobRect = knobImg.rectTransform;

            _baseRect.gameObject.SetActive(false);
            _knobRect.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Touch takes priority; fall back to mouse so it is testable on desktop.
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                UpdateFromPointer(
                    touch.primaryTouch.position.ReadValue(),
                    touch.primaryTouch.press.wasPressedThisFrame,
                    pointer: 0);
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    UpdateFromPointer(
                        mouse.position.ReadValue(),
                        mouse.leftButton.wasPressedThisFrame,
                        pointer: 1);
                }
                else
                {
                    Release();
                }
            }

            UpdateVisual();
        }

        private void UpdateFromPointer(Vector2 screenPos, bool pressedThisFrame, int pointer)
        {
            // Only claim presses that start on the lower-left of the screen: the right
            // half stays free for future buttons (jump, interact, etc.), and the top
            // band stays free for the HUD so its widgets don't double as a move pad.
            if (!_active)
            {
                if (!pressedThisFrame ||
                    screenPos.x > Screen.width * 0.5f ||
                    screenPos.y > Screen.height * (1f - hudReserveTop) ||
                    !Screen.safeArea.Contains(screenPos)) return;
                _active = true;
                _pointerId = pointer;
                _origin = screenPos;
            }
            else if (_pointerId != pointer)
            {
                return;
            }

            _knob = screenPos;
            Vector2 delta = (_knob - _origin) / radius;
            if (delta.magnitude < deadZone) delta = Vector2.zero;
            Direction = Vector2.ClampMagnitude(delta, 1f);
        }

        private void Release()
        {
            _active = false;
            _pointerId = -1;
            Direction = Vector2.zero;
        }

        private void UpdateVisual()
        {
            if (_baseRect == null) return;

            if (_baseRect.gameObject.activeSelf != _active)
            {
                _baseRect.gameObject.SetActive(_active);
                _knobRect.gameObject.SetActive(_active);
            }
            if (!_active) return;

            // Screen-space-overlay canvas: RectTransform.position is in screen pixels,
            // while sizeDelta is in scaled canvas units — divide by the scale factor.
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float baseSize = radius * 2f / scale;
            float knobSize = radius * 0.9f / scale;
            _baseRect.sizeDelta = new Vector2(baseSize, baseSize);
            _knobRect.sizeDelta = new Vector2(knobSize, knobSize);

            Vector2 knobCenter = _origin + Direction * radius;
            _baseRect.position = _origin;
            _knobRect.position = knobCenter;
        }
    }
}
