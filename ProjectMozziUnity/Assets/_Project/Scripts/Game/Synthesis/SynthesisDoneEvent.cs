namespace Mozzi.Game.Synthesis
{
    // 칸의 대기가 끝나 「받기」를 기다린다. 결과 종은 아직 안 알린다 (연출은 받을 때).
    public readonly struct SynthesisDoneEvent
    {
        public readonly int SlotIndex;

        public SynthesisDoneEvent(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
}
