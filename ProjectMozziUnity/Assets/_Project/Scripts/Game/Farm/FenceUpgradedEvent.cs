namespace Mozzi.Game.Farm
{
    // 담장을 한 단계 올렸다. 화면의 「n / 상한」 이 상한 쪽만 바뀌는 때라 Born·Placed 와 따로 낸다.
    public readonly struct FenceUpgradedEvent
    {
        public readonly int FenceLevel;
        public readonly int FenceMax;

        public FenceUpgradedEvent(int fenceLevel, int fenceMax)
        {
            FenceLevel = fenceLevel;
            FenceMax = fenceMax;
        }
    }
}
