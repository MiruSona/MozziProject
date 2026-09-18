namespace Mozzi.Game.Inventory
{
    // 창고 수량이 바뀌었다. Delta 는 들어오면 양수, 나가면 음수.
    public readonly struct IngredientChangedEvent
    {
        public readonly string IngredientId;
        public readonly int Count;
        public readonly int Delta;

        public IngredientChangedEvent(string ingredientId, int count, int delta)
        {
            IngredientId = ingredientId;
            Count = count;
            Delta = delta;
        }
    }
}
