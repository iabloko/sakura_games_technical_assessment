# Zoo World

Unity 6000.6.5f1. The current implementation prepares infrastructure; animal gameplay is the next stage.

## Startup

Build Settings start with `Initialization`, followed by `Game`. Zenject's persistent `ProjectContext`
installs services and starts `BootStrapState -> LoadLevelState -> GamePlayState`.
Starting `Game` directly in the Editor also works: `SceneLoader` skips an already active scene.
The orthographic camera looks down onto the XZ plane from `(0, 10, 0)`.

`SceneLoader.LoadAsync` returns `UniTask`, validates scene availability and rejects overlapping loads.
Unity cannot abort an ongoing native scene load: cancellation suppresses the continuation after that
load completes. Leaving `LoadLevelState` cancels its continuation, so an obsolete operation cannot
enter Gameplay. Loading errors are logged at the state boundary and do not advance the state.

## Assets and instances

`IAssetProvider.LoadAsync<T>(address, token)` returns an `AssetLease<T>`. The caller owns this lease
and disposes it after all consumers of `lease.Asset` have finished. Each load has its own ownership;
disposing the same lease twice is safe. Errors and cancellation propagate to the caller.

`InstantiateObjectAsync(address, token)` loads a prefab, creates it through Zenject and retains its
lease until the resulting object is actually destroyed. `InstantiateAsync<T>` additionally requires
a component on the prefab root. Missing components cause cleanup and a `MissingComponentException`.

`Instantiate(prefab)` and `InstantiateComponent(prefabComponent)` create directly referenced prefabs
through Zenject. They do not create Addressables ownership. For a prefab obtained from a manual
`LoadAsync`, its caller must keep the lease alive while clones exist.

Use `ReleaseInstance(instance)` to deactivate and destroy a created object. Destroying it directly
or unloading its scene is also supported. A Zenject late tick releases retained prefab leases after
destruction, including objects that were never active. Ordinary root instances belong to the active
scene; only an explicit `dontDestroy` request makes them persistent.

The old synchronous address-loading methods, ignored `CancellationTokenSource` parameters and
`trackHandle` switches have been removed. There were no live callers of those methods.

## Checks

Run `Template.StateMachine.Tests` in Unity Test Runner / EditMode. Tests cover dependency injection,
lease disposal, cancelled and failed loading, inactive-instance cleanup and scene-loading guards.
For a startup smoke check, enter Play Mode from `Initialization` and then directly from `Game`:
both should reach Gameplay once, with `Game` active and no duplicate scene loading.
