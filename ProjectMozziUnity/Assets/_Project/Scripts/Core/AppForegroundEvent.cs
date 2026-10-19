namespace Mozzi.Core
{
    // 앱이 뒤에서 앞으로 돌아왔다. Seconds 는 뒤에 있던 실제 초다. GameRunner 가 알린다.
    // 안드로이드는 앱을 죽이지 않고 멈춰 두기도 해서, 그동안 못 돈 시간을 Manager 들이 이걸로 따라잡는다.
    public readonly struct AppForegroundEvent
    {
        public readonly double Seconds;

        public AppForegroundEvent(double seconds)
        {
            Seconds = seconds;
        }
    }
}
