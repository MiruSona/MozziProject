using UnityEngine;

namespace Mozzi.Game.View
{
    // 메인 화면 건물 셋의 자리 (정식/메인 README 「코드에 넘길 것」). 540×960 왼쪽 위 원점 px, y 는 아래로다.
    // 발끝 = 그림 피벗(아래 가운데) 자리, 누르는 네모 = 그림 전체 상자. 긴 화면은 위로 하늘만 늘어 자리가 같다.
    // 이름표는 v7 시안처럼 길 위(합성소 · 가판대)나 궤짝 위(보관함)에 작게, 알림 점은 그림이 있는 오른쪽 위 자리에 둔다.
    public struct BuildingLayout
    {
        public Vector2 FootPx;
        public Rect HitPx;
        public Vector2 LabelPx;
        public Vector2 BadgePx;

        // 찹쌀이가 뛰어 들어가는 입구. 보관함 궤짝만 쓴다 — 궤짝 그림(92×77) 안 어두운 속(y 4~16)의 가운데다.
        public Vector2 MouthPx;

        public static BuildingLayout For(BuildingKind kind)
        {
            if (kind == BuildingKind.Synthesis)
            {
                return Make(88f, 428f, 21f, 156f, 284f, 428f, 88f, 442f, 140f, 292f);
            }

            if (kind == BuildingKind.Stall)
            {
                return Make(306f, 428f, 181f, 431f, 225f, 428f, 306f, 442f, 352f, 256f);
            }

            BuildingLayout storage = Make(484f, 470f, 438f, 530f, 393f, 470f, 484f, 385f, 522f, 400f);
            storage.MouthPx = new Vector2(484f, 404f);
            return storage;
        }

        public Vector2 MouthWorld
        {
            get { return new Vector2(LayoutConst.XFromLeft(MouthPx.x), LayoutConst.YFromTop(MouthPx.y)); }
        }

        // 발끝 월드 자리.
        public Vector2 FootWorld
        {
            get { return new Vector2(LayoutConst.XFromLeft(FootPx.x), LayoutConst.YFromTop(FootPx.y)); }
        }

        public Vector2 LabelWorld
        {
            get { return new Vector2(LayoutConst.XFromLeft(LabelPx.x), LayoutConst.YFromTop(LabelPx.y)); }
        }

        // 알림 점 가운데 월드 자리.
        public Vector2 BadgeWorld
        {
            get { return new Vector2(LayoutConst.XFromLeft(BadgePx.x), LayoutConst.YFromTop(BadgePx.y)); }
        }

        // 누르는 네모의 월드 Rect. px 는 y 가 아래로라 위끝(yMin px)이 월드 yMax 가 된다.
        public Rect HitWorld
        {
            get
            {
                return Rect.MinMaxRect(
                    LayoutConst.XFromLeft(HitPx.xMin),
                    LayoutConst.YFromTop(HitPx.yMax),
                    LayoutConst.XFromLeft(HitPx.xMax),
                    LayoutConst.YFromTop(HitPx.yMin));
            }
        }

        private static BuildingLayout Make(float footX, float footY, float xMin, float xMax, float yMin, float yMax, float labelX, float labelY,
            float badgeX, float badgeY)
        {
            BuildingLayout layout = new BuildingLayout();
            layout.FootPx = new Vector2(footX, footY);
            layout.HitPx = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            layout.LabelPx = new Vector2(labelX, labelY);
            layout.BadgePx = new Vector2(badgeX, badgeY);
            return layout;
        }
    }
}
