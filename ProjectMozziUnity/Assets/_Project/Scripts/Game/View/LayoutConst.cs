using UnityEngine;

namespace Mozzi.Game.View
{
    // 메인 화면 px 자리를 월드 단위로 옮긴 상수. 32px = 1 유닛, 월드 기준은 540×960 으로 UI 기준과 같다.
    // 화면이 기준보다 길쭉하거나 넓적해도 이 배율은 그대로다 — FarmViewManager 가 카메라를 캔버스 배율에 맞춘다.
    public static class LayoutConst
    {
        public const float PixelsPerUnit = 32f;
        public const float ScreenWidthPx = 540f;
        public const float ScreenHeightPx = 960f;

        // UI px(540 기준) → 월드 px. 960 / 960 = 1. 어느 화면 비율에서도 같다.
        public const float UiToWorldPx = ScreenHeightPx / Ui.LayoutConstUi.ReferenceHeight;

        // 메인 화면 줄 (위에서 잰 px · 정식/메인 README). 길 420~480 아래가 농장이다.
        public const float PathTopPx = 420f;
        public const float FarmTopPx = 480f;

        // 농장 격자. 칸 32px, 16×15 칸이라 480~960 (월드 아래끝까지)이다. 바깥 한 줄이 울타리다.
        public const float FarmCellPx = 32f;
        public const int FarmTileCols = 16;
        public const int FarmTileRows = 15;
        public const float FarmBottomPx = FarmTopPx + FarmTileRows * FarmCellPx;

        // 장식 칸(FarmDecor 표) 수. 울타리 줄을 뺀 안쪽 칸이다. 오른쪽 울타리 칸의 앞 15px 은 칸이 못 돼 뺀다.
        public const int DecorCols = FarmTileCols - 2;
        public const int DecorRows = FarmTileRows - 2;

        // 격자 왼쪽 끝 x. 울타리 기둥은 칸 안 x 15~31 에 있어서, 6 이면 보이는 울타리가 x 21~518 로 가운데 온다.
        public const float FarmGridLeftPx = 6f;

        // 울타리 기둥 · 세로 통나무가 칸 안에서 시작하는 x (fence_post · fence_v 그림).
        public const float FencePostInsetPx = 15f;

        // 가로 울타리 마디 간격. 기둥이 빽빽해 보이면 64 로 바꾼다 (그때는 64 폭 fence_h 그림도 같이).
        public const float FenceSegmentPx = 32f;

        // 하늘 아래끝 = 지평선. 월드 위끝에서 잰 px 다 (정식/메인 README 의 y 308). 그 아래는 잔디가 덮는다.
        // 그림이 없을 때 쓰는 단색 하늘 높이 720 은 하늘 그림과 같아 9:20 위 240 줄까지 덮는다.
        public const float SkyBottomPx = 308f;
        public const float SkyFallbackHeightPx = 720f;

        // 하늘 맨 윗줄 색 = 카메라 배경색. 하늘 그림의 하늘 면 전체가 이 한 색(#D4EEFA)이라 더 긴 폰에서도 이음새가 없다.
        public static readonly Color SkyTopColor = new Color32(212, 238, 250, 255);

        // 땅 층 정렬. 찹쌀이가 쓰는 132~566(MozziSortBase 100 - 발 y) 의 위아래로 뺐다. 하늘은 바닥보다 뒤다.
        public const int SkySortOrder = -20;
        public const int GroundSortOrder = -10;
        public const int PathSortOrder = -9;
        public const int DecorBackSortOrder = -8;
        public const int FenceBackSortOrder = -5;
        public const int FenceFrontSortOrder = 600;

        // 찹쌀이 그림은 64 캔버스에 그리고 그 안에서 몸(왕관 없이)은 세로 35px 이다 (body_hop0 불투명 bbox y16~50).
        public const float MozziCanvasPx = 64f;
        public const float MozziBodyHeightPx = 35f;

        // 찹쌀이 몸 높이 (px). 단계와 상관없이 모두 같다. 35 = 64 캔버스를 ×1 로 찍은 크기다.
        // 사용자 판정 대기 값이라 이 한 줄만 바꾸면 된다 (예 : 40 이면 ×1.143). 밀어내기 반지름도 이 값을 따른다.
        public const float MozziSizePx = MozziBodyHeightPx;

        // 몸 그림 피벗이 캔버스 가운데(32)라 발(몸 bbox 아래끝 50)이 루트보다 이만큼 아래다 (64 캔버스 px).
        // 앞뒤 정렬은 루트가 아니라 발 y 로 센다 — 장식 · 바닥 물건의 기준(칸 바닥)과 맞추려는 것이다.
        public const float MozziFootBelowCenterPx = 18f;

