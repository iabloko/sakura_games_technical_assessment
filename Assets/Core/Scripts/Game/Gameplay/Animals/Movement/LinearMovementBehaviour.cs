using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public sealed class LinearMovementBehaviour : AnimalMovementBehaviour
    {
        private readonly Rigidbody _body;
        private readonly float _speed;
        private readonly float _acceleration;

        public LinearMovementBehaviour(Rigidbody body, float speed, float acceleration)
        {
            _body = body;
            _speed = speed;
            _acceleration = acceleration;
        }

        public override void FixedTick(Vector3 direction, float deltaTime) =>
            AnimalMotion.AccelerateTowards(_body, direction * _speed, _acceleration, deltaTime);
    }
}
