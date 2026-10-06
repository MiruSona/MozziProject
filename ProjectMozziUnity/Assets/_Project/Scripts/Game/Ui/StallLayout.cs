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

        // 계산대 왼쪽 위. 왼끝이 화면 밖(−17)이라 화면 왼쪽부터 이어져 보인다. 그림(585 폭)은 오른끝도 화면 밖(568)까지 가고 Stage 의 RectMask2D 가 자른다.
        public const float CounterX = -17f;
        public const float CounterY = 660f;

        // 주인 그림 왼쪽 위. 허리 아래는 계산대 뒤로 숨는다. 문(x 364~507) 앞에 서서 왼쪽에서 오는 손님을 맞는다 (그림 125 폭 · 가운데 435).
        public const float OwnerX = 373f;
        public const float OwnerY = 518f;

        // 장식 다섯 (그림만, 누름 없음)의 왼쪽 아래 끝. 정본 : 정식/반복08/README 「코드에 넘길 것」.
        // 벽 셋(액자 · 선반 · 등나무 접시)은 배경 바로 위, 바닥 둘(화분 · 칠판 간판)은 계산대 앞 · 손님 뒤다.
        public const float DecorFrameX = 14f;
        public const float DecorFrameY = 335f;
        public const float DecorShelfX = 175f;
        public const float DecorShelfY = 330f;
        public const float DecorWreathX = 400f;
        public const float DecorWreathY = 330f;
        public const float DecorPlantX = 392f;
        public const float DecorPlantY = 785f;
        public const float DecorSignX = 474f;
        public const float DecorSignY = 785f;

        // 계산대 위 물건이 발을 디디는 줄과 물건마다 아래 가운데 x.
        public const float ItemLineY = 682f;
        public const float CaseX = 96f;
        public const float VaseX = 200f;
        public const float RegisterX = 359f;

        // 진열장 그림 왼쪽 위와, 그 안 접시 여섯(2단 × 3)의 가운데 x · 떡 바닥 y (진열장 그림 안 좌표).
        public const float CaseLeft = 8f;
        public const float CaseTop = 559f;
        public static readonly float[] PlateCenterX = { 41f, 88f, 135f };
        public static readonly float[] PlateCakeBottom = { 45f, 83f };

        // 접시에 쌓는 한 개짜리 떡 그림의 크기 (Art/CakeOne). 그림 22×16 = 떡 21×15 + 오른쪽 · 아래 빈 1줄.
        public const float ShelfCakeWidth = 22f;
        public const float ShelfCakeHeight = 16f;

        // 접시 하나에 쌓아 보이는 떡 수 : 개수가 문턱 k 번째 값 이상이면 k+1 개 (1~4개 = 1 · 5~9개 = 2 · 10개 이상 = 3).
        public static readonly int[] PlateStackThresholds = { 1, 5, 10 };

        // 쌓는 꼴. [보이는 떡 수 - 1][몇 번째 떡] = 접시 가운데에서 오른쪽 · 위로 옮기는 px. 뒤 번호가 앞에 그려진다.
        // 2개는 나란히 놓아 살짝 겹치고, 3개는 아래 둘 + 위 하나다. 아랫단은 선반 턱까지 26px 이라 위 떡(8 + 떡 높이 15)이 3px 남기고 든다.
        public static readonly float[][] PlateStackX = { new float[] { 0f }, new float[] { -7f, 7f }, new float[] { -9f, 9f, 0f } };
        public static readonly float[][] PlateStackY = { new float[] { 0f }, new float[] { 2f, 0f }, new float[] { 0f, 0f, 8f } };

        // 앞 손님 셋의 발끝 x (줄 번호 순서). 맨 앞이 등록기 앞이고 뒤로 갈수록 왼쪽, 모두 오른쪽(주인 쪽)을 본다.
        // 손님 몸은 발끝에서 왼쪽 51 · 오른쪽 44, 머리 폭은 약 90 이라 112 씩 띄운다. 넷째부터는 「뒤에 N명」 글자로만 센다.
        public static readonly float[] QueueFootX = { 300f, 188f, 76f };
        public const float QueueFootY = 1060f;
        public const int QueueShown = 3;

        // 새 손님은 화면 왼쪽 밖 발끝 x QueueEnterX 에서 제 자리로 걸어오고, 앞 손님이 빠지면 남은 손님이 한 칸씩 당겨진다.
        // 걷는 동안 QueueWalkHops 번 QueueHopHeight px 통통 뛴다.
        public const float QueueEnterX = -60f;
        public const float QueueWalkSec = 0.35f;
        public const float QueueHopHeight = 2f;
        public const int QueueWalkHops = 2;

        // 손님 겹 캔버스(120×240) 안 발끝 비율. 넷 다 같은 캔버스 · 같은 피벗이다.
        public const float CustomerPivotX = 0.55f;

        // 머리 꼭대기 = 발끝 − 215, 머리 가운데 = 발끝 x − 3 (보는 쪽 반대).
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
        // 떡 그림(캔버스 바닥 1줄 위)의 가운데가 풍선 안쪽 가운데에 오는 값. 외곽선 꼴 ① (떡 높이 20) 은 19, 꼴 ② (높이 18) 는 20.
        public const float BubbleIconBottom = 19f;

        // 「뒤에 N명」 글자 (가운데 · 높이). 줄 맨 끝(왼쪽) 손님 발밑과 버튼 줄 사이. 폭 140 이라 x 6~146 이다.
        public const float QueueMoreX = 76f;
        public const float QueueMoreY = 1078f;
        public const float QueueMoreWidth = 140f;
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
