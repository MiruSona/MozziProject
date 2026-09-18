namespace Mozzi.Game.Farm
{
    // 번식으로 한 마리가 태어나 농장에 놓였다. FarmCount 는 태어난 뒤 농장 전체 마릿수.
    public readonly struct MozziBornEvent
    {
        public readonly int MozziId;
        public readonly string SpeciesId;
        public readonly int FarmCount;

        public MozziBornEvent(int mozziId, string speciesId, int farmCount)
        {
            MozziId = mozziId;
            SpeciesId = speciesId;
            FarmCount = farmCount;
        }
    }
}
