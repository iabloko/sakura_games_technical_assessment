using Core.Scripts.Game.Infrastructure.StateMachines.BaseData;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base
{
    public abstract class StateBase : IState
    {
        protected readonly GameStateMachine GameStateMachine;

        public abstract string StateName { get; }

        protected StateBase(GameStateMachine gameStateMachineBaseBase)
        {
            GameStateMachine = gameStateMachineBaseBase;
        }

        public abstract void Enter();

        public abstract void Exit();
    }
}