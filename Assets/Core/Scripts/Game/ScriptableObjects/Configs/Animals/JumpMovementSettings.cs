using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs.Animals
{
    [CreateAssetMenu(menuName = "Settings/Animals/Movement/Jump", fileName = "JumpMovement")]
    public sealed class JumpMovementSettings : AnimalMovementSettings
    {
        [SerializeField, Min(0.01f)] private float _distance = 2f;
        [Tooltip("Time between the start of consecutive jumps, in seconds.")]
        [SerializeField, Min(0.01f)] private float _interval = 1f;
        [SerializeField, Min(0.01f)] private float _duration = 0.3f;
        [Tooltip("Visual jump height. The physics body will move on the ground plane.")]
        [SerializeField, Min(0f)] private float _height = 0.5f;

        public float Distance => _distance;
        public float Interval => _interval;
        public float Duration => _duration;
        public float Height => _height;

        protected override void OnValidate()
        {
            base.OnValidate();
            _distance = Mathf.Max(0.01f, _distance);
            _interval = Mathf.Max(0.01f, _interval);
            _duration = Mathf.Clamp(_duration, 0.01f, _interval);
            _height = Mathf.Max(0f, _height);
        }
    }
}
