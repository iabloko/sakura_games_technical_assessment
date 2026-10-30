using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;

namespace Core.Scripts.Game.Infrastructure.StateMachines.BaseData
{
    public interface IExitState
    {
        public string StateName { get; }
        void Exit();
    }

    public interface IState : IExitState
    {
        void Enter();
    }

    public interface IPayloadState<in TPayLoad> : IExitState where TPayLoad : IData
    {
        void Enter(TPayLoad payload);
    }
}