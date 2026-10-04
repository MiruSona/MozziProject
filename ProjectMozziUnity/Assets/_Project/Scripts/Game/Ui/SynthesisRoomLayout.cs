namespace Mozzi.Game.Ui
{
    // 합성소 화면 자리 (Stage 세계 좌표 540×1200 · 왼쪽 위 원점). 정본은 Docs/Reference/Art/정식/합성소/README.md 「코드에 넘길 것」.
    // 「조각 좌표」는 기계 조각(208×220) 왼쪽 위 원점이다. 그림 좌표가 바뀌면 여기 숫자만 베낀다.
    public static class SynthesisRoomLayout
    {
        // 칸 넷의 피벗(받침 발 가운데)이 놓일 세계 자리. 순서 = 칸 번호 (위 왼 · 위 오른 · 아래 왼 · 아래 오른).
        public static readonly float[] SlotPivotX = { 135f, 405f, 135f, 405f };
        public static readonly float[] SlotPivotY = { 670f, 670f, 950f, 950f };

        // 기계 조각 캔버스와 그 안의 피벗. 쉼 · 도는 8장 · 다 됨 · 살 칸 · 잠김이 모두 같은 캔버스 · 같은 피벗이다.
        public const float PieceWidth = 208f;
        public const float PieceHeight = 220f;
        public const float PiecePivotX = 104f;
        public const float PiecePivotY = 200f;

        // 깔때기 입구 가운데 (조각 좌표). 찹쌀이는 입구에 반쯤 걸치게 조금 위에 둔다.
        public const float InletAX = 56f;
        public const float InletBX = 142f;
        public const float InletY = 20f;
        public const float InletIconY = 4f;
        public const float InletTouchWidth = 80f;
        public const float InletTouchHeight = 72f;

        // 빈 깔때기 위 「+」 말풍선 (조각 좌표, 가운데). 깔때기 위끝보다 위로 뜬다.
        public const float PlusBubbleY = -22f;
        public const float PlusBubbleWidth = 36f;
        public const float PlusBubbleHeight = 34f;

        // 둥근 창 (음양 · 도는 통).
        public const float WindowX = 97f;
        public const float WindowY = 117f;
        public const float WindowRadius = 31f;
        public const float YinSize = 50f;
        public const float FlashSize = 70f;
        public const float SparkleSize = 11f;

        // 서랍 앞판 (쉼 그림). 다 됨 그림은 서랍이 아래로 나오므로 누름 칸은 발 아래까지 넉넉히 잡는다.
        public const float DrawerLeft = 52f;
        public const float DrawerTop = 164f;
        public const float DrawerRight = 120f;
        public const float DrawerBottom = 194f;
        public const float DrawerTouchBottom = 220f;
        public const float DrawerTouchPad = 12f;

        // 다 됨일 때 서랍 속 결과 그림자 · 「?」 가운데 (조각 좌표)와 그림자 배율 (찹쌀이 64 캔버스 → 32).
        public const float OutletX = 86f;
        public const float OutletY = 180f;
        public const float OutletScale = 0.5f;

        // 살 칸 간판의 빈 크림색 면 (조각 좌표). 「칸 사기 300G」 글자가 들어간다.
        public const float SignLeft = 48f;
        public const float SignTop = 48f;
        public const float SignRight = 160f;
        public const float SignBottom = 102f;

        // 나무 표 : 칸 피벗 y + PlateTop ~ + PlateBottom, 폭 PlateWidth 가운데 맞춤 (9-slice).
        public const float PlateTop = 4f;
        public const float PlateBottom = 30f;
        public const float PlateWidth = 150f;

        // 도는 프레임 한 장 시간 (8장 고리 0.72초).
        public const float RunFrameSec = 0.09f;

        // 아래 버튼 줄 폭 (단축 사기 · 찹쌀이 도감 · 닫기).
        public const float ShortcutButtonWidth = 220f;
        public const float CodexButtonWidth = 120f;
        public const float CloseButtonWidth = 100f;

        // 골드 판 (내용 칸 왼쪽 위 기준). 9:16 에서 벽 a 의 계기판 · 밸브를 피해 왼쪽 위 빈 회벽에 작게 둔다.
        public const float GoldPlateLeft = 50f;
        public const float GoldPlateTop = 4f;
        public const float GoldPlateWidth = 124f;
        public const float GoldPlateHeight = 36f;

        // 찹쌀이 고르기 판 : 2열 × 12줄 (내용 칸 아래, 버튼 줄 위).
        public const int PickColumns = 2;
        public const float PickRowWidth = 244f;
        public const float PickRowHeight = 40f;
        public const float PickRowGap = 6f;
        public const float PickBoxPad = 12f;
        public const float PickTitleHeight = 44f;
        public const float PickCloseWidth = 80f;

        // 결과 연출 상자 (내용 칸 가운데).
        public const float ResultBoxSize = 96f;
    }
}
