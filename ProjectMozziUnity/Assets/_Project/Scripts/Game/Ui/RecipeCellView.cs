using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 레시피 도감의 칸 하나가 쥔 글자 셋과 떡 그림. 그림 자식이 없는 프리팹이면 Icon 은 null 이다.
    public sealed class RecipeCellView
    {
        public readonly TMP_Text NameText;
        public readonly TMP_Text IngredientText;
        public readonly TMP_Text PriceText;
        public readonly Image Icon;

        public RecipeCellView(Transform cellRoot, string prefabName)
        {
            NameText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellNameText, prefabName);
            IngredientText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellIngredientText, prefabName);
            PriceText = UiBindUtil.Find<TMP_Text>(cellRoot, UiNames.RecipeCodexCellPriceText, prefabName);

            Image icon;
            UiBindUtil.TryFind<Image>(cellRoot, UiNames.CakeIcon, out icon);
            Icon = icon;
        }
    }
}
