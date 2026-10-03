using System.Collections.Generic;

namespace Mozzi.Data
{
    // Growth 표를 kind + level 로 찾는다. id 꼴(slot2 · pair1)에 기대지 않는다.
    public static class GrowthUtil
    {
        public const string KindSlot = "slot";
        public const string KindShortcut = "shortcut";
        public const string KindPair = "pair";
        public const string KindFence = "fence";

        public static GrowthRow Find(DataTable<GrowthRow> table, string kind, int level)
        {
            IReadOnlyList<GrowthRow> rows = table.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == kind && rows[i].Level == level)
                {
                    return rows[i];
                }
            }

            return null;
        }

        // 그 kind 의 가장 높은 level. 행이 없으면 0.
        public static int MaxLevel(DataTable<GrowthRow> table, string kind)
        {
            int max = 0;
            IReadOnlyList<GrowthRow> rows = table.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == kind && rows[i].Level > max)
                {
                    max = rows[i].Level;
                }
            }

            return max;
        }
    }
}
