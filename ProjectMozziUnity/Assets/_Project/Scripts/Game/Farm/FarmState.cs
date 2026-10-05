using System;
using System.Collections.Generic;
using Mozzi.Save;

namespace Mozzi.Game.Farm
{
    // FarmManager 의 세이브 칸. piles 의 id 는 부재료 id, clocks 의 id 는 종 id 다.
    [Serializable]
    public sealed class FarmState
    {
        public int nextMozziId = 1;
        // 담장 단계. 0 이면 기본 상한(Farm 표 fenceMax)이고 Growth 표 fence 행만큼 늘어난다.
        public int fenceLevel = 0;
        // 농장 강화 단계 (Growth 표 floor · collector · feeder). 0 이면 안 산 것이다.
        public int floorLevel = 0;
        public int collectorLevel = 0;
        public int feederLevel = 0;
        // 수집기가 지난번 걷은 뒤 흐른 초.
        public double collectorClock = 0;
        public List<FarmMozziState> mozzis = new List<FarmMozziState>();
        public List<CountEntry> piles = new List<CountEntry>();
        public List<FarmSpeciesClockState> clocks = new List<FarmSpeciesClockState>();
    }
}
