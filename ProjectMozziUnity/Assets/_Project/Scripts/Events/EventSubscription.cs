using System;

namespace Mozzi.Events
{
    // 구독할 때 돌려주는 손잡이. Dispose 하면 구독이 끊긴다.
    public sealed class EventSubscription : IDisposable
    {
        public readonly Type EventType;
        public readonly Delegate Handler;

        private bool _disposed;

        public bool IsDisposed
        {
            get { return _disposed; }
        }

        public EventSubscription(Type eventType, Delegate handler)
        {
            EventType = eventType;
            Handler = handler;
        }

        // 두 번 불려도 안전하다.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus.Unsubscribe(this);
        }
    }
}
