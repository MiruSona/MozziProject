using UnityEngine;

namespace Mozzi.Resource
{
    // 프리팹을 어디서 가져오나만 가른다. Resources 든 Addressables 든 이 뒤에 숨는다.
    public interface IPrefabSource
    {
        GameObject Load(string key);
    }
}
