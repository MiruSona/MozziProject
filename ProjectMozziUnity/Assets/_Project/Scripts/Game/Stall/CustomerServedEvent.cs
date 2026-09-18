namespace Mozzi.Game.Stall
{
    // 손님 한 명에게 팔았다. RecipeId 가 비면 평범한 떡, WasOrder 면 주문 배율이 붙은 값이다.
    public readonly struct CustomerServedEvent
    {
        public readonly int Gold;
        public readonly string RecipeId;
        public readonly bool WasOrder;
        public readonly int ServedToday;

        public CustomerServedEvent(int gold, string recipeId, bool wasOrder, int servedToday)
        {
            Gold = gold;
            RecipeId = recipeId;
            WasOrder = wasOrder;
            ServedToday = servedToday;
        }
    }
}
