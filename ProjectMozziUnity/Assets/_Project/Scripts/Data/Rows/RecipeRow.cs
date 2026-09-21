using System;
using System.Collections.Generic;

namespace Mozzi.Data.Rows
{
    // 레시피 한 줄. revealIngredientId 는 3재료 힌트로 미리 보여 줄 재료, 없으면 빈 문자열.
    // spriteKey 는 "Art/" 뒤에 붙는 자리다 (예 "Cake/redbean_cake" → Resources/Art/Cake/redbean_cake).
    // 비어 있으면 그림 없이 예전처럼 글자만 나온다.
    [Serializable]
    public sealed class RecipeRow : IDataRow
    {
        public string id;
        public string name;
        public List<string> ingredientIds = new List<string>();
        public string revealIngredientId;
        public string spriteKey;

        public string Id
        {
            get { return id; }
        }
    }
}
