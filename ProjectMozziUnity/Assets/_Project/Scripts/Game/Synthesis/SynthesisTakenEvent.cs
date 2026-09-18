namespace Mozzi.Game.Synthesis
{
    // 결과를 받아 보관함에 넣었다. IsNew 는 도감 칸이 이번에 처음 열렸는지다.
    public readonly struct SynthesisTakenEvent
    {
        public readonly int SlotIndex;
        public readonly string SpeciesId;
        public readonly bool IsNew;

        public SynthesisTakenEvent(int slotIndex, string speciesId, bool isNew)
        {
            SlotIndex = slotIndex;
            SpeciesId = speciesId;
            IsNew = isNew;
        }
    }
}
