namespace Mozzi.Game.Stall
{
    // Make 의 결과. 실패면 Succeeded 가 false 고 나머지는 비어 있다. RecipeId 가 비면 평범한 떡.
    public readonly struct MakeResult
    {
        public readonly bool Succeeded;
        public readonly string RecipeId;
        public readonly int Price;
        public readonly bool IsNewRecipe;

        public MakeResult(bool succeeded, string recipeId, int price, bool isNewRecipe)
        {
            Succeeded = succeeded;
            RecipeId = recipeId;
            Price = price;
            IsNewRecipe = isNewRecipe;
        }

        public static MakeResult Failed
        {
            get { return new MakeResult(false, "", 0, false); }
        }

        public bool IsPlain
        {
            get { return string.IsNullOrEmpty(RecipeId); }
        }
    }
}
