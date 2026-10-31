using Core.Scripts.Game.Gameplay.Animals.Spawning;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class CameraAnimalSpawnAreaTests
    {
        private GameObject _root;
        private Camera _camera;
        private UnityEngine.Random.State _random;

        [SetUp]
        public void SetUp()
        {
            _random = UnityEngine.Random.state;
            _root = new GameObject("Spawn camera test");
            _camera = _root.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5;
            _camera.aspect = 1.5f;
            _camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            UnityEngine.Random.state = _random;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SamplesStayOnTheGroundInsideCameraViewport(bool orthographic)
        {
            _camera.orthographic = orthographic;
            _camera.transform.position = new Vector3(0, 12, -4);
            _camera.transform.LookAt(Vector3.zero);
            var area = new CameraAnimalSpawnArea(_camera);
            for (var index = 0; index < 100; index++)
            {
                Assert.That(area.TryGetPosition(out var position), Is.True);
                var viewport = _camera.WorldToViewportPoint(position);
                Assert.That(position.y, Is.EqualTo(0).Within(0.001f));
                Assert.That(viewport.x, Is.InRange(0.1499f, 0.8501f));
                Assert.That(viewport.y, Is.InRange(0.1499f, 0.8501f));
                Assert.That(area.MovementBounds.TryGetReturnDirection(position, out _), Is.False);
            }
        }

        [Test]
        public void DestroyedOrDisabledCamera_DoesNotProduceSpawnPosition()
        {
            var area = new CameraAnimalSpawnArea(_camera);
            _camera.enabled = false;
            Assert.That(area.TryGetPosition(out _), Is.False);
            Object.DestroyImmediate(_root);
            Assert.That(area.TryGetPosition(out _), Is.False);
        }

        [Test]
        public void CameraNotFacingGround_RejectsUnusableView()
        {
            _camera.transform.rotation = Quaternion.identity;
            Assert.That(new CameraAnimalSpawnArea(_camera).TryGetPosition(out _), Is.False);
        }

        [Test]
        public void GroundBeyondFarClipPlane_DoesNotSpawnInvisibleAnimal()
        {
            _camera.farClipPlane = 5;
            Assert.That(new CameraAnimalSpawnArea(_camera).TryGetPosition(out _), Is.False);
        }
    }
}
