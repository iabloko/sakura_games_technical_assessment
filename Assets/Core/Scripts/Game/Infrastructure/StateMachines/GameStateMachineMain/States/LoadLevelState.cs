using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Core.Scripts.Game.Infrastructure.Loader;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base;
using UnityEngine.Scripting;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States
{
    public struct LoadLevelData : IData
    {
        public readonly string NextSceneName;
        public readonly Action OnSceneLoaded;

        public LoadLevelData(string nextSceneName, Action onSceneLoaded)
        {
            this.NextSceneName = nextSceneName;
            this.OnSceneLoaded = onSceneLoaded;
        }
    }

    public sealed class LoadLevelState : PayloadStateBase<LoadLevelData>
    {
        public override string StateName => "LoadLevelState";

        private readonly SceneLoader _sceneLoader;
        private CancellationTokenSource _loadCancellation;

        public LoadLevelState(GameStateMachine gameGameStateMachineBase, SceneLoader sceneLoader) : base(gameGameStateMachineBase)
        {
            _sceneLoader = sceneLoader;
        }

        public override void Enter(LoadLevelData payload)
        {
            _loadCancellation = new CancellationTokenSource();
            LoadAsync(payload, _loadCancellation.Token).Forget(Debug.LogException);
        }

        private async UniTask LoadAsync(LoadLevelData payload, CancellationToken cancellationToken)
        {
            try
            {
                await _sceneLoader.LoadAsync(payload.NextSceneName, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                payload.OnSceneLoaded?.Invoke();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The state has exited; the completed native load must not trigger a transition.
            }
        }

        public override void Exit()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }
        
        [Preserve]
        public sealed class Factory : PlaceholderFactory<GameStateMachine, LoadLevelState>
        {
            [Preserve]
            public Factory()
            {
            }
        }
    }
}
