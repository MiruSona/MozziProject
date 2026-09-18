namespace Mozzi.Game.Storage
{
    // 보관함의 한 종 마릿수가 바뀌었다. Delta 는 들어오면 양수, 나가면 음수.
    public readonly struct StorageChangedEvent
    {
        public readonly string SpeciesId;
        public readonly int Count;
        public readonly int Delta;

        public StorageChangedEvent(string speciesId, int count, int delta)
        {
            SpeciesId = speciesId;
            Count = count;
            Delta = delta;
        }
    }
}
