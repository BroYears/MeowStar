using UnityEngine;

namespace Nyangsta.Player
{
    /// <summary>
    /// 플로팅 조이스틱(화면 아무 곳 드래그) + CharacterController 이동.
    /// 버튼 없음 — 이동이 유일한 조작 (GDD 4.1).
    /// 레거시 Input 사용: Project Settings > Player > Active Input Handling = Both 또는 Input Manager(Old).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;       // 캐릭터 업그레이드 대상 스탯
        [SerializeField] private float rotateSpeed = 15f;
        [SerializeField] private float gravity = -20f;

        [Header("Joystick")]
        [SerializeField] private float joystickRadius = 120f; // px. 이 거리에서 최대 속도

        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public bool IsMoving { get; private set; }

        private CharacterController _cc;
        private Vector2 _touchOrigin;
        private bool _dragging;
        private float _yVelocity;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void Update()
        {
            Vector2 input = ReadJoystick();
            Vector3 move = new Vector3(input.x, 0f, input.y);
            IsMoving = move.sqrMagnitude > 0.001f;

            if (IsMoving)
            {
                // 카메라가 고정 탑뷰라는 전제 (월드 기준 이동)
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
            }

            _yVelocity = _cc.isGrounded ? -1f : _yVelocity + gravity * Time.deltaTime;
            Vector3 velocity = move * moveSpeed + Vector3.up * _yVelocity;
            _cc.Move(velocity * Time.deltaTime);
        }

        /// <summary>터치 시작점을 원점으로 하는 가상 조이스틱 (-1~1).</summary>
        private Vector2 ReadJoystick()
        {
            // 에디터/PC: 마우스
            if (Input.GetMouseButtonDown(0)) { _touchOrigin = Input.mousePosition; _dragging = true; }
            if (Input.GetMouseButtonUp(0)) _dragging = false;

            if (!_dragging) return Vector2.zero;

            Vector2 delta = (Vector2)Input.mousePosition - _touchOrigin;
            return Vector2.ClampMagnitude(delta / joystickRadius, 1f);
        }
    }
}
