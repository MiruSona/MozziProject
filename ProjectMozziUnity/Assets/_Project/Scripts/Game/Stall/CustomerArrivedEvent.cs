namespace Mozzi.Game.Stall
{
    // 손님이 줄에 섰다. QueueIndex 는 선 자리, OrderRecipeId 가 비면 아무거나, IsHint 면 못 찾은 레시피 주문.
    public readonly struct CustomerArrivedEvent
    {
        public readonly int QueueIndex;
        public readonly string OrderRecipeId;
        public readonly bool IsHint;

        public CustomerArrivedEvent(int queueIndex, string orderRecipeId, bool isHint)
        {
            QueueIndex = queueIndex;
            OrderRecipeId = orderRecipeId;
            IsHint = isHint;
        }
    }
}
