using System;

namespace Mozzi.Data.Rows
{
    // 찹쌀이 종 한 줄. family 는 grass/water/lava/swamp, nextSpeciesId 는 3단계면 빈 문자열.
    [Serializable]
    public sealed class MozziSpeciesRow : IDataRow
    {
        public string id;
        public string name;
        public string family;
        public int tier;
        public bool isShiny;
        public string ingredientId;
        public string nextSpeciesId;
        public string colorHex;
        public string spriteKey;

        public string Id
        {
            get { return id; }
        }
    }
}
