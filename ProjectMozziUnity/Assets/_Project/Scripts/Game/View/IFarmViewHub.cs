namespace Mozzi.Game.View
{
    // 농장 월드 화면 창구. UI 쪽(보관함 화면)이 월드 드래그를 시작시킬 때 쓴다.
    public interface IFarmViewHub
    {
        // 보관함 줄에서 끌어내기. 포인터 자리에 그 종의 임시 마리를 띄우고, 담장 안에 놓으면 농장에 놓인다.
        void BeginPlaceDrag(string speciesId);
    }
}
