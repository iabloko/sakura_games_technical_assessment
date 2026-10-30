using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.Services.AssetProviderService
{
    public sealed class AssetProvider : IAssetProvider
    {
        #region ASYNCHRONOUSLY

        async UniTask<T> IAssetProvider.InstantiateAsync<T>(string path,
            CancellationTokenSource cancellationTokenSource, Transform parent, bool dontDestroy,
            bool instantiateInWorldSpace, bool trackHandle)
        {
            T createdObject;

            try
            {
                createdObject =
                    (await Addressables.InstantiateAsync(path, parent, instantiateInWorldSpace, trackHandle))
                    .GetComponent<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"{path}");
                Debug.LogError($"{e.Message}");
                return default;
            }

            if (dontDestroy) Object.DontDestroyOnLoad(createdObject);

            return createdObject;
        }

        async UniTask<GameObject> IAssetProvider.InstantiateObjectAsync(GameObject creatableObject,
            CancellationTokenSource cancellationTokenSource, Transform parent,
            bool dontDestroy, bool instantiateInWorldSpace, bool trackHandle)
        {
            GameObject createdObject;
            try
            {
                createdObject =
                    (await Addressables.InstantiateAsync(creatableObject, parent, instantiateInWorldSpace, trackHandle))
                    .gameObject;
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}");
                return default;
            }

            if (dontDestroy) Object.DontDestroyOnLoad(createdObject);

            return createdObject;
        }

        async UniTask<GameObject> IAssetProvider.InstantiateObjectAsync(string path,
            CancellationTokenSource cancellationTokenSource, Transform parent, bool dontDestroy,
            bool instantiateInWorldSpace, bool trackHandle)
        {
            GameObject createdObject = null;

            try
            {
                createdObject =
                    (await Addressables.InstantiateAsync(path, parent, instantiateInWorldSpace, trackHandle));
            }
            catch (Exception e)
            {
                Debug.LogError($"{path}");
                Debug.LogError($"{e.Message}");
                return null;
            }

            if (dontDestroy) Object.DontDestroyOnLoad(createdObject);

            return createdObject;
        }

        async UniTask<T> IAssetProvider.LoadAsync<T>(string path, CancellationTokenSource cancellationTokenSource)
            => (await Addressables.LoadAssetAsync<T>(path));

        #endregion

        #region SYNCHRONOUSLY

        GameObject IAssetProvider.Instantiate(GameObject creatableObject, Transform parent, bool dontDestroy)
        {
            GameObject create = Object.Instantiate(creatableObject, parent);
            if (dontDestroy) Object.DontDestroyOnLoad(create);
            return create;
        }

        public T InstantiateComponent<T>(T reference, Transform parent = null, bool dontDestroy = false)
            where T : Component
        {
            T create = Object.Instantiate(reference, parent);
            if (dontDestroy) Object.DontDestroyOnLoad(create);
            return create;
        }

        GameObject IAssetProvider.Instantiate(string path, Transform parent, bool dontDestroy,
            bool instantiateInWorldSpace, bool trackHandle)
        {
            GameObject createdObject = null;

            try
            {
                createdObject = Addressables
                    .InstantiateAsync(path, parent, instantiateInWorldSpace, trackHandle)
                    .WaitForCompletion();
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}");
                return null;
            }

            if (dontDestroy) Object.DontDestroyOnLoad(createdObject);

            return createdObject;
        }

        T IAssetProvider.InstantiateObject<T>(string path, Transform parent, bool dontDestroy,
            bool instantiateInWorldSpace, bool trackHandle)
        {
            T createdObject = null;

            try
            {
                createdObject = Addressables
                    .InstantiateAsync(path, parent, instantiateInWorldSpace, trackHandle)
                    .WaitForCompletion()
                    .GetComponent<T>();
                createdObject.name = typeof(T).Name;
            }
            catch (Exception e)
            {
                Debug.LogError($"{path}");
                Debug.LogError($"{e.Message}");
                return null;
            }

            if (dontDestroy) Object.DontDestroyOnLoad(createdObject);

            return createdObject;
        }

        T IAssetProvider.Load<T>(string path)
            => Addressables.LoadAssetAsync<T>(path).WaitForCompletion();

        #endregion

        void IAssetProvider.ReleaseObject(Object obj) => Addressables.Release(obj);
        void IAssetProvider.ReleaseInstance(GameObject obj) => Addressables.ReleaseInstance(obj);
    }
}