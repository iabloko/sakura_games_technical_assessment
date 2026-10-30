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

        public IReadOnlyList<AnimalDefinition> Animals => _animals;
    }
}
