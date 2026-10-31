using System;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animal))]
    public sealed class AnimalMovementController : MonoBehaviour
    {
        private Animal _animal;
        private AnimalMovementBehaviour _movement;
        private IAnimalMovementBounds _bounds;
        private Vector3 _direction;
        private float _timeToTurn;
        private bool _resetOnEnable;

        public Vector3 Direction => _direction;

        internal void Initialize(Animal animal, AnimalMovementBehaviour movement, IAnimalMovementBounds bounds)
        {
            _animal = animal != null ? animal : throw new ArgumentNullException(nameof(animal));
            _movement = movement ?? throw new ArgumentNullException(nameof(movement));
            _bounds = bounds;
            _resetOnEnable = false;
            ChooseDirection();
        }

        private void FixedUpdate()
        {
            if (_animal == null || !_animal.IsAlive || _movement == null) return;

            if (_bounds != null && _bounds.TryGetReturnDirection(_animal.Body.position, out var inward))
            {
                _direction = inward;
                _timeToTurn = _animal.Definition.Movement.DirectionChangeInterval;
            }
            else
            {
                _timeToTurn -= Time.fixedDeltaTime;
                if (_timeToTurn <= 0f) ChooseDirection();
            }
            _movement.FixedTick(_direction, Time.fixedDeltaTime);
            // Rotate the physics owner, so its collider and visual share one heading.
            // Actual velocity preserves the launch heading during a jump and physical recoil.
            var heading = AnimalMotion.PlanarVelocity(_animal.Body);
            if (heading.sqrMagnitude < 0.000001f) heading = _direction;
            _animal.Body.MoveRotation(Quaternion.LookRotation(heading, Vector3.up));
        }

        private void LateUpdate()
        {
            if (_animal != null && _animal.IsAlive) _movement?.UpdateVisual();
        }

        internal void HandleCollision(Vector3 normal)
        {
            if (!isActiveAndEnabled || _animal == null || !_animal.IsAlive || _movement == null) return;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.000001f) return;
            normal.Normalize();
            if (Vector3.Dot(_direction, normal) < 0f)
                _direction = Vector3.Reflect(_direction, normal).normalized;
            _timeToTurn = _animal.Definition.Movement.DirectionChangeInterval;
            _movement.OnCollision();
        }

        private void ChooseDirection()
        {
            var heading = UnityEngine.Random.insideUnitCircle;
            _direction = heading.sqrMagnitude > 0.000001f
                ? new Vector3(heading.x, 0, heading.y).normalized
                : Vector3.forward;
            _timeToTurn = _animal.Definition.Movement.DirectionChangeInterval;
        }

        private void OnDisable()
        {
            _movement?.Stop();
            _resetOnEnable = true;
        }

        private void OnEnable()
        {
            if (!_resetOnEnable || _animal == null || _movement == null) return;
            ChooseDirection();
            _resetOnEnable = false;
        }
    }
}
