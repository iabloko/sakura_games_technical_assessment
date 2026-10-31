using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public sealed class JumpMovementBehaviour : AnimalMovementBehaviour
    {
        private readonly Rigidbody _body;
        private readonly Transform _visual;
        private readonly Vector3 _restPosition;
        private readonly JumpMovementSettings _settings;
        private double _time;
        private double _nextJumpTime;
        private float _elapsed;
        private bool _jumping;
        private bool _collided;
        private Vector3 _launchDirection;

        public JumpMovementBehaviour(Rigidbody body, Transform visual, JumpMovementSettings settings)
        {
            _body = body;
            _visual = visual;
            _restPosition = visual.localPosition;
            _settings = settings;
        }

        public override void FixedTick(Vector3 direction, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            var landed = false;
            var launched = false;

            if (_jumping && _elapsed >= _settings.Duration - 0.000001f)
            {
                _jumping = false;
                landed = true;
            }

            if (!_jumping && _time + 0.000001 >= _nextJumpTime)
            {
                _jumping = true;
                _collided = false;
                _elapsed = 0f;
                _launchDirection = direction;
                _nextJumpTime = _time + _settings.Interval;
                launched = true;
            }

            if (_jumping)
            {
                var remaining = _settings.Duration - _elapsed;
                // Correct only the last fractional step of an unobstructed jump.
                if (!_collided && (launched || remaining < deltaTime - 0.000001f))
                    AnimalMotion.SetPlanarVelocity(_body,
                        _launchDirection * (_settings.Distance / _settings.Duration) * Mathf.Min(1f, remaining / deltaTime));
                _elapsed = Mathf.Min(_elapsed + deltaTime, _settings.Duration);
            }
            else if (landed && !_collided)
            {
                // A collision owns the resulting recoil; do not erase it on landing.
                AnimalMotion.SetPlanarVelocity(_body, Vector3.zero);
            }
            else
            {
                AnimalMotion.AccelerateTowards(_body, Vector3.zero, _settings.Acceleration, deltaTime);
            }
            _time += deltaTime;
        }

        public override void OnCollision()
        {
            if (_jumping) _collided = true;
        }

        public override void UpdateVisual()
        {
            if (_visual == null) return;
            var height = _jumping
                ? Mathf.Max(0f, Mathf.Sin(Mathf.PI * (_elapsed / _settings.Duration))) * _settings.Height
                : 0f;
            _visual.localPosition = _restPosition + Vector3.up * height;
        }

        public override void Stop()
        {
            _time = 0;
            _nextJumpTime = 0;
            _elapsed = 0;
            _jumping = false;
            _collided = false;
            if (_visual != null) _visual.localPosition = _restPosition;
        }
    }
}