        // 루트에서 발까지 월드 길이 (유닛). 몸 크기를 바꾸면 같은 비로 따라간다.
        public static float MozziFootBelowUnits
        {
            get { return ToUnits(MozziFootBelowCenterPx * MozziSizePx / MozziBodyHeightPx); }
        }

        // 변종 테두리는 몸보다 이만큼 큰 네모다 (배율).
        public const float ShinyBorderScale = 1.2f;

        // 변종 표시는 네모 배경 대신 다른 방법으로 다시 정한다 — 2026-09-20 사용자 판정으로 끔.
        public const bool MozziShinyBorderVisible = false;

        // 찹쌀이 뜀 높이 (px). 뜨는 것은 Visual 자식뿐이라 콜라이더·담장 판정은 그대로다.
        public const float MozziHopHeightPx = 24f;

        // 찹쌀이 정렬 기준. 아래쪽이 앞에 오게 발 y 를 빼서 쓴다. 농장 안쪽(월드 y −32~−448px)이 132~548,
        // 발이 루트보다 18px 아래라 찹쌀이는 최대 566 이다 — 앞 담장 600 보다 뒤. 앞 겹 장식(FarmDecor Front)도 같은 식이다.
        public const int MozziSortBase = 100;

        // 들고 있는 찹쌀이. 앞 담장(600)보다 앞에 두고 Visual 을 이만큼 띄운다.
        public const int MozziDragSortOrder = 700;
        public const float MozziDragLiftPx = 20f;

        // 담장 밖에 놓았을 때 걷는 칸으로 미끄러져 돌아오는 시간 (초).
        public const float MozziDropSlideSec = 0.15f;

        // 보관함 궤짝에 넣을 때 입구로 폴짝 뛰어 들어가는 시간 · 뜀 높이 · 끝 크기 배율.
        // 궤짝 그림(정렬 0) 바로 앞에 그린다 — 이름표(5) · 알림 점(20)보다는 뒤다.
        public const float MozziStoreSec = 0.3f;
        public const float MozziStoreJumpPx = 24f;
        public const float MozziStoreEndScale = 0.2f;
        public const int MozziStoreSortOrder = 1;

        // 탭으로 바뀐 표정이 기본으로 돌아가기까지 (초).
        public const float FaceHoldSec = 1.5f;

        public const float PilePx = 28f;

        // 바닥 재료(낱개 · 꾸러미). back 장식(-8) 바로 위 · 찹쌀이(132~)보다 뒤에 고정한다. 수량 글자는 그림보다 한 칸 앞.
        public const int FloorItemSortOrder = -7;
        public const int FloorItemLabelSortOrder = -6;

        // 바닥에 보이는 재료 수 상한. 넘으면 가까운 같은 재료끼리 꾸러미(PileMax 개까지)로 뭉친다 (04-수치표).
        public const int FloorItemMax = 30;

        // 훑기 손가락 반지름 (px). 재료 그림 반쪽(PilePx / 2)까지 닿으면 줍는다.
        public const float SweepRadiusPx = 12f;

        // 수량 글자를 더미 한가운데에서 이만큼 아래로 내린다 (px). 그림 절반 14 + 사이 6 이다.
        public const float PileLabelBelowPx = 20f;

        // 건물 알림 점 크기 · 이름표 글자 크기 (px). 건물 자리는 BuildingLayout 에 있다.
        public const float BadgePx = 16f;
        public const float BuildingLabelPx = 12f;

        public static float HalfWidth
        {
            get { return ScreenWidthPx / PixelsPerUnit * 0.5f; }
        }

        public static float HalfHeight
        {
            get { return ScreenHeightPx / PixelsPerUnit * 0.5f; }
        }

        // 화면 위에서 잰 px 를 월드 y 로.
        public static float YFromTop(float px)
        {
            return HalfHeight - px / PixelsPerUnit;
        }

        // 화면 왼쪽에서 잰 px 를 월드 x 로.
        public static float XFromLeft(float px)
        {
            return -HalfWidth + px / PixelsPerUnit;
        }

        public static float ToUnits(float px)
        {
            return px / PixelsPerUnit;
        }

        // 울타리 안쪽. 왼쪽은 칸 0(왼 울타리) 다음, 오른쪽은 오른 울타리 통나무 앞, 위아래는 울타리 줄 한 칸씩 뺀 자리다.
        // 장식 칸(FarmDecor)의 원점은 이 네모의 왼쪽 아래다.
        public static Rect FarmRect
        {
            get
            {
                float xMin = XFromLeft(FarmGridLeftPx + FarmCellPx);
                float xMax = XFromLeft(FarmGridLeftPx + (FarmTileCols - 1) * FarmCellPx + FencePostInsetPx);
                float yMin = YFromTop(FarmBottomPx - FarmCellPx);
                float yMax = YFromTop(FarmTopPx + FarmCellPx);
                return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            }
        }
    }
}
