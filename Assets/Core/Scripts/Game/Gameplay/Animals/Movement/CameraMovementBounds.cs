using System;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals.Movement
{
    public sealed class CameraMovementBounds : IAnimalMovementBounds
    {
        private readonly Camera _camera;
        private readonly Plane _ground;
        private readonly float _inset;

        public CameraMovementBounds(Camera camera, float groundHeight = 0f, float viewportInset = 0.05f)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (viewportInset < 0f || viewportInset >= 0.5f)
                throw new ArgumentOutOfRangeException(nameof(viewportInset));
            _camera = camera;
            _ground = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
            _inset = viewportInset;
        }

        public bool TryGetReturnDirection(Vector3 position, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (_camera == null) return false;

            var viewport = _camera.WorldToViewportPoint(position);
            if (viewport.z > 0f && viewport.x >= _inset && viewport.x <= 1f - _inset &&
                viewport.y >= _inset && viewport.y <= 1f - _inset) return false;

            var centerRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (!_ground.Raycast(centerRay, out var distance)) return false;

            direction = centerRay.GetPoint(distance) - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f) return false;
            direction.Normalize();
            return true;
        }
    }
}
