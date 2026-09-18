namespace Mozzi.Game.Clock
{
    // 화면·Manager 가 시계를 보는 창구. 초는 GameRunner 가 주는 dt 의 누적이다.
    public interface IClockHub
    {
        double ElapsedSeconds { get; }

        int Day { get; }

        bool IsNight { get; }

        // 마지막 저장 UTC 틱과 지금의 차이. 세이브가 없으면 0. 상한 없음.
        double OfflineSeconds { get; }

        void FallNight();

        void AdvanceDay();
    }
}
