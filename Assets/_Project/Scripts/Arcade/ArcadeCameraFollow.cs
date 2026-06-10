using UnityEngine;

namespace Nyangsta.Arcade
{
    public class ArcadeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 9.0f, -8.8f);
        [SerializeField] private float pitch = 50f;
        [SerializeField] private float smoothTime = 0.14f;

        private Vector3 _velocity;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Ease toward the player but keep the world mostly in frame (gentle follow).
            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
