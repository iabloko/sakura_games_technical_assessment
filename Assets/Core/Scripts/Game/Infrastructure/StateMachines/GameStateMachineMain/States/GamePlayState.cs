using System;
using Core.Scripts.Game.Gameplay.World;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base;
using UnityEngine.Scripting;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States
{
    public struct GamePlayStateData : IData
    {
        public readonly GameWorld World;

        public GamePlayStateData(GameWorld world) => World = world;
    }

    public sealed class GamePlayState : PayloadStateBase<GamePlayStateData>
    {
        private GameWorld _world;
        
        public GamePlayState(GameStateMachine gameGameStateMachineBase) : base(gameGameStateMachineBase)
        {
        }

        public override string StateName => "GamePlayState";

        public override void Enter(GamePlayStateData data)
        {
            if (data.World == null) throw new ArgumentException("Gameplay requires a prepared world.", nameof(data));
            try
            {
                data.World.StartSession();
                _world = data.World;
            }
            catch
            {
                data.World.EndSession();
                throw;
            }
        }
        
        public override void Exit()
        {
            var world = _world;
            _world = null;
            if (world != null) world.EndSession();
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
