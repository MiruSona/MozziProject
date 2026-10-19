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
    }
}
