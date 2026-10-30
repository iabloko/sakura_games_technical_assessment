using Sirenix.OdinInspector;

namespace Core.Scripts.Game.ScriptableObjects.Settings
{
    public enum VsyncEnum
    {
        Yes,
        No,
    }

    [System.Serializable, HideLabel]
    public struct GameGlobalSettingsData
    {
        public VsyncEnum verticalSync;

        [ShowIf("verticalSync", VsyncEnum.No)] public int fpsLock;
    }
}