using UnityEngine;

namespace Mozzi.Game.View
{
    // 바닥 재료 하나를 셈에만 쓰려고 뗀 값. FloorItemMath 가 Unity 오브젝트 없이 받는다.
    public readonly struct FloorItemEntry
    {
        public readonly string IngredientId;
        public readonly int Amount;
        public readonly Vector2 Position;

        public FloorItemEntry(string ingredientId, int amount, Vector2 position)
        {
            IngredientId = ingredientId;
            Amount = amount;
            Position = position;
        }
    }
}
