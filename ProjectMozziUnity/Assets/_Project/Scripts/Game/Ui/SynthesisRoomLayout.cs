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

        // 둥근 창 유리 (조각 좌표). 유리는 x 65~124 · y 86~145 라 가운데 (95, 116) · 지름 60. 번쩍 · 반짝이도 이 가운데에서 난다.
        public const float WindowX = 95f;
        public const float WindowY = 116f;
        public const float WindowRadius = 30f;
        public const float FlashSize = 70f;

        // 창 안 두 색 반죽 소용돌이 (그림 fx_swirl_a · b · top, 60×60 × SwirlFrameCount 장). 유리 모양 가림판(MaskSize, 그림 fx_window_mask) 안에
        // 세 겹을 같은 자리에 포개고 SwirlFrameSec 마다 한 장씩 넘긴다 (8장 고리 0.88초).
        // 반죽 두 겹은 넣은 두 마리 몸 색을 곱하고, 진행률 p 만큼 상대 색으로 MixBlendMax·p 물든다.
        public const float MaskSize = 60f;
        public const int SwirlFrameCount = 8;
        public const float SwirlFrameSec = 0.11f;
        public const float MixBlendMax = 0.35f;
        public const float SparkleSize = 11f;

        // 깔때기 가림 칸의 아래끝 (조각 좌표 y) = 그릇 안쪽 앞 가장자리. 합성을 시작하면 찹쌀이가 이 줄 아래로 내려가며 사라진다.
        // 가림 칸은 찹쌀이 겹 자리(64)의 위끝부터 이 줄까지라, 그 높이만큼 내려가면 한 점도 안 남는다.
        public const float InletClipBottom = 25f;
        public const float InletClipHeight = InletClipBottom - InletIconY + LayoutConstUi.MozziIconSize * 0.5f;

        // 서랍 앞판 (쉼 그림). 다 됨 그림은 서랍이 아래로 나오므로 누름 칸은 발 아래까지 넉넉히 잡는다.
        public const float DrawerLeft = 52f;
        public const float DrawerTop = 164f;
        public const float DrawerRight = 120f;
        public const float DrawerBottom = 194f;
        public const float DrawerTouchBottom = 220f;
        public const float DrawerTouchPad = 12f;

        // 다 됨일 때 서랍 속 결과 그림자 · 「?」 가운데 (조각 좌표)와 그림자 배율 (찹쌀이 64 캔버스 그대로 ×1).
        // 다 됨 그림은 서랍이 앞으로 빠져나와 안쪽 칸이 x 70~124 · 바닥 y 192 다 (쉼 그림 앞판보다 오른쪽).
        // 몸(캔버스 y 16~51)의 아래끝을 바닥에 맞추면 머리가 서랍 위로 조금 나온다.
        public const float OutletX = 97f;
        public const float OutletY = 174f;
        public const float OutletScale = 1f;
        public const float OutletMarkFontSize = 20f;
        public const float OutletMarkSize = 32f;

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

        // 결과 연출 (내용 칸 가운데). 찹쌀이 64 캔버스를 정수 배 ×2 로 키워 상자가 128 이다.
        // NEW! · 이름 글자는 상자 가운데에서 위아래로 ResultTextOffset 만큼 떨어진다.
        public const float ResultMozziScale = 2f;
        public const float ResultBoxSize = 128f;
        public const float ResultTextOffset = 96f;
    }
}
