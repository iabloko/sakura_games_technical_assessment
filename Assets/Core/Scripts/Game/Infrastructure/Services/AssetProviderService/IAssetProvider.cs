using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Scripts.Game.Infrastructure.Services.AssetProviderService
{
    public interface IAssetProvider
    {
        // Keep the lease alive until every consumer of the asset has finished.
        UniTask<AssetLease<T>> LoadAsync<T>(string path, CancellationToken cancellationToken = default)
            where T : Object;

        // Direct prefab references are owned by the caller, not Addressables.
        GameObject Instantiate(GameObject prefab, Transform parent = null, bool dontDestroy = false);
        T InstantiateComponent<T>(T prefab, Transform parent = null, bool dontDestroy = false)
            where T : Component;

        // The provider retains the loaded prefab until the instance has actually been destroyed.
        UniTask<GameObject> InstantiateObjectAsync(string path, CancellationToken cancellationToken = default,
            Transform parent = null, bool dontDestroy = false);
        UniTask<T> InstantiateAsync<T>(string path, CancellationToken cancellationToken = default,
            Transform parent = null, bool dontDestroy = false) where T : Component;

        void ReleaseInstance(GameObject instance);
    }
}
