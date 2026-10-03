namespace Mozzi.Game.Ui
{
    // 화면 열고 닫기 창구. 덮개는 한 번에 하나만 열린다 (세로조각1판설계 3-1).
    public interface IUiHub
    {
        bool IsAnyOpen { get; }

        void OpenSynthesis();

        void OpenMozziCodex();

        void OpenStorage();

        void OpenStall();

        void OpenRecipeCodex();

        void CloseAll();

        // 농장 찹쌀이를 탭했을 때. 종 이름과 「보관함으로」 를 보여 준다.
        void OpenMozziPopup(int mozziId);

        // 화면 좌표가 아래 띠의 보관함 버튼 위인가. 찹쌀이를 끌어다 놓을 자리 판정이다.
        bool IsOverStorageDrop(UnityEngine.Vector2 screen);
    }
}
