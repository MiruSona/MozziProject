namespace Mozzi.Game.View
{
    // 농장 한 칸에 무엇이 올라가나. Ground 는 담장이 없는 안쪽 칸이라는 뜻이기도 하다.
    public enum FarmTileKind
    {
        Ground,
        FenceTop,
        FenceBottom,
        FenceSide,
        FenceCornerTop,
        FenceCornerBottom,
    }
}
