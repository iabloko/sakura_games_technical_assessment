using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class CameraMovementBoundsTests
    {
        private readonly List<GameObject> _owned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _owned)
                if (item != null) Object.DestroyImmediate(item);
            _owned.Clear();
        }

        [Test]
        public void InsideViewport_DoesNotOverrideWandering()
        {
            var camera = CreateCamera();
            camera.aspect = 2f;
            Assert.That(new CameraMovementBounds(camera).TryGetReturnDirection(new Vector3(8, 0, 0), out _), Is.False);
        }

        [Test]
        public void AspectChange_ImmediatelyUpdatesVisibleArea()
        {
            var camera = CreateCamera();
            var bounds = new CameraMovementBounds(camera);
            camera.aspect = 0.5f;
            Assert.That(bounds.TryGetReturnDirection(new Vector3(3, 0, 0), out var direction), Is.True);
            Assert.That(direction.x, Is.LessThan(0));
            camera.aspect = 2f;
            Assert.That(bounds.TryGetReturnDirection(new Vector3(3, 0, 0), out _), Is.False);
        }

        [Test]
        public void RotatedCamera_ReturnsTowardItsActualViewport()
        {
            var camera = CreateCamera();
            camera.transform.rotation = Quaternion.Euler(90, 45, 0);
            var position = GroundPoint(camera, new Vector2(1.2f, 0.5f));
            Assert.That(new CameraMovementBounds(camera).TryGetReturnDirection(position, out var direction), Is.True);
            Assert.That(Vector3.Dot(direction, camera.transform.right), Is.LessThan(-0.99f));
            Assert.That(direction.y, Is.Zero);
        }

        [Test]
        public void PerspectiveCamera_ReturnDirectionLeadsIntoTheScreen()
        {
            var camera = CreateCamera();
            camera.orthographic = false;
            camera.transform.position = new Vector3(0, 12, -8);
            camera.transform.LookAt(Vector3.zero);
            var position = GroundPoint(camera, new Vector2(1.1f, 0.5f));

            Assert.That(new CameraMovementBounds(camera).TryGetReturnDirection(position, out var direction), Is.True);

            Assert.That(camera.WorldToViewportPoint(position + direction * 0.1f).x, Is.LessThan(1.1f));
            Assert.That(direction.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ViewportInset_StartsReturningBeforeTheEdge()
        {
            var camera = CreateCamera();
            var position = GroundPoint(camera, new Vector2(0.02f, 0.5f));
            Assert.That(new CameraMovementBounds(camera).TryGetReturnDirection(position, out var direction), Is.True);
            Assert.That(direction.x, Is.GreaterThan(0));
        }

        [Test]
        public void DestroyedCamera_DoesNotProduceStaleSteering()
        {
            var camera = CreateCamera();
            var bounds = new CameraMovementBounds(camera);
            Object.DestroyImmediate(camera.gameObject);
            Assert.That(bounds.TryGetReturnDirection(new Vector3(100, 0, 0), out var direction), Is.False);
            Assert.That(direction, Is.EqualTo(Vector3.zero));
        }

        private Camera CreateCamera()
        {
            var root = new GameObject("Movement bounds test camera");
            _owned.Add(root);
            var camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;
            root.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            return camera;
        }

        private static Vector3 GroundPoint(Camera camera, Vector2 viewport)
        {
            var ray = camera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0));
            Assert.That(new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance), Is.True);
            return ray.GetPoint(distance);
        }
    }
}
