using System;

namespace Mozzi.Game.Farm
{
    // 종 하나의 생산 누적(개 단위 소수)과 번식 시계(초). 세이브 칸이다.
    [Serializable]
    public sealed class FarmSpeciesClockState
    {
        public string speciesId;
        public double produceAcc;
        public double breedClock;
    }
}
