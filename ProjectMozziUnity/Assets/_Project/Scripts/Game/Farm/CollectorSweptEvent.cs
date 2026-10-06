namespace Mozzi.Game.Farm
{
    // 수집기가 바닥 재료를 창고로 걷었다. 한 번 걷을 때 재료마다 하나씩 난다. Moved 는 그 재료를 걷은 수.
    // 손으로 주운 것과 가르려고 따로 둔다 (IngredientPickedEvent 는 둘 다 난다). 오프라인 처리에서는 안 난다.
    public readonly struct CollectorSweptEvent
    {
        public readonly string IngredientId;
        public readonly int Moved;

        public CollectorSweptEvent(string ingredientId, int moved)
        {
            IngredientId = ingredientId;
            Moved = moved;
        }
    }
}
