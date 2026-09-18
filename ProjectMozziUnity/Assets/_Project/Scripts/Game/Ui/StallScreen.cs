using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 가판대 화면 (세로조각1판설계 3-4). 머리 · 진열대 · 손님 줄 · 만들기 · 실험 · 버튼 띠, 위에 떡메 덮개와 발견 연출.
    // 찾은 레시피 · 해 본 조합은 탭 한 번에 Make, 처음 보는 조합만 PoundAgent 가 3탭을 돌린 뒤 Make 를 부른다.
    public sealed class StallScreen : OverlayScreen
    {
        private const string StallRowId = DataKeys.DefaultRowId;
        private const float FlashDuration = 0.4f;
        private const float DoughSquash = 0.6f;
        private const float DoughRecoverPerSec = 3f;
        private const string PlainName = "평범한 떡";

        private static readonly Color ToggleOff = new Color(0.35f, 0.38f, 0.55f, 1f);
        private static readonly Color ToggleOn = new Color(0.9f, 0.7f, 0.25f, 1f);
        private static readonly Color CellEmpty = new Color(0.2f, 0.2f, 0.26f, 1f);
        private static readonly Color CellCustomer = new Color(0.55f, 0.45f, 0.35f, 1f);
        private static readonly Color CellHint = new Color(0.35f, 0.55f, 0.65f, 1f);

        private readonly List<Button> _queueCells = new List<Button>();
        private readonly List<Button> _makeRows = new List<Button>();
        private readonly List<string> _makeRowRecipes = new List<string>();
        private readonly Dictionary<string, Button> _expToggles = new Dictionary<string, Button>();
        private readonly List<string> _selected = new List<string>();

        private IStallHub _stall;
        private IClockHub _clock;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private Text _status;
        private Text _shelf;
        private Text _expInfo;
        private Button _expMake;
        private Button _openButton;
        private Button _clerkButton;

        private GameObject _poundPanel;
        private Text _poundHint;
        private Text _poundCount;
        private RectTransform _poundDough;
        private PoundAgent _pound;

        private GameObject _madePanel;
        private Image _madeFlash;
        private GameObject _madeNew;
        private Text _madeName;
        private Text _madePrice;
        private float _flashTime;
        private bool _flashing;

        protected override string PrefabName
        {
            get { return "StallScreen"; }
        }

        protected override string Title
        {
            get { return "가판대"; }
        }

        protected override void BindChildren()
        {
            _stall = InGameHub.Get<IStallHub>();
            _clock = InGameHub.Get<IClockHub>();
            _codex = InGameHub.Get<ICodexHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _pound = new PoundAgent(this, _data.Stalls.Get(StallRowId));

            _status = UiBindUtil.Find<Text>(transform, UiNames.StallStatusText, PrefabName);
            _shelf = UiBindUtil.Find<Text>(transform, UiNames.StallShelfText, PrefabName);

            for (int i = 0; i < _stall.QueueMax; i++)
            {
                int index = i;
                _queueCells.Add(UiBindUtil.BindButton(transform, UiNames.StallQueueCellPrefix + i, PrefabName,
                    delegate { OnServe(index); }));
            }

            for (int i = 0; i < UiNames.StallMakeRowCount; i++)
            {
                int index = i;
                _makeRows.Add(UiBindUtil.BindButton(transform, UiNames.StallMakeRowPrefix + i, PrefabName,
                    delegate { OnMakeRow(index); }));
                _makeRowRecipes.Add(null);
            }

            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                _expToggles[id] = UiBindUtil.BindButton(transform, UiNames.StallExpTogglePrefix + id, PrefabName,
                    delegate { OnToggle(id); });
            }

            _expInfo = UiBindUtil.Find<Text>(transform, UiNames.StallExpInfoText, PrefabName);
            _expMake = UiBindUtil.BindButton(transform, UiNames.StallExpMakeButton, PrefabName, OnExpMake);
            _openButton = UiBindUtil.BindButton(transform, UiNames.StallOpenButton, PrefabName, OnOpenOrNextDay);
            _clerkButton = UiBindUtil.BindButton(transform, UiNames.StallClerkButton, PrefabName, OnBuyClerk);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);

            _poundPanel = UiBindUtil.FindObject(transform, UiNames.PoundPanel, PrefabName);
            _poundHint = UiBindUtil.Find<Text>(_poundPanel.transform, UiNames.PoundHintText, PrefabName);
            _poundCount = UiBindUtil.Find<Text>(_poundPanel.transform, UiNames.PoundCountText, PrefabName);
            _poundDough = UiBindUtil.Find<RectTransform>(_poundPanel.transform, UiNames.PoundDoughBox, PrefabName);
            Button poundButton = _poundPanel.GetComponent<Button>();
            poundButton.onClick.RemoveAllListeners();
            poundButton.onClick.AddListener(OnPoundTap);

            _madePanel = UiBindUtil.FindObject(transform, UiNames.StallMadePanel, PrefabName);
            _madeFlash = UiBindUtil.Find<Image>(_madePanel.transform, UiNames.StallMadeFlash, PrefabName);
            _madeNew = UiBindUtil.FindObject(_madePanel.transform, UiNames.StallMadeNewText, PrefabName);
            _madeName = UiBindUtil.Find<Text>(_madePanel.transform, UiNames.StallMadeNameText, PrefabName);
            _madePrice = UiBindUtil.Find<Text>(_madePanel.transform, UiNames.StallMadePriceText, PrefabName);
            Button madeButton = _madePanel.GetComponent<Button>();
            madeButton.onClick.RemoveAllListeners();
            madeButton.onClick.AddListener(CloseMade);

            UiBindUtil.SetActive(_poundPanel, false);
            UiBindUtil.SetActive(_madePanel, false);
        }

        protected override void UnbindChildren()
        {
            _queueCells.Clear();
            _makeRows.Clear();
            _makeRowRecipes.Clear();
            _expToggles.Clear();
            _selected.Clear();
            _flashing = false;
            _pound = null;

            _stall = null;
            _clock = null;
            _codex = null;
            _wallet = null;
            _inventory = null;
            _data = null;
            _ui = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshStatus();
            RefreshShelf();
            RefreshQueue();
            RefreshMakeRows();
            RefreshExperiment();
            RefreshButtons();
        }

        public override void Process(float deltaTime)
        {
            if (_poundPanel.activeSelf)
            {
                float y = Mathf.MoveTowards(_poundDough.localScale.y, 1f, DoughRecoverPerSec * deltaTime);
                _poundDough.localScale = new Vector3(2f - y, y, 1f);
            }

            if (_flashing == false)
            {
                return;
            }

            _flashTime += deltaTime;
            float alpha = 1f - Mathf.Clamp01(_flashTime / FlashDuration);
            _madeFlash.color = new Color(1f, 1f, 1f, alpha);
            if (alpha <= 0f)
            {
                _flashing = false;
            }
        }

        public override void OnClosed()
        {
            _pound.Cancel();
            _selected.Clear();
            _flashing = false;
            UiBindUtil.SetActive(_poundPanel, false);
            UiBindUtil.SetActive(_madePanel, false);
        }

        private void RefreshStatus()
        {
            string state = "닫힘";
            if (_clock.IsNight)
            {
                state = "밤";
            }
            else if (_stall.IsOpen)
            {
                state = "열림";
            }

            UiBindUtil.SetText(_status, "오늘 손님 " + _stall.ServedToday + "/" + _stall.CustomersPerDay + " · " + state);
        }

        // 종류별 수를 표 순서대로, 평범한 떡은 맨 뒤에 묶는다.
        private void RefreshShelf()
        {
            IReadOnlyList<ShelfItem> shelf = _stall.Shelf;
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int plain = 0;
            for (int i = 0; i < shelf.Count; i++)
            {
                if (shelf[i].IsPlain)
                {
                    plain++;
                    continue;
                }

                int count;
                counts.TryGetValue(shelf[i].recipeId, out count);
                counts[shelf[i].recipeId] = count + 1;
            }

            List<string> parts = new List<string>();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                int count;
                if (counts.TryGetValue(rows[i].id, out count))
                {
                    parts.Add(rows[i].name + "×" + count);
                }
            }

            if (plain > 0)
            {
                parts.Add(PlainName + "×" + plain);
            }

            string body = "비었음";
            if (parts.Count > 0)
            {
                body = string.Join("  ", parts);
            }

            UiBindUtil.SetText(_shelf, "진열대 " + shelf.Count + "/" + _stall.ShelfMax + "\n" + body);
        }

        private void RefreshQueue()
        {
            IReadOnlyList<StallCustomer> queue = _stall.Queue;
            for (int i = 0; i < _queueCells.Count; i++)
            {
                Button cell = _queueCells[i];
                if (i >= queue.Count)
                {
                    cell.image.color = CellEmpty;
                    cell.interactable = false;
                    UiBindUtil.SetLabel(cell, "");
                    continue;
                }

                StallCustomer customer = queue[i];
                cell.image.color = CellCustomer;
                if (customer.isHint)
                {
                    cell.image.color = CellHint;
                }

                cell.interactable = _stall.Shelf.Count > 0;
                UiBindUtil.SetLabel(cell, OrderLabel(customer));
            }
        }

        // 못 찾은 떡 주문은 「? 팥+콩」처럼 재료 이름만 보인다.
        private string OrderLabel(StallCustomer customer)
        {
            if (customer.HasOrder == false)
            {
                return "손님\n아무거나";
            }

            RecipeRow recipe = _data.Recipes.Get(customer.orderRecipeId);
            if (_codex.IsRecipeOpen(recipe.id))
            {
                return "손님\n" + recipe.name;
            }

            List<string> names = new List<string>();
            for (int i = 0; i < recipe.ingredientIds.Count; i++)
            {
                names.Add(_data.Ingredients.Get(recipe.ingredientIds[i]).name);
            }

            return "손님\n? " + string.Join("+", names);
        }

        // 찾은 레시피만 표 순서대로 한 줄씩. 재료가 있으면 밝게.
        private void RefreshMakeRows()
        {
            List<RecipeRow> open = new List<RecipeRow>();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (_codex.IsRecipeOpen(rows[i].id))
                {
                    open.Add(rows[i]);
                }
            }

            for (int i = 0; i < _makeRows.Count; i++)
            {
                bool has = i < open.Count;
                UiBindUtil.SetActive(_makeRows[i].gameObject, has);
                if (has == false)
                {
                    _makeRowRecipes[i] = null;
                    continue;
                }

                RecipeRow recipe = open[i];
                _makeRowRecipes[i] = recipe.id;
                UiBindUtil.SetLabel(_makeRows[i], recipe.name + " " + _stall.PreviewPrice(recipe.ingredientIds) + "G");
                _makeRows[i].interactable = _stall.CanMake(recipe.ingredientIds);
            }
        }

        private void RefreshExperiment()
        {
            bool full = _selected.Count >= 3;
            foreach (KeyValuePair<string, Button> pair in _expToggles)
            {
                string id = pair.Key;
                bool on = _selected.Contains(id);
                int count = _inventory.Count(id);
                pair.Value.image.color = ToggleOff;
                if (on)
                {
                    pair.Value.image.color = ToggleOn;
                }

                pair.Value.interactable = on || (count > 0 && full == false);
                UiBindUtil.SetLabel(pair.Value, _data.Ingredients.Get(id).name + " " + count);
            }

            UiBindUtil.SetText(_expInfo, ExperimentInfo());
            _expMake.interactable = _selected.Count > 0 && _stall.CanMake(_selected);
        }

        private string ExperimentInfo()
        {
            if (_selected.Count == 0)
            {
                return "재료를 1~3개 고르세요";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < _selected.Count; i++)
            {
                names.Add(_data.Ingredients.Get(_selected[i]).name);
            }

            string head = string.Join("+", names);
            RecipeRow recipe = RecipeMatchUtil.FindRecipe(_data.Recipes, _selected);
            if (recipe != null && _codex.IsRecipeOpen(recipe.id))
            {
                return head + " → " + recipe.name + " (찾음)";
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return head + " → 해 본 조합 · " + PlainName;
            }

            return head + " → 새 조합! 떡메를 친다";
        }

        private void RefreshButtons()
        {
            if (_clock.IsNight)
            {
                UiBindUtil.SetLabel(_openButton, "다음날로");
                _openButton.interactable = true;
            }
            else if (_stall.IsOpen)
            {
                UiBindUtil.SetLabel(_openButton, "열림");
                _openButton.interactable = false;
            }
            else
            {
                UiBindUtil.SetLabel(_openButton, "열기");
                _openButton.interactable = true;
            }

            if (_stall.HasClerk)
            {
                UiBindUtil.SetLabel(_clerkButton, "점원 있음");
                _clerkButton.interactable = false;
                return;
            }

            UiBindUtil.SetLabel(_clerkButton, "점원 " + _stall.ClerkPrice + "G");
            _clerkButton.interactable = _wallet.CanPay(_stall.ClerkPrice);
        }

        private void OnServe(int queueIndex)
        {
            _stall.Serve(queueIndex);
            Refresh();
        }

        private void OnMakeRow(int rowIndex)
        {
            string recipeId = _makeRowRecipes[rowIndex];
            if (recipeId == null || _pound.IsRunning)
            {
                return;
            }

            _stall.Make(_data.Recipes.Get(recipeId).ingredientIds);
            Refresh();
        }

        // 떡메 덮개가 떠 있는 동안은 밑의 버튼을 받지 않는다 (덮개가 레이캐스트를 막지만 코드 호출도 막는다).
        private void OnToggle(string ingredientId)
        {
            if (_pound.IsRunning)
            {
                return;
            }

            if (_selected.Remove(ingredientId) == false && _selected.Count < 3)
            {
                _selected.Add(ingredientId);
            }

            RefreshExperiment();
        }

        // 찾은 레시피 · 해 본 조합은 바로, 처음 보는 조합은 떡메 덮개로. 목록은 복사해 넘긴다 (이벤트 중 Refresh 가 끼어든다).
        private void OnExpMake()
        {
            List<string> ids = new List<string>(_selected);
            if (ids.Count == 0 || _pound.IsRunning || _stall.CanMake(ids) == false)
            {
                return;
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(ids)))
            {
                _selected.Clear();
                _stall.Make(ids);
                Refresh();
                return;
            }

            _pound.Begin(ids);
            _poundDough.localScale = Vector3.one;
            UiBindUtil.SetText(_poundHint, "떡메를 " + _pound.TotalTaps + "번 치세요");
            UiBindUtil.SetText(_poundCount, "0/" + _pound.TotalTaps);
            UiBindUtil.SetActive(_poundPanel, true);
        }

        private void OnPoundTap()
        {
            PoundTapResult result = _pound.Tap(Time.realtimeSinceStartup);
            if (result == PoundTapResult.Ignored)
            {
                return;
            }

            _poundDough.localScale = new Vector3(2f - DoughSquash, DoughSquash, 1f);
            UiBindUtil.SetText(_poundCount, _pound.Taps + "/" + _pound.TotalTaps);
            if (result == PoundTapResult.Counted)
            {
                return;
            }

            List<string> ids = new List<string>(_pound.IngredientIds);
            _selected.Clear();
            UiBindUtil.SetActive(_poundPanel, false);
            MakeResult made = _stall.Make(ids);
            Refresh();
            if (made.Succeeded)
            {
                PlayMade(made);
            }
        }

        // 발견 연출 : 새 레시피면 흰 번쩍(알파 1→0) + 「NEW 팥콩떡!」 + 값, 평범한 떡이면 이름만.
        private void PlayMade(MakeResult made)
        {
            string name = PlainName;
            if (made.IsPlain == false)
            {
                name = _data.Recipes.Get(made.RecipeId).name;
            }

            UiBindUtil.SetActive(_madeNew, made.IsNewRecipe);
            if (made.IsNewRecipe)
            {
                name = "NEW " + name + "!";
            }

            UiBindUtil.SetText(_madeName, name);
            UiBindUtil.SetText(_madePrice, made.Price + "G");

            _flashing = made.IsNewRecipe;
            _flashTime = 0f;
            float alpha = 0f;
            if (_flashing)
            {
                alpha = 1f;
            }

            _madeFlash.color = new Color(1f, 1f, 1f, alpha);
            UiBindUtil.SetActive(_madePanel, true);
        }

        private void CloseMade()
        {
            if (_flashing)
            {
                return;
            }

            UiBindUtil.SetActive(_madePanel, false);
        }

        private void OnOpenOrNextDay()
        {
            if (_clock.IsNight)
            {
                _clock.AdvanceDay();
            }
            else
            {
                _stall.Open();
            }

            Refresh();
        }

        private void OnBuyClerk()
        {
            _stall.BuyClerk();
            Refresh();
        }
    }
}
