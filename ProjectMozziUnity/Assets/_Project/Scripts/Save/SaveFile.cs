using System;
using System.Collections.Generic;

namespace Mozzi.Save
{
    // save.json 전체. savedAtUtcTicks 로 안 켠 시간을 센다.
    [Serializable]
    public sealed class SaveFile
    {
        public int version = 1;
        public long savedAtUtcTicks;
        public List<SaveEntry> entries = new List<SaveEntry>();
    }
}
