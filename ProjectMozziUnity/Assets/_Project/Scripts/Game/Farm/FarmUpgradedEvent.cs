namespace Mozzi.Game.Farm
{
    // 농장 강화(바닥 넓히기 · 수집기 · 먹이 공급기) 한 단계를 샀다. Kind 는 GrowthUtil 의 kind 상수다.
    public readonly struct FarmUpgradedEvent
    {
        public readonly string Kind;
        public readonly int Level;

        public FarmUpgradedEvent(string kind, int level)
        {
            Kind = kind;
            Level = level;
        }
    }
}
