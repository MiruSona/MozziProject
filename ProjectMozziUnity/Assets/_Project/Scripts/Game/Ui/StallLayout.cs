namespace Mozzi.Game.Ui
{
    // 가판대 화면 자리 (Stage 세계 좌표 540×1200 · 왼쪽 위 원점). 정본은 Docs/Reference/Art/정식/가판대/README.md
    // 「2차 · 코드에 넘길 것」 과 「손님 3차」. 그림 좌표가 바뀌면 여기 숫자만 베낀다. 그림 크기는 손질기가 sprite 에서 읽는다.
    public static class StallLayout
    {
        // 칠판 머리 장식 가운데. 「오늘의 떡」 글자를 그 위에 얹는다.
        public const float BoardTopperX = 213f;
        public const float BoardTopperY = 337f;

        // 칠판 안쪽 녹색 면 = 진열 요약 글자 자리 = 누르는 칸.
        public const float BoardLeft = 103f;
        public const float BoardTop = 347f;
        public const float BoardRight = 319f;
        public const float BoardBottom = 475f;

        // 계산대 왼쪽 위. 왼끝이 화면 밖(−17)이라 화면 왼쪽부터 이어져 보인다.
        public const float CounterX = -17f;
        public const float CounterY = 660f;

        // 주인 그림 왼쪽 위. 허리 아래는 계산대 뒤로 숨는다.
        public const float OwnerX = 206f;
        public const float OwnerY = 518f;

        // 계산대 위 물건이 발을 디디는 줄과 물건마다 아래 가운데 x.
        public const float ItemLineY = 682f;
        public const float CaseX = 96f;
        public const float VaseX = 200f;
        public const float RegisterX = 359f;

        // 메뉴 세움판은 계산대에 자리가 없어 문 앞 마루에 세운다 (아래 가운데).
        public const float MenuStandX = 468f;
        public const float MenuStandY = 712f;

        // 진열장 그림 왼쪽 위와, 그 안 접시 여섯(2단 × 3)의 가운데 x · 떡 바닥 y (진열장 그림 안 좌표).
        public const float CaseLeft = 8f;
        public const float CaseTop = 559f;
        public static readonly float[] PlateCenterX = { 41f, 88f, 135f };
        public static readonly float[] PlateCakeBottom = { 45f, 83f };
        public const float ShelfIconSize = 32f;

        // 앞 손님 셋의 발끝 x (줄 번호 순서)와 왼쪽을 보는지. 넷째부터는 「뒤에 N명」 글자로만 센다.
        // 오른쪽을 보는 손님 몸은 발끝에서 왼쪽 51 · 오른쪽 44, 머리 폭은 약 90 이라 왼쪽 둘은 112 떨어뜨린다.
        public static readonly float[] QueueFootX = { 190f, 410f, 78f };
        public static readonly bool[] QueueFacesLeft = { false, true, false };
        public const float QueueFootY = 1060f;
        public const int QueueShown = 3;

        // 손님 겹 캔버스(120×240) 안 발끝 비율. 넷 다 같은 캔버스 · 같은 피벗이다.
        public const float CustomerPivotX = 0.55f;

        // 머리 꼭대기 = 발끝 − 215, 머리 가운데 = 발끝 x 에서 보는 쪽 반대로 3.
        public const float HeadHeight = 215f;
        public const float HeadCenterOffset = 3f;

        // 말풍선. 꼬리 끝이 머리 꼭대기 위 BubbleGap 에 오고, 9-slice 판은 꼬리가 왼쪽 아래에서 BubbleTailX 안쪽이다.
        // 떡 그림 주문은 넓은 판(56×54), 「아무거나」 는 꼬리 가운데 작은 판(54×29 고정), 긴 글자는 넓은 판을 TextMaxWidth 까지 늘린다.
        public const float BubbleGap = 3f;
        public const float BubbleTailX = 10f;
        public const float BubbleIconWidth = 56f;
        public const float BubbleHeight = 54f;
        public const float BubbleTextMaxWidth = 96f;
        public const float BubbleIconSize = 32f;
        public const float BubbleIconBottom = 16f;

        // 「뒤에 N명」 글자 (가운데 · 높이). 오른쪽 손님 발밑과 버튼 줄 사이.
        public const float QueueMoreX = 410f;
        public const float QueueMoreY = 1078f;
        public const float QueueMoreWidth = 220f;
        public const float QueueMoreHeight = 30f;

        // 윗줄 손님 · 영업 상태 판 (골드 판 오른쪽 · 내용 칸 위끝 기준). 문(x 364~)·칠판 장식(y 328~)을 안 가린다.
        public const float StatusPlateLeft = 194f;
        public const float StatusPlateWidth = 160f;

        // 아래 버튼 줄 폭 (열기 · 주방 · 점원 · 레시피 도감 · 닫기). 합 496 + 사이 4칸 32 = 528 로 540 안에 든다.
        public const float OpenButtonWidth = 120f;
        public const float KitchenButtonWidth = 76f;
        public const float ClerkButtonWidth = 108f;
        public const float CodexButtonWidth = 116f;
        public const float CloseButtonWidth = 76f;
    }
}
