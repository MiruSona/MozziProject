using System;
using System.Collections.Generic;

namespace Mozzi.Hubs
{
    // 타입을 키로 삼아 서비스를 넣고 꺼내는 자리다.
    public sealed class HubTable
    {
        private readonly Dictionary<Type, object> _hubs = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _owners = new Dictionary<Type, object>();
        private readonly string _name;

        public HubTable(string name)
        {
            _name = name;
        }

        public T Get<T>() where T : class
        {
            T hub;
            if (TryGet<T>(out hub))
            {
                return hub;
            }

            throw new HubNotFoundException(typeof(T), _name, _hubs.Keys);
        }

        public bool TryGet<T>(out T hub) where T : class
        {
            hub = null;
            object stored;
            if (_hubs.TryGetValue(typeof(T), out stored) == false)
            {
                return false;
            }

            hub = stored as T;
            return hub != null;
        }

        public void Register<T>(T hub, object owner) where T : class
        {
            if (hub == null)
            {
                throw new ArgumentNullException(nameof(hub));
            }

            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (_hubs.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException(_name + " 에 " + typeof(T).Name + " 이 이미 등록돼 있다");
            }

            _hubs[typeof(T)] = hub;
            _owners[typeof(T)] = owner;
        }

        // 순회 중 사전을 고치지 말고 키 목록을 먼저 모은다.
        public void UnregisterAllBy(object owner)
        {
            if (owner == null)
            {
                return;
            }

            List<Type> keys = new List<Type>();
            foreach (KeyValuePair<Type, object> pair in _owners)
            {
                if (pair.Value == owner)
                {
                    keys.Add(pair.Key);
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                _hubs.Remove(keys[i]);
                _owners.Remove(keys[i]);
            }
        }

        public void Clear()
        {
            _hubs.Clear();
            _owners.Clear();
        }
    }
}
