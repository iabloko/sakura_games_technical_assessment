using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.Services.AssetProviderService
{
    public sealed class AssetProvider : IAssetProvider, ILateTickable, IDisposable
    {
        private readonly IInstantiator _instantiator;
        private readonly Dictionary<GameObject, AssetLease<GameObject>> _instances = new();
        private readonly List<GameObject> _destroyedInstances = new();
        private bool _disposed;

        public AssetProvider(IInstantiator instantiator) => _instantiator = instantiator;

        public async UniTask<AssetLease<T>> LoadAsync<T>(string path,
            CancellationToken cancellationToken = default) where T : Object
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Asset address is required.", nameof(path));
            return await AwaitAssetAsync(Addressables.LoadAssetAsync<T>(path), cancellationToken);
        }

        internal async UniTask<AssetLease<T>> AwaitAssetAsync<T>(AsyncOperationHandle<T> handle,
            CancellationToken cancellationToken) where T : Object
        {
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken, autoReleaseWhenCanceled: false);
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                return new AssetLease<T>(handle);
            }
            catch
            {
                // A pending load finishes internally, but cannot create a late instance.
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null, bool dontDestroy = false)
            => InstantiatePrefab(prefab, parent, dontDestroy, null);

        private GameObject InstantiatePrefab(GameObject prefab, Transform parent, bool dontDestroy,
            AssetLease<GameObject> lease)
        {
            GameObject staging = null;
            GameObject instance = null;
            try
            {
                ThrowIfDisposed();
                if (prefab == null) throw new ArgumentNullException(nameof(prefab));

                // Keep a partially injected clone inactive and reachable if Zenject throws.
                staging = new GameObject("Prefab initialization");
                staging.SetActive(false);
                if (lease != null) _instances.Add(staging, lease);
                instance = _instantiator.InstantiatePrefab(prefab, staging.transform);
                if (lease != null)
                {
                    _instances.Add(instance, lease);
                    _instances.Remove(staging);
                }

                instance.transform.SetParent(parent, false);
                if (parent == null && !dontDestroy)
                    SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
                if (dontDestroy)
                {
                    instance.transform.SetParent(null, true);
                    Object.DontDestroyOnLoad(instance);
                }
                ThrowIfDisposed();
                return instance;
            }
            catch
            {
                if (instance != null) ReleaseInstance(instance);
                if (staging == null) lease?.Dispose();
                throw;
            }
            finally
            {
                DestroyObject(staging);
            }
        }

        public T InstantiateComponent<T>(T prefab, Transform parent = null, bool dontDestroy = false)
            where T : Component
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return Instantiate(prefab.gameObject, parent, dontDestroy).GetComponent<T>();
        }

        public async UniTask<GameObject> InstantiateObjectAsync(string path,
            CancellationToken cancellationToken = default, Transform parent = null, bool dontDestroy = false)
        {
            var hadParent = parent != null;
            var lease = await LoadAsync<GameObject>(path, cancellationToken);
            if (hadParent && parent == null)
            {
                lease.Dispose();
                throw new MissingReferenceException("The parent was destroyed while loading the prefab.");
            }
            return InstantiateLoadedPrefab(lease, cancellationToken, parent, dontDestroy);
        }

        internal GameObject InstantiateLoadedPrefab(AssetLease<GameObject> lease, CancellationToken cancellationToken,
            Transform parent = null, bool dontDestroy = false)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                lease.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
            var instance = InstantiatePrefab(lease.Asset, parent, dontDestroy, lease);
            // Injection/Awake may have cancelled the owner during instantiation.
            if (cancellationToken.IsCancellationRequested)
            {
                ReleaseInstance(instance);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return instance;
        }

        public async UniTask<T> InstantiateAsync<T>(string path, CancellationToken cancellationToken = default,
            Transform parent = null, bool dontDestroy = false) where T : Component
        {
            var instance = await InstantiateObjectAsync(path, cancellationToken, parent, dontDestroy);
            if (instance.TryGetComponent<T>(out var component)) return component;
            ReleaseInstance(instance);
            throw new MissingComponentException($"Prefab '{path}' has no {typeof(T).Name} component on its root.");
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            DestroyObject(instance);
        }

        private static void DestroyObject(GameObject instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Object.Destroy(instance);
            else Object.DestroyImmediate(instance);
        }

        public void LateTick()
        {
            // OnDestroy is not guaranteed for objects that were never active. Observing actual
            // destruction also handles inactive prefabs and scene unloading without unloading
            // dependencies before Unity has destroyed their consumers.
            foreach (var entry in _instances)
                if (entry.Key == null) _destroyedInstances.Add(entry.Key);

            foreach (var instance in _destroyedInstances)
            {
                _instances[instance].Dispose();
                _instances.Remove(instance);
            }
            _destroyedInstances.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // ProjectContext owns this provider; disposal happens on application shutdown.
            foreach (var entry in _instances)
            {
                ReleaseInstance(entry.Key);
                ReleaseAfterDestructionAsync(entry.Key, entry.Value).Forget(Debug.LogException);
            }
            _instances.Clear();
            _destroyedInstances.Clear();
        }

        private static async UniTask ReleaseAfterDestructionAsync(GameObject instance, AssetLease<GameObject> lease)
        {
            if (instance != null) await UniTask.WaitUntil(() => instance == null);
            lease.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AssetProvider));
        }
    }
}
