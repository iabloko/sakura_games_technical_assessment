using Core.Scripts.Game.ScriptableObjects.Configs.Animals;

namespace Core.Scripts.Game.Gameplay.Animals
{
    public sealed class AnimalInteractionResolver
    {
        // True means one animal was eaten. Prey/prey contacts remain entirely physical.
        public bool TryResolve(Animal first, Animal second)
        {
            if (first == null || second == null || first == second || !first.IsAlive || !second.IsAlive)
                return false;

            var firstIsPredator = first.Definition.FoodRole == FoodRole.Predator;
            var secondIsPredator = second.Definition.FoodRole == FoodRole.Predator;
            if (!firstIsPredator && !secondIsPredator) return false;

            Animal eater;
            Animal victim;
            if (firstIsPredator && secondIsPredator)
            {
                // A stable winner for both callback orders, including compound colliders.
                var firstWins = first.Id < second.Id;
                eater = firstWins ? first : second;
                victim = firstWins ? second : first;
            }
            else
            {
                eater = firstIsPredator ? first : second;
                victim = firstIsPredator ? second : first;
            }

            // TryDie changes life state before observers run; reversed/reentrant callbacks
            // therefore cannot produce another death or another eating notification.
            if (!victim.TryDie()) return false;
            eater.NotifyAte();
            return true;
        }
    }
}
