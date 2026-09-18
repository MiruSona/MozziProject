using System;
using System.Collections.Generic;

namespace Mozzi.Data
{
    // JsonUtility 는 맨 바깥 배열을 못 읽어서 { "rows": [...] } 로 감싼 꼴을 받는 통이다.
    [Serializable]
    public sealed class DataTableJson<TRow>
    {
        public List<TRow> rows = new List<TRow>();
    }
}
