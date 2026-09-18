using System;

namespace Mozzi.Data.Rows
{
    // 농장 설정 한 줄(id "default" 하나). storageMax 0 은 상한 없음이다.
    [Serializable]
    public sealed class FarmRow : IDataRow
    {
        public string id;
        public int fenceMax;
        public float producePeriodSec;
        public float tierMult1;
        public float tierMult2;
        public float tierMult3;
        public float breedPeriodSec;
        public int storageSellPrice;
        public int storageMax;
        public string startSpeciesId;
        public int startCount;

        public string Id
        {
            get { return id; }
        }
    }
}
