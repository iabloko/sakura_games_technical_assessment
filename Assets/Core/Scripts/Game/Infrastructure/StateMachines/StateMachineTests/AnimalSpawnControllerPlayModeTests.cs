using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalSpawnControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator DisableRestartAndSceneUnload_StopPendingSpawns()
        {
            yield return new EnterPlayMode();
            using var world = new ControllerWorld();
            var scene = SceneManager.CreateScene("Spawner lifetime test");
            world.Create(scene);
            yield return WaitForSpawn(world.Counter, 1);
            Assert.That(world.Controller.IsRunning, Is.True);

            world.Controller.enabled = false;
            var stoppedCount = world.Counter.Spawns;
            Assert.That(world.Controller.IsRunning, Is.False);
            yield return AdvanceTime(0.2f);
            Assert.That(world.Counter.Spawns, Is.EqualTo(stoppedCount));

            world.Controller.enabled = true;
            yield return WaitForSpawn(world.Counter, stoppedCount + 1);
            Assert.That(world.Controller.IsRunning, Is.True);
            yield return SceneManager.UnloadSceneAsync(scene);
            var finalCount = world.Counter.Spawns;
            yield return AdvanceTime(0.2f);
            Assert.That(world.Counter.Spawns, Is.EqualTo(finalCount));
            Assert.That(world.Counter.Deaths, Is.Zero);
            Assert.That(world.Controller == null, Is.True);
        }

        [UnityTest]
        public IEnumerator GameScene_ResolvesParentDependenciesAndSpawnsVisibleAnimals()
        {
            yield return new EnterPlayMode();
            var container = ProjectContext.Instance.Container;
            using var assets = new AssetProvider(container);
            container.BindInstance<IAssetProvider>(assets);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Core/Settings/Configs/GameConfig.asset");
            container.BindInstance(config);
            container.BindInstance<IAnimalFactory>(
                new AnimalFactory(assets, new AnimalInteractionResolver(), new AnimalMovementFactory()));

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Game");
            var controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AnimalSpawnController>(true)).Single();
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
            var counter = new SpawnCounter();
            controller.Spawned += counter.OnSpawned;
            Assert.That(controller.IsRunning, Is.False, "The game state owns scene spawning.");
            controller.StartSpawning();
            yield return WaitForSpawn(counter, 1);

            Assert.That(controller.IsRunning, Is.True);
            Assert.That(config.MinSpawnInterval, Is.EqualTo(1));
            Assert.That(config.MaxSpawnInterval, Is.EqualTo(2));
            Assert.That(counter.LastScene, Is.EqualTo(scene));
            var viewport = camera.WorldToViewportPoint(counter.LastPosition);
            Assert.That(viewport.x, Is.InRange(0.1499f, 0.8501f));
            Assert.That(viewport.y, Is.InRange(0.1499f, 0.8501f));
            controller.StopSpawning();
            var count = counter.Spawns;
            yield return AdvanceTime(0.2f);
            Assert.That(counter.Spawns, Is.EqualTo(count));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static IEnumerator WaitForSpawn(SpawnCounter counter, int count)
        {
            var deadline = EditorApplication.timeSinceStartup + 10f;
            while (counter.Spawns < count && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(counter.Spawns, Is.GreaterThanOrEqualTo(count), "Spawner did not produce an animal.");
        }

        private static IEnumerator AdvanceTime(float duration)
        {
            var target = Time.timeAsDouble + duration;
            var deadline = EditorApplication.timeSinceStartup + 10f;
            while (Time.timeAsDouble < target && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(Time.timeAsDouble, Is.GreaterThanOrEqualTo(target), "Game time did not advance.");
        }

        private sealed class SpawnCounter
        {
            public int Spawns { get; private set; }
            public int Deaths { get; private set; }
            public Vector3 LastPosition { get; private set; }
            public Scene LastScene { get; private set; }

            public void OnSpawned(Animal animal)
            {
                Spawns++;
                LastPosition = animal.transform.position;
                LastScene = animal.gameObject.scene;
                animal.Died += OnDeath;
            }
            private void OnDeath(Animal animal) => Deaths++;
        }

        private sealed class ControllerWorld : IDisposable
        {
            private readonly AssetProvider _assets = new(new DiContainer());
            private readonly List<Object> _owned = new();
            public SpawnCounter Counter { get; } = new();
            public AnimalSpawnController Controller { get; private set; }

            public void Create(Scene scene)
            {
                var root = new GameObject("Spawn controller test owner");
                root.SetActive(false);
                _owned.Add(root);
                SceneManager.MoveGameObjectToScene(root, scene);
                Controller = root.AddComponent<AnimalSpawnController>();
                var camera = new GameObject("Camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.transform.localPosition = new Vector3(0, 10, 0);
                camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                var animalRoot = new GameObject("Animals").transform;
                animalRoot.SetParent(root.transform, false);
                var serializedController = new SerializedObject(Controller);
                serializedController.FindProperty("_camera").objectReferenceValue = camera;
                serializedController.FindProperty("_animalRoot").objectReferenceValue = animalRoot;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                var source = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Core/Settings/Configs/GameConfig.asset");
                var config = Object.Instantiate(source);
                _owned.Add(config);
                var serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("_minMaxInterval").vector2Value = new Vector2(0.05f, 0.05f);
                serializedConfig.FindProperty("_animals").arraySize = 1;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
                Controller.Construct(new AnimalFactory(_assets, new AnimalInteractionResolver(), new AnimalMovementFactory()), config);
                Controller.Spawned += Counter.OnSpawned;
                root.SetActive(true);
            }

            public void Dispose()
            {
                foreach (var item in _owned)
                    if (item != null) Object.Destroy(item);
                _assets.Dispose();
            }
        }
    }
}
