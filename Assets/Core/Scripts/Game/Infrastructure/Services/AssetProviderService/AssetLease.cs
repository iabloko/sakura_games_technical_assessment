using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Scripts.Game.Infrastructure.Services.AssetProviderService
{
    public sealed class AssetLease<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly AsyncOperationHandle<T> _handle;
        private bool _disposed;

        internal AssetLease(AsyncOperationHandle<T> handle) => _handle = handle;

        public T Asset => !_disposed ? _handle.Result : throw new ObjectDisposedException(nameof(AssetLease<T>));

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_handle.IsValid()) Addressables.Release(_handle);
        }
    }
}
