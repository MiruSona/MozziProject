using System.Collections.Generic;

namespace Mozzi.Game.Farm
{
    // 농장 창구. 규칙은 Docs/Design/03-시스템-농장.md 가 정본이다.
    public interface IFarmHub
    {
        // 담장 상한 = Farm 표 fenceMax + 담장 단계만큼 더한 수 (Growth 표 fence 행).
        int FenceMax { get; }

        int FenceLevel { get; }

        int MaxFenceLevel { get; }

        // 다음 담장 단계 값. 최대면 -1.
        int NextFencePrice { get; }

        int MozziCount { get; }

        IReadOnlyList<FarmMozzi> Mozzis { get; }

        IReadOnlyDictionary<string, int> Piles { get; }

        // 이번 켜기의 오프라인 진행 결과. 세이브가 없었으면 비어 있다.
        OfflineReport OfflineReport { get; }

        int CountOf(string speciesId);

        int PileOf(string ingredientId);

        // 더미 전부를 창고로 옮기고 실제로 들어간 수를 준다. 창고가 차면 남은 것은 더미에 그대로.
        int PickUp(string ingredientId);

        // 더미에서 amount 개까지만 옮긴다 (바닥 낱개 · 꾸러미 하나를 주울 때). 실제로 들어간 수를 준다.
        int PickUp(string ingredientId, int amount);

        // 담장이 차면 false.
        bool Place(string speciesId);

        bool Remove(int mozziId);

        // 담장을 한 단계 올린다. 최대이거나 골드가 모자라면 false.
        bool BuyFence();

        // 디버그용. 안 켠 것처럼 seconds 만큼 한 번에 진행하고 OfflineReport 를 갈아 끼운다. 이벤트는 안 낸다.
        void Skip(double seconds);
    }
}
