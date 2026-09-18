using System;
using System.Collections.Generic;

namespace Mozzi.Events
{
    // Manager 끼리 알리는 유일한 길.
    // 동기 즉시 호출이고 이벤트는 readonly struct 로 만든다.
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<EventSubscription>> _subscriptions
            = new Dictionary<Type, List<EventSubscription>>();
        private static readonly HashSet<Type> _publishing = new HashSet<Type>();

        public static EventSubscription Subscribe<T>(object owner, Action<T> handler) where T : struct
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type type = typeof(T);
            List<EventSubscription> list;
            if (_subscriptions.TryGetValue(type, out list) == false)
            {
                list = new List<EventSubscription>();
                _subscriptions[type] = list;
            }

            EventSubscription sub = new EventSubscription(type, owner, handler);
            list.Add(sub);
            return sub;
        }

        public static void Publish<T>(in T evt) where T : struct
        {
            Type type = typeof(T);
            if (_publishing.Contains(type))
            {
                throw new InvalidOperationException(typeof(T).Name + " 을 알리는 중에 다시 알렸다");
            }

            List<EventSubscription> list;
            if (_subscriptions.TryGetValue(type, out list) == false)
            {
                return;
            }

            // 순회 중 목록이 바뀔 수 있으니 복사본으로 돈다.
            _publishing.Add(type);
            try
            {
                EventSubscription[] snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    EventSubscription sub = snapshot[i];
                    if (sub.IsDisposed) { continue; }

                    ((Action<T>)sub.Handler)(evt);
                }
            }
            finally
            {
                _publishing.Remove(type);
            }
        }

        internal static void Unsubscribe(EventSubscription sub)
        {
            if (sub == null)
            {
                return;
            }

            List<EventSubscription> list;
            if (_subscriptions.TryGetValue(sub.EventType, out list) == false)
            {
                return;
            }

            list.Remove(sub);
        }

        public static void UnsubscribeAllBy(object owner)
        {
            if (owner == null)
            {
                return;
            }

            // 알리는 중에 끊겨도 스냅숏이 부르지 않게 표시를 먼저 한다.
            foreach (List<EventSubscription> list in _subscriptions.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Owner == owner) { list[i].MarkDisposed(); }
                }

                list.RemoveAll(sub => sub.Owner == owner);
            }
        }

        public static void Clear()
        {
            _subscriptions.Clear();
            _publishing.Clear();
        }
    }
}
