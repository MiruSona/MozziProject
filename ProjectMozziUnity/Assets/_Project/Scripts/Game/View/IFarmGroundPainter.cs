using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 농장 바닥·담장을 까는 이음매. 나중에 자체 타일맵으로 갈아 끼울 자리다.
    public interface IFarmGroundPainter
    {
        void Paint(Transform parent, IResourceHub resource);

        void Clear();
    }
}
