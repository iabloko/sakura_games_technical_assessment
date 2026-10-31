using System;
using Core.Scripts.Game.Gameplay.World;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States
{
    public sealed class WorldGenerationState : StateBase
    {
        public override string StateName => "WorldGenerationState";

        public WorldGenerationState(GameStateMachine machine) : base(machine) { }

        public override void Enter()
        {
            var scene = SceneManager.GetActiveScene();
            GameWorld world = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<GameWorld>(true))
            {
                if (world != null) throw new InvalidOperationException("The gameplay scene must contain exactly one GameWorld.");
                world = candidate;
            }
            if (world == null) throw new InvalidOperationException("The gameplay scene requires a GameWorld.");
            try
            {
                world.Prepare();
                GameStateMachine.Enter<GamePlayState, GamePlayStateData>(new GamePlayStateData(world));
            }
            catch
            {
                world.EndSession();
                throw;
            }
        }

        public override void Exit() { }

        [Preserve]
        public sealed class Factory : PlaceholderFactory<GameStateMachine, WorldGenerationState>
        {
            [Preserve]
            public Factory() { }
        }
    }
}
