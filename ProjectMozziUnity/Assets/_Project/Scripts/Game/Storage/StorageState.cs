using System;
using System.Collections.Generic;
using Mozzi.Save;

namespace Mozzi.Game.Storage
{
    // StorageManager 의 세이브 칸. id 는 종 id 다.
    [Serializable]
    public sealed class StorageState
    {
        public List<CountEntry> counts = new List<CountEntry>();
    }
}
