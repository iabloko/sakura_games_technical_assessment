using System;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public sealed class AnimalMovementFactory
    {
        public AnimalMovementBehaviour Create(Animal animal)
        {
            switch (animal.Definition.Movement)
            {
                case LinearMovementSettings linear:
                    return new LinearMovementBehaviour(animal.Body, linear.Speed, linear.Acceleration);
                case JumpMovementSettings jump:
                    if (animal.Visual == null || animal.Visual == animal.transform)
                        throw new InvalidOperationException("Jump movement requires a separate visual child.");
                    return new JumpMovementBehaviour(animal.Body, animal.Visual, jump);
                default:
                    throw new NotSupportedException(
                        $"Unsupported movement settings: {animal.Definition.Movement.GetType().Name}.");
            }
        }
    }
}
