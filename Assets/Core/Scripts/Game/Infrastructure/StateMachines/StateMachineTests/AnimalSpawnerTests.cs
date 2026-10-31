using System;
using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalSpawnerTests
    {
        private GameObject _root;
        private AssetProvider _assets;
        private RecordingFactory _factory;
        private FixedArea _area;
        private AnimalDefinition[] _catalog;
        private AnimalSpawner _spawner;
        private UnityEngine.Random.State _random;

        [SetUp]
        public void SetUp()
        {
            _random = UnityEngine.Random.state;
            UnityEngine.Random.InitState(1234);
            _root = new GameObject("Spawner test owner");
            _assets = new AssetProvider(new DiContainer());
            _factory = new RecordingFactory(new AnimalFactory(_assets, new AnimalInteractionResolver(), new AnimalMovementFactory()));
            _area = new FixedArea();
            _catalog = new[] { Definition("Frog"), Definition("Snake") };
        }

        [TearDown]
        public void TearDown()
        {
            _spawner?.Dispose();
            if (_root != null) Object.DestroyImmediate(_root);
            _assets.Dispose();
            UnityEngine.Random.state = _random;
        }

        [Test]
        public void FirstSpawn_WaitsForItsIntervalAndPublishesInitializedAnimal()
        {
            _spawner = Create(1, 1);
            Animal spawned = null;
            _spawner.Spawned += animal => spawned = animal;
            _spawner.Start();
            _spawner.Tick(0.99f);
            Assert.That(_factory.Count, Is.Zero);

            _spawner.Tick(0.01f);

            Assert.That(_factory.Count, Is.EqualTo(1));
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.IsInitialized && spawned.IsAlive, Is.True);
            Assert.That(_factory.LastParent, Is.EqualTo(_root.transform));
            Assert.That(_factory.LastBounds, Is.SameAs(_area.MovementBounds));
            Assert.That(spawned.transform.position, Is.EqualTo(_area.Position));
        }

        [Test]
        public void DefaultIntervals_RemainBetweenOneAndTwoSeconds()
        {
            _spawner = Create();
            _spawner.Start();
            for (var iteration = 0; iteration < 30; iteration++)
            {
                _spawner.Tick(0.99f);
                Assert.That(_factory.Count, Is.EqualTo(iteration));
                _spawner.Tick(1.02f);
                Assert.That(_factory.Count, Is.EqualTo(iteration + 1));
            }
        }

        [Test]
        public void StartWhileRunning_DoesNotResetTimerOrCreateDuplicateStream()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            _spawner.Tick(0.75f);
            _spawner.Start();
            _spawner.Tick(0.25f);
            Assert.That(_factory.Count, Is.EqualTo(1));
        }

        [Test]
        public void StopAndRestart_DiscardThePreviousCountdown()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            _spawner.Tick(0.75f);
            _spawner.Stop();
            _spawner.Tick(100);
            Assert.That(_factory.Count, Is.Zero);
            _spawner.Start();
            _spawner.Tick(0.75f);
            Assert.That(_factory.Count, Is.Zero);
            _spawner.Tick(0.25f);
            Assert.That(_factory.Count, Is.EqualTo(1));
        }

        [Test]
        public void LargeFrame_DoesNotCreateCatchUpBurst()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            _spawner.Tick(100);
            _spawner.Tick(0);
            _spawner.Tick(0.5f);
            Assert.That(_factory.Count, Is.EqualTo(1));
            _spawner.Tick(0.5f);
            Assert.That(_factory.Count, Is.EqualTo(2));
        }

        [Test]
        public void ObserverCanStopSpawnerWithoutLeavingPendingSpawn()
        {
            _spawner = Create(1, 1);
            _spawner.Spawned += _ => _spawner.Stop();
            _spawner.Start();
            _spawner.Tick(1);
            _spawner.Tick(100);
            Assert.That(_factory.Count, Is.EqualTo(1));
            Assert.That(_spawner.IsRunning, Is.False);
        }

        [Test]
        public void UnavailableArea_SkipsAttemptAndWaitsForNextInterval()
        {
            _area.Available = false;
            _spawner = Create(1, 1);
            _spawner.Start();
            _spawner.Tick(1);
            Assert.That(_factory.Count, Is.Zero);
            _area.Available = true;
            _spawner.Tick(0.5f);
            Assert.That(_factory.Count, Is.Zero);
            _spawner.Tick(0.5f);
            Assert.That(_factory.Count, Is.EqualTo(1));
        }

        [Test]
        public void DestroyedOwner_StopsWithoutCreatingUnparentedAnimal()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            Object.DestroyImmediate(_root);
            _spawner.Tick(1);
            Assert.That(_factory.Count, Is.Zero);
            Assert.That(_spawner.IsRunning, Is.False);
        }

        [Test]
        public void FactoryFailure_StopsInsteadOfRetryingEveryFrame()
        {
            _factory.Fail = true;
            _spawner = Create(1, 1);
            _spawner.Start();
            Assert.Throws<InvalidOperationException>(() => _spawner.Tick(1));
            _spawner.Tick(100);
            Assert.That(_factory.Count, Is.EqualTo(1));
            Assert.That(_spawner.IsRunning, Is.False);
        }

        [Test]
        public void CatalogIsSnapshottedAndSpeciesSelectionUsesAllEntries()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            _catalog[0] = _catalog[1];
            var selected = new HashSet<string>();
            for (var iteration = 0; iteration < 30; iteration++)
            {
                _spawner.Tick(1);
                selected.Add(_factory.LastDefinition.Id);
            }
            Assert.That(selected, Is.EquivalentTo(new[] { "frog", "snake" }));
        }

        [Test]
        public void DisposedSpawner_CannotRestart()
        {
            _spawner = Create(1, 1);
            _spawner.Start();
            _spawner.Dispose();
            _spawner.Tick(100);
            Assert.That(_factory.Count, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => _spawner.Start());
        }

        [Test]
        public void InvalidCatalogAndIntervals_AreRejectedBeforeStarting()
        {
            Assert.Throws<ArgumentException>(() => new AnimalSpawner(_factory,
                Array.Empty<AnimalDefinition>(), _area, _root.transform));
            Assert.Throws<ArgumentException>(() => new AnimalSpawner(_factory,
                new AnimalDefinition[] { null }, _area, _root.transform));
            Assert.Throws<ArgumentException>(() => new AnimalSpawner(_factory,
                new[] { _catalog[0], _catalog[0] }, _area, _root.transform));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(2, 1));
        }

        private AnimalSpawner Create(float min = 1, float max = 2) =>
            new(_factory, _catalog, _area, _root.transform, min, max);

        private static AnimalDefinition Definition(string name) => AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
            $"Assets/Core/Settings/Configs/Animals/{name}.asset");

        private sealed class FixedArea : IAnimalSpawnArea, IAnimalMovementBounds
        {
            public bool Available = true;
            public Vector3 Position => new(2, 0, 3);
            public IAnimalMovementBounds MovementBounds => this;
            public bool TryGetPosition(out Vector3 position)
            {
                position = Position;
                return Available;
            }
            public bool TryGetReturnDirection(Vector3 position, out Vector3 direction)
            {
                direction = Vector3.zero;
                return false;
            }
        }

        private sealed class RecordingFactory : IAnimalFactory
        {
            private readonly IAnimalFactory _inner;
            public bool Fail;
            public int Count { get; private set; }
            public AnimalDefinition LastDefinition { get; private set; }
            public Transform LastParent { get; private set; }
            public IAnimalMovementBounds LastBounds { get; private set; }

            public RecordingFactory(IAnimalFactory inner) => _inner = inner;
            public Animal Create(AnimalDefinition definition, Vector3 position, Transform parent = null,
                IAnimalMovementBounds movementBounds = null)
            {
                Count++;
                if (Fail) throw new InvalidOperationException("Expected factory failure.");
                LastDefinition = definition;
                LastParent = parent;
                LastBounds = movementBounds;
                return _inner.Create(definition, position, parent, movementBounds);
            }
        }
    }
}
