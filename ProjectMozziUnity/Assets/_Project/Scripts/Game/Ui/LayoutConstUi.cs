namespace Mozzi.Game.Ui
{
    // 자원바 · 띠의 px 자리 (세로조각1판설계 3-2). 기준은 540×960 이지만 아래 자리 값은 아직 옛 360×640 단위다 —
    // 옛 화면 프리팹이 Legacy360 껍데기(×Legacy360Scale) 안에 붙기 때문이다. 화면을 540 으로 옮길 때 그 값도 고친다.
    public static class LayoutConstUi
    {
        public const float ReferenceWidth = 540f;
        public const float ReferenceHeight = 960f;

        // 옛 기준. Legacy360 껍데기 안의 프리팹 · 아래 상수들이 이 단위다.
        public const float LegacyReferenceWidth = 360f;
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

        public const float ToggleIconSize = 20f;
        public const float ToggleIconLeft = 4f;
        public const float ToggleTextIndent = 24f;

        // 떡 그림 (px). 원본이 32px 이라 24 · 32 · 64 · 96 은 ×0.75 · ×1 · ×2 · ×3 이다.
        // 레시피 도감 칸(172×98) — 왼쪽에 64px 그림이 있으면 글자 셋을 오른쪽으로 민다.
        public const float CodexCellTextLeft = 72f;
        public const float CodexCellTextWidth = 94f;

        // 그림이 없을 때 글자 셋이 돌아가는 자리.
        public const float CodexCellTextLeftNoIcon = 6f;
        public const float CodexCellTextWidthNoIcon = 160f;

        // 만들기 줄(86×22). 아래 판에 4열 3줄로 들어간다.
        public const float CakeIconMakeRowSize = 20f;
        public const float CakeIconMakeRowLeft = 2f;
        public const float MakeRowTextIndent = 22f;

        // 손님 겹 그림 넷이 겹쳐 놓이는 캔버스. 실체는 38×51 이라 48×64 안에 든다.
        public const float CustomerCanvasWidth = 48f;
        public const float CustomerCanvasHeight = 64f;

        // 진열장 칸 안 떡 그림(원본 32px 그대로)과 개수 글자(×5) 크기.
        public const float CakeIconShelfSize = 32f;
        public const float ShelfCountFontSize = 10f;

        // ── 정식 화면 (가판대 · 합성소). 프리팹 자리는 Editor 의 FormalScreenSetup 이 이 값으로 놓는다.
        // 「그림 좌표」 = 옛 기준 360×640(Legacy360 안), 왼쪽 위 원점, y 아래로. 배경 그림은 360×800 이라 위아래로 80 씩 더 있다.
        // 장면 칸(Scene)은 배경 · 기계 · 손님을 다 품고 같이 움직인다. 내용 칸 위끝에 붙여, 화면이 기준보다 길면
        // 남는 세로를 전부 아래(바닥 쪽)로 준다 — SceneAnchorY 1 이 위끝, 0.5 면 가운데다.
        public const float SceneAnchorY = 1f;
        public const float SceneBgWidth = 360f;
        public const float SceneBgHeight = 800f;

        // 아래 판은 내용 칸 바닥에 붙은 고정 높이다. 길쭉한 폰에서는 판이 안 늘고 가게 · 마루가 더 보인다.
        public const float StallPanelHeight = 200f;
        public const float SynthesisPanelHeight = 176f;
        public const float StallTopBarHeight = 60f;
        public const float SynthesisTopBarHeight = 40f;

        // 가판대 : 유리 진열장 안 묶음 칸 여섯 (칸 36×48, 개수 글자가 위 · 떡 32px 가 아래).
        public const float StallGlassCellLeft = 76f;
        public const float StallGlassCellTop = 258f;
        public const float StallGlassCellPitch = 36f;
        public const float StallGlassCellWidth = 36f;
        public const float StallGlassCellHeight = 48f;
        public const float StallGlassIconLeft = 2f;
        public const float StallGlassIconTop = 14f;
        public const float StallGlassCountHeight = 13f;

        // 가판대 : 칠판 안쪽에 덧대는 판.
        public const float StallMenuBoardLeft = 56f;
        public const float StallMenuBoardTop = 150f;
        public const float StallMenuBoardWidth = 66f;
        public const float StallMenuBoardHeight = 56f;
        public const float StallMenuTitleHeight = 15f;
        public const int StallMenuLineMax = 3;

        // 가판대 : 손님 줄. 칸 68×96 다섯이 72 간격, 아래 64 는 손님 (발이 그림 좌표 y 432).
        // 말풍선은 칸 위 가운데에 뜬다. 떡 그림만일 때는 좁게, 글자(아무거나 · 못 찾은 떡)일 때는 넓게.
        public const float StallQueueLeft = 2f;
        public const float StallQueuePitch = 72f;
        public const float StallQueueTop = 338f;
        public const float StallQueueCellWidth = 68f;
        public const float StallQueueCellHeight = 96f;
        public const float StallBubbleHeight = 28f;
        public const float StallCustomerLeft = 10f;
        public const float StallCustomerTop = 30f;
        public const float StallBubbleWidthIcon = 30f;
        public const float StallBubbleWidthText = 60f;
        public const float StallBubbleIconSize = 24f;

        // 합성소 : 기계 그림(164×199) 왼쪽 위 · 창 중심 · 투입구 찹쌀이 중심 · 배출구 앞 결과 중심.
        public const float SynthesisMachineLeft = 98f;
        public const float SynthesisMachineTop = 193f;
        public const float SynthesisMachineWidth = 164f;
        public const float SynthesisMachineHeight = 199f;
        public const float SynthesisWindowCenterX = 179.5f;
        public const float SynthesisWindowCenterY = 286f;
        public const float SynthesisWindowSize = 48f;
        public const float SynthesisInletAX = 138f;
        public const float SynthesisInletBX = 223f;
        public const float SynthesisInletY = 194f;
        // 투입구 누름 칸 한 변. 두 투입구 사이가 85 라 서로 안 겹친다.
        public const float SynthesisInletTouchSize = 80f;
        // 빈 입구 표시(+)를 누름 칸 가운데에서 이만큼 내려 깔때기 안 어두운 자리에 둔다.
        public const float SynthesisInletPlusDrop = 17f;
        public const float SynthesisOutletX = 180f;
        public const float SynthesisOutletY = 407f;
        public const float SynthesisFlashSize = 64f;
        public const float SynthesisSparkleSize = 11f;

        // UI 찹쌀이 한 마리. 그림은 64 캔버스 그대로(몸 실체 약 42×35), 볼은 캔버스 가운데에서 좌우 12.5 · 아래 7.5.
        public const float MozziIconSize = 64f;
        public const float MozziIconCheekSize = 11f;
        public const float MozziIconCheekX = 12.5f;
        public const float MozziIconCheekY = 7.5f;
    }
}
