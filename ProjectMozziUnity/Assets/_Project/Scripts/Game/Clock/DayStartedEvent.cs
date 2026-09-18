namespace Mozzi.Game.Clock
{
    // 「다음날로」 뒤. day 는 새 날짜다.
    public readonly struct DayStartedEvent
    {
        public readonly int Day;

        public DayStartedEvent(int day)
        {
            Day = day;
        }
    }
}
