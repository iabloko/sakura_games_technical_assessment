using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalInteractionResolverTests
    {
        private AssetProvider _assets;
        private AnimalFactory _factory;
        private AnimalInteractionResolver _resolver;
        private readonly List<GameObject> _instances = new();

        [SetUp]
        public void SetUp()
        {
            _assets = new AssetProvider(new DiContainer());
            _resolver = new AnimalInteractionResolver();
            _factory = new AnimalFactory(_assets, _resolver);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in _instances)
                if (instance != null) Object.DestroyImmediate(instance);
            _instances.Clear();
            _assets.Dispose();
        }

        [Test]
        public void PreyPair_DoesNotKillOrEat()
        {
            var first = Create("Frog");
            var second = Create("Frog");
            var events = new ContactEvents();
            events.Observe(first);
            events.Observe(second);

            Assert.That(_resolver.TryResolve(first, second), Is.False);
            Assert.That(_resolver.TryResolve(second, first), Is.False);
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(events.Deaths, Is.Zero);
            Assert.That(events.Meals, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PredatorPreyPair_EatsOnceRegardlessOfCallbackOrder(bool reversed)
        {
            var prey = Create("Frog");
            var predator = Create("Snake");
            var events = new ContactEvents();
            events.Observe(prey);
            events.Observe(predator);

            Assert.That(reversed ? _resolver.TryResolve(predator, prey) : _resolver.TryResolve(prey, predator), Is.True);
            Assert.That(_resolver.TryResolve(prey, predator), Is.False);
            Assert.That(_resolver.TryResolve(predator, prey), Is.False);
            Assert.That(prey.IsAlive, Is.False);
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(events.Deaths, Is.EqualTo(1));
            Assert.That(events.Meals, Is.EqualTo(1));
            Assert.That(events.LastEater, Is.SameAs(predator));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PredatorPair_SmallerIdSurvivesBothCallbackOrders(bool reversed)
        {
            var older = Create("Snake");
            var newer = Create("Snake");
            var events = new ContactEvents();
            events.Observe(older);
            events.Observe(newer);

            Assert.That(reversed ? _resolver.TryResolve(newer, older) : _resolver.TryResolve(older, newer), Is.True);
            Assert.That(_resolver.TryResolve(newer, older), Is.False);
            Assert.That(older.IsAlive, Is.True);
            Assert.That(newer.IsAlive, Is.False);
            Assert.That(events.Deaths, Is.EqualTo(1));
            Assert.That(events.Meals, Is.EqualTo(1));
            Assert.That(events.LastEater, Is.SameAs(older));
        }

        [Test]
        public void ReentrantDeathAndEatingObservers_CannotResolveTheSameContactTwice()
        {
            var prey = Create("Frog");
            var predator = Create("Snake");
            var repeatedDeath = true;
            var repeatedMeal = true;
            var meals = 0;
            prey.Died += _ => repeatedDeath = _resolver.TryResolve(predator, prey);
            predator.Ate += _ =>
            {
                meals++;
                repeatedMeal = _resolver.TryResolve(prey, predator);
            };

            Assert.That(_resolver.TryResolve(prey, predator), Is.True);
            Assert.That(repeatedDeath, Is.False);
            Assert.That(repeatedMeal, Is.False);
            Assert.That(meals, Is.EqualTo(1));
        }

        [Test]
        public void DeadPredator_CannotEatAnotherAnimal()
        {
            var predator = Create("Snake");
            var prey = Create("Frog");
            predator.TryDie();

            Assert.That(_resolver.TryResolve(predator, prey), Is.False);
            Assert.That(_resolver.TryResolve(prey, predator), Is.False);
            Assert.That(prey.IsAlive, Is.True);
        }

        [Test]
        public void NullOrSameAnimal_IsIgnored()
        {
            var predator = Create("Snake");

            Assert.That(_resolver.TryResolve(null, predator), Is.False);
            Assert.That(_resolver.TryResolve(predator, null), Is.False);
            Assert.That(_resolver.TryResolve(predator, predator), Is.False);
            Assert.That(predator.IsAlive, Is.True);
        }

        [Test]
        public void Predator_CanEatDistinctVictimsOnceEach()
        {
            var predator = Create("Snake");
            var first = Create("Frog");
            var second = Create("Frog");
            var events = new ContactEvents();
            events.Observe(predator);
            events.Observe(first);
            events.Observe(second);

            Assert.That(_resolver.TryResolve(predator, first), Is.True);
            Assert.That(_resolver.TryResolve(second, predator), Is.True);
            Assert.That(events.Deaths, Is.EqualTo(2));
            Assert.That(events.Meals, Is.EqualTo(2));
            Assert.That(predator.IsAlive, Is.True);
        }

        private Animal Create(string species)
        {
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                $"Assets/Core/Settings/Configs/Animals/{species}.asset");
            var animal = _factory.Create(definition, Vector3.zero);
            _instances.Add(animal.gameObject);
            return animal;
        }

        private sealed class ContactEvents
        {
            public int Deaths { get; private set; }
            public int Meals { get; private set; }
            public Animal LastEater { get; private set; }

            public void Observe(Animal animal)
            {
                animal.Died += OnDeath;
                animal.Ate += OnMeal;
            }

            private void OnDeath(Animal animal) => Deaths++;
            private void OnMeal(Animal eater)
            {
                Meals++;
                LastEater = eater;
            }
        }
    }
}
