using System;
using System.Collections.Generic;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs
{
    [CreateAssetMenu(menuName = "Settings/Configs/GameConfig", fileName = "GameConfig")]
    public sealed partial class GameConfig : ScriptableObject
    {
        [SerializeField, MinMaxSlider(0.01f, 10, true)]
        private Vector2 _minMaxInterval;

        [SerializeField] private AnimalDefinition[] _animals = Array.Empty<AnimalDefinition>();

        public IReadOnlyList<AnimalDefinition> Animals => _animals;
        public float MinSpawnInterval => _minMaxInterval.x;
        public float MaxSpawnInterval => _minMaxInterval.y;

        private void OnValidate()
        {
            _minMaxInterval.x = Mathf.Max(0.01f, _minMaxInterval.x);
            _minMaxInterval.y = Mathf.Max(_minMaxInterval.x, _minMaxInterval.y);
        }
    }
}