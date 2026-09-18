using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 찹쌀이 도감의 칸 하나가 쥔 위젯 묶음.
    public sealed class CodexCellView
    {
        public readonly Image ColorBox;
        public readonly Text NameText;
        public readonly Text IngredientText;
        public readonly Button PairButton;

        public CodexCellView(Transform cellRoot, string prefabName, UnityAction onPair)
        {
            ColorBox = UiBindUtil.Find<Image>(cellRoot, UiNames.CodexCellColorBox, prefabName);
            NameText = UiBindUtil.Find<Text>(cellRoot, UiNames.CodexCellNameText, prefabName);
            IngredientText = UiBindUtil.Find<Text>(cellRoot, UiNames.CodexCellIngredientText, prefabName);
            PairButton = UiBindUtil.BindButton(cellRoot, UiNames.CodexCellPairButton, prefabName, onPair);
        }
    }
}
