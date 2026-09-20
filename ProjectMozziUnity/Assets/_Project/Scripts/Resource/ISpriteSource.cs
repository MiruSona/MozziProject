using UnityEngine;

namespace Mozzi.Resource
{
    // 스프라이트를 어디서 가져오나만 가른다. 프리팹과 달리 없는 것이 정상이라 예외 대신 null 을 준다.
    public interface ISpriteSource
    {
        Sprite Load(string key);
    }
}
