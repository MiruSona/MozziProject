using System;
using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Codex;
using Mozzi.Game.Stall;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 차림표 판 하나 (MenuPanel) : 찾은 떡을 한 번에 만드는 목록. 주방과 가판대가 같은 판을 쓴다 (짓기는 FormalUiBuild.MakeRecipeMenuPanel).
    // 판은 화면 아래(버튼 줄 바로 위)에 뜬다. 판 뒤 Blocker 를 누르면 닫히고, 줄을 누르면 그 레시피 id 를 onPick 으로 넘긴다. 만들지 말지(떡메 치는 중 등)는 쓰는 화면이 정한다.
    public sealed class RecipeMenuView
    {
        private readonly List<Button> _rows = new List<Button>();
        private readonly List<Image> _rowIcons = new List<Image>();
        private readonly List<string> _rowRecipes = new List<string>();
        private readonly List<RecipeRow> _open = new List<RecipeRow>();

        private readonly RectTransform _box;
        private readonly GameObject _panel;
        private readonly GameObject _empty;
        private readonly IResourceHub _resource;
        private readonly IStaticDataHub _data;
        private readonly ICodexHub _codex;
        private readonly IStallHub _stall;
        private readonly Action<string> _onPick;

        public RecipeMenuView(Transform screenRoot, string prefabName, IResourceHub resource, IStaticDataHub data,
            ICodexHub codex, IStallHub stall, Action<string> onPick)
        {
            _resource = resource;
            _data = data;
            _codex = codex;
            _stall = stall;
            _onPick = onPick;

            _panel = UiBindUtil.FindObject(screenRoot, UiNames.KitchenMenuPanel, prefabName);
            _box = UiBindUtil.Find<RectTransform>(_panel.transform, UiNames.KitchenMenuBox, prefabName);
            _empty = UiBindUtil.FindObject(_panel.transform, UiNames.KitchenMenuEmptyText, prefabName);
            UiBindUtil.BindButton(_panel.transform, UiNames.KitchenMenuBlocker, prefabName, Close);

            for (int i = 0; i < UiNames.KitchenMenuRowCount; i++)
            {
                int index = i;
                Button row = UiBindUtil.BindButton(_panel.transform, UiNames.KitchenMenuRowPrefix + i, prefabName,
                    delegate { OnRow(index); });
                _rows.Add(row);
                _rowIcons.Add(UiBindUtil.Find<Image>(row.transform, UiNames.CakeIcon, prefabName));
                _rowRecipes.Add(null);
            }

            Close();
        }

        public bool IsOpen
        {
            get { return _panel.activeSelf; }
        }

        public void Open()
        {
            UiBindUtil.SetActive(_panel, true);
            Refresh();
        }

        public void Close()
        {
            UiBindUtil.SetActive(_panel, false);
        }

        // 찾은 레시피만 표 순서대로, 이름 · 값. 재료가 모자라거나 진열장이 차면 못 누른다.
        // 상자는 아래끝이 버튼 줄 위에 붙어 있고(프리팹) 높이만 찾은 떡 줄 수에 맞춘다 — 줄이 늘면 위로 자란다.
        public void Refresh()
        {
            _open.Clear();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (_codex.IsRecipeOpen(rows[i].Id))
                {
                    _open.Add(rows[i]);
                }
            }

            int shown = Mathf.Min(_open.Count, _rows.Count);
            _box.sizeDelta = new Vector2(_box.sizeDelta.x, KitchenLayout.MenuBoxHeight(shown));
            UiBindUtil.SetActive(_empty, _open.Count == 0);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool has = i < _open.Count;
                UiBindUtil.SetActive(_rows[i].gameObject, has);
                if (has == false)
                {
                    _rowRecipes[i] = null;
                    continue;
                }

                RecipeRow recipe = _open[i];
                _rowRecipes[i] = recipe.Id;
                Sprite sprite = _resource.GetSprite(recipe.SpriteKey);
                _rowIcons[i].sprite = sprite;
                UiBindUtil.SetActive(_rowIcons[i].gameObject, sprite != null);
                UiBindUtil.SetLabel(_rows[i], recipe.Name + " " + _stall.PreviewPrice(recipe.IngredientIds) + "G");
                _rows[i].interactable = _stall.CanMake(recipe.IngredientIds);
            }
        }

        private void OnRow(int rowIndex)
        {
            string recipeId = _rowRecipes[rowIndex];
            if (recipeId == null)
            {
                return;
            }

            _onPick(recipeId);
        }
    }
}
