namespace Mozzi.Game.Synthesis
{
    // 칸에 재료 2마리를 넣고 골드를 내 시계가 돌기 시작했다.
    public readonly struct SynthesisStartedEvent
    {
        public readonly int SlotIndex;
        public readonly double WaitSec;

        public SynthesisStartedEvent(int slotIndex, double waitSec)
        {
            SlotIndex = slotIndex;
            WaitSec = waitSec;
        }
    }
}
