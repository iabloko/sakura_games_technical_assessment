# Zoo World

Unity **6000.6.5f1**, URP 17.6.0. Open `Initialization` and enter Play Mode. Starting from `Game` is also supported. Both scenes must remain enabled in Build Settings, with `Initialization` first.

## Gameplay

One randomly selected animal spawns every 1–2 seconds. Frogs are prey and jump a fixed distance at a configured interval; snakes are predators and move continuously. Prey bounce through Unity physics. A predator eats prey; when two predators meet, the older instance survives. Each confirmed death updates the uGUI counters, and each meal displays `Tasty!` below its predator.

Animals can leave the camera view and steer back, as requested in the brief. The camera boundary is a return direction, not a wall. A frog completes its current jump before using the next heading.

## Structure

`ProjectContext` installs shared services through Zenject. Startup follows `BootStrapState → LoadLevelState → WorldGenerationState → GamePlayState`. World preparation validates scene references and the animal catalog. Gameplay starts spawning; leaving the state stops spawning, releases animals without counting cleanup as deaths, and resets the HUD.

`AnimalFactory` creates inactive instances, initializes movement and collision components, then activates them. Food rules belong to `AnimalInteractionResolver`. Movement behaviours and their timers belong to individual animals; ScriptableObjects contain shared settings only. The Rigidbody owns the heading of both the collider and visual. Frog jump scale and a ground shadow make its arc readable in the orthographic top-down view.

Animal and pond artwork uses editable Shapes components. UI uses uGUI. Direct prefab references are intentional; the asset provider also supports owned Addressables leases for asynchronous assets.

## Adding an animal

1. Create a prefab with an `Animal`, dynamic `Rigidbody`, solid collider, `AnimalMovementController` and `AnimalCollisionHandler` on its root. Keep motion on XZ and use local +Z as the forward direction.
2. Assign a separate child to `Animal.Visual`; place decorative Shapes under that child. Ground shadows belong outside it. Keep the visible body reasonably aligned with its collider.
3. Create an `AnimalDefinition` with a unique ID, food role, prefab and movement settings. Existing linear or jumping settings can be shared across species.
4. Add the definition to `GameConfig.Animals`. A new species using existing movement requires no new species enum or food-rule branch. A new movement algorithm needs a settings type, behaviour and registration in `AnimalMovementFactory`.

Spawn intervals are configured in `GameConfig`; movement parameters live in the corresponding movement settings assets.

## Tests and builds

Run `Template.StateMachine.Tests` in Unity Test Runner under **EditMode**. Coroutine tests enter Play Mode explicitly, then call shared startup preparation. That preparation first verifies the real project installers and startup, then stops the session and clears scenes. Tests needing different services use child containers passed to `SceneContext`; they do not replace bindings in the global ProjectContext. Each fixture's teardown exits Play Mode and clears pending scene-container handoffs.

For command-line execution, use the matching Unity editor:

```text
Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode -testFilter Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests -testResults <results.xml> -logFile <editor.log>
```

Build a Windows x64 player from Build Settings with both startup scenes enabled. Check spawning, predation, counters, meal labels and camera return at landscape, 4:3 and portrait resolutions. Death counters reset when starting a new session; animal IDs increase throughout the factory's lifetime.
