using System;
using System.Collections;
using System.Threading;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Core.Scripts.Game.Infrastructure.StateMachines.StateMachineTests
{
    [Timeout(10000)]
    public sealed class AssetProviderTests
    {
        private AssetProvider _provider;
        private GameObject _prefab;
        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            var container = new DiContainer();
            container.BindInstance(new InjectionDependency());
            _provider = new AssetProvider(container);
            _prefab = new GameObject("Test prefab");
            _prefab.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            _provider.LateTick();
            _provider.Dispose();
            if (_prefab != null) Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void Instantiate_InjectsInactivePrefab()
        {
            _prefab.AddComponent<InjectionProbe>();
            _instance = _provider.Instantiate(_prefab);
            Assert.That(_instance.GetComponent<InjectionProbe>().Dependency, Is.Not.Null);
            Assert.That(_instance.activeSelf, Is.False);
        }

        [Test]
        public void FailedInjection_DestroysPartialCloneAndReleasesItsPrefab()
        {
            _provider.Dispose();
            _provider = new AssetProvider(new DiContainer());
            _prefab.AddComponent<InjectionProbe>();
            var handle = Addressables.ResourceManager.CreateCompletedOperation(_prefab, (string)null);

            Assert.Throws<ZenjectException>(() =>
                _provider.InstantiateLoadedPrefab(new AssetLease<GameObject>(handle), default));
            _provider.LateTick();

            Assert.That(handle.IsValid(), Is.False);
            Assert.That(_prefab != null, Is.True);
        }

        [Test]
        public void Lease_DisposeReleasesHandleOnceAndRejectsFurtherAccess()
        {
            var handle = Addressables.ResourceManager.CreateCompletedOperation(_prefab, (string)null);
            var lease = new AssetLease<GameObject>(handle);
            Assert.That(lease.Asset, Is.SameAs(_prefab));

            lease.Dispose();
            lease.Dispose();

            Assert.That(handle.IsValid(), Is.False);
            Assert.Throws<ObjectDisposedException>(() => _ = lease.Asset);
        }

        [Test]
        public void DestroyingNeverActiveInstance_ReleasesItsLoadedPrefab()
        {
            var handle = Addressables.ResourceManager.CreateCompletedOperation(_prefab, (string)null);
            _instance = _provider.InstantiateLoadedPrefab(new AssetLease<GameObject>(handle), default);
            Assert.That(handle.IsValid(), Is.True);
            Object.DestroyImmediate(_instance);

            _provider.LateTick();

            Assert.That(handle.IsValid(), Is.False);
        }

        [UnityTest]
        public IEnumerator CancelledLoad_ReleasesOwnershipBeforeLateCompletion() => UniTask.ToCoroutine(async () =>
        {
            using var cancellation = new CancellationTokenSource();
            var operation = new DeferredPrefabOperation();
            var handle = Addressables.ResourceManager.StartOperation(operation, default(AsyncOperationHandle));
            // Keep one observer reference to check that the provider releases exactly its own.
            _ = Addressables.ResourceManager.Acquire(handle);
            var referencesBeforeAwait = operation.CurrentReferenceCount;
            var task = _provider.AwaitAssetAsync(handle, cancellation.Token);
            cancellation.Cancel();
            var cancelled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
            Assert.That(operation.CurrentReferenceCount, Is.EqualTo(referencesBeforeAwait - 1));

            operation.Finish(_prefab);
            Addressables.Release(handle);
            Assert.That(operation.WasDestroyed, Is.True);
            Assert.That(handle.IsValid(), Is.False);
        });

        [UnityTest]
        public IEnumerator AlreadyCancelledRequest_DoesNotStartAddressablesLoad() => UniTask.ToCoroutine(async () =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = false;
            try
            {
                await _provider.LoadAsync<GameObject>("intentionally-invalid-address", cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            Assert.That(cancelled, Is.True);
        });

        [UnityTest]
        public IEnumerator FailedLoad_PropagatesErrorAndReleasesOwnership() => UniTask.ToCoroutine(async () =>
        {
            var expected = new InvalidOperationException("Expected load failure");
            AsyncOperationHandle<GameObject> handle = default;
            var operation = new DeferredPrefabOperation();
            var previousHandler = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
            try
            {
                UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = null;
                handle = Addressables.ResourceManager.StartOperation(operation, default(AsyncOperationHandle));
                _ = Addressables.ResourceManager.Acquire(handle);
                operation.Fail(expected);
                var referencesBeforeAwait = operation.CurrentReferenceCount;
                Exception actual = null;
                try
                {
                    await _provider.AwaitAssetAsync(handle, default);
                }
                catch (Exception exception)
                {
                    actual = exception;
                }
                Assert.That(actual, Is.SameAs(expected));
                Assert.That(operation.CurrentReferenceCount, Is.EqualTo(referencesBeforeAwait - 1));
            }
            finally
            {
                UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previousHandler;
                if (handle.IsValid()) Addressables.Release(handle);
            }
        });

        private sealed class DeferredPrefabOperation : AsyncOperationBase<GameObject>
        {
            public bool WasDestroyed { get; private set; }
            public int CurrentReferenceCount => ReferenceCount;
            protected override void Execute() { }
            public void Finish(GameObject prefab) => Complete(prefab, true, (string)null);
            public void Fail(Exception exception) => Complete(null, false, exception);
            protected override void Destroy() => WasDestroyed = true;
        }
    }
}
