namespace Mozzi.Game.Synthesis
{
    // 두 재료의 비용 · 대기와 ① 결과 확률. 화면이 ??? 감추기를 얹어 보여 준다.
    // FixedSpeciesId 가 null 이면 첫 슬롯이 3단계라 ①이 없다.
    public readonly struct SynthesisPreview
    {
        public readonly string FixedSpeciesId;
        public readonly float PFixed;
        public readonly int Gold;
        public readonly double WaitSec;

        public SynthesisPreview(string fixedSpeciesId, float pFixed, int gold, double waitSec)
        {
            FixedSpeciesId = fixedSpeciesId;
            PFixed = pFixed;
            Gold = gold;
            WaitSec = waitSec;
        }
    }
}
