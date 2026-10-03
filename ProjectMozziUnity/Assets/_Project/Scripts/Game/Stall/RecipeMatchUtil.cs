using System.Collections.Generic;
using Mozzi.Data;

namespace Mozzi.Game.Stall
{
    // 재료 조합 → 레시피 찾기와 조합 키. 순서는 안 본다. 키는 정렬한 id 를 + 로 잇는다 (예 "bean+redbean").
    public static class RecipeMatchUtil
    {
        public const char Separator = '+';

        public static string ComboKey(IReadOnlyList<string> ingredientIds)
        {
            List<string> sorted = new List<string>(ingredientIds);
            sorted.Sort(string.CompareOrdinal);
            return string.Join(Separator.ToString(), sorted);
        }

        // 같은 조합의 레시피. 없으면 null (평범한 떡).
        public static RecipeRow FindRecipe(DataTable<RecipeRow> recipes, IReadOnlyList<string> ingredientIds)
        {
            string key = ComboKey(ingredientIds);
            IReadOnlyList<RecipeRow> rows = recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (ComboKey(rows[i].IngredientIds) == key)
                {
                    return rows[i];
                }
            }

            return null;
        }
    }
}
