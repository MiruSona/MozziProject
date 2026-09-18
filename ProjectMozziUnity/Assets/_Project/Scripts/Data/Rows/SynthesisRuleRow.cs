using System;

namespace Mozzi.Data.Rows
{
    // 재료 단계별 합성 규칙 한 줄. ~Top 은 3단계 2마리(다음 단계 없음)일 때 확률이다.
    [Serializable]
    public sealed class SynthesisRuleRow : IDataRow
    {
        public string id;
        public int tier;
        public int gold;
        public int waitSec;
        public float pFixed;
        public float pOtherFamily;
        public float pShiny;
        public float pOtherFamilyTop;
        public float pShinyTop;

        public string Id
        {
            get { return id; }
        }
    }
}
