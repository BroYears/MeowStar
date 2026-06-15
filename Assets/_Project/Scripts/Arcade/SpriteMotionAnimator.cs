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
    /// Updated with a Spring-Mass-Damper model (Cats&Soup style) for organic, jelly-like
    /// elastic movement, squash & stretch on impact, and secondary inertial sways.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SpriteMotionAnimator : MonoBehaviour
    {
        private enum Mode { Actor, Facility }

        private const float MOVE_SPEED_THRESHOLD = 0.05f;
        private const float FLIP_SPEED_THRESHOLD = 0.1f;

        [Header("Base Motion")]
        [SerializeField] private float hopHeight = 0.09f;
        [SerializeField] private float hopFrequency = 9f;
        [SerializeField] private float leanAngle = 7f;
        [SerializeField] private float leanRefSpeed = 4f;
        [SerializeField] private float breatheAmount = 0.02f;
        [SerializeField] private float breatheFrequency = 2.2f;
        [SerializeField] private float wobbleAngle = 4f;
        [SerializeField] private float wobbleFrequency = 14f;
        [SerializeField] private float wobblePulse = 0.04f;

        [Header("Spring Physics (Cats&Soup Style)")]
        [SerializeField] private float scaleStiffness = 180f;
        [SerializeField] private float scaleDamping = 12f;
        [SerializeField] private float rotStiffness = 150f;
        [SerializeField] private float rotDamping = 10f;
        [SerializeField] private float walkImpactForce = 0.8f;
        [SerializeField] private float stopImpactForce = 1.8f;

        private Mode _mode = Mode.Actor;
        private Transform _source;
        private Func<bool> _isActive;
        private Vector3 _lastSourcePos;
        private Vector3 _velocity;
        private bool _useExternalVelocity;

        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale = Vector3.one;
        private float _hopPhase;
        private float _flipSign = 1f;

        // Spring State Variables
        private Vector3 _scaleSpringValue = Vector3.zero;     // Displacement from base scale
        private Vector3 _scaleSpringVelocity = Vector3.zero;
        private float _rotSpringValue = 0f;                   // Displacement from base rotation lean
        private float _rotSpringVelocity = 0f;

        private bool _wasMoving;
        private float _lastHopSin;

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

            // 1. Hop: standard vertical bounce on Y while moving.
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

            // Flip by horizontal move direction; idle keeps the last facing.
            if (vx > FLIP_SPEED_THRESHOLD) _flipSign = 1f;
            else if (vx < -FLIP_SPEED_THRESHOLD) _flipSign = -1f;

            // 2. Landing & Stop Impacts (Triggers force impulses on the springs)
            float currentSin = moving ? Mathf.Abs(Mathf.Sin(_hopPhase)) : 0f;
            if (moving)
            {
                // When landing on the ground during walk cycle
                if (_lastHopSin > 0.05f && currentSin <= 0.05f)
                {
                    _scaleSpringVelocity.y = -walkImpactForce;
                    _scaleSpringVelocity.x = walkImpactForce * 0.4f;
                }
                _lastHopSin = currentSin;
            }
            else
            {
                _lastHopSin = 0f;
            }

            // Sudden stop impact
            if (_wasMoving && !moving)
            {
                // Squash flat on impact
                _scaleSpringVelocity.y = -stopImpactForce;
                _scaleSpringVelocity.x = stopImpactForce * 0.4f;
                
                // Rotational whip in the direction of velocity (inertia)
                _rotSpringVelocity = (vx > 0f ? -1f : 1f) * stopImpactForce * 18f;
            }
            _wasMoving = moving;

            // 3. Spring Target Calculations
            // Target lean (tilt) based on velocity
            float targetLean = moving ? -Mathf.Clamp(vx / leanRefSpeed, -1f, 1f) * leanAngle : 0f;

            // Target scale (breathe when idle, stretch when moving)
            float targetScaleX = 0f;
            float targetScaleY = 0f;
            if (moving)
            {
                // Stretch along Y, squash along X proportional to speed
                float stretch = (planarSpeed / leanRefSpeed) * 0.07f;
                targetScaleY = stretch;
                targetScaleX = -stretch * 0.5f;
            }
            else
            {
                // Idle breathing squash & stretch
                float p = Mathf.Sin(Time.time * breatheFrequency) * breatheAmount;
                targetScaleY = p;
                targetScaleX = -p * 0.5f;
            }

            // 4. Update Springs (Euler integration steps)
            UpdateSpring(ref _scaleSpringValue.x, ref _scaleSpringVelocity.x, targetScaleX, scaleStiffness, scaleDamping, dt);
            UpdateSpring(ref _scaleSpringValue.y, ref _scaleSpringVelocity.y, targetScaleY, scaleStiffness, scaleDamping, dt);
            UpdateSpring(ref _rotSpringValue, ref _rotSpringVelocity, targetLean, rotStiffness, rotDamping, dt);

            // 5. Apply Spring Outputs to Transform
            if (Mathf.Abs(_rotSpringValue) > 0.01f)
            {
                transform.rotation *= Quaternion.Euler(0f, 0f, _rotSpringValue);
            }

            Vector3 finalScale = _baseLocalScale;
            finalScale.x = _baseLocalScale.x * (1f + _scaleSpringValue.x) * _flipSign;
            finalScale.y = _baseLocalScale.y * (1f + _scaleSpringValue.y);
            transform.localScale = finalScale;
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

        // 1D Spring-Mass-Damper Solver
        private void UpdateSpring(ref float value, ref float velocity, float target, float stiffness, float damping, float dt)
        {
            float force = -stiffness * (value - target) - damping * velocity;
            velocity += force * dt;
            value += velocity * dt;
        }
    }
}
