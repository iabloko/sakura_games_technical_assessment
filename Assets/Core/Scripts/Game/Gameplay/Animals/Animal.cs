using System;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Gameplay.Animals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Animal : MonoBehaviour
    {
        [SerializeField] private Transform _visual;

        private Action<GameObject> _releaseInstance;
        private bool _isAlive;

        public event Action<Animal> Died;

        public long Id { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsAlive => _isAlive && this != null;
        public Rigidbody Body { get; private set; }
        public Transform Visual => _visual;

        internal void Initialize(long id, AnimalDefinition definition, Action<GameObject> releaseInstance)
        {
            if (IsInitialized) throw new InvalidOperationException("An animal cannot be initialized twice.");
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (releaseInstance == null) throw new ArgumentNullException(nameof(releaseInstance));

            Id = id;
            Definition = definition;
            Body = GetComponent<Rigidbody>();
            _releaseInstance = releaseInstance;
            IsInitialized = true;
            _isAlive = true;
        }

        public bool TryDie()
        {
            if (!IsAlive) return false;

            // Change state before notifying observers: a reentrant contact cannot kill it again.
            _isAlive = false;
            Body.detectCollisions = false;
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.isKinematic = true;
            }

            var instance = gameObject;
            var releaseInstance = _releaseInstance;
            try
            {
                Died?.Invoke(this);
            }
            finally
            {
                Died = null;
                _releaseInstance = null;
                releaseInstance(instance);
            }
            return true;
        }

        private void OnDestroy()
        {
            // Scene cleanup and external destruction are not gameplay deaths.
            _isAlive = false;
            Died = null;
            _releaseInstance = null;
        }
    }
}
