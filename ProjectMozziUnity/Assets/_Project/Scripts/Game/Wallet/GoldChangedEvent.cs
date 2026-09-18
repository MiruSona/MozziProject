namespace Mozzi.Game.Wallet
{
    // 골드가 바뀌었다. Delta 는 벌면 양수, 내면 음수.
    public readonly struct GoldChangedEvent
    {
        public readonly int Gold;
        public readonly int Delta;

        public GoldChangedEvent(int gold, int delta)
        {
            Gold = gold;
            Delta = delta;
        }
    }
}
