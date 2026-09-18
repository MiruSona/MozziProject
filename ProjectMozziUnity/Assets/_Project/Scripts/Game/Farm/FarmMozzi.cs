namespace Mozzi.Game.Farm
{
    // 농장에 사는 찹쌀이 한 마리. Id 는 농장 안에서만 유일하고 화면이 Object 를 맞출 때 쓴다.
    public sealed class FarmMozzi
    {
        public readonly int Id;
        public readonly string SpeciesId;

        public FarmMozzi(int id, string speciesId)
        {
            Id = id;
            SpeciesId = speciesId;
        }
    }
}
