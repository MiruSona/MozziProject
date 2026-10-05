namespace Mozzi.Game.Ui
{
    // 메인 「강화」 판 한 줄의 글. 윗줄은 이름 · 단계, 아랫줄은 지금 효과 → 다음 효과다. 최대 단계면 화살표 없이 「최대」.
    // 숫자 꼴은 여기서만 정해 UiManager 는 값만 넘긴다.
    public static class FarmUpgradeText
    {
        public static string Line(string name, int level, int maxLevel, string now, string next)
        {
            string head = name + "  " + level + "/" + maxLevel + "단계";
            if (level >= maxLevel || string.IsNullOrEmpty(next))
            {
                return head + "\n" + now + " (최대)";
            }

            return head + "\n" + now + " → " + next;
        }

        public static string Collector(double periodSec)
        {
            if (periodSec <= 0)
            {
                return "없음";
            }

            return (int)periodSec + "초마다";
        }

        public static string Feeder(double bonus)
        {
            return "생산 +" + (int)System.Math.Round(bonus * 100) + "%";
        }
    }
}
