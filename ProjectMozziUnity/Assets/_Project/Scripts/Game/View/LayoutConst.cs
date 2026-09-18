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
        public const float FarmBottomPx = 560f;

        // 찹쌀이 단계별 크기 (px). 세로조각1판설계 3-2 의 40/48/56.
        public const float MozziTier1Px = 40f;
        public const float MozziTier2Px = 48f;
        public const float MozziTier3Px = 56f;

        // 변종 테두리는 몸보다 이만큼 큰 네모다 (배율).
        public const float ShinyBorderScale = 1.2f;

        public const float PilePx = 28f;
        public const float BuildingWidthPx = 140f;
        public const float BuildingHeightPx = 64f;

        // 건물 알림 점 크기 (px).
        public const float BadgePx = 14f;

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
