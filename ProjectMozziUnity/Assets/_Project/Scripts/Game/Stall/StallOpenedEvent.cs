namespace Mozzi.Game.Stall
{
    // 「열기」로 가게가 열렸다.
    public readonly struct StallOpenedEvent
    {
        public readonly int Day;

        public StallOpenedEvent(int day)
        {
            Day = day;
        }
    }
}
