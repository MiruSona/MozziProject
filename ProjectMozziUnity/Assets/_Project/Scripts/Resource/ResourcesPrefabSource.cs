using System;
using UnityEngine;

namespace Mozzi.Resource
{
    public sealed class ResourcesPrefabSource : IPrefabSource
    {
        public GameObject Load(string key)
        {
            GameObject prefab = Resources.Load<GameObject>(key);
            if (prefab == null)
            {
                throw new InvalidOperationException("Resources 에 " + key + " 프리팹이 없다");
            }

            return prefab;
        }
    }
}
