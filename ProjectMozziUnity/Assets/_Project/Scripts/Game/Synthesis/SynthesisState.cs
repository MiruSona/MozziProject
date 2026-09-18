using System;
using System.Collections.Generic;

namespace Mozzi.Game.Synthesis
{
    // SynthesisManager 의 세이브 칸. slots 는 최대 칸 수만큼 늘 들어 있다 (잠긴 칸도 포함).
    [Serializable]
    public sealed class SynthesisState
    {
        public int slotCount = 1;
        public int shortcutLevel;
        public List<SynthesisSlot> slots = new List<SynthesisSlot>();
    }
}
