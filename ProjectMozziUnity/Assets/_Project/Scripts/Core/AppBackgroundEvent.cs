namespace Mozzi.Core
{
    // 앱이 뒤로 갔다 (홈 버튼 · 다른 앱으로 전환 · 포커스 잃음). GameRunner 가 알린다.
    // 안드로이드는 이 뒤에 종료 알림 없이 죽을 수 있으니, 잃으면 안 되는 것은 이때 저장한다.
    public readonly struct AppBackgroundEvent
    {
    }
}
