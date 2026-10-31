using System;
using System.Collections;
using System.Linq;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Gameplay.UI;
using Core.Scripts.Game.Gameplay.World;
using Core.Scripts.Game.Infrastructure.Loader;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States;
using Core.Scripts.Game.ScriptableObjects.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class GameSessionPlayModeTests : GamePlayModeTestFixture
    {

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            SceneContext.ParentContainers = null;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator BootstrapPreparesWorldAndStopAllowsCleanRestart()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var session = new Session();
            session.Machine.Enter<BootStrapState>();
            yield return session.WaitForGameplay();
            Assert.That(session.World.IsPrepared, Is.True);
            Assert.That(session.World.IsRunning, Is.True);
            yield return session.WaitForAnimal();
            var animal = session.Spawner.AnimalRoot.GetComponentInChildren<Animal>();
            var oldStatistics = session.Hud.Statistics;
            animal.TryDie();
            Assert.That(oldStatistics.DeadPrey + oldStatistics.DeadPredators, Is.EqualTo(1));

            session.Machine.Stop();
            Assert.That(session.Machine.CurrentStateType, Is.Null);
            Assert.That(session.World.IsPrepared, Is.False);
            Assert.That(session.World.IsRunning, Is.False);
            Assert.That(session.Spawner.IsRunning, Is.False);
            Assert.That(oldStatistics.ObservedCount, Is.Zero);
            Assert.That(session.Hud.Statistics.DeadPrey + session.Hud.Statistics.DeadPredators, Is.Zero);
            Assert.That(session.Spawner.AnimalRoot.GetComponentsInChildren<Animal>(), Is.Empty);

            session.Machine.Enter<BootStrapState>();
            yield return session.WaitForGameplay();
            Assert.That(session.Hud.Statistics.ObservedCount, Is.Zero);
            yield return session.WaitForAnimal();
            Assert.That(session.Hud.Statistics.ObservedCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DirectGameEntryWaitsForPreparationAndDoesNotReloadScene()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var session = new Session();
            yield return session.LoadGame();
            var scene = SceneManager.GetActiveScene();
            Assert.That(session.Spawner.IsRunning, Is.False);
            Assert.That(session.World.IsPrepared, Is.False);
            Assert.Throws<InvalidOperationException>(session.World.StartSession);
            session.Machine.Enter<BootStrapState>();
            yield return session.WaitForGameplay();
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(session.Spawner.IsRunning, Is.True);
            Assert.That(session.Hud.Statistics.ObservedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InvalidCameraStopsPreparationBeforeGameplay()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var session = new Session();
            yield return session.LoadGame();
            session.Camera.enabled = false;
            Assert.Throws<InvalidOperationException>(session.Machine.Enter<WorldGenerationState>);
            Assert.That(session.Machine.CurrentStateType, Is.EqualTo(typeof(WorldGenerationState)));
            Assert.That(session.World.IsPrepared, Is.False);
            Assert.That(session.Spawner.IsRunning, Is.False);
            Assert.That(session.Hud.Statistics.ObservedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator StoppingDuringNativeLoadPreventsSessionFromStarting()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var session = new Session();
            session.Machine.Enter<BootStrapState>();
            Assert.That(session.Machine.CurrentStateType, Is.EqualTo(typeof(LoadLevelState)));
            session.Machine.Stop();
            var deadline = EditorApplication.timeSinceStartup + 15f;
            while ((!SceneManager.GetSceneByName("Game").isLoaded || SceneManager.GetActiveScene().name != "Game") &&
                   EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetSceneByName("Game").isLoaded, Is.True);
            yield return null;
            Assert.That(session.Machine.CurrentStateType, Is.Null);
            Assert.That(session.Spawner.IsRunning, Is.False);
            Assert.That(session.World.IsPrepared, Is.False);
            Assert.That(session.Hud.Statistics.ObservedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DisablingWorldCleansAnimalsAndDoesNotCountDeaths()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var session = new Session();
            session.Machine.Enter<BootStrapState>();
            yield return session.WaitForGameplay();
            yield return session.WaitForAnimal();
            var oldStatistics = session.Hud.Statistics;
            session.World.enabled = false;
            Assert.That(session.World.IsRunning, Is.False);
            Assert.That(session.Spawner.IsRunning, Is.False);
            Assert.That(oldStatistics.DeadPrey + oldStatistics.DeadPredators, Is.Zero);
            Assert.That(oldStatistics.ObservedCount, Is.Zero);
            Assert.That(session.Spawner.AnimalRoot.GetComponentsInChildren<Animal>(), Is.Empty);
            session.World.enabled = true;
            Assert.That(session.Spawner.IsRunning, Is.False);
        }

        private sealed class Session : IDisposable
        {
            private readonly AssetProvider _assets;
            public GameStateMachine Machine { get; }
            public GameWorld World => Find<GameWorld>();
            public AnimalSpawnController Spawner => Find<AnimalSpawnController>();
            public AnimalHudController Hud => Find<AnimalHudController>();
            public Camera Camera => Find<Camera>();

            public Session()
            {
                var container = ProjectContext.Instance.Container.CreateSubContainer();
                _assets = new AssetProvider(container);
                container.BindInstance<IAssetProvider>(_assets);
                container.BindInstance(AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Core/Settings/Configs/GameConfig.asset"));
                container.BindInstance<IAnimalFactory>(new AnimalFactory(_assets, new AnimalInteractionResolver(), new AnimalMovementFactory()));
                container.Bind<SceneLoader>().AsSingle();
                container.BindFactory<GameStateMachine, BootStrapState, BootStrapState.Factory>().AsSingle();
                container.BindFactory<GameStateMachine, LoadLevelState, LoadLevelState.Factory>().AsSingle();
                container.BindFactory<GameStateMachine, WorldGenerationState, WorldGenerationState.Factory>().AsSingle();
                container.BindFactory<GameStateMachine, GamePlayState, GamePlayState.Factory>().AsSingle();
                container.Bind<GameStateMachine>().AsSingle();
                Machine = container.Resolve<GameStateMachine>();
                SceneContext.ParentContainers = new[] { container };
            }

            public IEnumerator LoadGame()
            {
                var operation = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
                var deadline = EditorApplication.timeSinceStartup + 15f;
                while (!operation.isDone && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(operation.isDone, Is.True);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName("Game"));
            }

            public IEnumerator WaitForGameplay()
            {
                var deadline = EditorApplication.timeSinceStartup + 15f;
                while (Machine.CurrentStateType != typeof(GamePlayState) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(Machine.CurrentStateType, Is.EqualTo(typeof(GamePlayState)));
            }

            public IEnumerator WaitForAnimal()
            {
                var deadline = EditorApplication.timeSinceStartup + 10f;
                while (Hud.Statistics.ObservedCount == 0 && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(Hud.Statistics.ObservedCount, Is.GreaterThan(0));
            }

            private static T Find<T>() where T : Component => SceneManager.GetSceneByName("Game").GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

            public void Dispose()
            {
                Machine.Stop();
                _assets.Dispose();
            }
        }
    }
}
