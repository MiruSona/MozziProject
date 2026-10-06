namespace Mozzi.Game.Ui
{
    // 주방 화면 자리 (Stage 세계 좌표 540×1200 · 왼쪽 위 원점). 그림 정본은 Docs/Reference/Art/정식/주방/v3/README.md (반복 07)
    // · Docs/Reference/Art/정식/반복08/README.md (반복 08 : 입체 서랍장 · 맷돌 · 찜기 몸통+뚜껑 · 새 반죽 프레임).
    public static class KitchenLayout
    {
        // 입체 서랍장 그림 (238×284) 의 왼쪽 위. 칸 4열 × 3줄은 그림 안 앞판 자리다 (칸 덧그림 없음 — 앞판이 그림에 들어 있다).
        public const int DrawerColumns = 4;
        public const float DrawerLeft = 300f;
        public const float DrawerTop = 470f;
        public const float DrawerWidth = 238f;
        public const float DrawerHeight = 284f;

        // 칸 왼쪽 위 (그림 안 좌표). 열 x · 줄 y, 칸 크기 42×70 (위쪽 10px 은 손잡이).
        public static readonly float[] DrawerCellX = { 13f, 62f, 111f, 159f };
        public static readonly float[] DrawerCellY = { 22f, 102f, 178f };
        public const float DrawerCellWidth = 42f;
        public const float DrawerCellHeight = 70f;

        // 칸 안 : 재료 아이콘 가운데 (21, 32) · 남은 수 글자(가운데 y). 칸 왼쪽 위 기준.
        public const float DrawerIconSize = 28f;
        public const float DrawerIconX = 21f;
        public const float DrawerIconY = 32f;
        public const float DrawerCountY = 57f;
        public const float DrawerCountHeight = 22f;

        // 고른 재료 그릇 (가운데) : 고른 1~3개 아이콘이 한 줄로 뜬다.
        public const float BowlX = 240f;
        public const float BowlY = 778f;
        public const float BowlWidth = 110f;
        public const float BowlHeight = 36f;
        public const float MortarSlotSize = 28f;
        public const float MortarSlotGap = 4f;

        // 작업대 두 줄 지그재그 (반복 08). 흐름 순서가 왼쪽 → 오른쪽으로 읽힌다 :
        //   뒷줄 맷돌(왼쪽 끝) → 앞줄 절구 → 앞줄 빚기판 → 뒷줄 찜기(오른쪽 끝, 서랍장 아래).
        // 앞줄 바닥은 WorkBottom, 뒷줄 바닥은 BackBottom. 도구 상자(누름 칸)는 서로 안 겹친다.
        public const float WorkBottom = 1040f;
        public const float BackBottom = 952f;

        // 맷돌 (99×96 · 0 쉼 · 1~4 돌기).
        public const float MillLeft = 12f;
        public const float MillTop = BackBottom - 96f;
        public const float MillRight = MillLeft + 99f;

        // 찜기 몸통 · 뚜껑 (둘 다 150×140, 같은 자리에 겹친다).
        public const float SteamerLeft = 380f;
        public const float SteamerRight = 530f;
        public const float SteamerTop = BackBottom - 140f;
        public const float SteamerX = (SteamerLeft + SteamerRight) * 0.5f;

        // 절구 + 떡메 조각의 피벗(절구 바닥 가운데) 세계 자리와 조각 안 피벗 px.
        // 조각 212 폭 중 피벗 −79 ~ +133 (몸 −76 ~ +75, 떡메 끝 +128) → 조각 119~331 이라 맷돌(~111) · 빚기판(340~) 과 안 겹친다.
        public const float MortarPivotX = 198f;
        public const float MortarPivotY = WorkBottom;
        public const float MortarSpritePivotX = 79f;

        // 빚기판은 앞줄 오른쪽. 위끝(960)이 찜기 바닥(952) 아래라 찜기와 안 겹친다.
        public const float BoardLeft = 340f;
        public const float BoardRight = 490f;
        public const float BoardTop = 960f;
        public const float BoardX = (BoardLeft + BoardRight) * 0.5f;
        // 빚기판은 다리 달린 낮은 상이라 윗면이 BoardTop 보다 아래에 있다 (반복 07 정식 그림 · v3 README).
        public const float BoardSurfaceY = BoardTop + 14f;

        // 도구를 누르면 상자가 한 번 통통 (scale 1 → BounceScale → 1).
        public const float BounceSec = 0.15f;
        public const float BounceScale = 1.12f;

        // 결과 판 (반복 08 — 접시 없음) : 서랍장 왼쪽 빈 바닥(문 아래 · 그릇 위)에 남색 반투명 둥근 판을 깔고
        // 왼쪽에 떡 그림, 오른쪽에 「이름 / 값」 두 줄, 값 줄 오른쪽 끝에 NEW 를 둔다.
        // 찜기 위 · 서랍장 아래 틈(754~812)은 좁아 글자가 서랍장과 겹쳤고, 찜기 왼쪽 바닥은 떡메 자루가 지나가서 피했다.
        // 판 x 20~290 · y 664~752 : 서랍장(300~) · 그릇(위 약 765) · 문 바닥(약 655) 과 안 겹친다.
        public const float ResultPanelLeft = 20f;
        public const float ResultPanelTop = 664f;
        public const float ResultPanelWidth = 270f;
        public const float ResultPanelHeight = 88f;
        public const float ResultPanelMidY = ResultPanelTop + ResultPanelHeight * 0.5f;
        public const float ResultIconSize = 64f;
        public const float ResultIconLeft = ResultPanelLeft + 8f;
        public const float ResultTextLeft = ResultIconLeft + ResultIconSize + 8f;
        public const float ResultTextRight = ResultPanelLeft + ResultPanelWidth - 8f;
        public const float ResultTextHeight = 64f;
        public const float ResultNewWidth = 100f;
        public const float ResultNewHeight = 28f;
        // NEW 는 값 줄(아랫줄) 높이에 오른쪽 끝으로 붙인다.
        public const float ResultNewY = ResultPanelMidY + ResultTextHeight * 0.25f;

        // 찌기 (반복 08) : 누를 것 없이 SteamSec 초(실제 시간) 기다리는 동안 김이 되풀이되고, 끝나면 뚜껑이 열리며 결과가 뜬다.
        public const float SteamSec = 2.5f;
        // 뚜껑 올림 칸 (px, 위가 +). 그림 정본 찜기뚜껑_3x.gif 의 y 0 → −6 → −14 → −24.
        public static readonly float[] LidOpenRise = { 0f, 6f, 14f, 24f };
        // 한 칸 시간 : 다 찐 뒤는 천천히, 바로 만들기 · 차림표 · 연타 건너뛰기는 짧게.
        public const float LidStepSec = 0.1f;
        public const float LidQuickStepSec = 0.05f;

        // 가판대로 돌아가는 문 (분홍 발 문).
        public const float DoorLeft = 22f;
        public const float DoorTop = 325f;
        public const float DoorRight = 185f;
        public const float DoorBottom = 657f;

        // 아래 버튼 줄 폭 (차림표 · 레시피 도감 · 닫기). 합 + 사이 2칸이 540 안에 든다.
        public const float MenuButtonWidth = 140f;
        public const float CodexButtonWidth = 160f;
        public const float CloseButtonWidth = 120f;

        // 차림표 판 : 2열 × 최대 6줄, 상자 폭 512. 가판대 칠판 판도 같은 판이다 (FormalUiBuild.MakeRecipeMenuPanel).
        // 손이 닿기 쉽게 화면 아래에 둔다 (반복 06) : 상자 아래끝 = 내용 칸 아래끝에서 MenuBoxBottom = 아래 버튼 줄 위끝(104) + 8.
        // 높이는 찾은 떡 줄 수만큼 위로 자란다 (MenuBoxHeight). 다 차도 456 이라 위끝이 아래에서 568 — 9:16(높이 960)에서도 다 보인다.
        public const float MenuBoxBottom = FormalLayout.RowBottom + FormalLayout.RowHeight + 8f;
        // 찾은 떡이 없을 때 안내 글(줄 높이 둘)이 들어갈 줄 수.
        public const int MenuEmptyLines = 2;
        public const int MenuColumns = 2;
        public const float MenuRowWidth = 236f;
        public const float MenuRowHeight = 56f;
        public const float MenuRowGap = 8f;
        public const float MenuBoxPad = 16f;
        public const float MenuTitleHeight = 40f;

        // 떡 rowCount 개를 담는 상자 높이. 2열이라 줄 수는 올림이고, 0개면 안내 글 자리(MenuEmptyLines)만큼이다.
        public static float MenuBoxHeight(int rowCount)
        {
            int lines = rowCount <= 0 ? MenuEmptyLines : (rowCount + MenuColumns - 1) / MenuColumns;
            return MenuBoxPad * 2f + MenuTitleHeight + lines * (MenuRowHeight + MenuRowGap);
        }

        // 떡메 한 번 치기 프레임 간격(초). 5번이 맞는 순간이다 (반복 08 새 프레임 — 4 → 5).
        public static readonly float[] PoundFrameSec = { 0.06f, 0.05f, 0.05f, 0.04f, 0.04f, 0.12f, 0.06f };
        public const int PoundHitFrame = 5;

        // 맷돌 한 번 돌리기 (0 쉼 · 1~4 돌기). 묶음이 하나라 맞는 장이 없다 (0 을 넘긴다).
        public static readonly float[] MillFrameSec = { 0.03f, 0.07f, 0.07f, 0.07f, 0.07f };

        // 떡메 단계 (반죽 모습) : 0 간 가루 · 1 거친 반죽 · 2 매끈한 떡 (반복 08 — 첫 단계가 쌀에서 가루로).
        // 친 수만큼 한 단계씩 나아가고 마지막 단계에서 멈춘다. 반죽은 맞는 순간(PoundHitFrame)에 다음 단계로 바뀐다
        // — 친 덕에 반죽이 변한 것으로 읽히게. 그래서 k 번째 타는 앞 3장이 (k−1) 단계, 맞는 장부터가 k 단계다.
        public const int PoundStageLast = 2;

        public static int PoundStageAfter(int taps)
        {
            if (taps <= 0)
            {
                return 0;
            }

            if (taps >= PoundStageLast)
            {
                return PoundStageLast;
            }

            return taps;
        }

        // 한 번 치기 안에서 frame 장이 쓸 단계. 맞는 장(hitFrame) 앞은 from, 맞는 장부터는 to.
        public static int FrameStage(int frame, int hitFrame, int from, int to)
        {
            if (frame < hitFrame)
            {
                return from;
            }

            return to;
        }

        // 김 : 찜기 뚜껑 손잡이 바로 위 (64×64 그림의 왼쪽 위 자리, 찜기 왼쪽 위에서 (43, −58)). 흰 한 색 그림에 알파를 곱한다.
        // 찌는 동안 4장을 되풀이한다.
        public const float SteamLeft = SteamerLeft + 43f;
        public const float SteamTop = SteamerTop - 58f;
        public const float SteamAlpha = 200f / 255f;
        public static readonly float[] SteamFrameSec = { 0.15f, 0.15f, 0.15f, 0.15f };

        // 빚기 : 64×48 그림의 떡 바닥 가운데 (32, 42) 를 빚기판 윗면 가운데 (BoardX, BoardSurfaceY) 에. 3장 한 바퀴 뒤 숨는다.
        public const float ShapeSpriteWidth = 64f;
        public const float ShapeSpriteHeight = 48f;
        public const float ShapeSpriteBaseY = 42f;
        public static readonly float[] ShapeFrameSec = { 0.25f, 0.25f, 0.6f };
    }
}
