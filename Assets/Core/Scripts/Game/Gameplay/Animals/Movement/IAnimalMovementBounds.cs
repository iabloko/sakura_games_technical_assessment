using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public interface IAnimalMovementBounds
    {
        bool TryGetReturnDirection(Vector3 position, out Vector3 direction);
    }
}
