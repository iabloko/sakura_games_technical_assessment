using Core.Scripts.Game.Gameplay.Animals.Movement;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Spawning
{
    public interface IAnimalSpawnArea
    {
        IAnimalMovementBounds MovementBounds { get; }
        bool TryGetPosition(out Vector3 position);
    }
}
