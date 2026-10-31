using System;
using System.Collections.Generic;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Spawning
{
    public sealed class AnimalSpawner : IDisposable
    {
        private readonly IAnimalFactory _factory;
        private readonly AnimalDefinition[] _catalog;
        private readonly IAnimalSpawnArea _area;
        private readonly Transform _parent;
        private readonly float _minInterval;
        private readonly float _maxInterval;
        private float _remaining;
        private bool _disposed;

        public event Action<Animal> Spawned;
        public bool IsRunning { get; private set; }

        public AnimalSpawner(IAnimalFactory factory, IReadOnlyList<AnimalDefinition> catalog,
            IAnimalSpawnArea area, Transform parent, float minInterval = 1f, float maxInterval = 2f)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _area = area ?? throw new ArgumentNullException(nameof(area));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (float.IsNaN(minInterval) || float.IsInfinity(minInterval) || minInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minInterval));
            if (float.IsNaN(maxInterval) || float.IsInfinity(maxInterval) || maxInterval < minInterval)
                throw new ArgumentOutOfRangeException(nameof(maxInterval));
            if (catalog == null || catalog.Count == 0)
                throw new ArgumentException("The animal catalog cannot be empty.", nameof(catalog));

            _catalog = new AnimalDefinition[catalog.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < catalog.Count; index++)
            {
                var definition = catalog[index];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) ||
                    definition.Prefab == null || definition.Movement == null)
                    throw new ArgumentException($"Animal catalog entry {index} is incomplete.", nameof(catalog));
                if (!ids.Add(definition.Id))
                    throw new ArgumentException($"Duplicate animal ID '{definition.Id}'.", nameof(catalog));
                _catalog[index] = definition;
            }

            _parent = parent;
            _minInterval = minInterval;
            _maxInterval = maxInterval;
        }

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AnimalSpawner));
            if (_parent == null) throw new InvalidOperationException("The animal owner has been destroyed.");
            if (IsRunning) return;
            _remaining = NextInterval();
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            _remaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (_parent == null || !_parent.gameObject.activeInHierarchy)
            {
                Stop();
                return;
            }
            if (deltaTime == 0f) return;
            _remaining -= deltaTime;
            if (_remaining > 0f) return;

            // Arm the next interval first, so reentrant Stop/Start from an observer wins.
            // A long frame creates at most one animal instead of a catch-up burst.
            _remaining = NextInterval();
            try
            {
                if (!_area.TryGetPosition(out var position)) return;
                var definition = _catalog[UnityEngine.Random.Range(0, _catalog.Length)];
                var animal = _factory.Create(definition, position, _parent, _area.MovementBounds);
                if (animal == null || !animal.IsAlive)
                    throw new InvalidOperationException("The animal factory did not return a living animal.");
                Spawned?.Invoke(animal);
            }
            catch
            {
                Stop();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            Spawned = null;
            _disposed = true;
        }

        private float NextInterval() => UnityEngine.Random.Range(_minInterval, _maxInterval);
    }
}
