using System;
using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Procedural sprite animation without Animator clips. Runs after SpriteBillboard
    /// (execution order 50) so the lean/wobble roll multiplies on top of the billboard
    /// rotation that was just written this frame. Bounce uses localPosition and
    /// flip/breathe use localScale, so nothing fights the billboard's rotation.
    ///
    /// Actor mode derives velocity from the motion source transform's position delta
    /// (defaults to the parent, which is the moving capsule for player/staff/customer);
    /// SetVelocity lets a controller push velocity explicitly instead. Facility mode
    /// wobbles while the activity delegate returns true. Zero GC alloc per frame.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SpriteMotionAnimator : MonoBehaviour
    {
        private enum Mode { Actor, Facility }

        private const float MOVE_SPEED_THRESHOLD = 0.05f;
        private const float FLIP_SPEED_THRESHOLD = 0.1f;

        [SerializeField] private float hopHeight = 0.09f;
        [SerializeField] private float hopFrequency = 9f;
        [SerializeField] private float leanAngle = 7f;
        [SerializeField] private float leanRefSpeed = 4f;
        [SerializeField] private float breatheAmount = 0.02f;
        [SerializeField] private float breatheFrequency = 2.2f;
        [SerializeField] private float wobbleAngle = 4f;
        [SerializeField] private float wobbleFrequency = 14f;
        [SerializeField] private float wobblePulse = 0.04f;

        private Mode _mode = Mode.Actor;
        private Transform _source;
        private Func<bool> _isActive;
        private Vector3 _lastSourcePos;
        private Vector3 _velocity;
        private bool _useExternalVelocity;

        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale = Vector3.one;
        private float _hopPhase;
        private float _lean;
        private float _flipSign = 1f;

        /// <summary>Drive movement animation from a transform's per-frame position delta.</summary>
        public void ConfigureActor(Transform motionSource)
        {
            _mode = Mode.Actor;
            _source = motionSource;
            if (motionSource != null) _lastSourcePos = motionSource.position;
        }

        /// <summary>Wobble while the delegate returns true (e.g. cook station busy).</summary>
        public void ConfigureFacility(Func<bool> isActive)
        {
            _mode = Mode.Facility;
            _isActive = isActive;
        }

        /// <summary>Push velocity explicitly instead of sampling the source transform.</summary>
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            _useExternalVelocity = true;
        }

        /// <summary>Re-cache the rest scale after an external resize (e.g. sprite swap).</summary>
        public void CaptureBaseScale()
        {
            _baseLocalScale = transform.localScale;
        }

        private void Awake()
        {
            _baseLocalPos = transform.localPosition;
            _baseLocalScale = transform.localScale;
            // Default motion source: the moving root this sprite hangs under.
            if (_source == null && transform.parent != null) _source = transform.parent;
            if (_source != null) _lastSourcePos = _source.position;
        }

        private void LateUpdate()
        {
            if (_mode == Mode.Facility)
            {
                TickFacility();
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            TickActor(dt);
        }

        private void TickActor(float dt)
        {
            if (!_useExternalVelocity && _source != null)
            {
                Vector3 pos = _source.position;
                _velocity = (pos - _lastSourcePos) / dt;
                _lastSourcePos = pos;
            }

            float vx = _velocity.x;
            float planarSpeed = Mathf.Sqrt(_velocity.x * _velocity.x + _velocity.z * _velocity.z);
            bool moving = planarSpeed > MOVE_SPEED_THRESHOLD;

            // Hop: sin bounce on local Y while moving (billboard owns rotation, not position).
            float hop = 0f;
            if (moving)
            {
                _hopPhase += dt * hopFrequency;
                hop = Mathf.Abs(Mathf.Sin(_hopPhase)) * hopHeight;
            }
            else
            {
                _hopPhase = 0f;
            }
            Vector3 lp = _baseLocalPos;
            lp.y += hop;
            transform.localPosition = lp;

            // Lean into the horizontal move direction (screen X ~ world X with this camera).
            float targetLean = moving ? -Mathf.Clamp(vx / leanRefSpeed, -1f, 1f) * leanAngle : 0f;
            _lean = Mathf.Lerp(_lean, targetLean, dt * 10f);
            if (Mathf.Abs(_lean) > 0.01f)
                transform.rotation *= Quaternion.Euler(0f, 0f, _lean);

            // Flip by horizontal move direction; idle keeps the last facing.
            if (vx > FLIP_SPEED_THRESHOLD) _flipSign = 1f;
            else if (vx < -FLIP_SPEED_THRESHOLD) _flipSign = -1f;

            // Idle breathing: tiny squash & stretch pulse.
            Vector3 scale = _baseLocalScale;
            if (!moving)
            {
                float p = Mathf.Sin(Time.time * breatheFrequency) * breatheAmount;
                scale.y *= 1f + p;
                scale.x *= 1f - p * 0.5f;
            }
            scale.x *= _flipSign;
            transform.localScale = scale;
        }

        private void TickFacility()
        {
            bool active = _isActive != null && _isActive();
            if (!active)
            {
                transform.localScale = _baseLocalScale;
                return;
            }

            float t = Time.time;
            // Roll wobble multiplies after the billboard rotation written this frame.
            transform.rotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(t * wobbleFrequency) * wobbleAngle);
            float pulse = 1f + Mathf.Abs(Mathf.Sin(t * wobbleFrequency * 0.5f)) * wobblePulse;
            transform.localScale = _baseLocalScale * pulse;
        }
    }
}
