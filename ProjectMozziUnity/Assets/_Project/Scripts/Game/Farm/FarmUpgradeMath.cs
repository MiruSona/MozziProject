using System;
using System.Collections.Generic;

namespace Mozzi.Game.Farm
{
    // 농장 강화 · 바닥 한도의 셈. Unity 를 안 써서 EditMode 시험이 바로 부른다.
    // 규칙은 Docs/Design/03-시스템-농장.md 「바닥 한도」 · 「농장 강화」 가 정본이다.
    public static class FarmUpgradeMath
    {
        // 먹이 공급기는 속도를 (1 + bonus) 배로 올린다. 그래서 주기는 나눈다.
        public static double ProducePeriod(double basePeriod, double tierMult, double feederBonus)
        {
            return basePeriod * tierMult / (1.0 + feederBonus);
        }

        // 이번 걸음에 떨굴 수 있는 수. 바닥 합계가 한도를 넘지 않게 자른다.
        public static int ClampDrop(int dropped, int floorTotal, int floorMax)
        {
            int room = Math.Max(0, floorMax - floorTotal);
            return Math.Min(dropped, room);
        }

        // 합계가 max 를 넘으면 각 칸을 비율대로 줄이고, 버림으로 남는 몫은 앞 칸부터 하나씩 되돌린다.
        // 옛 세이브의 바닥 재료가 한도를 넘을 때 쓴다. counts 를 그 자리에서 고친다.
        public static void TrimToMax(List<int> counts, int max)
        {
            long total = 0;
            for (int i = 0; i < counts.Count; i++)
            {
                total += counts[i];
            }

            if (total <= max)
            {
                return;
            }

            int kept = 0;
            for (int i = 0; i < counts.Count; i++)
            {
                counts[i] = (int)((long)counts[i] * max / total);
                kept += counts[i];
            }

            for (int i = 0; i < counts.Count && kept < max; i++)
            {
                if (counts[i] <= 0)
                {
                    continue;
                }

                counts[i]++;
                kept++;
            }
        }
    }
}
