using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Codex;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using TMPro;
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

        // 못 찾은 떡은 같은 그림을 검은 실루엣으로 보여 준다 (알파는 그림 것을 그대로 쓴다).
        private static readonly Color UnknownTint = new Color(0.118f, 0.118f, 0.141f, 1f);

        private readonly Dictionary<string, RecipeCellView> _cells = new Dictionary<string, RecipeCellView>();

        private ICodexHub _codex;
        private IStallHub _stall;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private TMP_Text _countText;

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
            _countText = UiBindUtil.Find<TMP_Text>(transform, UiNames.RecipeCodexCountText, PrefabName);

            IResourceHub resource = GlobalHub.Get<IResourceHub>();
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

                RecipeCellView cell = new RecipeCellView(cellRoot, PrefabName);
                _cells[rows[i].id] = cell;
                SetCellIcon(cell, resource.GetSprite(rows[i].spriteKey));
            }
        }

        // 그림이 없으면 아이콘을 꺼 두고 글자 셋을 칸 왼쪽(아이콘 넣기 전 자리)으로 되돌린다.
        private void SetCellIcon(RecipeCellView cell, Sprite sprite)
        {
            if (cell.Icon == null)
            {
                return;
            }

            cell.Icon.sprite = sprite;
            UiBindUtil.SetActive(cell.Icon.gameObject, sprite != null);

            float left = LayoutConstUi.CodexCellTextLeftNoIcon;
            float width = LayoutConstUi.CodexCellTextWidthNoIcon;
            if (sprite != null)
            {
                left = LayoutConstUi.CodexCellTextLeft;
                width = LayoutConstUi.CodexCellTextWidth;
            }

            MoveCellText(cell.NameText, left, width);
            MoveCellText(cell.IngredientText, left, width);
            MoveCellText(cell.PriceText, left, width);
        }

        // 글자 한 줄의 가로 자리와 폭만 바꾼다. 세로(y·높이)는 프리팹 값을 그대로 둔다.
        private static void MoveCellText(TMP_Text text, float left, float width)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = new Vector2(left, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
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
            if (cell.Icon != null)
            {
                Color tint = UnknownTint;
                if (open)
                {
                    tint = Color.white;
                }

                cell.Icon.color = tint;
            }

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
