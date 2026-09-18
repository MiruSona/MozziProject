namespace Mozzi.Game.Farm
{
    // 찹쌀이가 바닥 더미에 부재료 1개를 떨어뜨렸다. PileCount 는 떨어뜨린 뒤 더미 수.
    public readonly struct IngredientDroppedEvent
    {
        public readonly string IngredientId;
        public readonly int PileCount;

        public IngredientDroppedEvent(string ingredientId, int pileCount)
        {
            IngredientId = ingredientId;
            PileCount = pileCount;
        }
    }
}
