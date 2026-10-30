using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Scripts.Game.Infrastructure.Loader
{
    public sealed class SceneLoader
    {
        private bool _isLoading;

        public async UniTask LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Scene name is required.", nameof(sceneName));
            if (_isLoading) throw new InvalidOperationException("A scene load is already in progress.");

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == sceneName && activeScene.isLoaded) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new ArgumentException($"Scene '{sceneName}' is not enabled in Build Settings.", nameof(sceneName));

            _isLoading = true;
            try
            {
                // Unity cannot abort a scene load. Keep the guard until native loading finishes,
                // then suppress the continuation if the caller has already exited its state.
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).ToUniTask();
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
