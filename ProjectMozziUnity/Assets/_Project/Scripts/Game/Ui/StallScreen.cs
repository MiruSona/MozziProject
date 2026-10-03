using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Clock;
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
        private readonly List<Image> _makeRowIcons = new List<Image>();
        private readonly List<Image> _queueIcons = new List<Image>();
        private readonly List<CustomerCellView> _queueCustomers = new List<CustomerCellView>();
        private readonly List<ShelfCellView> _shelfCells = new List<ShelfCellView>();
        private readonly Dictionary<string, Button> _expToggles = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _expIcons = new Dictionary<string, Image>();
        private readonly List<string> _selected = new List<string>();

        private IResourceHub _resource;
        private IStallHub _stall;
        private IClockHub _clock;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private TMP_Text _status;
        private TMP_Text _shelf;
        private TMP_Text _expInfo;
        private Button _expMake;
        private Button _openButton;
        private Button _clerkButton;

        private GameObject _poundPanel;
        private TMP_Text _poundHint;
        private TMP_Text _poundCount;
        private RectTransform _poundDough;
        private PoundAgent _pound;

        private GameObject _madePanel;
        private Image _madeFlash;
        private Image _madeIcon;
        private GameObject _madeNew;
        private TMP_Text _madeName;
        private TMP_Text _madePrice;
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
            _resource = GlobalHub.Get<IResourceHub>();
            _pound = new PoundAgent(this, _data.Stalls.Get(StallRowId));

            _status = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallStatusText, PrefabName);
            _shelf = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallShelfText, PrefabName);
            BindShelfCells();

            for (int i = 0; i < _stall.QueueMax; i++)
            {
                int index = i;
                Button cell = UiBindUtil.BindButton(transform, UiNames.StallQueueCellPrefix + i, PrefabName,
                    delegate { OnServe(index); });
                _queueCells.Add(cell);
                _queueIcons.Add(IconOf(cell.transform));
                _queueCustomers.Add(new CustomerCellView(cell.transform));
            }

            for (int i = 0; i < UiNames.StallMakeRowCount; i++)
            {
                int index = i;
                Button row = UiBindUtil.BindButton(transform, UiNames.StallMakeRowPrefix + i, PrefabName,
                    delegate { OnMakeRow(index); });
                _makeRows.Add(row);
                _makeRowRecipes.Add(null);
                _makeRowIcons.Add(IconOf(row.transform));
            }

            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                Button toggle = UiBindUtil.BindButton(transform, UiNames.StallExpTogglePrefix + id, PrefabName,
                    delegate { OnToggle(id); });
                _expToggles[id] = toggle;
                SetToggleIcon(toggle, id, _resource.GetSprite(_data.Ingredients.Get(id).SpriteKey));
            }

            _expInfo = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallExpInfoText, PrefabName);
            _expMake = UiBindUtil.BindButton(transform, UiNames.StallExpMakeButton, PrefabName, OnExpMake);
            _openButton = UiBindUtil.BindButton(transform, UiNames.StallOpenButton, PrefabName, OnOpenOrNextDay);
            _clerkButton = UiBindUtil.BindButton(transform, UiNames.StallClerkButton, PrefabName, OnBuyClerk);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);

            _poundPanel = UiBindUtil.FindObject(transform, UiNames.PoundPanel, PrefabName);
            _poundHint = UiBindUtil.Find<TMP_Text>(_poundPanel.transform, UiNames.PoundHintText, PrefabName);
            _poundCount = UiBindUtil.Find<TMP_Text>(_poundPanel.transform, UiNames.PoundCountText, PrefabName);
            _poundDough = UiBindUtil.Find<RectTransform>(_poundPanel.transform, UiNames.PoundDoughBox, PrefabName);
            Button poundButton = _poundPanel.GetComponent<Button>();
            poundButton.onClick.RemoveAllListeners();
            poundButton.onClick.AddListener(OnPoundTap);

            _madePanel = UiBindUtil.FindObject(transform, UiNames.StallMadePanel, PrefabName);
            _madeFlash = UiBindUtil.Find<Image>(_madePanel.transform, UiNames.StallMadeFlash, PrefabName);
            _madeIcon = IconOf(_madePanel.transform);
            _madeNew = UiBindUtil.FindObject(_madePanel.transform, UiNames.StallMadeNewText, PrefabName);
            _madeName = UiBindUtil.Find<TMP_Text>(_madePanel.transform, UiNames.StallMadeNameText, PrefabName);
            _madePrice = UiBindUtil.Find<TMP_Text>(_madePanel.transform, UiNames.StallMadePriceText, PrefabName);
            Button madeButton = _madePanel.GetComponent<Button>();
            madeButton.onClick.RemoveAllListeners();
            madeButton.onClick.AddListener(CloseMade);

            UiBindUtil.SetActive(_poundPanel, false);
            UiBindUtil.SetActive(_madePanel, false);
        }

        // 진열대 묶음 칸 24개. 하나도 없는 옛 프리팹이면 비워 두고 ShelfText 글자판으로 돌아간다.
        private void BindShelfCells()
        {
            for (int i = 0; i < UiNames.StallShelfCellCount; i++)
            {
                RectTransform cell;
                if (UiBindUtil.TryFind<RectTransform>(transform, UiNames.StallShelfCellPrefix + i, out cell) == false)
                {
                    break;
                }

                ShelfCellView view = new ShelfCellView(cell);
                if (view.HasParts == false)
                {
                    break;
                }

                _shelfCells.Add(view);
            }

            UiBindUtil.SetActive(_shelf.gameObject, _shelfCells.Count == 0);
        }

        // 칸 안의 떡 그림 자식. 프리팹에 없으면 null 이고, 그때는 예전처럼 글자만 나온다.
        private static Image IconOf(Transform root)
        {
            Image icon;
            UiBindUtil.TryFind<Image>(root, UiNames.CakeIcon, out icon);
            return icon;
        }

        // 그림이 없으면 아이콘을 끄고 글씨를 가운데로 되돌린다. 토글이 54px 이라 이름과 그림 둘 다는 못 넣는다.
        private void SetToggleIcon(Button toggle, string ingredientId, Sprite sprite)
        {
            Image icon;
            if (UiBindUtil.TryFind<Image>(toggle.transform, UiNames.IngredientIcon, out icon) == false)
            {
                return;
            }

            _expIcons[ingredientId] = icon;
            icon.sprite = sprite;
            UiBindUtil.SetActive(icon.gameObject, sprite != null);

            TMP_Text label = UiBindUtil.LabelOf(toggle);
            if (label == null)
            {
                return;
            }

            float indent = 0f;
            if (sprite != null)
            {
                indent = LayoutConstUi.ToggleTextIndent;
            }

            label.margin = new Vector4(indent, 0f, 0f, 0f);
        }

        private bool HasToggleIcon(string ingredientId)
        {
            Image icon;
            if (_expIcons.TryGetValue(ingredientId, out icon) == false)
            {
                return false;
            }

            return icon.gameObject.activeSelf;
        }

        protected override void UnbindChildren()
        {
            _queueCells.Clear();
            _queueIcons.Clear();
            _queueCustomers.Clear();
            _shelfCells.Clear();
            _expIcons.Clear();
            _makeRows.Clear();
            _makeRowRecipes.Clear();
            _makeRowIcons.Clear();
            _expToggles.Clear();
            _selected.Clear();
            _flashing = false;
            _pound = null;
            _madeIcon = null;

            _resource = null;
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

            UiBindUtil.SetText(_status, "오늘 손님 " + _stall.ServedToday + "/" + _stall.CustomersPerDay + " · " + state
                + " · 진열 " + _stall.Shelf.Count + "/" + _stall.ShelfMax);
        }

        // 묶음 칸 하나에 드는 떡 수 = ceil(상한 ÷ 6). 올림이라야 한 종류가 상한까지 차도 6칸 안에 든다.
        private static int ShelfUnit(int shelfMax)
        {
            return (shelfMax + 5) / 6;
        }

        // 종류별 수를 표 순서대로, 평범한 떡은 맨 뒤에 묶는다. 칸이 있으면 묶음 칸으로, 없으면 옛 글자판으로.
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

            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            if (_shelfCells.Count > 0)
            {
                RefreshShelfCells(rows, counts, plain);
                return;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                int count;
                if (counts.TryGetValue(rows[i].Id, out count))
                {
                    parts.Add(rows[i].Name + "×" + count);
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

        // 레시피표 순서로 묶음 칸을 펼치고 평범한 떡은 맨 뒤. 남은 칸은 끈다.
        private void RefreshShelfCells(IReadOnlyList<RecipeRow> rows, Dictionary<string, int> counts, int plain)
        {
            int unit = ShelfUnit(_stall.ShelfMax);
            int cellIndex = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                int count;
                if (counts.TryGetValue(rows[i].Id, out count) == false)
                {
                    continue;
                }

                cellIndex = FillCells(cellIndex, _resource.GetSprite(rows[i].SpriteKey), count, unit);
            }

            if (plain > 0)
            {
                cellIndex = FillCells(cellIndex, _resource.GetSprite(SpriteKeys.CakePlain), plain, unit);
            }

            for (int i = cellIndex; i < _shelfCells.Count; i++)
            {
                _shelfCells[i].Hide();
            }
        }

        // 한 종류 count 개를 unit 씩 칸에 나눠 담고 다음 칸 번호를 돌려준다. 칸이 모자라면 경고만 남기고 멈춘다.
        private int FillCells(int cellIndex, Sprite sprite, int count, int unit)
        {
            int left = count;
            while (left > 0)
            {
                if (cellIndex >= _shelfCells.Count)
                {
                    Debug.LogWarning("[StallScreen] 진열대 칸 " + _shelfCells.Count + " 을 넘겼다 — 남은 떡 " + left);
                    return cellIndex;
                }

                int inCell = Mathf.Min(left, unit);
                _shelfCells[cellIndex].Set(sprite, inCell);
                left -= inCell;
                cellIndex++;
            }

            return cellIndex;
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
                    _queueCustomers[i].Hide();
                    SetQueueIcon(i, null);
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
                SetQueueCell(i, customer);
            }
        }

        // 손님 그림을 겹으로 깔고 주문 떡은 오른쪽 위 구석에. 겹 자리가 없는 프리팹은 예전 글자판 그대로다.
        private void SetQueueCell(int index, StallCustomer customer)
        {
            Sprite sprite = null;
            if (customer.HasOrder && _codex.IsRecipeOpen(customer.orderRecipeId))
            {
                sprite = _resource.GetSprite(_data.Recipes.Get(customer.orderRecipeId).SpriteKey);
            }

            CustomerCellView view = _queueCustomers[index];
            view.Apply(_resource, CustomerLook.FromSeed(customer.lookSeed), FaceOf(customer));
            SetQueueIcon(index, sprite);

            if (view.IsShowing)
            {
                UiBindUtil.SetLabel(_queueCells[index], OrderLine(customer));
                return;
            }

            if (sprite == null)
            {
                UiBindUtil.SetLabel(_queueCells[index], OrderLabel(customer));
                return;
            }

            UiBindUtil.SetLabel(_queueCells[index], _data.Recipes.Get(customer.orderRecipeId).Name);
        }

        // 주문 떡이 진열대에 있으면 웃고, 없으면 시무룩. 아무거나 사는 손님은 기본 얼굴이다.
        private CustomerFace FaceOf(StallCustomer customer)
        {
            if (customer.HasOrder == false)
            {
                return CustomerFace.Idle;
            }

            IReadOnlyList<ShelfItem> shelf = _stall.Shelf;
            for (int i = 0; i < shelf.Count; i++)
            {
                if (shelf[i].recipeId == customer.orderRecipeId)
                {
                    return CustomerFace.Happy;
                }
            }

            return CustomerFace.Sad;
        }

        // 손님 그림이 「손님」 글자를 대신하므로 주문만 한 줄로 쓴다.
        private string OrderLine(StallCustomer customer)
        {
            if (customer.HasOrder == false)
            {
                return "아무거나";
            }

            RecipeRow recipe = _data.Recipes.Get(customer.orderRecipeId);
            if (_codex.IsRecipeOpen(recipe.Id))
            {
                return recipe.Name;
            }

            List<string> names = new List<string>();
            for (int i = 0; i < recipe.IngredientIds.Length; i++)
            {
                names.Add(_data.Ingredients.Get(recipe.IngredientIds[i]).Name);
            }

            return "? " + string.Join("+", names);
        }

        // 그림이 켜지면 글자를 그 아래로 민다. 손님 겹이 있으면 떡 그림과 상관없이 늘 민다.
        private void SetQueueIcon(int index, Sprite sprite)
        {
            Image icon = _queueIcons[index];
            if (icon == null)
            {
                return;
            }

            icon.sprite = sprite;
            UiBindUtil.SetActive(icon.gameObject, sprite != null);

            TMP_Text label = UiBindUtil.LabelOf(_queueCells[index]);
            if (label == null)
            {
                return;
            }

            float top = 0f;
            if (sprite != null || _queueCustomers[index].IsShowing)
            {
                top = LayoutConstUi.QueueTextTopMargin;
            }

            label.margin = new Vector4(0f, top, 0f, 0f);
        }

        // 못 찾은 떡 주문은 「? 팥+콩」처럼 재료 이름만 보인다.
        private string OrderLabel(StallCustomer customer)
        {
            if (customer.HasOrder == false)
            {
                return "손님\n아무거나";
            }

            RecipeRow recipe = _data.Recipes.Get(customer.orderRecipeId);
            if (_codex.IsRecipeOpen(recipe.Id))
            {
                return "손님\n" + recipe.Name;
            }

            List<string> names = new List<string>();
            for (int i = 0; i < recipe.IngredientIds.Length; i++)
            {
                names.Add(_data.Ingredients.Get(recipe.IngredientIds[i]).Name);
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
                if (_codex.IsRecipeOpen(rows[i].Id))
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
                _makeRowRecipes[i] = recipe.Id;
                SetMakeRowIcon(i, _resource.GetSprite(recipe.SpriteKey));
                UiBindUtil.SetLabel(_makeRows[i], recipe.Name + " " + _stall.PreviewPrice(recipe.IngredientIds) + "G");
                _makeRows[i].interactable = _stall.CanMake(recipe.IngredientIds);
            }
        }

        // 재료 토글과 같은 셈법 — 그림을 켜면 라벨을 그만큼 오른쪽으로 민다.
        private void SetMakeRowIcon(int rowIndex, Sprite sprite)
        {
            Image icon = _makeRowIcons[rowIndex];
            if (icon == null)
            {
                return;
            }

            icon.sprite = sprite;
            UiBindUtil.SetActive(icon.gameObject, sprite != null);

            TMP_Text label = UiBindUtil.LabelOf(_makeRows[rowIndex]);
            if (label == null)
            {
                return;
            }

            float indent = 0f;
            if (sprite != null)
            {
                indent = LayoutConstUi.MakeRowTextIndent;
            }

            label.margin = new Vector4(indent, 0f, 0f, 0f);
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

                // 아이콘이 켜져 있으면 이름은 그림이 대신하므로 숫자만 쓴다.
                string head = _data.Ingredients.Get(id).Name + " ";
                if (HasToggleIcon(id))
                {
                    head = "";
                }

                UiBindUtil.SetLabel(pair.Value, head + count);
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

            _stall.Make(_data.Recipes.Get(recipeId).IngredientIds);
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
            string spriteKey = SpriteKeys.CakePlain;
            if (made.IsPlain == false)
            {
                RecipeRow recipe = _data.Recipes.Get(made.RecipeId);
                name = recipe.Name;
                spriteKey = recipe.SpriteKey;
            }

            SetMadeIcon(_resource.GetSprite(spriteKey));
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

        private void SetMadeIcon(Sprite sprite)
        {
            if (_madeIcon == null)
            {
                return;
            }

            _madeIcon.sprite = sprite;
            UiBindUtil.SetActive(_madeIcon.gameObject, sprite != null);
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
