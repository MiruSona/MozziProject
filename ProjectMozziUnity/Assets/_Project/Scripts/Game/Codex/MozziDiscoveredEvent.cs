namespace Mozzi.Game.Codex
{
    // 찹쌀이 도감에 새 칸이 열렸다. OpenCount 는 열린 뒤 칸 수.
    public readonly struct MozziDiscoveredEvent
    {
        public readonly string SpeciesId;
        public readonly int OpenCount;

        public MozziDiscoveredEvent(string speciesId, int openCount)
        {
            SpeciesId = speciesId;
            OpenCount = openCount;
        }
    }
}
