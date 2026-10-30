using UnityEngine;

namespace Core.Scripts.Game.ScriptableObjects.Configs.Animals
{
    [CreateAssetMenu(menuName = "Settings/Animals/Animal Definition", fileName = "AnimalDefinition")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private FoodRole _foodRole;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private AnimalMovementSettings _movement;

        public string Id => _id;
        public FoodRole FoodRole => _foodRole;
        public GameObject Prefab => _prefab;
        public AnimalMovementSettings Movement => _movement;
    }
}
