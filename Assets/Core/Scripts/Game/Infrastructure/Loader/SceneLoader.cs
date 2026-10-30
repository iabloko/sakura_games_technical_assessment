using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Core.Scripts.Game.Infrastructure.Loader
{
    public sealed class SceneLoader
    {
        public async UniTaskVoid MmSceneLoad(string nextScene, Action onLoaded = null) =>
            await LoadSceneAsyncWithFeedback(nextScene, onLoaded);

        private async UniTask LoadSceneAsyncWithFeedback(string nextScene, Action onLoaded = null)
        {
            await SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single).ToUniTask();
            onLoaded?.Invoke();
        }
    }
}