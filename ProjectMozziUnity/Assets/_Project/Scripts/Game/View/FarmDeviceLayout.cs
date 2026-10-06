using UnityEngine;

namespace Mozzi.Game.View
{
    // 농장 장치(수집기 · 먹이 공급기)의 자리와 크기 (월드 540×960, 위에서 · 왼쪽에서 잰 px). 정본 : 정식/반복08/README 「코드에 넘길 것」.
    // 장치는 옆 울타리 세로 칸 몇 개를 대신한다. 어느 칸을 빼나는 장치 높이로 정한다 (FenceRowsCovered).
    public static class FarmDeviceLayout
    {
        // 수집기 (136×107 · 7장). 오른쪽 울타리 자리, 나팔은 왼쪽(농장 안)을 본다.
        public const float CollectorWidthPx = 136f;
        public const float CollectorHeightPx = 107f;
        public const float CollectorRightPx = 538f;
        public const float CollectorBottomPx = 608f;

        // 수집기 나팔 입 (그림 왼쪽 아래 끝에서 잰 px). 걷힌 바닥 재료가 이리로 빨려 든다.
        public const float CollectorMouthXPx = 8f;
        public const float CollectorMouthYPx = 62f;

        // 먹이 공급기 (116×147 · 4장). 왼쪽 울타리 자리, 주둥이는 오른쪽을 본다.
        // README 의 아래 608 은 위 끝(461)이 농장 위 끝(480)을 넘어 640 으로 내렸다 (위 끝 493).
        public const float FeederWidthPx = 116f;
        public const float FeederHeightPx = 147f;
        public const float FeederLeftPx = 2f;
        public const float FeederBottomPx = 640f;

        // 그림 장 수. 0 이 쉼이고 1 부터가 한 바퀴다.
        public const int CollectorFrameCount = 7;
        public const int FeederFrameCount = 4;

        // 한 장 시간 (초, 게임 시간). 수집기 1~6 은 0.48초, 공급기 1~3 은 0.36초에 한 바퀴다.
        public const float CollectorFrameSec = 0.08f;
        public const float FeederFrameSec = 0.12f;

        // 공급기는 이 초마다 한 바퀴 알갱이를 내보낸다 (게임 시간). 생산 틱마다 돌리면 마리가 많을 때 쉬지 않고 돌아서 시계로 뗐다.
        public const float FeederPuffIntervalSec = 4f;

        // 수집기가 빨 때 크기를 살짝 눌렀다 편다 (README 제안 가로 1.05 · 세로 0.95).
        public const float CollectorSquashX = 1.05f;
        public const float CollectorSquashY = 0.95f;

        // 장치는 그 강화를 한 단계라도 샀을 때만 보인다 (0 단계 = 안 삼 → 울타리 그대로).
        public static bool IsBought(int upgradeLevel)
        {
            return upgradeLevel > 0;
        }

        public static float CollectorLeftPx
        {
            get { return CollectorRightPx - CollectorWidthPx; }
        }

        // 옆 울타리 세로 칸 가운데 [bottomPx − heightPx, bottomPx] 와 겹치는 줄 범위 (칸 1 ~ rows−2 안).
        // 세로 칸 row 는 위에서 FarmBottomPx − (row+1)·cell ~ FarmBottomPx − row·cell 를 차지한다 (SpriteFarmGroundPainter.PaintFence).
        // 하나도 안 겹치면 false.
        public static bool FenceRowsCovered(float bottomPx, float heightPx, out int fromRow, out int toRow)
        {
            float topPx = bottomPx - heightPx;
            float cell = LayoutConst.FarmCellPx;
            fromRow = int.MaxValue;
            toRow = int.MinValue;

            for (int row = 1; row < LayoutConst.FarmTileRows - 1; row++)
            {
                float cellBottom = LayoutConst.FarmBottomPx - row * cell;
                float cellTop = cellBottom - cell;
                if (cellTop >= bottomPx || cellBottom <= topPx)
                {
                    continue;
                }

                fromRow = Mathf.Min(fromRow, row);
                toRow = Mathf.Max(toRow, row);
            }

            return fromRow <= toRow;
        }
    }
}
