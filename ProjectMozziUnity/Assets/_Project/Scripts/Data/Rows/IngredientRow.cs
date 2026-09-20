using System;

namespace Mozzi.Data.Rows
{
    // 부재료 한 줄. cap 은 창고(주운 것) 기준 상한이다.
    // spriteKey 는 "Art/" 뒤에 붙는 자리다 (예 "Ingredient/redbean" → Resources/Art/Ingredient/redbean).
    // 비어 있으면 그림 없이 예전처럼 colorHex 네모로 그린다.
    [Serializable]
    public sealed class IngredientRow : IDataRow
    {
        public string id;
        public string name;
        public int tier;
        public int value;
        public int cap;
        public string colorHex;
        public string spriteKey;

        public string Id
        {
            get { return id; }
        }
    }
}
