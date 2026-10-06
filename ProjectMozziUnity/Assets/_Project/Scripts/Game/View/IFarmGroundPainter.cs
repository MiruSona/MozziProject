using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 농장 바닥·담장을 까는 이음매. 나중에 자체 타일맵으로 갈아 끼울 자리다.
    public interface IFarmGroundPainter
    {
        void Paint(Transform parent, IResourceHub resource);

        void Clear();

        // 옆 울타리 세로 칸 fromRow ~ toRow 를 빼거나(open) 되돌린다. 장치를 산 뒤 그 자리에 장치가 선다.
        void SetSideGap(FenceSide side, int fromRow, int toRow, bool open);
    }
}
