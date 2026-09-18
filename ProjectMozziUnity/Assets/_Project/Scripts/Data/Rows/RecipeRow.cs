using System;
using System.Collections.Generic;

namespace Mozzi.Data.Rows
{
    // 레시피 한 줄. revealIngredientId 는 3재료 힌트로 미리 보여 줄 재료, 없으면 빈 문자열.
    [Serializable]
    public sealed class RecipeRow : IDataRow
    {
        public string id;
        public string name;
        public List<string> ingredientIds = new List<string>();
        public string revealIngredientId;

        public string Id
        {
            get { return id; }
        }
    }
}
