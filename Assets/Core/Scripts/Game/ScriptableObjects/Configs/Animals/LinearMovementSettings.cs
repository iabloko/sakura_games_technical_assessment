using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs.Animals
{
    [CreateAssetMenu(menuName = "Settings/Animals/Movement/Linear", fileName = "LinearMovement")]
    public sealed class LinearMovementSettings : AnimalMovementSettings
    {
        [SerializeField, Min(0.01f)] private float _speed = 2f;

        public float Speed => _speed;

        protected override void OnValidate()
        {
            base.OnValidate();
            _speed = Mathf.Max(0.01f, _speed);
        }
    }
}
