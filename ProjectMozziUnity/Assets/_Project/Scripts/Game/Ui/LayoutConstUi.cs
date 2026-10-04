namespace Mozzi.Game.Ui
{
    // 자원바 · 띠의 px 자리 (세로조각1판설계 3-2). 기준은 540×960 이지만 아래 자리 값은 아직 옛 360×640 단위다 —
    // 옛 화면 프리팹이 Legacy360 껍데기(×Legacy360Scale) 안에 붙기 때문이다. 화면을 540 으로 옮길 때 그 값도 고친다.
    public static class LayoutConstUi
    {
        public const float ReferenceWidth = 540f;
        public const float ReferenceHeight = 960f;

        // 옛 기준 높이(360×640). Legacy360 껍데기 안의 프리팹 · 아래 상수들이 이 단위다.
        public const float LegacyReferenceHeight = 640f;

        // 옛 px 하나가 새 px 몇 개인가. 960 / 640 = 1.5.
        public const float Legacy360Scale = ReferenceHeight / LegacyReferenceHeight;

        // 띠를 안전 영역 밖(카메라 구멍 · 둥근 모서리 쪽)으로 잇는 덧띠 길이. 어느 폰의 여백보다 넉넉한 값이다.
        public const float BarBleedLength = 200f;

        // 누른 채 이만큼(UI px, 540 기준) 움직이면 탭이 아니라 드래그다. 실제 화면 px 로는 ScreenFitUtil.CanvasScale 을 곱한다.
        // 옛 360 기준 8 과 같은 화면 거리다.
        public const float DragThresholdPx = 12f;

        // 자원바 부재료 그림 칸 (px, 540 기준 — 농장 화면은 껍데기 밖이다). 아이콘을 켜면 글씨를 그만큼 오른쪽으로 민다.
        public const float IngredientIconSize = 27f;
        public const float IngredientIconLeft = 3f;
        public const float IngredientTextIndent = 30f;

        // 떡 그림 (px). 원본이 32px 이라 24 · 32 · 64 · 96 은 ×0.75 · ×1 · ×2 · ×3 이다.
        // 레시피 도감 칸(172×98) — 왼쪽에 64px 그림이 있으면 글자 셋을 오른쪽으로 민다.
        public const float CodexCellTextLeft = 72f;
        public const float CodexCellTextWidth = 94f;

        // 그림이 없을 때 글자 셋이 돌아가는 자리.
        public const float CodexCellTextLeftNoIcon = 6f;
        public const float CodexCellTextWidthNoIcon = 160f;

        // UI 찹쌀이 한 마리. 그림은 64 캔버스 그대로(몸 실체 약 42×35), 볼은 캔버스 가운데에서 좌우 12.5 · 아래 7.5.
        public const float MozziIconSize = 64f;
        public const float MozziIconCheekSize = 11f;
        public const float MozziIconCheekX = 12.5f;
        public const float MozziIconCheekY = 7.5f;
    }
}
