using System.Collections.Generic;

namespace Mozzi.Game.Stall
{
    // 가판대 창구. 진열대 · 열림 · 손님 줄 · 제작 · 판매 · 점원. 떡메 3탭은 화면 몫이고 여기는 모른다.
    public interface IStallHub
    {
        bool IsOpen { get; }

        bool HasClerk { get; }

        int ServedToday { get; }

        int CustomersPerDay { get; }

        int QueueMax { get; }

        int ShelfMax { get; }

        int ClerkPrice { get; }

        IReadOnlyList<ShelfItem> Shelf { get; }

        IReadOnlyList<StallCustomer> Queue { get; }

        // PostInit 에서 안 켠 시간만큼 점원이 판 결과. 이벤트는 안 낸다.
        StallOfflineReport OfflineReport { get; }

        // 낮이고 닫혀 있을 때만 열린다.
        bool Open();

        // 재료가 다 있고 진열대에 자리가 있나.
        bool CanMake(IReadOnlyList<string> ingredientIds);

        // 재료 1~3개로 떡 하나. 조합이 레시피면 그 떡, 아니면 평범한 떡. 새 레시피면 도감 칸을 연다.
        MakeResult Make(IReadOnlyList<string> ingredientIds);

        // 이 조합으로 만들면 얼마인가 (주문 배율 없이). 찾은 레시피 값 표시에 쓴다.
        int PreviewPrice(IReadOnlyList<string> ingredientIds);

        // 줄의 그 손님에게 판다. 떡이 없으면 false 고 손님은 그대로 기다린다.
        bool Serve(int queueIndex);

        bool BuyClerk();

        // 진열대 비우기. 진열된 떡을 모두 정가 × clearRate(Stall 표)에 판다. 판 골드를 준다 (비었으면 0).
        int ClearShelf();

        // 지금 비우면 받을 골드.
        int ClearShelfPrice();

        // 디버그용. 안 켠 것처럼 seconds 만큼 점원이 팔고 OfflineReport 를 갈아 끼운다. 골드 · 밤 이벤트는 난다.
        void Skip(double seconds);
    }
}
