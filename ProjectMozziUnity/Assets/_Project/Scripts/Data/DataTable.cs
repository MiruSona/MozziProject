using System;
using System.Collections.Generic;

namespace Mozzi.Data
{
    // 표 하나. id 로 행을 찾기만 하고 값 해석은 안 한다.
    public sealed class DataTable<TRow> where TRow : class, IDataRow
    {
        private readonly Dictionary<string, TRow> _byId = new Dictionary<string, TRow>();
        private readonly List<TRow> _all = new List<TRow>();
        private readonly string _name;

        public DataTable(string name, IReadOnlyList<TRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            _name = name;

            for (int i = 0; i < rows.Count; i++)
            {
                AddRow(rows[i]);
            }
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

        // id 가 비었거나 겹치면 JSON 이 잘못된 것이니 바로 알린다.
        private void AddRow(TRow row)
        {
            if (row == null || string.IsNullOrEmpty(row.Id))
            {
                throw new InvalidOperationException(_name + " 표에 id 가 빈 행이 있다");
            }

            if (_byId.ContainsKey(row.Id))
            {
                throw new InvalidOperationException(_name + " 표에 id '" + row.Id + "' 가 두 번 있다");
            }

            _byId[row.Id] = row;
            _all.Add(row);
        }
    }
}
