using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Visible floating joystick for mobile. Anchors to wherever the player first
    /// touches the left half of the screen, then reports a normalized direction.
    /// Drawn with OnGUI so the prototype needs no Canvas/prefab setup.
    /// </summary>
    public class ArcadeJoystick : MonoBehaviour
    {
        [SerializeField] private float radius = 130f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;

        private bool _active;
        private int _pointerId = -1;
        private Vector2 _origin;     // screen-space, y-up
        private Vector2 _knob;       // screen-space, y-up

        private Texture2D _baseTex;
        private Texture2D _knobTex;

        /// <summary>Normalized move direction (x = right, y = forward). Zero when idle.</summary>
        public Vector2 Direction { get; private set; }
        public bool IsActive => _active;

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
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                UpdateFromPointer(
                    mouse.position.ReadValue(),
                    mouse.leftButton.wasPressedThisFrame,
                    pointer: 1);
                return;
            }

            Release();
        }

        private void UpdateFromPointer(Vector2 screenPos, bool pressedThisFrame, int pointer)
        {
            // Only claim presses that start on the left half of the screen, so the
            // right half stays free for future buttons (jump, interact, etc.).
            if (!_active)
            {
                if (!pressedThisFrame || screenPos.x > Screen.width * 0.5f) return;
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

        private void OnGUI()
        {
            if (!_active) return;

            _baseTex ??= MakeCircleTex(new Color(1f, 1f, 1f, 0.18f));
            _knobTex ??= MakeCircleTex(new Color(1f, 0.55f, 0.24f, 0.85f));

            // GUI is y-down; flip from our y-up screen-space coords.
            Vector2 baseCenter = new(_origin.x, Screen.height - _origin.y);
            Vector2 knobCenter = baseCenter + new Vector2(Direction.x, -Direction.y) * radius;

            float baseSize = radius * 2f;
            GUI.DrawTexture(new Rect(baseCenter.x - radius, baseCenter.y - radius, baseSize, baseSize), _baseTex);

            float knobR = radius * 0.45f;
            GUI.DrawTexture(new Rect(knobCenter.x - knobR, knobCenter.y - knobR, knobR * 2f, knobR * 2f), _knobTex);
        }

        private static Texture2D MakeCircleTex(Color color, int size = 96)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f;
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Soft edge for a less jagged circle.
                float a = Mathf.Clamp01((r - dist) / 2f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                if (a <= 0f) tex.SetPixel(x, y, clear);
            }
            tex.Apply();
            return tex;
        }
    }
}
