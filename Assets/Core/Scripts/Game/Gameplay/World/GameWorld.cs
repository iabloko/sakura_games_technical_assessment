using System;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Gameplay.UI;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Game.Gameplay.World
{
    [DisallowMultipleComponent]
    public sealed class GameWorld : MonoBehaviour
    {
        [SerializeField] private AnimalSpawnController _spawner;
        [SerializeField] private AnimalHudController _hud;

        private IAssetProvider _assets;
        private GameConfig _config;
        public bool IsPrepared { get; private set; }
        public bool IsRunning { get; private set; }

        [Inject]
        public void Construct(IAssetProvider assets, GameConfig config)
        {
            _assets = assets;
            _config = config;
        }

        public void Prepare()
        {
            if (IsRunning) throw new InvalidOperationException("A running world cannot be prepared again.");
            IsPrepared = false;
            if (!isActiveAndEnabled || _spawner == null || _hud == null || !_hud.isActiveAndEnabled)
                throw new InvalidOperationException("World references must be assigned and active.");
            if (_assets == null || _config == null)
                throw new InvalidOperationException("World dependencies have not been injected.");
            _spawner.PrepareForSession();
            foreach (var definition in _config.Animals) AnimalFactory.ValidateDefinition(definition);
            ClearAnimals();
            _hud.ResetSession();
            IsPrepared = true;
        }

        public void StartSession()
        {
            if (!isActiveAndEnabled || !IsPrepared)
                throw new InvalidOperationException("The world must be prepared before gameplay starts.");
            if (IsRunning) return;
            _spawner.StartSpawning();
            if (!_spawner.IsRunning) throw new InvalidOperationException("The world spawn controller cannot start.");
            IsRunning = true;
        }

        public void EndSession()
        {
            if (_spawner != null) _spawner.StopSpawning();
            if (!IsPrepared && !IsRunning) return;
            IsRunning = false;
            IsPrepared = false;
            ClearAnimals();
            if (_hud != null && _hud.Statistics != null) _hud.ResetSession();
        }

        private void ClearAnimals()
        {
            if (_spawner == null || _spawner.AnimalRoot == null || _assets == null) return;
            // Session cleanup releases instances without publishing gameplay deaths.
            foreach (var animal in _spawner.AnimalRoot.GetComponentsInChildren<Animal>(true))
                if (animal != null) _assets.ReleaseInstance(animal.gameObject);
        }

        private void OnDisable() => EndSession();
        private void OnDestroy() => EndSession();
    }
}
