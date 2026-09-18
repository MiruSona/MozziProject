using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Farm;
using Mozzi.Game.Stall;
using Mozzi.Save;

namespace Mozzi.Game.Ui
{
    // 돌아왔어요 팝업의 글. 재료 · 종은 id 가 아니라 표의 이름으로 적는다.
    public static class OfflineTextUtil
    {
        // "5분 동안 자리를 비웠어요". 1분 밑이면 초로.
        public static string TimeLine(double seconds)
        {
            int minutes = (int)(seconds / 60);
            if (minutes < 1)
            {
                return ((int)seconds) + "초 동안 자리를 비웠어요";
            }

            if (minutes >= 60)
            {
                int hours = minutes / 60;
                return hours + "시간 " + (minutes % 60) + "분 동안 자리를 비웠어요";
            }

            return minutes + "분 동안 자리를 비웠어요";
        }

        // 농장 · 합성 · 가판대 세 줄. 아무것도 없으면 「없음」.
        public static string Body(IStaticDataHub data, OfflineReport farm, int synthesisDone, StallOfflineReport stall)
        {
            List<string> lines = new List<string>();
            lines.Add("농장 : " + FarmLine(data, farm));
            lines.Add("합성소 : " + SynthesisLine(synthesisDone));
            lines.Add("가판대 : " + StallLine(stall));
            return string.Join("\n", lines);
        }

        private static string FarmLine(IStaticDataHub data, OfflineReport report)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < report.Produced.Count; i++)
            {
                CountEntry entry = report.Produced[i];
                parts.Add(IngredientName(data, entry.id) + " +" + entry.count);
            }

            for (int i = 0; i < report.Born.Count; i++)
            {
                CountEntry entry = report.Born[i];
                parts.Add(SpeciesName(data, entry.id) + " " + entry.count + "마리 태어남");
            }

            if (parts.Count == 0)
            {
                return "쌓인 것 없음";
            }

            return string.Join(", ", parts);
        }

        private static string SynthesisLine(int done)
        {
            if (done <= 0)
            {
                return "끝난 합성 없음";
            }

            return "합성 " + done + "칸 완료 · 받으러 가요";
        }

        private static string StallLine(StallOfflineReport report)
        {
            if (report.IsEmpty)
            {
                return "판 떡 없음";
            }

            string tail = "";
            if (report.ClosedForNight)
            {
                tail = " · 손님을 다 받아 밤";
            }

            return report.ServedCount + "명에게 팔아 " + report.GoldEarned + "G" + tail;
        }

        private static string IngredientName(IStaticDataHub data, string id)
        {
            IngredientRow row;
            if (data.Ingredients.TryGet(id, out row))
            {
                return row.name;
            }

            return id;
        }

        private static string SpeciesName(IStaticDataHub data, string id)
        {
            MozziSpeciesRow row;
            if (data.Species.TryGet(id, out row))
            {
                return row.name;
            }

            return id;
        }
    }
}
