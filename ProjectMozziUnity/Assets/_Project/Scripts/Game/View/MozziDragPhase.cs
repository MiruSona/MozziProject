namespace Mozzi.Game.View
{
    // 찹쌀이 드래그의 단계. Pressed 는 아직 탭인지 드래그인지 모르는 때다.
    public enum MozziDragPhase
    {
        None,
        Pressed,
        // 농장 찹쌀이를 들고 옮기는 중. 보관함 버튼에 놓으면 넣는다.
        DragFarm,
        // 보관함 줄에서 끌어낸 임시 마리를 들고 있는 중. 담장 안에 놓으면 농장에 놓인다.
        DragPlace,
    }
}
