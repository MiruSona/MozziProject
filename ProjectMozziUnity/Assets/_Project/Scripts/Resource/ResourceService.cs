using System;
using System.Collections.Generic;
using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Resource
{
    public sealed class ResourceService : ServiceBase, IResourceHub
    {
        // 키 → 스프라이트. 못 찾은 것도 null 로 담아 매번 다시 찾지 않는다.
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        // 키 → 빌려줄 준비가 된 인스턴스 stack.
        private readonly Dictionary<string, Stack<GameObject>> _pools = new Dictionary<string, Stack<GameObject>>();

        // 빌려준 인스턴스 → 키. 반납 시 어떤 풀로 넣을지 여기서 찾는다.
        private readonly Dictionary<GameObject, string> _keyOf = new Dictionary<GameObject, string>();

        // 반납된 것을 매다는 비활성 루트.
        private Transform _poolRoot;

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
                GameObject prefab = Resources.Load<GameObject>(key);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Resources 에 " + key + " 프리팹이 없다");
                }

                go = UnityEngine.Object.Instantiate(prefab);
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

        public Sprite GetSprite(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            Sprite cached;
            if (_sprites.TryGetValue(key, out cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(SpriteKeys.Root + key);
            _sprites[key] = sprite;

            // 캐시에 담고 나서 알리므로 키마다 한 번만 뜬다.
            if (sprite == null)
            {
                Debug.LogWarning("[Resource] 스프라이트 " + key + " 를 못 찾아 네모로 그린다");
            }

            return sprite;
        }
    }
}
