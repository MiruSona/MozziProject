namespace Mozzi.Game.Farm
{
    // 더미를 주웠다. Moved 는 창고로 들어간 수, PileCount 는 창고가 차서 남은 더미 수.
    public readonly struct IngredientPickedEvent
    {
        public readonly string IngredientId;
        public readonly int Moved;
        public readonly int PileCount;

        public IngredientPickedEvent(string ingredientId, int moved, int pileCount)
        {
            IngredientId = ingredientId;
            Moved = moved;
            PileCount = pileCount;
        }
    }
}
