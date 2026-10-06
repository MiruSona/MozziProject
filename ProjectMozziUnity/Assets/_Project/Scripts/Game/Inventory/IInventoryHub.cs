using System.Collections.Generic;

namespace Mozzi.Game.Inventory
{
    // 창고(주운 부재료) 창구. 상한은 Ingredient 표 cap + 창고 넓히기 단계만큼 더한 수 (Growth 표 storage 행)다.
    // 표에 없는 id(빈 문자열 포함)는 던지지 않고 Count · Cap 0 · IsFull · Has false 다.
    public interface IInventoryHub
    {
        IReadOnlyList<string> IngredientIds { get; }

        int StorageLevel { get; }

        int MaxStorageLevel { get; }

        // 다음 창고 넓히기 값. 최대면 -1.
        int NextStoragePrice { get; }

        // 모든 재료 상한에 더해지는 수.
        int StorageBonus { get; }

        int Count(string ingredientId);

        int Cap(string ingredientId);

        bool IsFull(string ingredientId);

        // 상한에서 잘라 넣고 실제로 들어간 수를 준다.
        int Add(string ingredientId, int amount);

        // 창고를 한 단계 넓힌다. 최대이거나 골드가 모자라면 false.
        bool BuyStorage();

        bool Has(string ingredientId, int amount);

        // 모자라면 false 를 주고 아무것도 안 뺀다.
        bool Take(string ingredientId, int amount);

        // 창고의 모든 재료를 개당 Ingredient 표 sellPrice 에 판다. 판 골드를 준다 (없으면 0).
        int SellAll();

        // 지금 다 팔면 받을 골드.
        int SellAllPrice();
    }
}
