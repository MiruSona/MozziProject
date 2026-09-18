using System;
using System.Collections.Generic;
using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Resource
{
    public sealed class ResourceService : ServiceBase, IResourceHub
    {
        private readonly IPrefabSource _source;

        // 키 → 빌려줄 준비가 된 인스턴스 stack.
        private readonly Dictionary<string, Stack<GameObject>> _pools = new Dictionary<string, Stack<GameObject>>();

        // 빌려준 인스턴스 → 키. 반납 시 어떤 풀로 넣을지 여기서 찾는다.
        private readonly Dictionary<GameObject, string> _keyOf = new Dictionary<GameObject, string>();

        // 반납된 것을 매다는 비활성 루트.
        private Transform _poolRoot;

        public ResourceService(IPrefabSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
        }

        public override void Init()
        {
            _poolRoot = new GameObject("ResourcePool").transform;
            UnityEngine.Object.DontDestroyOnLoad(_poolRoot.gameObject);
            _poolRoot.gameObject.SetActive(false);
        }

        public T Rent<T>(string key, Transform parent) where T : Component
        {
            Stack<GameObject> pool;
            bool hasCached = false;
            if (_pools.TryGetValue(key, out pool))
            {
                hasCached = pool.Count > 0;
            }

            GameObject go;
            if (hasCached)
            {
                go = pool.Pop();
            }
            else
            {
                // 뼈대 전체에서 Instantiate 를 부르는 유일한 자리다.
                go = UnityEngine.Object.Instantiate(_source.Load(key));
            }

            // 빌려줄 수 있는지 먼저 본다. 못 빌려주면 꺼낸 것을 되돌려 반쯤 빌려준 상태를 남기지 않는다.
            T component = go.GetComponent<T>();
            if (component == null)
            {
                if (hasCached)
                {
                    pool.Push(go);
                }
                else
                {
                    UnityEngine.Object.Destroy(go);
                }

                throw new InvalidOperationException(key + " 프리팹에 " + typeof(T).Name + " 이 없다");
            }

            go.transform.SetParent(parent);
            go.SetActive(true);
            _keyOf[go] = key;

            IPooledObject pooled = go.GetComponent<IPooledObject>();
            if (pooled != null)
            {
                pooled.OnRent();
            }

            return component;
        }

        public void Return<T>(T instance) where T : Component
        {
            // 제네릭 T 로는 Unity 의 == 가 안 걸려 Component 로 받아 검사한다.
            Component asComponent = instance;
            if (asComponent == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            GameObject go = asComponent.gameObject;
            string key;
            if (_keyOf.TryGetValue(go, out key) == false)
            {
                throw new InvalidOperationException("빌려주지 않은 오브젝트를 반납했다");
            }

            IPooledObject pooled = go.GetComponent<IPooledObject>();
            if (pooled != null)
            {
                pooled.OnReturn();
            }

            go.SetActive(false);
            go.transform.SetParent(_poolRoot);

            Stack<GameObject> pool;
            if (_pools.TryGetValue(key, out pool) == false)
            {
                pool = new Stack<GameObject>();
                _pools[key] = pool;
            }

            pool.Push(go);
            _keyOf.Remove(go);
        }

        public void ClearPool(string key)
        {
            Stack<GameObject> pool;
            if (_pools.TryGetValue(key, out pool) == false)
            {
                return;
            }

            while (pool.Count > 0)
            {
                GameObject go = pool.Pop();
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            _pools.Remove(key);
        }

        public override void Dispose()
        {
            List<string> keys = new List<string>(_pools.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                ClearPool(keys[i]);
            }

            _keyOf.Clear();

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }
    }
}
