using System;

namespace Mozzi.Core
{
    // 씬을 넘어 앱 내내 사는 층. 만드는 이는 GameMaster 뿐이다.
    public interface IService : IDisposable
    {
        void Init();

        void PostInit();
    }
}
