using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Scripts.Game.Infrastructure.StateMachines.BaseData
{
    public abstract class UIAsyncPayloadView : MonoBehaviour, IAsyncStateView
    {
        public bool IsOpened { get; private set; }
        public event Action Opened;
        public event Action Closed;

        public abstract UniTask Close();
        public abstract UniTask Open();

        protected abstract void OnBeforeOpen();

        protected abstract void OnBeforeClosed();

        protected virtual void OnOpened()
        {
            IsOpened = true;
            Opened?.Invoke();
        }

        protected virtual void OnClosed()
        {
            IsOpened = false;
            Closed?.Invoke();
        }
    }
}