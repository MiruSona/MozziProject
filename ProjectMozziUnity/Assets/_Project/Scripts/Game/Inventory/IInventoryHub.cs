using System.Collections.Generic;

namespace Mozzi.Game.Inventory
{
    // 창고(주운 부재료) 창구. 상한은 Ingredient 표 cap 이다.
    // 표에 없는 id(빈 문자열 포함)는 던지지 않고 Count · Cap 0 · IsFull · Has false 다.
    public interface IInventoryHub
    {
        IReadOnlyList<string> IngredientIds { get; }

        int Count(string ingredientId);

        int Cap(string ingredientId);

        bool IsFull(string ingredientId);

        // 상한에서 잘라 넣고 실제로 들어간 수를 준다.
        int Add(string ingredientId, int amount);

        bool Has(string ingredientId, int amount);

        // 모자라면 false 를 주고 아무것도 안 뺀다.
        bool Take(string ingredientId, int amount);
    }
}
