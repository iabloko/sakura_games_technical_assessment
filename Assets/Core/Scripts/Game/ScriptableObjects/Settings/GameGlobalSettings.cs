using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Settings
{
    [CreateAssetMenu(menuName = "Settings/Configs/GameGlobalSettings", fileName = "GameGlobalSettings")]
    public sealed class GameGlobalSettings : ScriptableObject
    {
        [Title("GAME", "Settings", TitleAlignments.Right)]
        public GameGlobalSettingsData settings;

        public void ApplyGameGlobalSettings()
        {
            QualitySettings.vSyncCount = (int)settings.verticalSync;

            int targetFrameRate = settings.verticalSync.Equals(VsyncEnum.Yes) ? 0 : settings.fpsLock;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}