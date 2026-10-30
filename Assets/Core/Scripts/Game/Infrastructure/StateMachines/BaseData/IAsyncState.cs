using Core.Scripts.Game.Infrastructure.StateMachines.DataInterfaces;
using Cysharp.Threading.Tasks;

namespace Core.Scripts.Game.Infrastructure.StateMachines.BaseData
{
    public interface IPayload : IData
    {
    }

    public interface IAsyncExitState
    {
        public string StateName { get; }
        UniTask Exit();
    }

    public interface IAsyncState : IAsyncExitState
    {
        UniTask Enter();
    }

    public interface IAsyncPayloadState<TPayload> : IAsyncExitState
    {
        public TPayload Payload { get; }
        UniTask Enter(TPayload payload);
    }

    public abstract class AsyncAsyncPayloadState<TPayload> : IAsyncPayloadState<TPayload>
    {
        public TPayload Payload { get; private set; }
        public abstract string StateName { get; }
        public abstract IAsyncStateView StateView { get; }

        public virtual async UniTask Enter(TPayload payload)
        {
            Payload = payload;
            await StateView.Open();
        }

        public virtual async UniTask Exit()
        {
            await StateView.Close();
        }
    }

    public interface IAsyncStateView
    {
        public bool IsOpened { get; }
        public UniTask Open();
        public UniTask Close();
    }
}