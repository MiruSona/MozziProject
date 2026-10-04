namespace Mozzi.Game.Ui
{
    // 화면 열고 닫기 창구. 덮개는 한 번에 하나만 열린다 (세로조각1판설계 3-1).
    public interface IUiHub
    {
        bool IsAnyOpen { get; }

        void OpenSynthesis();

        // 다른 덮개 위에서 열면 닫을 때 그 덮개로 돌아간다 (CloseTop).
        void OpenMozziCodex();

        void OpenStorage();

        void OpenStall();

        // 가판대에서 열면 닫을 때 가판대로 돌아간다 (CloseTop).
        void OpenKitchen();

        // 주방을 열고 차림표 판(찾은 떡 바로 만들기)까지 띄운다. 가판대 칠판에서 온다.
        void OpenKitchenMenu();

        // 다른 덮개 위에서 열면 닫을 때 그 덮개로 돌아간다 (CloseTop).
        void OpenRecipeCodex();

        // 덮개의 「닫기」. 다른 덮개에서 왔으면(가판대 → 주방 → 도감) 한 겹씩 되돌아가고, 아니면 농장으로.
        void CloseTop();

        void CloseAll();

        // 농장 찹쌀이를 탭했을 때. 종 이름과 「보관함으로」 를 보여 준다.
        void OpenMozziPopup(int mozziId);
    }
}
