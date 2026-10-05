using System.Collections.Generic;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 바닥 재료의 셈 (맞추기 · 뭉치기 · 훑기 판정). Unity 오브젝트를 안 만들어 EditMode 시험이 바로 부른다.
    // 목록 순서는 놓인 순서다 — 앞이 오래된 것, 뒤가 새것.
    public static class FloorItemMath
    {
        // 화면에 보이는 그 재료의 합계.
        public static int ShownOf(IReadOnlyList<FloorItemEntry> items, string ingredientId)
        {
            int sum = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IngredientId == ingredientId)
                {
                    sum += items[i].Amount;
                }
            }

            return sum;
        }

        // 같은 재료 가운데 가장 가까운 둘. keep 은 양이 많은 쪽(같으면 먼저 놓인 쪽)이고 그 자리에 합친다.
        // 같은 재료 둘이 없으면 false.
        public static bool FindMergePair(IReadOnlyList<FloorItemEntry> items, out int keep, out int drop)
        {
            keep = -1;
            drop = -1;
            float best = float.PositiveInfinity;

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (items[i].IngredientId != items[j].IngredientId)
                    {
                        continue;
                    }

                    float dist = (items[i].Position - items[j].Position).sqrMagnitude;
                    if (dist >= best)
                    {
                        continue;
                    }

                    best = dist;
                    keep = i;
                    drop = j;
                    if (items[j].Amount > items[i].Amount)
                    {
                        keep = j;
                        drop = i;
                    }
                }
            }

            return keep >= 0;
        }

        // 화면이 모델보다 need 만큼 많을 때 깎을 양. 새것(뒤)부터 깎아 각 칸의 남을 양을 amounts 에 적는다.
        // 0 이 된 칸은 걷는다. 다른 재료 칸은 그대로 옮겨 적는다.
        public static void PlanRemove(IReadOnlyList<FloorItemEntry> items, string ingredientId, int need, List<int> amounts)
        {
            amounts.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                amounts.Add(items[i].Amount);
            }

            for (int i = items.Count - 1; i >= 0 && need > 0; i--)
            {
                if (items[i].IngredientId != ingredientId)
                {
                    continue;
                }

                int cut = Mathf.Min(amounts[i], need);
                amounts[i] -= cut;
                need -= cut;
            }
        }

        // 누른 자리에 겹친 것들 가운데 무엇이 누름을 받나. 순서 : 주울 수 있는 재료 → 찹쌀이 → 건물 → 창고가 차서 못 줍는 재료 → 빈 땅.
        // 못 줍는 재료를 뒤로 미는 까닭 : 앞에 두면 줍지도 못하면서 밑에 깔린 찹쌀이의 누름을 가로챈다.
        // 겹친 것이 없을 때는 그 재료가 받아 「못 줍는다」 흔들림을 그대로 보여 준다.
        public static FarmPressTarget ChoosePressTarget(bool hasRoomItem, bool hasFullItem, bool hasMozzi, bool hasBuilding)
        {
            if (hasRoomItem)
            {
                return FarmPressTarget.FloorItem;
            }

            if (hasMozzi)
            {
                return FarmPressTarget.Mozzi;
            }

            if (hasBuilding)
            {
                return FarmPressTarget.Building;
            }

            return hasFullItem ? FarmPressTarget.FloorItem : FarmPressTarget.Ground;
        }

        // 점 p 에서 선분 a-b 까지 거리. 훑기는 지난 프레임 손가락 자리 → 지금 자리 선분으로 판정한다.
        public static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq <= 0f)
            {
                return Vector2.Distance(a, p);
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            return Vector2.Distance(a + ab * t, p);
        }
    }
}
