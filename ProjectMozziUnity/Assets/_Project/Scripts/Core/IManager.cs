using System;

namespace Mozzi.Core
{
    public interface IManager : IDisposable
    {
        // 자기 것만 챙긴다. Hub.Get 금지.
        void Init();

        // 전원 등록 뒤에 불린다. Hub.Get 은 여기서만 하고 필드에 캐싱한다.
        void PostInit();
    }
}
