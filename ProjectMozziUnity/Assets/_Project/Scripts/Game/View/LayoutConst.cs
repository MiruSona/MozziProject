using UnityEngine;

namespace Mozzi.Game.View
{
    // 세로조각1판설계 3-2 의 px 자리를 월드 단위로 옮긴 상수. 기준 360×640 · 32px = 1 유닛 · 카메라 size 10.
    public static class LayoutConst
    {
        public const float PixelsPerUnit = 32f;
        public const float ScreenWidthPx = 360f;
        public const float ScreenHeightPx = 640f;

        public const float ResourceBarBottomPx = 56f;
        public const float BuildingStripBottomPx = 136f;
        public const float FarmBottomPx = 552f;

        // 농장 바닥 타일 칸 수. 세로 13칸 × 32 = 416 이라 136~552 에 딱 떨어진다.
        public const int FarmTileCols = 12;
        public const int FarmTileRows = 13;

        // 담장 두께 (유닛). 타일 한 칸이다.
        public const float FenceUnits = 1f;

        // 바닥 변종을 섞는 고정 시드. 캡처 판정을 되풀이할 수 있어야 한다.
        public const int FarmGroundSeed = 20260920;

        // 바닥 변종 비율. 기본 · 꽃이고 나머지가 돌이다.
        public const double FarmGroundBaseRatio = 0.82;
        public const double FarmGroundFlowerRatio = 0.1;

        // 타일맵 층 정렬. 찹쌀이가 쓰는 145~446 의 위아래로 뺐다.
        public const int GroundSortOrder = -10;
        public const int FenceBackSortOrder = -5;
        public const int FenceFrontSortOrder = 600;

        // 찹쌀이 단계별 크기 (px). 세로조각1판설계 3-2 의 40/48/56.
        public const float MozziTier1Px = 40f;
        public const float MozziTier2Px = 48f;
        public const float MozziTier3Px = 56f;

        // 찹쌀이 그림은 64 캔버스에 그리고 그 안에서 몸(왕관 없이)은 세로 35px 이다 (body_hop0 불투명 bbox y16~50).
        // 위 40/48/56 은 몸 높이 기준이라 캔버스 여백만큼 크게 찍어야 맞는다.
        public const float MozziCanvasPx = 64f;
        public const float MozziBodyHeightPx = 35f;

        // 변종 테두리는 몸보다 이만큼 큰 네모다 (배율).
        public const float ShinyBorderScale = 1.2f;

        // 변종 표시는 네모 배경 대신 다른 방법으로 다시 정한다 — 2026-09-20 사용자 판정으로 끔.
        public const bool MozziShinyBorderVisible = false;

        // 찹쌀이 뜀 높이 (px). 뜨는 것은 Visual 자식뿐이라 콜라이더·담장 판정은 그대로다.
        public const float MozziHopHeightPx = 12f;

        // 찹쌀이 정렬 기준. 아래쪽 마리가 앞에 오게 y 를 빼서 쓴다.
        public const int MozziSortBase = 300;

        public const float PilePx = 28f;

        // 수량 글자를 더미 한가운데에서 이만큼 아래로 내린다 (px). 그림 절반 14 + 사이 6 이다.
        public const float PileLabelBelowPx = 20f;

        public const float BuildingWidthPx = 140f;
        public const float BuildingHeightPx = 64f;

        // 건물 알림 점 크기 (px).
        public const float BadgePx = 14f;

        // 건물 그림은 아래 18px 을 이름 띠로 비워 놨다 (그림 위끝에서 y 46~64, 가운데 y≈55).
        // 건물 한가운데에서 이만큼 아래로 내리면 띠 가운데다.
        public const float BuildingLabelBelowPx = 23f;

        // 이름 글자 크기 (px). 18px 띠 안에 들어가야 한다.
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

        public static float ToUnits(float px)
        {
            return px / PixelsPerUnit;
        }

        // 담장 안. 가장자리 여백 0.4 유닛을 뺐다.
        public static Rect FarmRect
        {
            get
            {
                float margin = 0.4f;
                float xMin = -HalfWidth + margin;
                float yMin = YFromTop(FarmBottomPx) + margin;
                float xMax = HalfWidth - margin;
                float yMax = YFromTop(BuildingStripBottomPx) - margin;
                return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            }
        }

        public static float BuildingCenterY
        {
            get { return YFromTop((ResourceBarBottomPx + BuildingStripBottomPx) * 0.5f); }
        }

        public static float BuildingCenterX
        {
            get { return HalfWidth * 0.5f; }
        }
    }
}
