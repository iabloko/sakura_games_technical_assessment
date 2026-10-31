using System;
using System.Collections.Generic;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;

namespace Core.Scripts.Game.Gameplay.Animals.Statistics
{
    public sealed class AnimalDeathStatistics : IDisposable
    {
        private readonly HashSet<Animal> _observed = new();
        private bool _disposed;

        public event Action Changed;
        public int DeadPrey { get; private set; }
        public int DeadPredators { get; private set; }
        public int ObservedCount => _observed.Count;

        public void Observe(Animal animal)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AnimalDeathStatistics));
            if (animal == null || !animal.IsAlive || !_observed.Add(animal)) return;
            animal.Died += OnDied;
            animal.Removed += OnRemoved;
        }

        private void OnDied(Animal animal)
        {
            if (!Unobserve(animal)) return;
            if (animal.Definition.FoodRole == FoodRole.Predator) DeadPredators++;
            else DeadPrey++;
            Changed?.Invoke();
        }

        private void OnRemoved(Animal animal) => Unobserve(animal);

        private bool Unobserve(Animal animal)
        {
            if (!_observed.Remove(animal)) return false;
            animal.Died -= OnDied;
            animal.Removed -= OnRemoved;
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var animal in _observed)
            {
                if (animal == null) continue;
                animal.Died -= OnDied;
                animal.Removed -= OnRemoved;
            }
            _observed.Clear();
            Changed = null;
            _disposed = true;
        }
    }
}
