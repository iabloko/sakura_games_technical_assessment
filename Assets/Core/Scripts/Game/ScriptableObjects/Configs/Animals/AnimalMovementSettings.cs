using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs.Animals
{
    // Shared authoring data. Timers, direction and other per-instance state belong to the animal.
    public abstract class AnimalMovementSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _directionChangeInterval = 2f;
        [SerializeField, Min(0.01f)] private float _acceleration = 10f;

        public float DirectionChangeInterval => _directionChangeInterval;
        public float Acceleration => _acceleration;

        protected virtual void OnValidate()
        {
            _directionChangeInterval = Mathf.Max(0.01f, _directionChangeInterval);
            _acceleration = Mathf.Max(0.01f, _acceleration);
        }
    }
}
