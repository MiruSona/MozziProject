using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Codex;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 레시피 도감 (세로조각1판설계 3-5). 10칸 — 찾은 칸은 이름 · 재료 · 값(주문 아닌 값), 안 찾은 칸은 ???.
    // 3재료 칸은 revealIngredientId 재료 하나를 미리 보여 준다 (「? + ? + 팥」).
    public sealed class RecipeCodexScreen : OverlayScreen
    {
        private const string Unknown = UiText.Unknown;
        private const string Hidden = "?";
        private const string Joiner = " + ";

        private readonly Dictionary<string, RecipeCellView> _cells = new Dictionary<string, RecipeCellView>();

        private ICodexHub _codex;
        private IStallHub _stall;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private Text _countText;

        protected override string PrefabName
        {
            get { return "RecipeCodexScreen"; }
        }

        protected override string Title
        {
            get { return "레시피 도감"; }
        }

        protected override void BindChildren()
        {
            _codex = InGameHub.Get<ICodexHub>();
            _stall = InGameHub.Get<IStallHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _countText = UiBindUtil.Find<Text>(transform, UiNames.RecipeCodexCountText, PrefabName);

            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                string cellName = UiNames.RecipeCodexCellPrefix + rows[i].id;
                Transform cellRoot;
                if (UiBindUtil.TryFind<Transform>(transform, cellName, out cellRoot) == false)
                {
                    Debug.LogWarning("[RecipeCodex] 프리팹에 칸 " + cellName + " 이 없다");
                    continue;
                }

                _cells[rows[i].id] = new RecipeCellView(cellRoot, PrefabName);
            }
        }

        protected override void UnbindChildren()
        {
            _cells.Clear();
            _codex = null;
            _stall = null;
            _wallet = null;
            _data = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);

            int openCount = 0;
            foreach (KeyValuePair<string, RecipeCellView> pair in _cells)
            {
                RecipeRow row = _data.Recipes.Get(pair.Key);
                bool open = _codex.IsRecipeOpen(row.id);
                if (open)
                {
                    openCount++;
                }

                RefreshCell(row, pair.Value, open);
            }

            UiBindUtil.SetText(_countText, openCount + "/" + _cells.Count);
        }

        private void RefreshCell(RecipeRow row, RecipeCellView cell, bool open)
        {
            if (open == false)
            {
                UiBindUtil.SetText(cell.NameText, Unknown);
                UiBindUtil.SetText(cell.IngredientText, HiddenIngredients(row));
                UiBindUtil.SetText(cell.PriceText, "");
                return;
            }

            UiBindUtil.SetText(cell.NameText, row.name);
            UiBindUtil.SetText(cell.IngredientText, IngredientNames(row.ingredientIds));
            UiBindUtil.SetText(cell.PriceText, _stall.PreviewPrice(row.ingredientIds) + "G");
        }

        // 재료 수만큼 ? 를 놓고, 미리 공개 재료가 있으면 맨 뒤 하나를 이름으로 바꾼다.
        private string HiddenIngredients(RecipeRow row)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < row.ingredientIds.Count; i++)
            {
                parts.Add(Hidden);
            }

            IngredientRow reveal;
            if (parts.Count > 0 && _data.Ingredients.TryGet(row.revealIngredientId, out reveal))
            {
                parts[parts.Count - 1] = reveal.name;
            }

            return string.Join(Joiner, parts);
        }

        private string IngredientNames(IReadOnlyList<string> ingredientIds)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < ingredientIds.Count; i++)
            {
                parts.Add(_data.Ingredients.Get(ingredientIds[i]).name);
            }

            return string.Join(Joiner, parts);
        }
    }
}
