using System;

namespace Mozzi.Game.Farm
{
    // 세이브에 담는 찹쌀이 한 마리.
    [Serializable]
    public sealed class FarmMozziState
    {
        public int id;
        public string speciesId;
    }
}
