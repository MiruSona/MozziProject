using System;

namespace Mozzi.Game.Clock
{
    // ClockManager 의 세이브 칸.
    [Serializable]
    public sealed class ClockState
    {
        public int day = 1;
        public bool isNight;
    }
}
