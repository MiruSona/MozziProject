using UnityEngine;

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
        // 반복 09 : 칸3 에서는 이 자리가 빚기 판 위쪽을 가려, 벽 가운데 결과 떡(CakeSpot 270,480) 바로 아래 y 516~604 로 옮겼다.
        // 아래 끝 604 : 찜기 뚜껑 꼭지(약 606)에 안 닿는다.
        public const float ResultPanelLeft = 135f;
        public const float ResultPanelTop = 440f;
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
        public const float SteamSec = 4f;
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

        // 아래 버튼 줄 폭 (차림표 · 레시피 도감 · 재료 팔기 · 닫기). 합 468 + 사이 3칸 24 = 492 로 540 안에 들고 좌우 24 씩 남는다.
        public const float MenuButtonWidth = 112f;
        public const float CodexButtonWidth = 132f;
        public const float SellAllButtonWidth = 124f;
        public const float CloseButtonWidth = 100f;

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

        // 떡메 pound15 박자(초). 단계 8장 · 넘어감(t12 · t23) 4장 — pound14 README 값.
        public static readonly float[] Pound15StageSec = { 0.11f, 0.08f, 0.09f, 0.14f, 0.045f, 0.16f, 0.09f, 0.14f };
        public static readonly float[] Pound15TurnSec = { 0.13f, 0.13f, 0.13f, 0.16f };
        // 캔버스 440×560. 바닥 가운데 (Pound9X, Pound9Bottom) = (270,815) 라 좌상이 (50,255) 가 된다.
        public const float Pound15Width = 440f;
        public const float Pound15Height = 560f;

        // 반죽이 떡메 위로 오는 「묻힘」 장인가. 단계 0 · 5 · 7장, 넘어감 4장 전부.
        public static bool Pound15DoughOnTop(bool turn, int frame)
        {
            if (turn)
            {
                return true;
            }

            return frame == 0 || frame == 5 || frame == 7;
        }

        // 맷돌 한 번 돌리기 (0 쉼 · 1~4 돌기). 묶음이 하나라 맞는 장이 없다 (0 을 넘긴다).
        public static readonly float[] MillFrameSec = { 0.03f, 0.07f, 0.07f, 0.07f, 0.07f };

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

        // ── 반복 09 : 옆으로 세 칸 밀리는 주방 ──
        // 아래 값은 모두 「칸 안 좌표」다 (한 칸 540×1050 · 왼쪽 위 원점 · y 아래로 증가). 칸 n 의 띠 x = n * PanWidth + x.
        // 정본은 Docs/Reference/Art/정식/반복09/견줌/README.md 「쓴 자리 값」과 견줌/build_view.py 맨 위 상수.
        // 「아래 가운데」 자리는 그림의 보이는 픽셀(투명 여백 뺀 상자) 아래 가운데다 — 캔버스 아래가 아니다.

        // 칸과 띠 : 천장 1620×250 은 세 칸에 걸친 한 장, 칸 배경은 칸마다 한 장 (y 250 부터).
        public const float PanWidth = 540f;
        public const int PanCount = 3;
        public const float StripHeight = 1050f;
        public const float CeilHeight = 250f;
        public const float BgTop = 250f;
        public const float PanSlideSec = 0.35f;
        public const int PanPrep = 0;
        public const int PanPound = 1;
        public const int PanFinish = 2;

        // 칸1 재료 선반 6열 × 3줄 (18병 한 쪽). 선반 판 460×17 을 (ShelfLeft, 줄 y) 에 깐다.
        public const int ShelfColumns = 6;
        public const int ShelfRows = 3;
        public const float ShelfLeft = 40f;
        public static readonly float[] ShelfBoardY = { 400f, 487f, 574f };

        // 병 48×57 왼쪽 위 = (JarLeft0 + 열 * JarDx, JarTopY[줄]). 병 위 y = 선반 윗변 − 57 + 3 (병 바닥 3px 이 판에 묻힌다).
        public const float JarLeft0 = 54f;
        public const float JarDx = 72f;
        public const float JarWidth = 48f;
        public const float JarHeight = 57f;
        public static readonly float[] JarTopY = { 346f, 433f, 520f };

        // 병 안 재료 아이콘 상자 (병 왼쪽 위 기준, 32×32). 28×28 아이콘을 1배로 가운데에 둔다.
        public const float JarIconLeft = 8f;
        public const float JarIconTop = 18f;
        public const float JarIconRight = 40f;
        public const float JarIconBottom = 50f;

        // 쪽 넘김 : 화살표 27×25 왼쪽 위. 오른쪽은 같은 그림을 좌우로 뒤집는다. 쪽 점은 x 252 부터 PageDotGap 간격.
        public const float ArrowLeftX = 4f;
        public const float ArrowRightX = 509f;
        public const float ArrowY = 455f;
        public const float ArrowHitSize = 48f;
        public const float PageDotLeft = 252f;
        public const float PageDotY = 586f;
        public const float PageDotGap = 12f;

        // 칸1 : 쟁반 (왼쪽 위, 246×91) · 믹서기 (아래 가운데).
        public const float TrayLeft = 60f;
        public const float TrayTop = 715f;
        public const float TrayWidth = 246f;
        public const float TrayHeight = 91f;
        // 쟁반 위 고른 재료 칸 (쟁반 왼쪽 위 기준, 칸 가운데). 셋을 쟁반 가운데로 고르게.
        public const float TraySlotSize = 48f;
        public const float TraySlotGap = 64f;
        public const float TraySlotFirstX = TrayWidth * 0.5f - TraySlotGap;
        public const float TraySlotY = TrayHeight * 0.45f;
        public const float BlenderX = 440f;
        public const float BlenderBottom = 800f;

        // 칸2 : 절구 + 떡메 (아래 가운데) · 걸이 레일 · 화분 선반 (둘 다 왼쪽 위).
        public const float Pound9X = 270f;
        public const float Pound9Bottom = 815f;
        public const float RailLeft = 145f;
        public const float RailTop = 345f;
        public const float PlantShelfLeft = 30f;
        public const float PlantShelfTop = 480f;

        // 칸3 : 벽시계 (왼쪽 위) · 빚기 판 · 찜기 · 김 (아래 가운데) · 결과 떡 자리 (가운데). 판 위 떡은 정수 2배.
        public const float ClockLeft = 440f;
        public const float ClockTop = 370f;
        public const float Board9X = 150f;
        public const float Board9Bottom = 822f;
        public const float Steamer9X = 400f;
        public const float Steamer9Bottom = 805f;
        public const float Steam9X = 404f;
        public const float Steam9Bottom = 682f;
        // 뚜껑 닫힌 채 찌는 동안의 김 아래 : 찜기 윗변(약 606) 바로 위라 찜기 그림과 안 겹친다. 뚜껑이 열리면 Steam9Bottom.
        public const float Steam9ClosedBottom = 634f;

        // 1단 유리 찜기 연출 (반복 09 steamer2, README 장·ms 표). 0 은 반죽이 찜기에 닿은 순간, 끝은 SteamSec.
        public const float Steamer2LidCloseSec = 0.1f;
        public const float Steamer2FogStartSec = 0.3f;
        public const float Steamer2FogFullSec = 2f;
        public const float Steamer2FogOutStartSec = 3.6f;
        public const float Steamer2SteamStartSec = 0.8f;
        public static readonly float[] Steamer2RattleSec = { 1.5f, 2.3f, 3f };
        public const float Steamer2RattleLen = 0.1f;
        public const float Steamer2RattleUp = 1f;
        public const float Steamer2CookStartSec = 0.5f;
        public const float Steamer2CookEndSec = 3.5f;
        public static readonly Color Steamer2CookedColor = new Color(1f, 241f / 255f, 206f / 255f, 1f);
        public const float Steamer2PuffSec = 2.5f;
        // 떡 57×44 → 59×46.
        public static readonly Vector3 Steamer2PuffScale = new Vector3(59f / 57f, 46f / 44f, 1f);
        // 떡 아래 가운데 (캔버스 x 61~118, 아래 y 133, 캔버스 180×199).
        public static readonly Vector2 Steamer2DoughPivot = new Vector2(89.5f / 180f, (199f - 133f) / 199f);
        // 빚기 : 빚기 판을 이 수만큼 눌러 빚는다. 반죽 6장을 누름마다 두 장씩 넘긴다.
        public const int ShapeTapCount = 3;
        public const int ShapeFramesPerTap = 2;
        // 빚은 반죽을 찜기에 넣는 연출 : 판 위(반죽 아래 가운데)에서 찜 칸 안까지 포물선으로 날아가며 작아진다.
        public const float ShapeFlySec = 0.6f;
        public const float ShapeFlyToX = 400f;
        public const float ShapeFlyToY = 700f;
        // 포물선 솟음 (px) : 시작 ~ 끝 직선 위로 더 솟는 높이.
        public const float ShapeFlyArc = 90f;
        public const float ShapeFlyEndScale = 0.6f;
        public const float CakeSpotX = 270f;
        public const float CakeSpotY = 480f;
        public const int CakeScale = 2;
        // 결과 떡 한 변 : 원본 32px 의 2배. 스프라이트 원본 크기와 상관없이 이 상자에 비율 그대로 맞춘다.
        public const float CakeResultSize = 64f;

        // 반복 09 기구 틀 시간 (초). 김은 3장 되풀이, 빚기는 장당 0.09 · 끝 장에서 잠깐 멈춤.
        // 새 믹서기 blender10 : 장당 0.07. 그림 권장(0 → 1~8 한 번 → 5~8 되풀이)을 누름 셋으로 나눈다.
        // 1번째 층이 보이며 갈리기 시작(1~4), 2번째 다 섞임(5~8), 3번째 매끈하게 두 바퀴 돌고 8(다 갈린 모습)에서 멈춰 기다린다.
        public const float BlenderFrameStepSec = 0.07f;
        public static readonly int[][] BlenderTapFrames =
        {
            new[] { 1, 2, 3, 4 },
            new[] { 5, 6, 7, 8 },
            new[] { 5, 6, 7, 8, 5, 6, 7, 8 },
        };
        public static readonly float[] Steam9FrameSec = { 0.15f, 0.15f, 0.15f };
        public static readonly float[] Shape9FrameSec = { 0.09f, 0.09f, 0.09f, 0.09f, 0.09f, 0.4f };

        // 칸3 반죽 물들이기 세기 : 반죽 색 = lerp(흰색, 재료 평균색, 이 값). 「옅게」라 0.45.
        public const float DoughTintStrength = 0.45f;

        // 고정 UI : 안내 글 상자 (왼쪽 위, 칸이 밀려도 그 자리).
        // 3판 : 「가게로」는 없애고 「재료 팔기」는 아래 버튼 줄로 옮겨 폭을 다시 420 (60~480) 으로 넓혔다.
        public const float HintLeft = 60f;
        public const float HintTop = 992f;
        public const float HintWidth = 420f;
        public const float HintHeight = 44f;

        // 쪽 점은 이만큼 만들어 두고 쪽 수만큼만 켠다.
        public const int PageDotMax = 4;

        // 병 개수 글자 : 병 왼쪽 위 기준 가운데 y. 병 아래 선반 판(병 바닥 3px 이 묻힌 17px 판) 위.
        public const float JarCountY = 62f;
        public const float JarCountHeight = 16f;

        // 칸3 빚기 반죽 (캔버스 128×96, 아래 가운데). 판 윗면은 화면 y 약 738~808 (비스듬히 본 면)이라
        // 반죽 바닥을 윗면 가운데(약 773)보다 조금 아래 795 에 둔다 — 811 이면 앞 모서리에 걸려 보였다 (반복 09 캡처).
        // 찜기로 날아가는 출발 자리도 이 두 값을 쓴다.
        public const float Shape9X = Board9X;
        public const float Shape9Bottom = 795f;
        // 치기 21장 · 김 3장은 장마다 보이는 상자가 달라 캔버스 아래 가운데로 놓는다. 찜기 셋은 캔버스 바닥이 보이는 바닥과 같다 (2판에서 PIL 로 잼).
    }
}
