#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs
{
    public sealed partial class GameConfig
    {
        [MenuItem("GameMenu/SelectGameConfig")]
        private static void SelectGameConfig()
        {
            const string assetPath = "Assets/Core/Settings/Configs/GameConfig.asset";

            try
            {
                GameConfig asset = AssetDatabase.LoadAssetAtPath<GameConfig>(assetPath);
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not find Game Config Error: {e.Message}");
                throw;
            }
        }
    }
}
#endif