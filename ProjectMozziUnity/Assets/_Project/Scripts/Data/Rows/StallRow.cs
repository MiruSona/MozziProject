using System;

namespace Mozzi.Data.Rows
{
    // 가판대 설정 한 줄(id "default" 하나).
    [Serializable]
    public sealed class StallRow : IDataRow
    {
        public string id;
        public int customersPerDay;
        public float customerGapSec;
        public int queueMax;
        public int shelfMax;
        public int hintEvery;
        public float orderMult;
        public float plainRate;
        public float countMult1;
        public float countMult2;
        public float countMult3;
        public int clerkPrice;
        public float clerkSpeedSec;
        public int poundTaps;
        public int poundGapMs;
        public int doubleTapMs;

        public string Id
        {
            get { return id; }
        }
    }
}
