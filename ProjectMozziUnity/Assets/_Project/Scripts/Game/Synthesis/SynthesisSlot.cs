using System;

namespace Mozzi.Game.Synthesis
{
    // 합성 칸 하나. 런타임 상태이자 세이브 칸이다. 결과 종은 시작 때 굴려 두고 끝나면 그대로 준다.
    [Serializable]
    public sealed class SynthesisSlot
    {
        public SynthesisSlotStatus status = SynthesisSlotStatus.Locked;
        public string speciesA = "";
        public string speciesB = "";
        public string resultSpeciesId = "";
        public double remainingSec;

        public void Clear(SynthesisSlotStatus next)
        {
            status = next;
            speciesA = "";
            speciesB = "";
            resultSpeciesId = "";
            remainingSec = 0;
        }
    }
}
