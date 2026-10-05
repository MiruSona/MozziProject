namespace Mozzi.Game.View
{
    // 농장 월드를 눌렀을 때 누름을 받는 쪽. 고르는 순서는 FloorItemMath.ChoosePressTarget 이 정본이다.
    public enum FarmPressTarget
    {
        Ground,
        FloorItem,
        Mozzi,
        Building,
    }
}
