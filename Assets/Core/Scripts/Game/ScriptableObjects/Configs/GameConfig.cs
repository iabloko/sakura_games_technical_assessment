using System;
using System.Collections.Generic;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs
{
    [CreateAssetMenu(menuName = "Settings/Configs/GameConfig", fileName = "GameConfig")]
    public sealed partial class GameConfig : ScriptableObject
    {
        [SerializeField] private AnimalDefinition[] _animals = Array.Empty<AnimalDefinition>();
        [SerializeField, Min(0.01f)] private float _minSpawnInterval = 1f;
        [SerializeField, Min(0.01f)] private float _maxSpawnInterval = 2f;

        public IReadOnlyList<AnimalDefinition> Animals => _animals;
        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;

        private void OnValidate()
        {
            _minSpawnInterval = Mathf.Max(0.01f, _minSpawnInterval);
            _maxSpawnInterval = Mathf.Max(_minSpawnInterval, _maxSpawnInterval);
        }
    }
}
