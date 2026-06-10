using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Floating joystick movement for the arcade-idle M1 prototype.
    /// Uses the new Input System so it works with the current project settings.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ArcadePlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.2f;
        [SerializeField] private float rotateSpeed = 16f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private ArcadeJoystick joystick;

        private CharacterController _controller;
        private float _yVelocity;

        public bool IsMoving { get; private set; }

        public void Configure(ArcadeJoystick stick)
        {
            joystick = stick;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = ReadKeyboard();
            if (input.sqrMagnitude < 0.01f && joystick != null)
                input = joystick.Direction;

            Vector3 move = new(input.x, 0f, input.y);
            IsMoving = move.sqrMagnitude > 0.001f;

            if (IsMoving)
            {
                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
            }

            _yVelocity = _controller.isGrounded ? -1f : _yVelocity + gravity * Time.deltaTime;
            Vector3 velocity = move * moveSpeed + Vector3.up * _yVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private static Vector2 ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
