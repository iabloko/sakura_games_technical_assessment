using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    [DisallowMultipleComponent]
    public sealed class AnimalVisualOrientation : MonoBehaviour
    {
        private AnimalMovementController _movement;

        private void Awake() => _movement = GetComponentInParent<AnimalMovementController>();

        private void LateUpdate()
        {
            if (_movement == null || !_movement.isActiveAndEnabled) return;
            var direction = _movement.Direction;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
