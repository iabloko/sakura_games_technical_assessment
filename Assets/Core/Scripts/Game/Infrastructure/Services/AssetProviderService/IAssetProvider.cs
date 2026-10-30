using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Scripts.Game.Infrastructure.Services.AssetProviderService
{
    public interface IAssetProvider
    {
        void ReleaseObject(Object obj);
        void ReleaseInstance(GameObject obj);

        public GameObject Instantiate(GameObject creatableObject, Transform parent = null, bool dontDestroy = false);

        public GameObject Instantiate(string path, Transform parent, bool dontDestroy, bool instantiateInWorldSpace,
            bool trackHandle);

        public T InstantiateComponent<T>(T reference, Transform parent = null, bool dontDestroy = false)
            where T : Component;

        public T InstantiateObject<T>(string path, Transform parent = null, bool dontDestroy = false,
            bool instantiateInWorldSpace = true, bool trackHandle = true) where T : MonoBehaviour;

        public T Load<T>(string path) where T : Object;

        public UniTask<GameObject> InstantiateObjectAsync(GameObject creatableObject,
            CancellationTokenSource cancellationTokenSource, Transform parent = null,
            bool dontDestroy = false, bool instantiateInWorldSpace = true, bool trackHandle = true);

        public UniTask<GameObject> InstantiateObjectAsync(string path, CancellationTokenSource cancellationTokenSource,
            Transform parent = null, bool dontDestroy = false, bool instantiateInWorldSpace = true,
            bool trackHandle = true);

        public UniTask<T> InstantiateAsync<T>(string path, CancellationTokenSource cancellationTokenSource,
            Transform parent = null, bool dontDestroy = false, bool instantiateInWorldSpace = true,
            bool trackHandle = true) where T : MonoBehaviour;

        public UniTask<T> LoadAsync<T>(string path, CancellationTokenSource cancellationTokenSource) where T : Object;
    }
}