using System;
using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Spawning;
using Core.Scripts.Game.Gameplay.Animals.Statistics;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Scripts.Game.Gameplay.UI
{
    [DisallowMultipleComponent]
    public sealed class AnimalHudController : MonoBehaviour
    {
        [SerializeField] private AnimalSpawnController _source;
        [SerializeField] private Camera _camera;
        [SerializeField] private Text _deadPrey;
        [SerializeField] private Text _deadPredators;
        [SerializeField] private RectTransform _labelRoot;
        [SerializeField] private Text _tastyTemplate;
        [SerializeField, Min(0.01f)] private float _labelDuration = 1f;
        [SerializeField, Min(0f)] private float _labelGap = 8f;

        private readonly HashSet<Animal> _predators = new();
        private readonly Dictionary<Animal, MealLabel> _labels = new();
        private readonly List<Animal> _expired = new();

        public AnimalDeathStatistics Statistics { get; private set; }

        private void Awake()
        {
            if (_source == null || _camera == null || _deadPrey == null || _deadPredators == null ||
                _labelRoot == null || _tastyTemplate == null)
                throw new InvalidOperationException("Animal HUD references are incomplete.");

            Statistics = new AnimalDeathStatistics();
            Statistics.Changed += RefreshCounters;
            _source.Spawned += ObserveAnimal;
            RefreshCounters();
        }

        public void ObserveAnimal(Animal animal)
        {
            Statistics.Observe(animal);
            if (animal == null || !animal.IsAlive || animal.Definition.FoodRole != FoodRole.Predator ||
                !_predators.Add(animal)) return;
            animal.Ate += OnAte;
            animal.Removed += OnRemoved;
        }

        private void OnAte(Animal eater)
        {
            if (!isActiveAndEnabled || eater == null || !eater.IsAlive) return;
            if (!_labels.TryGetValue(eater, out var label))
            {
                var text = Instantiate(_tastyTemplate, _labelRoot);
                text.text = "Tasty!";
                text.gameObject.SetActive(true);
                var renderers = eater.Visual != null ? eater.Visual.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
                label = new MealLabel(text, renderers);
                _labels.Add(eater, label);
            }
            label.Remaining = _labelDuration;
            label.Text.color = _tastyTemplate.color;
            PositionLabel(eater, label);
        }

        private void LateUpdate()
        {
            foreach (var entry in _labels)
            {
                var label = entry.Value;
                label.Remaining -= Time.deltaTime;
                if (entry.Key == null || !entry.Key.IsAlive || label.Remaining <= 0f || _camera == null)
                {
                    _expired.Add(entry.Key);
                    continue;
                }
                PositionLabel(entry.Key, label);
                var color = label.Text.color;
                color.a = _tastyTemplate.color.a * Mathf.Clamp01(label.Remaining / _labelDuration);
                label.Text.color = color;
            }
            foreach (var animal in _expired) RemoveLabel(animal);
            _expired.Clear();
        }

        private void PositionLabel(Animal animal, MealLabel label)
        {
            if (_camera == null) return;
            var screen = _camera.WorldToScreenPoint(animal.transform.position);
            label.Text.enabled = screen.z > 0f && animal.gameObject.activeInHierarchy;
            if (!label.Text.enabled) return;

            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in label.Renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!hasBounds) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                hasBounds = true;
            }
            if (hasBounds)
            {
                // Project the visible model bounds so the caption sits below the animal at any camera angle.
                for (var corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                        (corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                        (corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                    var point = _camera.WorldToScreenPoint(bounds.center + offset);
                    if (point.z > 0f) screen.y = Mathf.Min(screen.y, point.y);
                }
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_labelRoot, screen, null, out var local);
            label.Text.rectTransform.anchoredPosition =
                local - Vector2.up * (label.Text.rectTransform.rect.height * 0.5f + _labelGap);
        }

        private void RefreshCounters()
        {
            if (Statistics == null || !isActiveAndEnabled) return;
            _deadPrey.text = $"Dead prey: {Statistics.DeadPrey}";
            _deadPredators.text = $"Dead predators: {Statistics.DeadPredators}";
        }

        private void OnRemoved(Animal animal)
        {
            _predators.Remove(animal);
            animal.Ate -= OnAte;
            animal.Removed -= OnRemoved;
            RemoveLabel(animal);
        }

        private void RemoveLabel(Animal animal)
        {
            if (!_labels.TryGetValue(animal, out var label)) return;
            _labels.Remove(animal);
            if (label.Text == null) return;
            label.Text.gameObject.SetActive(false);
            Destroy(label.Text.gameObject);
        }

        private void ClearLabels()
        {
            foreach (var label in _labels.Values)
            {
                if (label.Text == null) continue;
                label.Text.gameObject.SetActive(false);
                Destroy(label.Text.gameObject);
            }
            _labels.Clear();
            _expired.Clear();
        }

        private void OnEnable() => RefreshCounters();
        private void OnDisable() => ClearLabels();

        private void OnDestroy()
        {
            if (_source != null) _source.Spawned -= ObserveAnimal;
            Statistics?.Dispose();
            ClearObservers();
        }

        public void ResetSession()
        {
            if (Statistics == null) throw new InvalidOperationException("The animal HUD has not been initialized.");
            Statistics.Dispose();
            ClearObservers();
            Statistics = new AnimalDeathStatistics();
            Statistics.Changed += RefreshCounters;
            RefreshCounters();
        }

        private void ClearObservers()
        {
            foreach (var animal in _predators)
            {
                if (animal == null) continue;
                animal.Ate -= OnAte;
                animal.Removed -= OnRemoved;
            }
            _predators.Clear();
            ClearLabels();
        }

        private sealed class MealLabel
        {
            public readonly Text Text;
            public readonly Renderer[] Renderers;
            public float Remaining;

            public MealLabel(Text text, Renderer[] renderers)
            {
                Text = text;
                Renderers = renderers;
            }
        }
    }
}
