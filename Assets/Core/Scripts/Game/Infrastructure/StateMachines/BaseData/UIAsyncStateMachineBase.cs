using System;
using System.Collections.Generic;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Core.Scripts.Game.Infrastructure.StateMachines.BaseData
{
    public abstract class UIAsyncStateMachineBase : StateMachineBase
    {
        public event Action OnStateMachineClosed;
        protected abstract string MainViewPath { get; }

        protected readonly IAssetProvider AssetProvider;

        protected Dictionary<Type, IAsyncExitState> States;

        private IAsyncExitState _activeState = null;

        [Inject]
        protected UIAsyncStateMachineBase(IAssetProvider assetProvider)
        {
            AssetProvider = assetProvider;
        }

        public virtual async UniTask OpenOrEnter<TState>(Action entered = null)
            where TState : class, IAsyncState
        {
            await EnterAsync<TState>(entered);
        }

        public virtual async UniTask OpenOrEnter<TState, TPayLoad>(TPayLoad payLoad, Action entered = null)
            where TState : class, IAsyncPayloadState<TPayLoad>
            where TPayLoad : IData
        {
            await EnterAsync<TState, TPayLoad>(payLoad, entered);
        }

        public virtual async UniTask EnterAsync<TState>(Action entered = null)
            where TState : class, IAsyncState
        {
            if (_activeState != null) await _activeState.Exit();

            TState state = GetState<TState>();
            _activeState = state;
            await state.Enter();
            entered?.Invoke();
        }

        protected virtual async UniTask EnterAsync<TState, TPayLoad>(TPayLoad payload, Action entered = null)
            where TState : class, IAsyncPayloadState<TPayLoad>
            where TPayLoad : IData
        {
            if (_activeState != null) await _activeState.Exit();

            TState state = GetState<TState>();
            _activeState = state;
            await state.Enter(payload);
            entered?.Invoke();
        }

        public virtual async UniTask CloseStateMachine(Action closed = null)
        {
            UnSubscribeEvents();
            await CloseActiveState(closed);
            OnStateMachineClosed?.Invoke();
        }

        protected virtual async UniTask CloseActiveState(Action closed)
        {
            if (_activeState != null) await _activeState.Exit();

            _activeState = null;
            closed?.Invoke();
        }

        protected virtual TState GetState<TState>() where TState : class, IAsyncExitState
            => States[typeof(TState)] as TState;

        protected virtual bool CompareStatesWitchActive<TState>() where TState : class, IAsyncExitState
        {
            TState state = GetState<TState>();
            return state.Equals(_activeState);
        }

        public abstract void SetUIStateView(UIAsyncPayloadView view);
        protected abstract void SubscribeEvents();
        protected abstract void UnSubscribeEvents();
    }
}