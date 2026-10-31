using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals
{
    public interface IAnimalFactory
    {
        Animal Create(AnimalDefinition definition, Vector3 position, Transform parent = null,
            IAnimalMovementBounds movementBounds = null);
    }
}
