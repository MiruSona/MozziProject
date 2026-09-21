namespace Mozzi.Game.Ui
{
    // 자원바 · 띠의 px 자리 (세로조각1판설계 3-2). 기준 360×640. 프리팹을 만들 때와 Canvas 설정에 쓴다.
    public static class LayoutConstUi
    {
        public const float ReferenceWidth = 360f;
        public const float ReferenceHeight = 640f;

        public const float ResourceBarHeight = 56f;
        public const float BuildingStripHeight = 80f;
        public const float BottomBarHeight = 80f;

        // 부재료 그림 칸 (px). 아이콘을 켜면 글씨를 그만큼 오른쪽으로 민다.
        public const float IngredientIconSize = 18f;
        public const float IngredientIconLeft = 2f;
        public const float IngredientTextIndent = 20f;

        public const float ToggleIconSize = 20f;
        public const float ToggleIconLeft = 4f;
        public const float ToggleTextIndent = 24f;

        // 떡 그림 (px). 원본이 32px 이라 24 · 32 · 64 · 96 은 ×0.75 · ×1 · ×2 · ×3 이다.
        // 레시피 도감 칸(172×98) — 왼쪽에 그림, 글자 셋은 오른쪽으로 민다.
        public const float CakeIconCodexSize = 64f;
        public const float CakeIconCodexLeft = 4f;
        public const float CodexCellTextLeft = 72f;
        public const float CodexCellTextWidth = 94f;

        // 그림이 없을 때 글자 셋이 돌아가는 자리 — 아이콘을 넣기 전 프리팹 값이다.
        public const float CodexCellTextLeftNoIcon = 6f;
        public const float CodexCellTextWidthNoIcon = 160f;

        // 발견 연출 — 이름 글자 위. NEW 글자는 그림 위로 올린다.
        public const float CakeIconMadeSize = 96f;
        public const float CakeIconMadeY = 90f;
        public const float MadeNewTextY = 160f;

        // 만들기 줄(170×27) · 손님 주문 칸(66×56).
        public const float CakeIconMakeRowSize = 24f;
        public const float CakeIconMakeRowLeft = 3f;
        public const float MakeRowTextIndent = 28f;

        public const float CakeIconQueueSize = 32f;
        public const float CakeIconQueueTop = 1f;
        public const float QueueTextTopMargin = 30f;
    }
}
