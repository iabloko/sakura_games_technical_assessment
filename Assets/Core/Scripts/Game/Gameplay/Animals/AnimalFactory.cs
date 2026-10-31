using System;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals
{
    public sealed class AnimalFactory : IAnimalFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly AnimalInteractionResolver _interactionResolver;
        private long _nextId;

        public AnimalFactory(IAssetProvider assetProvider, AnimalInteractionResolver interactionResolver)
        {
            _assetProvider = assetProvider;
            _interactionResolver = interactionResolver ?? throw new ArgumentNullException(nameof(interactionResolver));
        }

        public Animal Create(AnimalDefinition definition, Vector3 position, Transform parent = null)
        {
            ValidateDefinition(definition);

            var staging = new GameObject("Animal creation");
            staging.SetActive(false);
            GameObject instance = null;
            try
            {
                // Even an active prefab must stay inactive in the hierarchy until setup completes.
                instance = _assetProvider.Instantiate(definition.Prefab, staging.transform);
                instance.SetActive(false);
                var animal = instance.GetComponent<Animal>();
                animal.Initialize(++_nextId, definition, _assetProvider.ReleaseInstance);
                instance.GetComponent<AnimalCollisionHandler>().Initialize(animal, _interactionResolver);

                instance.name = $"{definition.Id} ({animal.Id})";
                instance.transform.SetParent(parent, false);
                instance.transform.SetPositionAndRotation(position, Quaternion.identity);
                instance.SetActive(true);
                return animal;
            }
            catch
            {
                if (instance != null) _assetProvider.ReleaseInstance(instance);
                throw;
            }
            finally
            {
                _assetProvider.ReleaseInstance(staging);
            }
        }

        private static void ValidateDefinition(AnimalDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new InvalidOperationException("The animal definition requires a stable ID.");
            if (definition.Movement == null)
                throw new InvalidOperationException($"Animal '{definition.Id}' has no movement settings.");

            var prefab = definition.Prefab;
            if (prefab == null)
                throw new InvalidOperationException($"Animal '{definition.Id}' has no prefab.");
            if (!prefab.TryGetComponent<Animal>(out _))
                throw new InvalidOperationException($"Prefab '{prefab.name}' requires an Animal on its root.");
            if (!prefab.TryGetComponent<AnimalCollisionHandler>(out _))
                throw new InvalidOperationException($"Prefab '{prefab.name}' requires an AnimalCollisionHandler on its root.");
            if (!prefab.TryGetComponent<Rigidbody>(out var body) || body.isKinematic)
                throw new InvalidOperationException($"Prefab '{prefab.name}' requires a dynamic Rigidbody on its root.");

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
                // Prefab assets are not in a physics scene, so attachedRigidbody is not reliable here.
                if (collider.enabled && !collider.isTrigger && collider.GetComponentInParent<Rigidbody>(true) == body) return;

            throw new InvalidOperationException($"Prefab '{prefab.name}' requires a solid collider attached to its Rigidbody.");
        }
    }
}
