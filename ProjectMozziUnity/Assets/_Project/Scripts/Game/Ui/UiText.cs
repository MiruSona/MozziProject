namespace Mozzi.Game.Ui
{
    // 여러 화면이 같이 쓰는 글자. 화면 하나만 쓰는 글자는 그 화면에 둔다.
    public static class UiText
    {
        // 안 열린 도감 칸 · 모르는 종 이름.
        public const string Unknown = "???";

        // 가판대 칠판. 찾은 레시피가 없으면 기본 메뉴 한 줄을 쓴다.
        public const string StallMenuTitle = "오늘의 떡";
        public const string StallMenuDefault = "찹쌀떡";
    }
}
