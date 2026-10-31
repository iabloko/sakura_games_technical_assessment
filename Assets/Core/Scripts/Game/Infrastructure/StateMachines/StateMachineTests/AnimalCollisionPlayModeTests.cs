using System;
using System.Collections;
using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalCollisionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PreyPair_PhysicallyBouncesWithoutDeaths()
        {
            yield return new EnterPlayMode();
            using var world = new CollisionWorld();
            var first = world.Create("Frog", new Vector3(0, 0, -1.5f));
            var second = world.Create("Frog", new Vector3(0, 0, 1.5f));
            first.Body.linearVelocity = Vector3.forward * 3;
            second.Body.linearVelocity = Vector3.back * 3;

            world.Simulate();

            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(first.Body.linearVelocity.z, Is.LessThan(0));
            Assert.That(second.Body.linearVelocity.z, Is.GreaterThan(0));
            Assert.That(world.Events.Deaths, Is.Zero);
            Assert.That(world.Events.Meals, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PredatorPreyContact_ProducesOneDeathAndOneMeal()
        {
            yield return new EnterPlayMode();
            using var world = new CollisionWorld();
            var prey = world.Create("Frog", new Vector3(0, 0, -1.5f));
            var predator = world.Create("Snake", new Vector3(0, 0, 1.5f));
            prey.Body.linearVelocity = Vector3.forward * 3;
            predator.Body.linearVelocity = Vector3.back * 3;

            world.Simulate();

            Assert.That(prey.IsAlive, Is.False);
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(world.Events.Deaths, Is.EqualTo(1));
            Assert.That(world.Events.Meals, Is.EqualTo(1));
            Assert.That(world.Events.LastEater, Is.SameAs(predator));
            yield return null;
            Assert.That(prey == null, Is.True);
        }

        [UnityTest]
        public IEnumerator PredatorPair_LeavesOnlyTheOlderPredatorAlive()
        {
            yield return new EnterPlayMode();
            using var world = new CollisionWorld();
            var older = world.Create("Snake", new Vector3(0, 0, -1.5f));
            var newer = world.Create("Snake", new Vector3(0, 0, 1.5f));
            older.Body.linearVelocity = Vector3.forward * 3;
            newer.Body.linearVelocity = Vector3.back * 3;

            world.Simulate();

            Assert.That(older.IsAlive, Is.True);
            Assert.That(newer.IsAlive, Is.False);
            Assert.That(world.Events.Deaths, Is.EqualTo(1));
            Assert.That(world.Events.Meals, Is.EqualTo(1));
            Assert.That(world.Events.LastEater, Is.SameAs(older));
        }

        [UnityTest]
        public IEnumerator ChildCollider_ResolvesItsOwningAnimal()
        {
            yield return new EnterPlayMode();
            using var world = new CollisionWorld();
            var sourceDefinition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                "Assets/Core/Settings/Configs/Animals/Frog.asset");
            var source = world.Own(Object.Instantiate(sourceDefinition.Prefab));
            source.SetActive(false);
            var originalCollider = source.GetComponent<Collider>();
            originalCollider.enabled = false;
            var child = new GameObject("Child collider");
            child.transform.SetParent(source.transform, false);
            child.AddComponent<SphereCollider>().sharedMaterial = originalCollider.sharedMaterial;
            var definition = world.Own(Object.Instantiate(sourceDefinition));
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_prefab").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prey = world.Create(definition, new Vector3(0, 0, -1.5f));
            var predator = world.Create("Snake", new Vector3(0, 0, 1.5f));
            prey.Body.linearVelocity = Vector3.forward * 3;
            predator.Body.linearVelocity = Vector3.back * 3;

            world.Simulate();

            Assert.That(prey.IsAlive, Is.False);
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(world.Events.Deaths, Is.EqualTo(1));
            Assert.That(world.Events.Meals, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StaticObstacle_OnlyCausesPhysicalResponse()
        {
            yield return new EnterPlayMode();
            using var world = new CollisionWorld();
            var obstacle = world.Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            obstacle.transform.position = new Vector3(0, 0, 0.75f);
            obstacle.transform.localScale = new Vector3(3, 1, 0.2f);
            var prey = world.Create("Frog", new Vector3(0, 0, -1.5f));
            prey.Body.linearVelocity = Vector3.forward * 3;

            world.Simulate();

            Assert.That(prey.IsAlive, Is.True);
            Assert.That(prey.Body.linearVelocity.z, Is.LessThan(0));
            Assert.That(world.Events.Deaths, Is.Zero);
            Assert.That(world.Events.Meals, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private sealed class CollisionWorld : IDisposable
        {
            private readonly AssetProvider _assets = new(new DiContainer());
            private readonly AnimalFactory _factory;
            private readonly List<Object> _owned = new();
            public ContactEvents Events { get; } = new();

            public CollisionWorld() => _factory = new AnimalFactory(_assets, new AnimalInteractionResolver());

            public Animal Create(string species, Vector3 position)
            {
                var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                    $"Assets/Core/Settings/Configs/Animals/{species}.asset");
                return Create(definition, position);
            }

            public Animal Create(AnimalDefinition definition, Vector3 position)
            {
                var animal = _factory.Create(definition, position);
                Own(animal.gameObject);
                Events.Observe(animal);
                return animal;
            }

            public T Own<T>(T item) where T : Object
            {
                _owned.Add(item);
                return item;
            }

            public void Simulate()
            {
                var previousMode = Physics.simulationMode;
                try
                {
                    Physics.simulationMode = SimulationMode.Script;
                    Physics.SyncTransforms();
                    for (var step = 0; step < 60; step++) Physics.Simulate(0.02f);
                }
                finally
                {
                    Physics.simulationMode = previousMode;
                }
            }

            public void Dispose()
            {
                foreach (var item in _owned)
                    if (item != null) Object.Destroy(item);
                _assets.Dispose();
            }
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
            private void OnMeal(Animal animal)
            {
                Meals++;
                LastEater = animal;
            }
        }
    }
}
