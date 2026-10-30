using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States
{
    public struct GamePlayStateData : IData
    {
    }

    public sealed class GamePlayState : PayloadStateBase<GamePlayStateData>
    {
        private readonly IAssetProvider _assetProvider;
        
        public GamePlayState(GameStateMachine gameGameStateMachineBase, IAssetProvider assetProvider) : base(gameGameStateMachineBase)
        {
            _assetProvider = assetProvider;
        }

        public override string StateName => "GamePlayState";

        public override void Enter(GamePlayStateData data)
        {
            InitializeGame().Forget();
        }

        private async UniTaskVoid InitializeGame()
        {
            await UniTask.Yield();
            Debug.Log("InitializeGame");
        }
        
        public override void Exit()
        {
        }
        
        [Preserve]
        public sealed class Factory : PlaceholderFactory<GameStateMachine, GamePlayState>
        {
            [Preserve]
            public Factory()
            {
            }
        }
    }
}