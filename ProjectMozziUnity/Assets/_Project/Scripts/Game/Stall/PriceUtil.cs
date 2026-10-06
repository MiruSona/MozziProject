using Mozzi.Data;
using UnityEngine;

namespace Mozzi.Game.Stall
{
    // 떡 값 셈법 (가판대 문서 7절 · 수치표 2절). 값은 전부 Stall 표에서 온다.
    // 정수 반올림은 .5 를 올린다 (Mathf.RoundToInt 는 짝수로 가서 22.5 → 22 가 된다).
    public static class PriceUtil
    {
        // 레시피 떡 = 재료 값 합 × 개수 계수 · 평범한 떡 = 합 × plainRate(계수 없음).
        public static int CakePrice(int valueSum, int ingredientCount, bool isRecipe, StallRow stall)
        {
            if (isRecipe == false)
            {
                return RoundHalfUp(valueSum * stall.PlainRate);
            }

            return RoundHalfUp(valueSum * CountMult(ingredientCount, stall));
        }

        // 주문이 맞으면 × orderMult.
        public static int OrderPrice(int basePrice, StallRow stall)
        {
            return RoundHalfUp(basePrice * stall.OrderMult);
        }

        // 진열대 비우기로 팔 때 떡 하나 값 = 정가 × clearRate.
        public static int ClearPrice(int price, StallRow stall)
        {
            return RoundHalfUp(price * stall.ClearRate);
        }

        public static int RoundHalfUp(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        // 개수 계수는 1·2·3개 열 셋뿐이다. 그 밖은 3개 값으로 본다.
        public static float CountMult(int ingredientCount, StallRow stall)
        {
            if (ingredientCount <= 1)
            {
                return stall.CountMult1;
            }

            if (ingredientCount == 2)
            {
                return stall.CountMult2;
            }

            return stall.CountMult3;
        }
    }
}
