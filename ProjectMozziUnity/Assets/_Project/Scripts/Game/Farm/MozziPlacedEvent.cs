namespace Mozzi.Game.Farm
{
    // 밖(보관함 · 짝 구매 · 시작 상태)에서 한 마리를 농장에 놓았다.
    public readonly struct MozziPlacedEvent
    {
        public readonly int MozziId;
        public readonly string SpeciesId;
        public readonly int FarmCount;

        public MozziPlacedEvent(int mozziId, string speciesId, int farmCount)
        {
            MozziId = mozziId;
            SpeciesId = speciesId;
            FarmCount = farmCount;
        }
    }
}
