using System;
using System.Collections;
using System.Collections.Generic;
using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Gameplay.Animals.Movement;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.ScriptableObjects.Configs.Animals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    public sealed class AnimalMovementPlayModeTests : GamePlayModeTestFixture
    {

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterTest()
        {
            SceneContext.ParentContainers = null;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator LinearMovement_ReachesConfiguredSpeedAndKeepsIt()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            var body = world.CreateBody();
            var movement = new LinearMovementBehaviour(body, 2f, 10f);
            for (var step = 0; step < 50; step++) physics.Step(movement, body);
            Assert.That(body.linearVelocity.z, Is.EqualTo(2f).Within(0.01f));
            var position = body.position;
            for (var step = 0; step < 10; step++) physics.Step(movement, body);
            Assert.That(body.position.z - position.z, Is.EqualTo(0.4f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator JumpDistance_IsCorrectForWholeFractionalAndShortDurations()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            foreach (var duration in new[] { 0.3f, 0.35f, 0.01f })
            {
                var settings = world.JumpSettings(duration, 1f);
                var body = world.CreateBody();
                var visual = world.CreateVisual(body);
                var movement = new JumpMovementBehaviour(body, visual, settings);
                var maximumHeight = 0f;
                for (var step = 0; step < 30; step++)
                {
                    physics.Step(movement, body);
                    movement.UpdateVisual();
                    maximumHeight = Mathf.Max(maximumHeight, visual.localPosition.y);
                }
                Assert.That(body.position.z, Is.EqualTo(settings.Distance).Within(0.01f), $"duration={duration}");
                Assert.That(body.position.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.001f));
                Assert.That(visual.localPosition.y, Is.EqualTo(0f).Within(0.001f));
                if (duration > 0.02f) Assert.That(maximumHeight, Is.GreaterThan(0.4f));
            }
        }

        [UnityTest]
        public IEnumerator ConsecutiveJumps_DoNotApplyLandingAndLaunchImpulsesTwice()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            var settings = world.JumpSettings(0.3f, 0.3f);
            var body = world.CreateBody();
            var movement = new JumpMovementBehaviour(body, world.CreateVisual(body), settings);
            for (var step = 0; step < 30; step++) physics.Step(movement, body);
            Assert.That(body.position.z, Is.EqualTo(settings.Distance * 2f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator SharedJumpSettings_DoNotShareTimers()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            var settings = world.JumpSettings(0.3f, 1f);
            var first = world.CreateBody();
            var second = world.CreateBody();
            var firstMovement = new JumpMovementBehaviour(first, world.CreateVisual(first), settings);
            var secondMovement = new JumpMovementBehaviour(second, world.CreateVisual(second), settings);
            for (var step = 0; step < 15; step++) physics.Step(firstMovement, first);
            Assert.That(first.position.z, Is.EqualTo(2f).Within(0.01f));
            Assert.That(second.position.z, Is.Zero);
            firstMovement.FixedTick(Vector3.forward, 0.02f);
            secondMovement.FixedTick(Vector3.forward, 0.02f);
            physics.Simulate();
            Assert.That(first.position.z, Is.EqualTo(2f).Within(0.01f));
            Assert.That(second.position.z, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator JumpLanding_DoesNotEraseCollisionRecoil()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            var body = world.CreateBody();
            var movement = new JumpMovementBehaviour(body, world.CreateVisual(body), world.JumpSettings(0.3f, 1f));
            for (var step = 0; step < 5; step++) physics.Step(movement, body);
            movement.OnCollision();
            body.linearVelocity = Vector3.back * 3f;
            for (var step = 0; step < 11; step++) physics.Step(movement, body);
            Assert.That(body.linearVelocity.z, Is.LessThan(-2.5f));
        }

        [UnityTest]
        public IEnumerator MovingPrey_KeepPhysicalBounceAfterControllerUpdates()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            var definition = world.LinearPrey();
            var first = world.Create(definition, new Vector3(-1.5f, 0, 0), new InitialDirectionBounds(Vector3.right));
            var second = world.Create(definition, new Vector3(1.5f, 0, 0), new InitialDirectionBounds(Vector3.left));
            var fixedDeadline = Time.fixedTimeAsDouble + 3f;
            var wallDeadline = EditorApplication.timeSinceStartup + 10f;
            while (first.Body.linearVelocity.x >= -0.1f && Time.fixedTimeAsDouble < fixedDeadline &&
                   EditorApplication.timeSinceStartup < wallDeadline) yield return null;

            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(first.Body.linearVelocity.x, Is.LessThan(0));
            Assert.That(second.Body.linearVelocity.x, Is.GreaterThan(0));
            Assert.That(first.GetComponent<AnimalMovementController>().Direction.x, Is.LessThan(0));
            yield return AdvanceFixedTime(0.02f);
            Assert.That(first.Body.linearVelocity.x, Is.LessThan(0));
            Assert.That(second.Body.linearVelocity.x, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator BothMovementTypes_ReturnIntoCameraViewport()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            var camera = world.CreateCamera();
            var bounds = new CameraMovementBounds(camera);
            var frog = world.Create(world.Definition("Frog"), new Vector3(6, 0, 2), bounds);
            var snake = world.Create(world.Definition("Snake"), new Vector3(-6, 0, -2), bounds);
            var fixedDeadline = Time.fixedTimeAsDouble + 3f;
            var wallDeadline = EditorApplication.timeSinceStartup + 10f;
            while ((bounds.TryGetReturnDirection(frog.Body.position, out _) ||
                    bounds.TryGetReturnDirection(snake.Body.position, out _)) &&
                   Time.fixedTimeAsDouble < fixedDeadline && EditorApplication.timeSinceStartup < wallDeadline)
                yield return null;

            Assert.That(frog.IsAlive && snake.IsAlive, Is.True);
            var frogViewport = camera.WorldToViewportPoint(frog.Body.position);
            var snakeViewport = camera.WorldToViewportPoint(snake.Body.position);
            Assert.That(frogViewport.x, Is.InRange(0.05f, 0.95f));
            Assert.That(frogViewport.y, Is.InRange(0.05f, 0.95f));
            Assert.That(snakeViewport.x, Is.InRange(0.05f, 0.95f));
            Assert.That(snakeViewport.y, Is.InRange(0.05f, 0.95f));
        }

        [UnityTest]
        public IEnumerator DisablingMovement_ResetsVisualAndReenablingStartsFreshJump()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            var frog = world.Create(world.Definition("Frog"), Vector3.zero);
            var controller = frog.GetComponent<AnimalMovementController>();
            yield return AdvanceFixedTime(0.08f);
            yield return null;
            Assert.That(frog.Visual.localPosition.y, Is.GreaterThan(0));
            controller.enabled = false;
            Assert.That(frog.Visual.localPosition.y, Is.EqualTo(0f).Within(0.001f));
            for (var step = 0; step < 5; step++) yield return null;
            Assert.That(frog.Visual.localPosition.y, Is.EqualTo(0f).Within(0.001f));
            controller.enabled = true;
            yield return AdvanceFixedTime(0.06f);
            yield return null;
            Assert.That(frog.Visual.localPosition.y, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Wandering_ChoosesNewHeadingAfterConfiguredInterval()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var random = new RandomScope();
            using var world = new MovementWorld();
            var definition = world.LinearPrey();
            var serialized = new SerializedObject(definition.Movement);
            serialized.FindProperty("_directionChangeInterval").floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var animal = world.Create(definition, Vector3.zero);
            var controller = animal.GetComponent<AnimalMovementController>();
            var initial = controller.Direction;
            yield return AdvanceFixedTime(0.3f);
            Assert.That(Vector3.Distance(initial, controller.Direction), Is.GreaterThan(0.001f));
            Assert.That(controller.Direction.y, Is.Zero);
            Assert.That(controller.Direction.sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SnakeColliderAndVisualFollowTheSameHeading()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            var snake = world.Create(world.Definition("Snake"), Vector3.zero, new InitialDirectionBounds(Vector3.right));
            yield return AdvanceFixedTime(0.2f);
            Assert.That(Vector3.Dot(snake.transform.forward, Vector3.right), Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(snake.Visual.forward, snake.transform.forward), Is.GreaterThan(0.99f));
            var frog = world.Create(world.Definition("Frog"), snake.Body.position + Vector3.right * .8f);
            frog.GetComponent<AnimalMovementController>().enabled = false;
            frog.Body.isKinematic = true;
            Physics.SyncTransforms();
            var capsule = snake.GetComponent<CapsuleCollider>();
            var sphere = frog.GetComponent<SphereCollider>();
            Assert.That(Physics.ComputePenetration(capsule, snake.Body.position, snake.Body.rotation,
                sphere, frog.transform.position, frog.transform.rotation, out _, out _), Is.True,
                "A prey touching the visible head must intersect the snake's physics shape.");
            frog.transform.position = snake.Body.position + Vector3.forward * 1.1f;
            Assert.That(Physics.ComputePenetration(capsule, snake.Body.position, snake.Body.rotation,
                sphere, frog.transform.position, frog.transform.rotation, out _, out _), Is.False,
                "The snake must not eat prey separated from its visible side.");
        }

        [UnityTest]
        public IEnumerator JumpHasVisibleScaleAndStopRestoresAuthoredScale()
        {
            yield return new EnterPlayMode();
            yield return StartWithStoppedProjectContext();
            using var world = new MovementWorld();
            using var physics = new ManualPhysics();
            var body = world.CreateBody();
            var visual = world.CreateVisual(body);
            var restScale = new Vector3(.8f, 1f, .7f);
            visual.localScale = restScale;
            var movement = new JumpMovementBehaviour(body, visual, world.JumpSettings(.3f, 1f));
            for (var step = 0; step < 8; step++) physics.Step(movement, body);
            movement.UpdateVisual();
            Assert.That(visual.localScale.x, Is.GreaterThan(restScale.x));
            Assert.That(visual.localScale.z, Is.GreaterThan(restScale.z));
            Assert.That(body.position.y, Is.EqualTo(0f).Within(.001f));
            movement.Stop();
            Assert.That(visual.localScale, Is.EqualTo(restScale));
            Assert.That(visual.localPosition, Is.EqualTo(Vector3.zero));
        }

        private static IEnumerator AdvanceFixedTime(float duration)
        {
            var target = Time.fixedTimeAsDouble + duration;
            var deadline = EditorApplication.timeSinceStartup + 10f;
            while (Time.fixedTimeAsDouble < target && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(Time.fixedTimeAsDouble, Is.GreaterThanOrEqualTo(target), "Physics did not advance.");
        }

        private sealed class MovementWorld : IDisposable
        {
            private readonly List<Object> _owned = new();
            private readonly AssetProvider _assets = new(new DiContainer());
            private readonly AnimalFactory _factory;

            public MovementWorld() =>
                _factory = new AnimalFactory(_assets, new AnimalInteractionResolver(), new AnimalMovementFactory());

            public AnimalDefinition Definition(string species) => AssetDatabase.LoadAssetAtPath<AnimalDefinition>(
                $"Assets/Core/Settings/Configs/Animals/{species}.asset");

            public Animal Create(AnimalDefinition definition, Vector3 position, IAnimalMovementBounds bounds = null)
            {
                var animal = _factory.Create(definition, position, movementBounds: bounds);
                Own(animal.gameObject);
                return animal;
            }

            public Rigidbody CreateBody()
            {
                var body = Own(new GameObject("Movement test body")).AddComponent<Rigidbody>();
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                return body;
            }

            public Transform CreateVisual(Rigidbody body)
            {
                var visual = new GameObject("Visual").transform;
                visual.SetParent(body.transform, false);
                return visual;
            }

            public JumpMovementSettings JumpSettings(float duration, float interval)
            {
                var settings = Own(Object.Instantiate((JumpMovementSettings)Definition("Frog").Movement));
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_duration").floatValue = duration;
                serialized.FindProperty("_interval").floatValue = interval;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return settings;
            }

            public AnimalDefinition LinearPrey()
            {
                var definition = Own(Object.Instantiate(Definition("Frog")));
                var movement = Own(Object.Instantiate((LinearMovementSettings)Definition("Snake").Movement));
                var serializedMovement = new SerializedObject(movement);
                serializedMovement.FindProperty("_directionChangeInterval").floatValue = 1000f;
                serializedMovement.FindProperty("_speed").floatValue = 3f;
                serializedMovement.ApplyModifiedPropertiesWithoutUndo();
                var serializedDefinition = new SerializedObject(definition);
                serializedDefinition.FindProperty("_movement").objectReferenceValue = movement;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
                return definition;
            }

            public Camera CreateCamera()
            {
                var camera = Own(new GameObject("Movement test camera")).AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 1f;
                camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
                return camera;
            }

            private T Own<T>(T item) where T : Object
            {
                _owned.Add(item);
                return item;
            }

            public void Dispose()
            {
                foreach (var item in _owned)
                    if (item != null) Object.Destroy(item);
                _assets.Dispose();
            }
        }

        private sealed class ManualPhysics : IDisposable
        {
            private readonly SimulationMode _previous = Physics.simulationMode;

            public ManualPhysics() => Physics.simulationMode = SimulationMode.Script;
            public void Step(AnimalMovementBehaviour movement, Rigidbody body)
            {
                movement.FixedTick(Vector3.forward, 0.02f);
                Simulate();
            }
            public void Simulate() => Physics.Simulate(0.02f);
            public void Dispose() => Physics.simulationMode = _previous;
        }

        private sealed class InitialDirectionBounds : IAnimalMovementBounds
        {
            private readonly Vector3 _direction;
            private bool _used;
            public InitialDirectionBounds(Vector3 direction) => _direction = direction;
            public bool TryGetReturnDirection(Vector3 position, out Vector3 direction)
            {
                direction = _direction;
                if (_used) return false;
                _used = true;
                return true;
            }
        }

        private sealed class RandomScope : IDisposable
        {
            private readonly UnityEngine.Random.State _previous = UnityEngine.Random.state;
            public RandomScope() => UnityEngine.Random.InitState(12345);
            public void Dispose() => UnityEngine.Random.state = _previous;
        }
    }
}
