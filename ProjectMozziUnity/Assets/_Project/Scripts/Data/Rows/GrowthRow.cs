using System;

namespace Mozzi.Data.Rows
{
    // 성장 구매 한 줄. kind 는 slot/shortcut/pair/fence 다.
    // rate 의 뜻은 kind 마다 다르다 : shortcut 은 대기 배율(0.9~0.5), fence 는 담장에 더해지는 마리 수(누적 · 10/20/30). slot·pair 는 안 쓴다.
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
