using UnityEngine;

namespace Mozzi.Resource
{
    public interface IResourceHub
    {
        T Rent<T>(string key, Transform parent) where T : Component;

        void Return<T>(T instance) where T : Component;

        void ClearPool(string key);

        // 그림이 아직 없는 종도 있어 못 찾으면 null 을 준다.
        Sprite GetSprite(string key);
    }
}
