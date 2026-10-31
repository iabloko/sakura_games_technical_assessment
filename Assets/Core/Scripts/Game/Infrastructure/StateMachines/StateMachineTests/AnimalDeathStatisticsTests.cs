using System;
using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Gameplay.Animals.Statistics;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalDeathStatisticsTests
    {
        private AssetProvider _assets;
        private AnimalFactory _factory;
        private AnimalDeathStatistics _statistics;
        private readonly List<GameObject> _owned = new();

        [SetUp]
        public void SetUp()
        {
            _assets = new AssetProvider(new DiContainer());
            _factory = new AnimalFactory(_assets, new AnimalInteractionResolver(), new AnimalMovementFactory());
            _statistics = new AnimalDeathStatistics();
        }

        [TearDown]
        public void TearDown()
        {
            _statistics.Dispose();
            foreach (var item in _owned)
                if (item != null) Object.DestroyImmediate(item);
            _owned.Clear();
            _assets.Dispose();
        }

        [Test]
        public void RepeatedObservationAndDeath_IncrementOnlyOnce()
        {
            var prey = Create("Frog");
            _statistics.Observe(prey);
            _statistics.Observe(prey);
            var changes = 0;
            _statistics.Changed += () => changes++;
            Assert.That(_statistics.ObservedCount, Is.EqualTo(1));
            prey.TryDie();
            prey.TryDie();
            Assert.That(_statistics.DeadPrey, Is.EqualTo(1));
            Assert.That(_statistics.DeadPredators, Is.Zero);
            Assert.That(_statistics.ObservedCount, Is.Zero);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void CountersUseVictimRoleForPredationAndPredatorDuels()
        {
            var older = Create("Snake");
            var newer = Create("Snake");
            var prey = Create("Frog");
            _statistics.Observe(older);
            _statistics.Observe(newer);
            _statistics.Observe(prey);
            var resolver = new AnimalInteractionResolver();
            resolver.TryResolve(older, prey);
            resolver.TryResolve(newer, older);
            Assert.That(_statistics.DeadPrey, Is.EqualTo(1));
            Assert.That(_statistics.DeadPredators, Is.EqualTo(1));
            Assert.That(_statistics.ObservedCount, Is.EqualTo(1));
        }

        [Test]
        public void DeadOrNullAnimal_IsNotObserved()
        {
            var prey = Create("Frog");
            prey.TryDie();
            _statistics.Observe(prey);
            _statistics.Observe(null);
            Assert.That(_statistics.ObservedCount, Is.Zero);
            Assert.That(_statistics.DeadPrey, Is.Zero);
        }

        [Test]
        public void DisposalUnsubscribesBeforeFurtherDeaths()
        {
            var prey = Create("Frog");
            _statistics.Observe(prey);
            _statistics.Dispose();
            prey.TryDie();
            Assert.That(_statistics.DeadPrey, Is.Zero);
            Assert.That(_statistics.ObservedCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => _statistics.Observe(null));
        }

        private Animal Create(string name)
        {
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                $"Assets/Core/Settings/Configs/Animals/{name}.asset");
            var animal = _factory.Create(definition, Vector3.zero);
            _owned.Add(animal.gameObject);
            return animal;
        }
    }
}
