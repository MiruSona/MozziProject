using System;

namespace Mozzi.Data.Rows
{
    // 성장 구매 한 줄. kind 는 slot/shortcut/pair, rate 는 shortcut 의 대기 배율(0.9~0.5)이다.
    [Serializable]
    public sealed class GrowthRow : IDataRow
    {
        public string id;
        public string kind;
        public int level;
        public int price;
        public float rate;

        public string Id
        {
            get { return id; }
        }
    }
}
