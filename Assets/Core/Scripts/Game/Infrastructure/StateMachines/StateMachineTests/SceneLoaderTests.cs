using System;
using System.Collections;
using System.Threading;
using Core.Scripts.Game.Infrastructure.Loader;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    [Timeout(10000)]
    public sealed class SceneLoaderTests
    {
        [UnityTest]
        public IEnumerator AlreadyLoadedScene_IsNotReloaded() => UniTask.ToCoroutine(async () =>
        {
            var scene = SceneManager.GetActiveScene();
            var originalName = scene.name;
            try
            {
                if (string.IsNullOrEmpty(scene.name)) scene.name = "InfrastructureTestScene";
                var sceneCount = SceneManager.sceneCount;
                await new SceneLoader().LoadAsync(scene.name);
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle));
                Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            }
            finally
            {
                scene.name = originalName;
            }
        });

        [UnityTest]
        public IEnumerator CancelledRequest_DoesNotLoadScene() => UniTask.ToCoroutine(async () =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var scene = SceneManager.GetActiveScene();
            var cancelled = false;
            try
            {
                await new SceneLoader().LoadAsync("Game", cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle));
        });

        [UnityTest]
        public IEnumerator UnknownScene_ReportsErrorWithoutChangingScene() => UniTask.ToCoroutine(async () =>
        {
            var scene = SceneManager.GetActiveScene();
            var rejected = false;
            try
            {
                await new SceneLoader().LoadAsync("MissingInfrastructureTestScene");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Assert.That(rejected, Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene.handle));
        });
    }
}
