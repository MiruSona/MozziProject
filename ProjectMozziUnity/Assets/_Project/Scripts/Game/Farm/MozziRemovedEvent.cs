namespace Mozzi.Game.Farm
{
    // 농장에서 한 마리를 뺐다 (보관함으로 옮김). FarmCount 는 뺀 뒤 농장 전체 마릿수.
    public readonly struct MozziRemovedEvent
    {
        public readonly int MozziId;
        public readonly string SpeciesId;
        public readonly int FarmCount;

        public MozziRemovedEvent(int mozziId, string speciesId, int farmCount)
        {
            MozziId = mozziId;
            SpeciesId = speciesId;
            FarmCount = farmCount;
        }
    }
}
