using System.Collections;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator Death_DisablesPhysicsImmediatelyAndDestroysOnNextFrame()
        {
            yield return new EnterPlayMode();
            using var assets = new AssetProvider(new DiContainer());
            var factory = new AnimalFactory(assets, new AnimalInteractionResolver());
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                "Assets/Core/Settings/Configs/Animals/Frog.asset");
            var animal = factory.Create(definition, Vector3.zero);
            var deaths = new DeathCounter();
            animal.Died += deaths.OnDeath;

            Assert.That(animal.TryDie(), Is.True);
            Assert.That(animal.TryDie(), Is.False);
            Assert.That(animal.IsAlive, Is.False);
            Assert.That(animal.Body.detectCollisions, Is.False);
            Assert.That(animal.gameObject.activeSelf, Is.False);
            Assert.That(deaths.Count, Is.EqualTo(1));

            yield return null;
            Assert.That(animal == null, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneUnload_RemovesAnimalWithoutGameplayDeath()
        {
            yield return new EnterPlayMode();
            using var assets = new AssetProvider(new DiContainer());
            var factory = new AnimalFactory(assets, new AnimalInteractionResolver());
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                "Assets/Core/Settings/Configs/Animals/Snake.asset");
            var scene = SceneManager.CreateScene("Animal lifecycle test");
            var animal = factory.Create(definition, Vector3.zero);
            SceneManager.MoveGameObjectToScene(animal.gameObject, scene);
            var deaths = new DeathCounter();
            animal.Died += deaths.OnDeath;

            yield return SceneManager.UnloadSceneAsync(scene);

            Assert.That(animal == null, Is.True);
            Assert.That(deaths.Count, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        // A captured local would be hoisted before EnterPlayMode and lost on domain reload.
        private sealed class DeathCounter
        {
            public int Count { get; private set; }
            public void OnDeath(Animal animal) => Count++;
        }
    }
}
