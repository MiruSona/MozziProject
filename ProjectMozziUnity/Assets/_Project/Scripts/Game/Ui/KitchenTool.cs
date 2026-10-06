namespace Mozzi.Game.Ui
{
    // 주방 작업대에서 누르는 도구. None 은 「지금 누를 도구가 없다」(재료를 아직 안 골랐다 · 찌는 중).
    // 찜기는 누를 것이 없어(기다리기만) 여기 없다 (반복 08).
    public enum KitchenTool
    {
        None,
        Mill,
        Mortar,
        Board
    }
}
