namespace Mozzi.Game.Ui
{
    // 주방 만들기 흐름의 단계. 찌기는 찜기를 한 번 누르면 바로 끝나서 따로 머무는 단계가 없다 (고름 → [찜] → 침 → 빚음 → 끝).
    public enum KitchenStep
    {
        Pick,
        Pound,
        Shape
    }
}
