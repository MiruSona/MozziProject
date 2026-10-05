namespace Mozzi.Game.Farm
{
    // 찹쌀이가 바닥 더미에 부재료 1개를 떨어뜨렸다. PileCount 는 떨어뜨린 뒤 더미 수.
    // SpeciesId 는 떨군 종이다 — 화면이 그 종 찹쌀이 하나의 발밑에서 재료를 튀어 나오게 한다.
    public readonly struct IngredientDroppedEvent
    {
        public readonly string IngredientId;
        public readonly int PileCount;
        public readonly string SpeciesId;

        public IngredientDroppedEvent(string ingredientId, int pileCount, string speciesId)
        {
            IngredientId = ingredientId;
            PileCount = pileCount;
            SpeciesId = speciesId;
        }
    }
}
