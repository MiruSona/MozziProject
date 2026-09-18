namespace Mozzi.Game.Clock
{
    // 가판대가 하루 손님을 다 받아 밤이 됐다.
    public readonly struct NightFellEvent
    {
        public readonly int Day;

        public NightFellEvent(int day)
        {
            Day = day;
        }
    }
}
