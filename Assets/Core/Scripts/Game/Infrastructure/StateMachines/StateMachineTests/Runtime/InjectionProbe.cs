using UnityEngine;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class InjectionDependency { }

    public sealed class InjectionProbe : MonoBehaviour
    {
        [Inject] public InjectionDependency Dependency { get; private set; }
    }
}
