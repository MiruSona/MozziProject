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
    }
}
