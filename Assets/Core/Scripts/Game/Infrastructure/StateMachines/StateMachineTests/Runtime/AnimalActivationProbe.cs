using Core.Scripts.Game.Gameplay.Animals;
using UnityEngine;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    [ExecuteAlways]
    public sealed class AnimalActivationProbe : MonoBehaviour
    {
        public bool InitializedOnEnable { get; private set; }

        private void OnEnable()
        {
            var animal = GetComponent<Animal>();
            InitializedOnEnable = animal != null && animal.IsInitialized && animal.IsAlive;
        }
    }
}
