using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 레시피 도감의 칸 하나가 쥔 글자 셋.
    public sealed class RecipeCellView
    {
        public readonly Text NameText;
        public readonly Text IngredientText;
        public readonly Text PriceText;

        public RecipeCellView(Transform cellRoot, string prefabName)
        {
            NameText = UiBindUtil.Find<Text>(cellRoot, UiNames.RecipeCodexCellNameText, prefabName);
            IngredientText = UiBindUtil.Find<Text>(cellRoot, UiNames.RecipeCodexCellIngredientText, prefabName);
            PriceText = UiBindUtil.Find<Text>(cellRoot, UiNames.RecipeCodexCellPriceText, prefabName);
        }
    }
}
