namespace Mozzi.Game.Ui
{
    // 주방 화면 자리 (Stage 세계 좌표 540×1200 · 왼쪽 위 원점). 정본은 Docs/Reference/Art/정식/주방/README.md 「코드에 넘길 것」.
    // 그림 좌표가 바뀌면 여기 숫자만 베낀다. 그림 크기는 손질기가 sprite 에서 읽는다.
    public static class KitchenLayout
    {
        // 재료 쟁반 왼쪽 위.
        public const float TrayX = 255f;
        public const float TrayY = 520f;

        // 쟁반 칸 여섯의 세계 x (왼끝 · 오른끝)과 y 범위. 재료 아이콘은 칸 아래끝에 바닥을 맞춘다.
        public static readonly float[] TrayCellLeft = { 276f, 313f, 350f, 387f, 424f, 462f };
        public static readonly float[] TrayCellRight = { 304f, 342f, 379f, 417f, 455f, 491f };
        public const float TrayCellTop = 526f;
        public const float TrayCellBottom = 565f;
        public const float TrayIconSize = 28f;

        // 남은 수 글자 가운데 y 와 높이.
        public const float TrayCountY = 580f;
        public const float TrayCountHeight = 22f;

        // 절구 + 떡메 조각의 피벗(절구 바닥 가운데) 세계 자리와 조각 안 피벗 px.
        public const float MortarPivotX = 190f;
        public const float MortarPivotY = 1000f;
        public const float MortarSpritePivotX = 79f;

        // 고른 재료를 절구 아래에 한 줄로 보인다 (가운데 x 는 절구 피벗).
        public const float MortarSlotTop = 1008f;
        public const float MortarSlotSize = 28f;
        public const float MortarSlotGap = 4f;

        // 결과 접시 (아래 가운데)와 떡 · 이름 · NEW 자리.
        public const float PlateX = 420f;
        public const float PlateY = 1000f;
        public const float ResultIconSize = 32f;
        public const float ResultIconBottom = 966f;
        public const float ResultNewY = 900f;
        public const float ResultNameTop = 1008f;
        public const float ResultTextWidth = 220f;

        // 가판대로 돌아가는 문 (분홍 발 문).
        public const float DoorLeft = 22f;
        public const float DoorTop = 325f;
        public const float DoorRight = 185f;
        public const float DoorBottom = 657f;

        // 아래 버튼 줄 폭 (떡메 치기 · 차림표 · 레시피 도감 · 닫기). 합 + 사이 3칸이 540 안에 든다.
        public const float PoundButtonWidth = 160f;
        public const float MenuButtonWidth = 110f;
        public const float CodexButtonWidth = 110f;
        public const float CloseButtonWidth = 100f;

        // 차림표 판 : 2열 × 6줄 (내용 칸 가운데).
        public const int MenuColumns = 2;
        public const float MenuRowWidth = 236f;
        public const float MenuRowHeight = 56f;
        public const float MenuRowGap = 8f;
        public const float MenuBoxPad = 16f;
        public const float MenuTitleHeight = 40f;

        // 떡메 한 번 치기 프레임 간격(초). 4번이 맞는 순간이다.
        public static readonly float[] PoundFrameSec = { 0.06f, 0.05f, 0.05f, 0.04f, 0.12f, 0.06f, 0.06f };
        public const int PoundHitFrame = 4;
    }
}
