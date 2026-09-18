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
        public List<FarmMozziState> mozzis = new List<FarmMozziState>();
        public List<CountEntry> piles = new List<CountEntry>();
        public List<FarmSpeciesClockState> clocks = new List<FarmSpeciesClockState>();
    }
}
