using Core.Scripts.Game.Infrastructure.StateMachines.BaseData;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;

namespace Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States.Base
{
    public abstract class PayloadStateBase<T> : IPayloadState<T> where T : IData
    {
        protected readonly GameStateMachineBase GameStateMachineBaseBase;

        public virtual string StateName => "PayloadStateBase";

        protected PayloadStateBase(GameStateMachineBase gameStateMachineBaseBase)
        {
            GameStateMachineBaseBase = gameStateMachineBaseBase;
        }

        public abstract void Enter(T payload);

        public abstract void Exit();
    }
}