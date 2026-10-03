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

        // 누른 채 이만큼(UI px) 움직이면 탭이 아니라 드래그다. 실제 화면 px 로는 Screen.height / ReferenceHeight 를 곱한다.
        public const float DragThresholdPx = 8f;

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

        // 만들기 줄(170×27) · 손님 주문 칸(68×80).
        public const float CakeIconMakeRowSize = 24f;
        public const float CakeIconMakeRowLeft = 3f;
        public const float MakeRowTextIndent = 28f;

        // 손님 주문 칸. 손님 그림이 들어가며 66×56 에서 키웠고 아래 위젯을 QueueRowShiftY 만큼 내렸다.
        // 폭 68 은 다섯 칸이 360 폭에 드는 상한이다.
        public const float QueueCellWidth = 68f;
        public const float QueueCellHeight = 80f;
        public const float QueueRowShiftY = 24f;

        // 손님 겹 그림 넷이 겹쳐 놓이는 자리 (칸 왼쪽 위 기준). 실체는 38×51 이라 48×64 안에 든다.
        public const float CustomerCanvasWidth = 48f;
        public const float CustomerCanvasHeight = 64f;
        public const float CustomerLayerLeft = -3f;
        public const float CustomerLayerTop = 2f;

        // 주문 떡 그림은 칸 오른쪽 위 구석에 작게. 글자는 손님 그림 아래로 내린다.
        public const float CakeIconQueueSize = 24f;
        public const float CakeIconQueueTop = 2f;
        public const float CakeIconQueueRight = 2f;
        public const float QueueTextTopMargin = 64f;

        // 진열대 묶음 칸 (진열대칸설계 2-1). 40×40 · 사이 3 · 8×3 = 341×126, 옛 ShelfText 자리 (8, -60) 부터.
        // 아래 위젯은 ShelfShiftY 만큼 내렸다 (구역 아래 끝 -186 + 틈 4 = QueueCell -190).
        public const float ShelfCellSize = 40f;
        public const float ShelfCellGap = 3f;
        public const int ShelfCellColumns = 8;
        public const int ShelfCellRows = 3;
        public const float ShelfCellLeft = 8f;
        public const float ShelfCellTop = 60f;
        public const float ShelfShiftY = 86f;

        // 칸 안 떡 그림은 원본 32px 그대로 왼쪽 위, 개수 글자(×5)는 오른쪽 아래 구석.
        public const float CakeIconShelfSize = 32f;
        public const float CakeIconShelfInset = 4f;
        public const float ShelfCountTextWidth = 22f;
        public const float ShelfCountTextHeight = 12f;
        public const float ShelfCountTextInset = 1f;
        public const float ShelfCountFontSize = 10f;
    }
}
