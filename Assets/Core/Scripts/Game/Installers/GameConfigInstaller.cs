using Core.Scripts.Game.ScriptableObjects.Configs;
using Sirenix.Serialization;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Game.Installers
{
    [CreateAssetMenu(fileName = "GameConfigInstaller", menuName = "Isekai/Installers/GameConfigInstaller")]
    public sealed class GameConfigInstaller : ScriptableObjectInstaller<GameConfigInstaller>
    {
        [OdinSerialize] public GameConfig gameConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(gameConfig).AsSingle();
        }
    }
}