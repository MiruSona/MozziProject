namespace Mozzi.Game.Codex
{
    // 레시피 도감에 새 칸이 열렸다 (5단계 가판대가 낸다).
    public readonly struct RecipeDiscoveredEvent
    {
        public readonly string RecipeId;
        public readonly int OpenCount;

        public RecipeDiscoveredEvent(string recipeId, int openCount)
        {
            RecipeId = recipeId;
            OpenCount = openCount;
        }
    }
}
