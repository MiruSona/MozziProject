namespace Mozzi.Game.Stall
{
    // 떡 하나를 만들어 진열했다. RecipeId 가 비면 평범한 떡, IsNewRecipe 면 이번에 도감 칸이 열렸다.
    public readonly struct RiceCakeMadeEvent
    {
        public readonly string RecipeId;
        public readonly int Price;
        public readonly bool IsNewRecipe;

        public RiceCakeMadeEvent(string recipeId, int price, bool isNewRecipe)
        {
            RecipeId = recipeId;
            Price = price;
            IsNewRecipe = isNewRecipe;
        }
    }
}
