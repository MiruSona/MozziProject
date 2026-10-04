using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 주방 화면 (임시판). 쟁반에서 재료 1~3개를 고르고 「떡메 치기」 → 처음 보는 조합이면 절구를 3번 눌러 만든다.
    // 찾은 레시피 · 해 본 조합은 바로 Make. 차림표 판에서는 찾은 떡을 한 번에 만든다. 결과는 접시 위에 보인다.
    // 다음 반복에 화면을 통째로 바꿀 예정이라 배치는 KitchenScreenSetup · KitchenLayout, 프레임은 KitchenMortarView 에 두고
    // 여기는 고르기 · 만들기 흐름만 든다.
    public sealed class KitchenScreen : OverlayScreen
    {
        private const string StallRowId = DataKeys.DefaultRowId;
        private const string PlainName = "평범한 떡";
        private const float FlashDuration = 0.4f;
        private const int MaxSelected = 3;

        private static readonly Color CellOff = new Color(1f, 1f, 1f, 0f);
        private static readonly Color CellOn = new Color(1f, 0.85f, 0.3f, 0.55f);
        private static readonly Color IconEmpty = new Color(0.35f, 0.35f, 0.35f, 1f);

        private readonly Dictionary<string, Button> _trayCells = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _trayIcons = new Dictionary<string, Image>();
        private readonly Dictionary<string, TMP_Text> _trayCounts = new Dictionary<string, TMP_Text>();
        private readonly List<Image> _mortarSlots = new List<Image>();
        private readonly List<Button> _menuRows = new List<Button>();
        private readonly List<Image> _menuRowIcons = new List<Image>();
        private readonly List<string> _menuRowRecipes = new List<string>();
        private readonly List<string> _selected = new List<string>();

        private IResourceHub _resource;
        private IStallHub _stall;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private PoundAgent _pound;
        private KitchenMortarView _mortar;
        private TMP_Text _hint;
        private Button _poundButton;

        private GameObject _result;
        private Image _resultIcon;
        private TMP_Text _resultName;
        private GameObject _resultNew;
        private Image _flash;
        private float _flashTime;
        private bool _flashing;

        private GameObject _menuPanel;
        private GameObject _menuEmpty;

        protected override string PrefabName
        {
            get { return "KitchenScreen"; }
        }

        protected override string Title
        {
            get { return "주방"; }
        }

        protected override void BindChildren()
        {
            _stall = InGameHub.Get<IStallHub>();
            _codex = InGameHub.Get<ICodexHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _resource = GlobalHub.Get<IResourceHub>();
            _pound = new PoundAgent(this, _data.Stalls.Get(StallRowId));

            BindTray();
            BindMortar();

            _result = UiBindUtil.FindObject(transform, UiNames.KitchenResult, PrefabName);
            _resultIcon = UiBindUtil.Find<Image>(_result.transform, UiNames.CakeIcon, PrefabName);
            _resultName = UiBindUtil.Find<TMP_Text>(_result.transform, UiNames.KitchenResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_result.transform, UiNames.KitchenResultNewText, PrefabName);
            _flash = UiBindUtil.Find<Image>(transform, UiNames.KitchenResultFlash, PrefabName);
            _hint = UiBindUtil.Find<TMP_Text>(transform, UiNames.KitchenHintText, PrefabName);

            _poundButton = UiBindUtil.BindButton(transform, UiNames.KitchenPoundButton, PrefabName, OnPoundButton);
            UiBindUtil.BindButton(transform, UiNames.KitchenMenuButton, PrefabName, ToggleMenu);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);
            UiBindUtil.BindButton(transform, UiNames.KitchenDoorButton, PrefabName, _ui.OpenStall);

            BindMenu();
            ClearResult();
        }

        private void BindTray()
        {
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                Button cell = UiBindUtil.BindButton(transform, UiNames.KitchenTrayCellPrefix + id, PrefabName,
                    delegate { OnToggle(id); });
                _trayCells[id] = cell;
                _trayCounts[id] = UiBindUtil.Find<TMP_Text>(cell.transform, UiNames.KitchenTrayCountText, PrefabName);

                Image icon = UiBindUtil.Find<Image>(cell.transform, UiNames.IngredientIcon, PrefabName);
                icon.sprite = IngredientSprite(id);
                UiBindUtil.SetActive(icon.gameObject, icon.sprite != null);
                _trayIcons[id] = icon;
            }
        }

        private void BindMortar()
        {
            Button mortar = UiBindUtil.BindButton(transform, UiNames.KitchenMortar, PrefabName, OnMortarTap);
            Sprite[] frames = new Sprite[SpriteKeys.KitchenPoundFrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = _resource.GetSprite(SpriteKeys.KitchenPound(i));
            }

            _mortar = new KitchenMortarView(mortar.image, frames, KitchenLayout.PoundFrameSec);

            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                _mortarSlots.Add(UiBindUtil.Find<Image>(transform, UiNames.KitchenMortarSlotPrefix + i, PrefabName));
            }
        }

        private void BindMenu()
        {
            _menuPanel = UiBindUtil.FindObject(transform, UiNames.KitchenMenuPanel, PrefabName);
            _menuEmpty = UiBindUtil.FindObject(_menuPanel.transform, UiNames.KitchenMenuEmptyText, PrefabName);
            UiBindUtil.BindButton(_menuPanel.transform, UiNames.KitchenMenuBlocker, PrefabName, CloseMenu);

            for (int i = 0; i < UiNames.KitchenMenuRowCount; i++)
            {
                int index = i;
                Button row = UiBindUtil.BindButton(_menuPanel.transform, UiNames.KitchenMenuRowPrefix + i, PrefabName,
                    delegate { OnMenuRow(index); });
                _menuRows.Add(row);
                _menuRowIcons.Add(UiBindUtil.Find<Image>(row.transform, UiNames.CakeIcon, PrefabName));
                _menuRowRecipes.Add(null);
            }

            UiBindUtil.SetActive(_menuPanel, false);
        }

        protected override void UnbindChildren()
        {
            _trayCells.Clear();
            _trayIcons.Clear();
            _trayCounts.Clear();
            _mortarSlots.Clear();
            _menuRows.Clear();
            _menuRowIcons.Clear();
            _menuRowRecipes.Clear();
            _selected.Clear();
            _flashing = false;
            _pound = null;
            _mortar = null;

            _resource = null;
            _stall = null;
            _codex = null;
            _wallet = null;
            _inventory = null;
            _data = null;
            _ui = null;
        }

        // 가판대 칠판에서 바로 차림표로 올 때도 이걸 부른다.
        public void OpenMenu()
        {
            if (_pound.IsRunning)
            {
                return;
            }

            UiBindUtil.SetActive(_menuPanel, true);
            RefreshMenu();
        }

        // 아래 줄 「차림표」는 열려 있으면 닫는다 (판이 가림막을 거의 다 덮는다).
        private void ToggleMenu()
        {
            if (_menuPanel.activeSelf)
            {
                CloseMenu();
                return;
            }

            OpenMenu();
        }

        private void CloseMenu()
        {
            UiBindUtil.SetActive(_menuPanel, false);
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshTray();
            RefreshMortarSlots();
            RefreshHint();
            if (_menuPanel.activeSelf)
            {
                RefreshMenu();
            }
        }

        public override void Process(float deltaTime)
        {
            _mortar.Process(deltaTime);
            if (_flashing == false)
            {
                return;
            }

            _flashTime += deltaTime;
            float alpha = 1f - Mathf.Clamp01(_flashTime / FlashDuration);
            _flash.color = new Color(1f, 1f, 1f, alpha);
            if (alpha <= 0f)
            {
                _flashing = false;
            }
        }

        public override void OnClosed()
        {
            _pound.Cancel();
            _mortar.Stop();
            _selected.Clear();
            _flashing = false;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            ClearResult();
            CloseMenu();
        }

        // 고른 칸은 노랗게, 남은 수가 0 이면 그림을 어둡게. 다 골랐으면(3개) 안 고른 칸은 못 누른다.
        private void RefreshTray()
        {
            bool full = _selected.Count >= MaxSelected;
            foreach (KeyValuePair<string, Button> pair in _trayCells)
            {
                string id = pair.Key;
                int count = _inventory.Count(id);
                bool on = _selected.Contains(id);

                pair.Value.image.color = CellOff;
                if (on)
                {
                    pair.Value.image.color = CellOn;
                }

                pair.Value.interactable = on || (count > 0 && full == false);
                _trayIcons[id].color = Color.white;
                if (count <= 0)
                {
                    _trayIcons[id].color = IconEmpty;
                }

                UiBindUtil.SetText(_trayCounts[id], count.ToString());
            }
        }

        // 절구 아래에 고른 재료 그림을 차례로.
        private void RefreshMortarSlots()
        {
            for (int i = 0; i < _mortarSlots.Count; i++)
            {
                bool has = i < _selected.Count;
                UiBindUtil.SetActive(_mortarSlots[i].gameObject, has);
                if (has)
                {
                    _mortarSlots[i].sprite = IngredientSprite(_selected[i]);
                }
            }
        }

        private void RefreshHint()
        {
            if (_pound.IsRunning)
            {
                UiBindUtil.SetText(_hint, "절구를 눌러 떡메를 치세요 " + _pound.Taps + "/" + _pound.TotalTaps);
                UiBindUtil.SetLabel(_poundButton, "치는 중");
                _poundButton.interactable = false;
                return;
            }

            UiBindUtil.SetText(_hint, ExperimentInfo());
            UiBindUtil.SetLabel(_poundButton, "떡메 치기");
            _poundButton.interactable = _selected.Count > 0 && _stall.CanMake(_selected);
        }

        private string ExperimentInfo()
        {
            if (_stall.Shelf.Count >= _stall.ShelfMax)
            {
                return "진열대가 가득 찼어요. 가판대에서 팔고 오세요";
            }

            if (_selected.Count == 0)
            {
                return "쟁반에서 재료를 1~3개 고르세요";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < _selected.Count; i++)
            {
                names.Add(_data.Ingredients.Get(_selected[i]).Name);
            }

            string head = string.Join("+", names);
            RecipeRow recipe = RecipeMatchUtil.FindRecipe(_data.Recipes, _selected);
            if (recipe != null && _codex.IsRecipeOpen(recipe.Id))
            {
                return head + " → " + recipe.Name + " (찾음)";
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return head + " → 해 본 조합 · " + PlainName;
            }

            return head + " → 새 조합! 떡메를 친다";
        }

        // 찾은 레시피만 표 순서대로. 재료가 모자라면 못 누른다.
        private void RefreshMenu()
        {
            List<RecipeRow> open = new List<RecipeRow>();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (_codex.IsRecipeOpen(rows[i].Id))
                {
                    open.Add(rows[i]);
                }
            }

            UiBindUtil.SetActive(_menuEmpty, open.Count == 0);
            for (int i = 0; i < _menuRows.Count; i++)
            {
                bool has = i < open.Count;
                UiBindUtil.SetActive(_menuRows[i].gameObject, has);
                if (has == false)
                {
                    _menuRowRecipes[i] = null;
                    continue;
                }

                RecipeRow recipe = open[i];
                _menuRowRecipes[i] = recipe.Id;
                Sprite sprite = _resource.GetSprite(recipe.SpriteKey);
                _menuRowIcons[i].sprite = sprite;
                UiBindUtil.SetActive(_menuRowIcons[i].gameObject, sprite != null);
                UiBindUtil.SetLabel(_menuRows[i], recipe.Name + " " + _stall.PreviewPrice(recipe.IngredientIds) + "G");
                _menuRows[i].interactable = _stall.CanMake(recipe.IngredientIds);
            }
        }

        private Sprite IngredientSprite(string ingredientId)
        {
            return _resource.GetSprite(_data.Ingredients.Get(ingredientId).SpriteKey);
        }

        // 떡메를 치는 동안은 고르기를 받지 않는다.
        private void OnToggle(string ingredientId)
        {
            if (_pound.IsRunning)
            {
                return;
            }

            if (_selected.Remove(ingredientId) == false && _selected.Count < MaxSelected)
            {
                _selected.Add(ingredientId);
            }

            ClearResult();
            Refresh();
        }

        // 찾은 레시피 · 해 본 조합은 바로, 처음 보는 조합은 떡메 3탭으로. 목록은 복사해 넘긴다 (이벤트 중 Refresh 가 끼어든다).
        private void OnPoundButton()
        {
            List<string> ids = new List<string>(_selected);
            if (ids.Count == 0 || _pound.IsRunning || _stall.CanMake(ids) == false)
            {
                return;
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(ids)))
            {
                _selected.Clear();
                _mortar.Play();
                MakeAndShow(ids);
                return;
            }

            ClearResult();
            _pound.Begin(ids);
            Refresh();
        }

        // 치는 중이 아니면 「떡메 치기」를 누른 것과 같다.
        private void OnMortarTap()
        {
            if (_pound.IsRunning == false)
            {
                OnPoundButton();
                return;
            }

            PoundTapResult result = _pound.Tap(Time.realtimeSinceStartup);
            if (result == PoundTapResult.Ignored)
            {
                return;
            }

            _mortar.Play();
            if (result == PoundTapResult.Counted)
            {
                RefreshHint();
                return;
            }

            List<string> ids = new List<string>(_pound.IngredientIds);
            _selected.Clear();
            MakeAndShow(ids);
        }

        private void OnMenuRow(int rowIndex)
        {
            string recipeId = _menuRowRecipes[rowIndex];
            if (recipeId == null || _pound.IsRunning)
            {
                return;
            }

            MakeAndShow(_data.Recipes.Get(recipeId).IngredientIds);
        }

        private void MakeAndShow(IReadOnlyList<string> ids)
        {
            MakeResult made = _stall.Make(ids);
            Refresh();
            if (made.Succeeded)
            {
                ShowResult(made);
            }
        }

        // 접시 위에 떡 그림 · 이름 · 값. 새 레시피면 「NEW」 와 흰 번쩍(알파 1→0).
        private void ShowResult(MakeResult made)
        {
            string name = PlainName;
            string spriteKey = SpriteKeys.CakePlain;
            if (made.IsPlain == false)
            {
                RecipeRow recipe = _data.Recipes.Get(made.RecipeId);
                name = recipe.Name;
                spriteKey = recipe.SpriteKey;
            }

            Sprite sprite = _resource.GetSprite(spriteKey);
            _resultIcon.sprite = sprite;
            UiBindUtil.SetActive(_resultIcon.gameObject, sprite != null);
            UiBindUtil.SetActive(_resultNew, made.IsNewRecipe);
            UiBindUtil.SetText(_resultName, name + "\n" + made.Price + "G");
            UiBindUtil.SetActive(_result, true);

            _flashing = made.IsNewRecipe;
            _flashTime = 0f;
            float alpha = 0f;
            if (_flashing)
            {
                alpha = 1f;
            }

            _flash.color = new Color(1f, 1f, 1f, alpha);
        }

        private void ClearResult()
        {
            UiBindUtil.SetActive(_result, false);
        }
    }
}
