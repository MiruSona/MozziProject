namespace Mozzi.Game.Stall
{
    // 안 켠 동안 점원이 판 결과. 점원이 없거나 닫혀 있었으면 전부 0 이다. 6단계 「돌아왔어요」 팝업이 읽는다.
    public readonly struct StallOfflineReport
    {
        public readonly double Seconds;
        public readonly int ServedCount;
        public readonly int GoldEarned;
        public readonly bool ClosedForNight;

        public StallOfflineReport(double seconds, int servedCount, int goldEarned, bool closedForNight)
        {
            Seconds = seconds;
            ServedCount = servedCount;
            GoldEarned = goldEarned;
            ClosedForNight = closedForNight;
        }

        public bool IsEmpty
        {
            get { return ServedCount == 0; }
        }

        public string Describe()
        {
            if (IsEmpty)
            {
                return "안 켠 동안 판 떡 없음";
            }

            string tail = "";
            if (ClosedForNight)
            {
                tail = " · 손님을 다 받아 밤";
            }

            return "안 켠 " + Seconds.ToString("F0") + "초 : " + ServedCount + "명에게 팔아 " + GoldEarned + "G" + tail;
        }
    }
}
