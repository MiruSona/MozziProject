using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Codex;
using Mozzi.Game.Wallet;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 찹쌀이 도감 (세로조각1판설계 3-5). 24칸 격자 — 열린 칸은 색 · 이름 · 뱉는 재료, 안 열린 칸은 ???.
    // 열린 1·2단계 일반 종에는 「짝 사기」 버튼이 붙는다. 칸 이름은 계열·단계·변종으로 짓는다.
    public sealed class MozziCodexScreen : OverlayScreen
    {
        private const string Unknown = UiText.Unknown;

        private readonly Dictionary<string, CodexCellView> _cells = new Dictionary<string, CodexCellView>();

        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private Text _countText;

        protected override string PrefabName
        {
            get { return "MozziCodexScreen"; }
        }

        protected override string Title
        {
            get { return "찹쌀이 도감"; }
        }

        protected override void BindChildren()
        {
            _codex = InGameHub.Get<ICodexHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _countText = UiBindUtil.Find<Text>(transform, UiNames.CodexCountText, PrefabName);

            IReadOnlyList<MozziSpeciesRow> rows = _data.Species.All;
            for (int i = 0; i < rows.Count; i++)
            {
                MozziSpeciesRow row = rows[i];
                Transform cellRoot;
                if (UiBindUtil.TryFind<Transform>(transform, CellNameOf(row), out cellRoot) == false)
                {
                    Debug.LogWarning("[Codex] 프리팹에 칸 " + CellNameOf(row) + " 이 없다 (" + row.id + ")");
                    continue;
                }

                string speciesId = row.id;
                _cells[speciesId] = new CodexCellView(cellRoot, PrefabName, delegate { OnBuyPair(speciesId); });
            }
        }

        protected override void UnbindChildren()
        {
            _cells.Clear();
            _codex = null;
            _wallet = null;
            _data = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            UiBindUtil.SetText(_countText, _codex.OpenSpecies.Count + "/" + _cells.Count);

            foreach (KeyValuePair<string, CodexCellView> pair in _cells)
            {
                RefreshCell(_data.Species.Get(pair.Key), pair.Value);
            }
        }

        private void RefreshCell(MozziSpeciesRow species, CodexCellView cell)
        {
            bool open = _codex.IsOpen(species.id);
            if (open == false)
            {
                cell.ColorBox.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                UiBindUtil.SetText(cell.NameText, Unknown);
                UiBindUtil.SetText(cell.IngredientText, "");
                UiBindUtil.SetActive(cell.PairButton.gameObject, false);
                return;
            }

            cell.ColorBox.color = View.SpriteUtil.ParseHex(species.colorHex, Color.white);
            UiBindUtil.SetText(cell.NameText, species.name);
            UiBindUtil.SetText(cell.IngredientText, IngredientLabel(species));

            int price = _codex.PairPrice(species.id);
            if (price < 0)
            {
                UiBindUtil.SetActive(cell.PairButton.gameObject, false);
                return;
            }

            UiBindUtil.SetActive(cell.PairButton.gameObject, true);
            UiBindUtil.SetLabel(cell.PairButton, "짝 사기 " + price);
            cell.PairButton.interactable = _wallet.CanPay(price);
        }

        private string IngredientLabel(MozziSpeciesRow species)
        {
            IngredientRow ingredient;
            if (_data.Ingredients.TryGet(species.ingredientId, out ingredient) == false)
            {
                return "-";
            }

            return ingredient.name;
        }

        private void OnBuyPair(string speciesId)
        {
            _codex.BuyPair(speciesId);
            Refresh();
        }

        private static string CellNameOf(MozziSpeciesRow row)
        {
            string name = UiNames.CodexCellPrefix + row.family + "_" + row.tier;
            if (row.isShiny)
            {
                name += UiNames.CodexCellShinySuffix;
            }

            return name;
        }
    }
}
