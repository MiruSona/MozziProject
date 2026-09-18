using System;

namespace Mozzi.Data.Rows
{
    // 부재료 한 줄. cap 은 창고(주운 것) 기준 상한이다.
    [Serializable]
    public sealed class IngredientRow : IDataRow
    {
        public string id;
        public string name;
        public int tier;
        public int value;
        public int cap;
        public string colorHex;

        public string Id
        {
            get { return id; }
        }
    }
}
