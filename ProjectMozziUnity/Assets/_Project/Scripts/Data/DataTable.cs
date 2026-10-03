using System;
using System.Collections.Generic;

namespace Mozzi.Data
{
    // 표 하나. 구운 데이터(GameDataTables)의 행 목록과 id 사전을 감싸 id 로 찾기만 한다.
    public sealed class DataTable<TRow> where TRow : class
    {
        private readonly IReadOnlyDictionary<string, TRow> _byId;
        private readonly IReadOnlyList<TRow> _all;
        private readonly string _name;

        public DataTable(string name, IReadOnlyList<TRow> rows, IReadOnlyDictionary<string, TRow> byId)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (byId == null)
            {
                throw new ArgumentNullException(nameof(byId));
            }

            _name = name;
            _all = rows;
            _byId = byId;
        }

        public string Name
        {
            get { return _name; }
        }

        public int Count
        {
            get { return _all.Count; }
        }

        public IReadOnlyList<TRow> All
        {
            get { return _all; }
        }

        public TRow Get(string id)
        {
            TRow row;
            if (TryGet(id, out row))
            {
                return row;
            }

            throw new KeyNotFoundException(_name + " 표에 id '" + id + "' 가 없다");
        }

        public bool TryGet(string id, out TRow row)
        {
            row = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            return _byId.TryGetValue(id, out row);
        }
    }
}
