using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Gameplay.UI;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator CountersAndMealLabelFollowConfirmedPredation()
        {
            yield return new EnterPlayMode();
            using var world = new HudWorld();
            yield return world.Load();
            Assert.That(world.Text("Dead Prey").text, Is.EqualTo("Dead prey: 0"));
            Assert.That(world.Text("Dead Predators").text, Is.EqualTo("Dead predators: 0"));
            var predator = world.Create("Snake", Vector3.zero);
            var prey = world.Create("Frog", Vector3.right * 3f);
            var newerPredator = world.Create("Snake", Vector3.left * 3f);

            world.Resolver.TryResolve(predator, prey);
            world.Resolver.TryResolve(newerPredator, predator);

            Assert.That(world.Text("Dead Prey").text, Is.EqualTo("Dead prey: 1"));
            Assert.That(world.Text("Dead Predators").text, Is.EqualTo("Dead predators: 1"));
            Assert.That(world.MealTexts().Length, Is.EqualTo(1));
            var label = world.MealTexts().Single();
            Assert.That(label.text, Is.EqualTo("Tasty!"));
            Assert.That(label.raycastTarget, Is.False);
            Assert.That(label.rectTransform.position.y,
                Is.LessThan(world.Camera.WorldToScreenPoint(predator.transform.position).y));
            var initialX = label.rectTransform.position.x;
            predator.transform.position += Vector3.right;
            yield return AdvanceTime(0.05f);
            Assert.That(label.rectTransform.position.x, Is.GreaterThan(initialX));
            AssertLabelBelowVisual(world.Camera, predator, label);
            yield return AdvanceTime(1.1f);
            Assert.That(world.MealTexts(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator RepeatedMealsRefreshOneLabelAndPredatorDeathRemovesIt()
        {
            yield return new EnterPlayMode();
            using var world = new HudWorld();
            yield return world.Load();
            var predator = world.Create("Snake", Vector3.zero);
            world.Resolver.TryResolve(predator, world.Create("Frog", Vector3.right * 3));
            var firstLabel = world.MealTexts().Single();
            yield return AdvanceTime(0.6f);
            world.Resolver.TryResolve(predator, world.Create("Frog", Vector3.left * 3));
            Assert.That(world.MealTexts().Single(), Is.SameAs(firstLabel));
            yield return AdvanceTime(0.5f);
            Assert.That(world.MealTexts().Length, Is.EqualTo(1));
            predator.TryDie();
            yield return AdvanceTime(0.05f);
            Assert.That(world.MealTexts(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator HiddenHudKeepsStatisticsWithoutCreatingOverdueLabels()
        {
            yield return new EnterPlayMode();
            using var world = new HudWorld();
            yield return world.Load();
            var predator = world.Create("Snake", Vector3.zero);
            world.Hud.gameObject.SetActive(false);
            world.Resolver.TryResolve(predator, world.Create("Frog", Vector3.right * 3));
            Assert.That(world.Hud.Statistics.DeadPrey, Is.EqualTo(1));
            world.Hud.gameObject.SetActive(true);
            Assert.That(world.Text("Dead Prey").text, Is.EqualTo("Dead prey: 1"));
            Assert.That(world.MealTexts(), Is.Empty);
            world.Resolver.TryResolve(predator, world.Create("Frog", Vector3.left * 3));
            Assert.That(world.Text("Dead Prey").text, Is.EqualTo("Dead prey: 2"));
            Assert.That(world.MealTexts().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExternalRemovalDoesNotCountDeathAndSceneUnloadDisposesObservers()
        {
            yield return new EnterPlayMode();
            using var world = new HudWorld();
            yield return world.Load();
            var predator = world.Create("Snake", Vector3.zero);
            world.Resolver.TryResolve(predator, world.Create("Frog", Vector3.right * 3));
            var statistics = world.Hud.Statistics;
            Object.Destroy(predator.gameObject);
            yield return AdvanceTime(0.05f);
            Assert.That(statistics.DeadPrey, Is.EqualTo(1));
            Assert.That(statistics.DeadPredators, Is.Zero);
            Assert.That(statistics.ObservedCount, Is.Zero);
            Assert.That(world.MealTexts(), Is.Empty);
            world.Create("Frog", Vector3.zero);
            Assert.That(statistics.ObservedCount, Is.EqualTo(1));
            yield return world.Unload();
            Assert.That(statistics.ObservedCount, Is.Zero);
            Assert.That(statistics.DeadPrey, Is.EqualTo(1));
            Assert.That(statistics.DeadPredators, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RealSpawnerAutomaticallyRegistersAnimalsWithStatistics()
        {
            yield return new EnterPlayMode();
            using var world = new HudWorld();
            yield return world.Load();
            world.Spawner.enabled = true;
            world.Spawner.StartSpawning();
            var deadline = EditorApplication.timeSinceStartup + 10f;
            while (world.Hud.Statistics.ObservedCount == 0 && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            world.Spawner.StopSpawning();
            Assert.That(world.Hud.Statistics.ObservedCount, Is.GreaterThan(0));
            Assert.That(world.Hud.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            var counterPanel = world.Text("Dead Prey").transform.parent as RectTransform;
            Assert.That(counterPanel.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(counterPanel.pivot, Is.EqualTo(Vector2.one));
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static IEnumerator AdvanceTime(float seconds)
        {
            var target = Time.timeAsDouble + seconds;
            var deadline = EditorApplication.timeSinceStartup + 10f;
            while (Time.timeAsDouble < target && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(Time.timeAsDouble, Is.GreaterThanOrEqualTo(target));
        }

        private static void AssertLabelBelowVisual(Camera camera, Animal animal, Text label)
        {
            var bounds = new Bounds(animal.Visual.position, Vector3.zero);
            foreach (var renderer in animal.Visual.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var bottom = float.PositiveInfinity;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                bottom = Mathf.Min(bottom, camera.WorldToScreenPoint(point).y);
            }
            var corners = new Vector3[4];
            label.rectTransform.GetWorldCorners(corners);
            Assert.That(corners[1].y, Is.LessThan(bottom), "The caption must be below the entire multi-part animal visual.");
        }

        private sealed class HudWorld : IDisposable
        {
            private readonly List<GameObject> _owned = new();
            private AssetProvider _assets;
            private AnimalFactory _factory;
            private Scene _scene;
            public AnimalInteractionResolver Resolver { get; } = new();
            public AnimalHudController Hud { get; private set; }
            public AnimalSpawnController Spawner { get; private set; }
            public Camera Camera { get; private set; }

            public IEnumerator Load()
            {
                var container = ProjectContext.Instance.Container;
                _assets = new AssetProvider(container);
                container.BindInstance<IAssetProvider>(_assets);
                _factory = new AnimalFactory(_assets, Resolver, new AnimalMovementFactory());
                container.BindInstance<IAnimalFactory>(_factory);
                container.BindInstance(AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Core/Settings/Configs/GameConfig.asset"));
                var loading = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
                var deadline = EditorApplication.timeSinceStartup + 10f;
                while (!loading.isDone && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(loading.isDone, Is.True, "Game scene did not finish loading.");
                _scene = SceneManager.GetSceneByName("Game");
                Hud = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AnimalHudController>()).Single();
                Spawner = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AnimalSpawnController>()).Single();
                Camera = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
                Spawner.StopSpawning();
                Spawner.enabled = false;
            }

            public Animal Create(string species, Vector3 position)
            {
                var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                    $"Assets/Core/Settings/Configs/Animals/{species}.asset");
                var animal = _factory.Create(definition, position);
                animal.GetComponent<AnimalMovementController>().enabled = false;
                animal.Body.isKinematic = true;
                _owned.Add(animal.gameObject);
                Hud.ObserveAnimal(animal);
                return animal;
            }

            public Text Text(string name) => Hud.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
            public Text[] MealTexts() => Hud.GetComponentsInChildren<Text>()
                .Where(text => text.text == "Tasty!").ToArray();

            public IEnumerator Unload()
            {
                foreach (var actor in _owned)
                    if (actor != null) Object.Destroy(actor);
                var unloading = SceneManager.UnloadSceneAsync(_scene);
                var deadline = EditorApplication.timeSinceStartup + 10f;
                while (!unloading.isDone && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(unloading.isDone, Is.True, "Game scene did not finish unloading.");
            }

            public void Dispose()
            {
                foreach (var actor in _owned)
                    if (actor != null) Object.Destroy(actor);
                _assets?.Dispose();
            }
        }
    }
}
