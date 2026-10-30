using Core.Scripts.Game.ScriptableObjects.Settings;
using Sirenix.Serialization;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Game.Installers
{
    [CreateAssetMenu(fileName = "GameGlobalSettingsInstaller", menuName = "Isekai/Installers/GameGlobalSettingsInstaller")]
    public sealed class GameGlobalSettingsInstaller : ScriptableObjectInstaller<GameGlobalSettingsInstaller>
    {
        [OdinSerialize] public GameGlobalSettings gameGlobalSettings;

        public override void InstallBindings()
        {
            gameGlobalSettings.ApplyGameGlobalSettings();
            Container.BindInstance(gameGlobalSettings).AsSingle();
        }
    }
}