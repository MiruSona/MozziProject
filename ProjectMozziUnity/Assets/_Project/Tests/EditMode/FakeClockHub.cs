using Mozzi.Game.Clock;

namespace Mozzi.Tests
{
    // 시험용 시계. 안 켠 시간은 0 이라 오프라인 진행이 없다.
    public sealed class FakeClockHub : IClockHub
    {
        public double ElapsedSeconds
        {
            get { return 0; }
        }

        public int Day
        {
            get { return 1; }
        }

        public bool IsNight
        {
            get { return false; }
        }

        public double OfflineSeconds
        {
            get { return 0; }
        }

        public void FallNight()
        {
        }

        public void AdvanceDay()
        {
        }
    }
}
