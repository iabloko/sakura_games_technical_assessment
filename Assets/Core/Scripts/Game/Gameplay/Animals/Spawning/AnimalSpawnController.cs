using System;
using Core.Scripts.Game.ScriptableObjects.Configs;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Game.Gameplay.Animals.Spawning
{
    [DisallowMultipleComponent]
    public sealed class AnimalSpawnController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _animalRoot;
        [SerializeField] private float _groundHeight;
        [SerializeField, Range(0f, 0.45f)] private float _viewportInset = 0.15f;
        [SerializeField] private bool _autoStart = true;

        private AnimalSpawner _spawner;
        private bool _started;

        public event Action<Animal> Spawned;
        public bool IsRunning => _spawner != null && _spawner.IsRunning;
        public Transform AnimalRoot => _animalRoot;

        public void PrepareForSession()
        {
            _autoStart = false;
            StopSpawning();
            if (_spawner == null) throw new InvalidOperationException("The spawn controller has not been initialized.");
            if (!isActiveAndEnabled || _animalRoot == null || !_animalRoot.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The animal spawn controller and its owner must be active.");
            if (_camera == null || !_camera.isActiveAndEnabled)
                throw new InvalidOperationException("The world camera must be active.");
            var plane = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));
            foreach (var viewport in new[] { new Vector3(_viewportInset, _viewportInset),
                         new Vector3(_viewportInset, 1f - _viewportInset),
                         new Vector3(1f - _viewportInset, _viewportInset),
                         new Vector3(1f - _viewportInset, 1f - _viewportInset), new Vector3(0.5f, 0.5f) })
            {
                var ray = _camera.ViewportPointToRay(viewport);
                if (!plane.Raycast(ray, out var distance))
                    throw new InvalidOperationException("The world camera does not see the animal plane.");
                var depth = _camera.WorldToViewportPoint(ray.GetPoint(distance)).z;
                if (depth < _camera.nearClipPlane || depth > _camera.farClipPlane)
                    throw new InvalidOperationException("The animal plane is outside the camera clipping range.");
            }
        }

        [Inject]
        public void Construct(IAnimalFactory factory, GameConfig config)
        {
            if (_spawner != null) throw new InvalidOperationException("The spawn controller is already initialized.");
            if (config == null) throw new ArgumentNullException(nameof(config));
            var area = new CameraAnimalSpawnArea(_camera, _groundHeight, _viewportInset);
            _spawner = new AnimalSpawner(factory, config.Animals, area, _animalRoot,
                config.MinSpawnInterval, config.MaxSpawnInterval);
            _spawner.Spawned += OnSpawned;
            if (_started && _autoStart && isActiveAndEnabled) StartSpawning();
        }

        public void StartSpawning()
        {
            if (_spawner == null) throw new InvalidOperationException("The spawn controller has not been initialized.");
            if (isActiveAndEnabled) _spawner.Start();
        }

        public void StopSpawning() => _spawner?.Stop();

        private void Start()
        {
            _started = true;
            if (_autoStart) StartSpawning();
        }

        private void OnEnable()
        {
            if (_started && _autoStart && _spawner != null) StartSpawning();
        }

        private void Update() => _spawner?.Tick(Time.deltaTime);
        private void OnDisable() => StopSpawning();

        private void OnDestroy()
        {
            _spawner?.Dispose();
            Spawned = null;
        }

        private void OnSpawned(Animal animal) => Spawned?.Invoke(animal);
    }
}
