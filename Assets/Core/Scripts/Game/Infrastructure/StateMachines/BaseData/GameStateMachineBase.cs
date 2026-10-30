using System;
using System.Collections.Generic;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;

namespace Core.Scripts.Game.Infrastructure.StateMachines.BaseData
{
    public abstract class GameStateMachineBase : StateMachineBase
    {
        protected Dictionary<Type, IExitState> States;
        private IExitState ActiveState { get; set; }

        public virtual void Enter<TState>() where TState : class, IState
        {
            IState state = ChangeState<TState>();
            ShowLog<TState>(string.Concat("ENTER", " : ", state.StateName));

            state.Enter();
        }

        public virtual void Enter<TState, TPayload>(TPayload payload)
            where TState : class, IPayloadState<TPayload>
            where TPayload : IData
        {
            TState state = ChangeState<TState>();
            ShowLog<TState>(string.Concat("ENTER", " : ", state.StateName));

            state.Enter(payload);
        }

        protected virtual TState ChangeState<TState>() where TState : class, IExitState
        {
            if (ActiveState != null)
            {
                ShowLog<TState>(string.Concat("EXIT", " : ", ActiveState.StateName));
                ActiveState.Exit();
            }

            TState state = GetState<TState>();
            ActiveState = state;

            return state;
        }

        private TState GetState<TState>() where TState : class, IExitState
            => States[typeof(TState)] as TState;

        protected abstract void ShowLog<TState>(string message) where TState : class, IExitState;
    }
}