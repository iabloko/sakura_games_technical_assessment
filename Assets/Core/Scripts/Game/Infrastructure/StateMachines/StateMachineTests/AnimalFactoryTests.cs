using System;
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
    public sealed class AnimalFactoryTests
    {
        private AssetProvider _assets;
        private AnimalFactory _factory;
        private readonly List<Object> _owned = new();

        [SetUp]
        public void SetUp()
        {
            _assets = new AssetProvider(new DiContainer());
            _factory = new AnimalFactory(_assets, new AnimalInteractionResolver());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _owned)
                if (item != null) Object.DestroyImmediate(item);
            _owned.Clear();
            _assets.Dispose();
        }

        [TestCase("Frog", FoodRole.Prey)]
        [TestCase("Snake", FoodRole.Predator)]
        public void Create_ReturnsConfiguredLivingAnimal(string species, FoodRole role)
        {
            var definition = LoadDefinition(species);
            var parent = Own(new GameObject("Animals"));
            parent.transform.position = new Vector3(10, 0, 20);
            var position = new Vector3(2, 0, 3);

            var animal = Create(definition, position, parent.transform);

            Assert.That(animal.Definition, Is.SameAs(definition));
            Assert.That(animal.Definition.FoodRole, Is.EqualTo(role));
            Assert.That(animal.Id, Is.GreaterThan(0));
            Assert.That(animal.IsInitialized, Is.True);
            Assert.That(animal.IsAlive, Is.True);
            Assert.That(animal.gameObject.activeSelf, Is.True);
            Assert.That(animal.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(Vector3.Distance(animal.transform.position, position), Is.LessThan(0.001f));
            Assert.That(animal.Body.isKinematic, Is.False);
            Assert.That(animal.Body.detectCollisions, Is.True);
            Assert.That(animal.Visual, Is.Not.Null);
        }

        [Test]
        public void Create_AssignsDistinctIdsAndIndependentLifeState()
        {
            var first = Create(LoadDefinition("Frog"));
            var second = Create(LoadDefinition("Frog"));
            var predator = Create(LoadDefinition("Snake"));

            Assert.That(new HashSet<long> { first.Id, second.Id, predator.Id }.Count, Is.EqualTo(3));
            first.TryDie();

            Assert.That(second.IsAlive, Is.True);
            Assert.That(predator.IsAlive, Is.True);
        }

        [Test]
        public void TryDie_NotifiesOnceAndRejectsReentrantDeath()
        {
            var animal = Create(LoadDefinition("Frog"));
            var deaths = 0;
            var reentrantDeath = true;
            animal.Died += dying =>
            {
                deaths++;
                Assert.That(dying.IsAlive, Is.False);
                Assert.That(dying.Body.detectCollisions, Is.False);
                Assert.That(dying.Body.isKinematic, Is.True);
                reentrantDeath = dying.TryDie();
            };

            Assert.That(animal.TryDie(), Is.True);
            Assert.That(animal.TryDie(), Is.False);
            Assert.That(reentrantDeath, Is.False);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(animal == null, Is.True);
        }

        [Test]
        public void TryDie_CleansUpEvenWhenObserverThrows()
        {
            var animal = Create(LoadDefinition("Snake"));
            animal.Died += _ => throw new InvalidOperationException("Observer failed");

            Assert.Throws<InvalidOperationException>(() => animal.TryDie());

            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal == null, Is.True);
        }

        [Test]
        public void ExternalDestruction_DoesNotPublishGameplayDeath()
        {
            var animal = Create(LoadDefinition("Frog"));
            var deaths = 0;
            animal.Died += _ => deaths++;

            Object.DestroyImmediate(animal.gameObject);

            Assert.That(animal.IsAlive, Is.False);
            Assert.That(deaths, Is.Zero);
        }

        [Test]
        public void DestroyingNeverActiveAnimal_DoesNotLeaveItAliveOrPublishDeath()
        {
            var parent = Own(new GameObject("Inactive animal owner"));
            parent.SetActive(false);
            var animal = Create(LoadDefinition("Frog"), Vector3.zero, parent.transform);
            var deaths = 0;
            animal.Died += _ => deaths++;
            Assert.That(animal.IsAlive, Is.True);
            Assert.That(animal.gameObject.activeInHierarchy, Is.False);

            Object.DestroyImmediate(animal.gameObject);

            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal.TryDie(), Is.False);
            Assert.That(deaths, Is.Zero);
        }

        [Test]
        public void Create_InitializesBeforeOnEnableEvenForActivePrefab()
        {
            var source = Own(Object.Instantiate(LoadDefinition("Frog").Prefab));
            source.SetActive(false);
            source.AddComponent<AnimalActivationProbe>();
            source.SetActive(true);
            var definition = Own(Object.Instantiate(LoadDefinition("Frog")));
            AssignPrefab(definition, source);

            var animal = Create(definition);

            Assert.That(animal.GetComponent<AnimalActivationProbe>().InitializedOnEnable, Is.True);
        }

        [Test]
        public void Create_RejectsMissingPrefabWithoutCreatingAnimal()
        {
            var definition = Own(Object.Instantiate(LoadDefinition("Frog")));
            AssignPrefab(definition, null);
            var countBefore = Object.FindObjectsByType<Animal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            Assert.Throws<InvalidOperationException>(() => _factory.Create(definition, Vector3.zero));

            Assert.That(Object.FindObjectsByType<Animal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(countBefore));
        }

        [Test]
        public void Create_RejectsPrefabWithoutAnimalComponent()
        {
            var invalidPrefab = Own(new GameObject("Invalid animal prefab"));
            var definition = Own(Object.Instantiate(LoadDefinition("Snake")));
            AssignPrefab(definition, invalidPrefab);

            Assert.Throws<InvalidOperationException>(() => _factory.Create(definition, Vector3.zero));
        }

        [Test]
        public void Create_RejectsKinematicBody()
        {
            var source = Own(Object.Instantiate(LoadDefinition("Frog").Prefab));
            source.GetComponent<Rigidbody>().isKinematic = true;
            var definition = Own(Object.Instantiate(LoadDefinition("Frog")));
            AssignPrefab(definition, source);

            Assert.Throws<InvalidOperationException>(() => _factory.Create(definition, Vector3.zero));
        }

        [Test]
        public void Create_RejectsNullDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => _factory.Create(null, Vector3.zero));
        }

        private Animal Create(AnimalDefinition definition, Vector3 position = default, Transform parent = null)
        {
            var animal = _factory.Create(definition, position, parent);
            Own(animal.gameObject);
            return animal;
        }

        private T Own<T>(T item) where T : Object
        {
            _owned.Add(item);
            return item;
        }

        private static AnimalDefinition LoadDefinition(string species)
        {
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                $"Assets/Core/Settings/Configs/Animals/{species}.asset");
            Assert.That(definition, Is.Not.Null);
            return definition;
        }

        private static void AssignPrefab(AnimalDefinition definition, GameObject prefab)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
