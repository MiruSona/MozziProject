using UnityEngine;

namespace Mozzi.Resource
{
    public interface IResourceHub
    {
        T Rent<T>(string key, Transform parent) where T : Component;

        void Return<T>(T instance) where T : Component;

        void ClearPool(string key);
    }
}
