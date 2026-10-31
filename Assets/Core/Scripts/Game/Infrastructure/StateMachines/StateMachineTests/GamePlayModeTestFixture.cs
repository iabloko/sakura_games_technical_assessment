using System.Collections;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public abstract class GamePlayModeTestFixture
    {
        protected static IEnumerator StartWithStoppedProjectContext()
        {
            var context = ProjectContext.Instance;
            context.GetComponent<ProjectKernel>().Initialize();
            var machine = context.Container.Resolve<GameStateMachine>();
            var deadline = EditorApplication.timeSinceStartup + 20f;
            while (machine.CurrentStateType != typeof(GamePlayState) && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(machine.CurrentStateType, Is.EqualTo(typeof(GamePlayState)), "The real project installers must start gameplay.");
            machine.Stop();

            var empty = SceneManager.CreateScene("Isolated gameplay test");
            SceneManager.SetActiveScene(empty);
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene == empty || !scene.isLoaded) continue;
                var unloading = SceneManager.UnloadSceneAsync(scene);
                deadline = EditorApplication.timeSinceStartup + 10f;
                while (!unloading.isDone && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(unloading.isDone, Is.True, "The initial scene did not unload.");
            }
        }

    }
}
