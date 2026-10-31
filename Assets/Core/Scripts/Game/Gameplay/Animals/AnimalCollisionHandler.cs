using System;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animal))]
    public sealed class AnimalCollisionHandler : MonoBehaviour
    {
        private Animal _owner;
        private AnimalInteractionResolver _resolver;

        internal void Initialize(Animal owner, AnimalInteractionResolver resolver)
        {
            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isActiveAndEnabled || _owner == null || !_owner.IsAlive || _resolver == null) return;

            // The other Rigidbody identifies the owner even if its collider is on a child.
            // Static obstacles and non-animal bodies are handled only by the physics solver.
            var otherBody = collision.rigidbody;
            if (otherBody != null && otherBody.TryGetComponent<Animal>(out var other))
                _resolver.TryResolve(_owner, other);
        }
    }
}
