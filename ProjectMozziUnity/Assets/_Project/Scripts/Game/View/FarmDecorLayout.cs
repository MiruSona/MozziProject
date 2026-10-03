using System;
using System.Collections.Generic;
using Mozzi.Data;
using UnityEngine;

namespace Mozzi.Game.View
{
    // FarmDecor 표 행을 울타리 안 칸 자리로 바꾸고, 칸 밖 · 같은 칸 두 번을 막는다. Unity 오브젝트를 안 만들어 EditMode 시험이 바로 부른다.
    public static class FarmDecorLayout
    {
        // 칸 밖이거나 같은 칸에 두 줄이 있으면 던진다. 표를 고친 사람이 바로 알게 첫 건이 아니라 다 모아서 알린다.
        public static void Validate(IReadOnlyList<FarmDecorRow> rows, int cols, int rowCount)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            List<string> problems = new List<string>();
            Dictionary<int, string> usedBy = new Dictionary<int, string>();

            for (int i = 0; i < rows.Count; i++)
            {
                FarmDecorRow row = rows[i];
                if (row.Col < 0 || row.Col >= cols || row.Row < 0 || row.Row >= rowCount)
                {
                    problems.Add(row.Id + " 칸 (" + row.Col + "," + row.Row + ") 이 " + cols + "×" + rowCount + " 밖이다");
                    continue;
                }

                int cell = row.Row * cols + row.Col;
                string other;
                if (usedBy.TryGetValue(cell, out other))
                {
                    problems.Add(row.Id + " 칸 (" + row.Col + "," + row.Row + ") 을 " + other + " 가 이미 쓴다");
                    continue;
                }

                usedBy[cell] = row.Id;
            }

            if (problems.Count > 0)
            {
                throw new InvalidOperationException("FarmDecor 표가 틀렸다 — " + string.Join(" / ", problems));
            }
        }

        // 칸 (col, row) 의 바닥 가운데 월드 자리. 장식 그림은 아래 가운데 피벗이라 이 자리에 그대로 놓는다.
        public static Vector2 CellBottomCenter(Rect area, int col, int row)
        {
            float cell = LayoutConst.ToUnits(LayoutConst.FarmCellPx);
            return new Vector2(area.xMin + (col + 0.5f) * cell, area.yMin + row * cell);
        }
    }
}
