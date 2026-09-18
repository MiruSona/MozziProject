using System.Collections.Generic;
using Mozzi.Save;

namespace Mozzi.Game.Farm
{
    // 안 켠 동안 농장이 굴러간 결과. Produced 는 부재료 id 별 더미에 쌓인 수, Born 은 종 id 별 태어난 수.
    public readonly struct OfflineReport
    {
        public readonly double Seconds;
        public readonly IReadOnlyList<CountEntry> Produced;
        public readonly IReadOnlyList<CountEntry> Born;

        public OfflineReport(double seconds, IReadOnlyList<CountEntry> produced, IReadOnlyList<CountEntry> born)
        {
            Seconds = seconds;
            Produced = produced;
            Born = born;
        }

        public bool IsEmpty
        {
            get { return Produced.Count == 0 && Born.Count == 0; }
        }

        public string Describe()
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < Produced.Count; i++)
            {
                parts.Add(Produced[i].id + " +" + Produced[i].count);
            }

            for (int i = 0; i < Born.Count; i++)
            {
                parts.Add(Born[i].id + " 태어남 " + Born[i].count);
            }

            if (parts.Count == 0)
            {
                parts.Add("없음");
            }

            return "안 켠 " + Seconds.ToString("F0") + "초 : " + string.Join(", ", parts);
        }
    }
}
