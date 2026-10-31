using System;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Spawning
{
    public sealed class CameraAnimalSpawnArea : IAnimalSpawnArea
    {
        private readonly Camera _camera;
        private readonly Plane _ground;
        private readonly float _inset;

        public IAnimalMovementBounds MovementBounds { get; }

        public CameraAnimalSpawnArea(Camera camera, float groundHeight = 0f, float viewportInset = 0.15f)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (float.IsNaN(viewportInset) || viewportInset < 0f || viewportInset >= 0.5f)
                throw new ArgumentOutOfRangeException(nameof(viewportInset));
            _camera = camera;
            _ground = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
            _inset = viewportInset;
            MovementBounds = new CameraMovementBounds(camera, groundHeight);
        }

        public bool TryGetPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (_camera == null || !_camera.isActiveAndEnabled) return false;
            var viewport = new Vector3(UnityEngine.Random.Range(_inset, 1f - _inset),
                UnityEngine.Random.Range(_inset, 1f - _inset), 0);
            var ray = _camera.ViewportPointToRay(viewport);
            if (!_ground.Raycast(ray, out var distance)) return false;
            var candidate = ray.GetPoint(distance);
            var depth = _camera.WorldToViewportPoint(candidate).z;
            if (depth < _camera.nearClipPlane || depth > _camera.farClipPlane) return false;
            position = candidate;
            return true;
        }
    }
}
