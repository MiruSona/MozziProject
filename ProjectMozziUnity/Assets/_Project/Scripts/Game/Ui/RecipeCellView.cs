using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 레시피 도감의 칸 하나가 쥔 글자 셋.
    public sealed class RecipeCellView
    {
        public readonly TMP_Text NameText;
        public readonly TMP_Text IngredientText;
        public readonly TMP_Text PriceText;

        public RecipeCellView(Transform cellRoot, string prefabName)
        {
            NameText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellNameText, prefabName);
            IngredientText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellIngredientText, prefabName);
            PriceText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellPriceText, prefabName);
        }
    }
}
