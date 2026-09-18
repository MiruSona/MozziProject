using System.Collections.Generic;

namespace Mozzi.Game.Synthesis
{
    // 두 재료로 나올 수 있는 결과와 확률. 화면이 ??? 감추기를 얹어 보여 준다.
    // FixedSpeciesId 가 null 이면 첫 슬롯이 3단계라 ①이 없고 ②·③ 만 나온다.
    public readonly struct SynthesisPreview
    {
        public readonly string FixedSpeciesId;
        public readonly IReadOnlyList<string> OtherFamilyIds;
        public readonly string ShinySpeciesId;
        public readonly float PFixed;
        public readonly float POtherFamily;
        public readonly float PShiny;
        public readonly int Gold;
        public readonly double WaitSec;

        public SynthesisPreview(string fixedSpeciesId, IReadOnlyList<string> otherFamilyIds, string shinySpeciesId,
            float pFixed, float pOtherFamily, float pShiny, int gold, double waitSec)
        {
            FixedSpeciesId = fixedSpeciesId;
            OtherFamilyIds = otherFamilyIds;
            ShinySpeciesId = shinySpeciesId;
            PFixed = pFixed;
            POtherFamily = pOtherFamily;
            PShiny = pShiny;
            Gold = gold;
            WaitSec = waitSec;
        }
    }
}
