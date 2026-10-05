namespace Mozzi.Game.Ui
{
    // 주방 화면 자리 (Stage 세계 좌표 540×1200 · 왼쪽 위 원점). 그림 정본은 Docs/Reference/Art/정식/주방/v3/README.md (반복 07).
    // 서랍장 · 찜기 · 빚기판 · 그릇은 반복 06 상자 판의 자리 그대로 정식 그림을 얹었다 (그림 크기가 상자 크기에 맞춰 나왔다).
    public static class KitchenLayout
    {
        // 서랍장 (오른쪽 벽~마루) : 4열 × 3줄. 틀은 칸을 Pad 만큼 둘러싼 어두운 상자다.
        public const int DrawerColumns = 4;
        public const float DrawerLeft = 302f;
        public const float DrawerTop = 480f;
        public const float DrawerCellWidth = 50f;
        public const float DrawerCellHeight = 80f;
        public const float DrawerGapX = 6f;
        public const float DrawerGapY = 12f;
        public const float DrawerFramePad = 10f;

        // 칸 안 : 재료 아이콘(가운데 y) · 남은 수 글자(가운데 y). 칸 위끝 기준.
        public const float DrawerIconSize = 28f;
        public const float DrawerIconY = 30f;
        public const float DrawerCountY = 62f;
        public const float DrawerCountHeight = 22f;

        // 고른 재료 그릇 (가운데) : 고른 1~3개 아이콘이 한 줄로 뜬다.
        public const float BowlX = 240f;
        public const float BowlY = 778f;
        public const float BowlWidth = 110f;
        public const float BowlHeight = 36f;
        public const float MortarSlotSize = 28f;
        public const float MortarSlotGap = 4f;

        // 낮은 작업대 한 줄 (왼쪽 → 오른쪽 : 찜기 → 절구 → 빚기판 + 접시). 바닥 y 는 셋 다 WorkBottom.
        public const float WorkBottom = 1040f;
        public const float SteamerLeft = 30f;
        public const float SteamerRight = 180f;
        public const float SteamerTop = 900f;

        // 절구 + 떡메 조각의 피벗(절구 바닥 가운데) 세계 자리와 조각 안 피벗 px.
        // 조각 212 폭 중 절구 몸은 피벗 −76 ~ +75, 떡메 머리는 위쪽에서 피벗 +128 까지 나간다 (몸 194~345 · 떡메 끝 398).
        public const float MortarPivotX = 270f;
        public const float MortarPivotY = WorkBottom;
        public const float MortarSpritePivotX = 79f;

        // 빚기판은 떡메 머리(위쪽)를 피해 낮게 둔다.
        public const float BoardLeft = 370f;
        public const float BoardRight = 520f;
        public const float BoardTop = 960f;

        // 도구를 누르면 상자가 한 번 통통 (scale 1 → BounceScale → 1).
        public const float BounceSec = 0.15f;
        public const float BounceScale = 1.12f;

        // 결과 접시는 빚기판 위에 얹고, 떡 · 이름 · NEW 는 그 위로 쌓는다.
        // 떡 그림은 캔버스 바닥에 붙어 있어 ResultIconBottom 이 곧 접시 면 위 떡 밑이다 (접시 바닥에서 23 위).
        public const float PlateX = 452f;
        // 빚기판은 다리 달린 낮은 상이라 윗면이 BoardTop 보다 아래에 있다 (반복 07 정식 그림 · v3 README).
        public const float PlateY = BoardTop + 14f;
        public const float ResultIconSize = 32f;
        public const float ResultIconBottom = PlateY - 23f;
        public const float ResultNewY = 822f;
        public const float ResultNameTop = 846f;
        public const float ResultTextWidth = 170f;

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

        // 떡메 한 번 치기 프레임 간격(초). 4번이 맞는 순간이다.
        public static readonly float[] PoundFrameSec = { 0.06f, 0.05f, 0.05f, 0.04f, 0.12f, 0.06f, 0.06f };
        public const int PoundHitFrame = 4;

        // 떡메 단계 (반죽 모습) : 0 쌀 · 1 거친 반죽 · 2 매끈한 떡 (반복 07).
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

        // 한 번 치기 안에서 frame 장이 쓸 단계. 맞는 장 앞은 from, 맞는 장부터는 to.
        public static int PoundFrameStage(int frame, int from, int to)
        {
            if (frame < PoundHitFrame)
            {
                return from;
            }

            return to;
        }

        // 김 : 찜기 뚜껑 손잡이 바로 위 (64×64 그림의 왼쪽 위 자리). 흰 한 색 그림에 알파를 곱한다. 누름 한 번에 4장 한 바퀴.
        public const float SteamLeft = 73f;
        public const float SteamTop = 842f;
        public const float SteamAlpha = 200f / 255f;
        public static readonly float[] SteamFrameSec = { 0.15f, 0.15f, 0.15f, 0.15f };

        // 빚기 : 64×48 그림의 떡 바닥 가운데 (32, 42) 를 접시 자리 (PlateX, PlateY) 에. 3장 한 바퀴 뒤 접시 · 결과가 뜬다.
        public const float ShapeSpriteWidth = 64f;
        public const float ShapeSpriteHeight = 48f;
        public const float ShapeSpriteBaseY = 42f;
        public static readonly float[] ShapeFrameSec = { 0.25f, 0.25f, 0.6f };
    }
}
