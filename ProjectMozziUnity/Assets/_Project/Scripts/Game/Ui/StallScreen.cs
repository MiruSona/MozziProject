using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using LitMotion;
using Mozzi.Game.Stall;
using Mozzi.Game.Vfx;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 가판대 화면 (가게 안쪽 정식 화면). 칠판에 진열 요약, 진열장에 떡 그림, 마루에 앞 손님 셋과 주문 말풍선.
    // 자리는 StallLayout 이 정본이고 프리팹은 Editor 의 StallScreenSetup 이 짓는다. 칠판을 누르면 차림표 판에서 찾은 떡을 바로 만들고,
    // 새 조합 실험(떡메)은 주방 화면(KitchenScreen) 몫이다. 손님은 왼쪽에서 걸어 들어와 한 줄로 서고, 앞 손님이 빠지면 당겨진다.
    public sealed class StallScreen : OverlayScreen
    {
        private const string PlainName = "평범한 떡";

        // 칠판에 떡 종류를 몇 줄까지 쓰나. 넘치면 마지막 줄을 「외 N종」 으로 줄인다.
        private const int BoardItemLineMax = 4;

        // 주문 말풍선 색. 힌트 손님(못 찾은 떡을 찾는 손님)은 하늘색이다.
        private static readonly Color BubbleCustomer = Color.white;
        private static readonly Color BubbleHint = new Color(0.72f, 0.89f, 0.97f, 1f);

        private readonly List<Button> _customerButtons = new List<Button>();
        private readonly List<CustomerCellView> _customers = new List<CustomerCellView>();
        private readonly List<OrderBubbleView> _bubbles = new List<OrderBubbleView>();
        private readonly List<CustomerWalkView> _walkers = new List<CustomerWalkView>();
        private readonly List<ShelfCellView> _shelfCells = new List<ShelfCellView>();
        // 걷기 연출 모션 묶음. 화면이 닫히면 한 번에 끊는다.
        private readonly CompositeMotionHandle _motions = new CompositeMotionHandle();
        // RefreshQueue 가 칸을 바꾸기 전에 옛 줄(누가 · 어디)을 베껴 두는 자리.
        private readonly List<StallCustomer> _lastWho = new List<StallCustomer>();
        private readonly List<float> _lastX = new List<float>();

        private IResourceHub _resource;
        private IStallHub _stall;
        private IClockHub _clock;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private TMP_Text _status;
        private TMP_Text _boardBody;
        private TMP_Text _queueMore;
        private Button _openButton;
        private Button _clerkButton;
        private RecipeMenuView _menu;
        private IMotionScheduler _scheduler;
        // 화면을 열고 첫 줄 그리기인가. 그때는 걸어 들어오지 않고 제 자리에 세운다.
        private bool _queueFresh = true;

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
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _resource = GlobalHub.Get<IResourceHub>();
            // 화면 연출이라 실제 시간으로 걷는다 — 디버그 배속을 올려도 걷는 빠르기는 같다.
            _scheduler = InGameHub.Get<IVfxHub>().Ui;

            UiBindUtil.SetText(UiBindUtil.Find<TMP_Text>(transform, UiNames.StallMenuTitleText, PrefabName), UiText.StallMenuTitle);
            _boardBody = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallMenuBodyText, PrefabName);
            _status = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallStatusText, PrefabName);
            _queueMore = UiBindUtil.Find<TMP_Text>(transform, UiNames.StallQueueMoreText, PrefabName);

            // 칠판을 누르면 가판대 위에 차림표 판이 뜨고, 줄을 누르면 그 자리에서 떡을 만든다.
            _menu = new RecipeMenuView(transform, PrefabName, _resource, _data, _codex, _stall, OnMenuPick);
            UiBindUtil.BindButton(transform, UiNames.StallMenuBoard, PrefabName, _menu.Open);

            for (int i = 0; i < UiNames.StallShelfCellCount; i++)
            {
                Transform cell = UiBindUtil.FindObject(transform, UiNames.StallShelfCellPrefix + i, PrefabName).transform;
                _shelfCells.Add(new ShelfCellView(cell, PrefabName));
            }

            // 손님 · 말풍선은 같은 이름 자식이 셋씩이라 각 뿌리 밑에서 찾는다.
            for (int i = 0; i < StallLayout.QueueShown; i++)
            {
                int index = i;
                Button customer = UiBindUtil.BindButton(transform, UiNames.StallCustomerPrefix + i, PrefabName,
                    delegate { OnServe(index); });
                _customerButtons.Add(customer);
                _customers.Add(new CustomerCellView(customer.transform));

                Transform bubble = UiBindUtil.FindObject(transform, UiNames.StallBubblePrefix + i, PrefabName).transform;
                OrderBubbleView bubbleView = new OrderBubbleView(bubble, PrefabName);
                _bubbles.Add(bubbleView);
                _walkers.Add(new CustomerWalkView(customer.transform as RectTransform, bubbleView, StallLayout.QueueFootX[i]));
                _lastWho.Add(null);
                _lastX.Add(0f);
            }

            _openButton = UiBindUtil.BindButton(transform, UiNames.StallOpenButton, PrefabName, OnOpenOrNextDay);
            _clerkButton = UiBindUtil.BindButton(transform, UiNames.StallClerkButton, PrefabName, OnBuyClerk);
            // 진열장을 누르면 「진열 비우기」 팝업이 뜬다 (찹쌀이 팝업과 같은 모양).
            UiBindUtil.BindButton(transform, UiNames.StallShelfTapArea, PrefabName, OnShelfTap);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);
            UiBindUtil.BindButton(transform, UiNames.StallKitchenButton, PrefabName, _ui.OpenKitchen);
        }

        protected override void UnbindChildren()
        {
            _motions.Cancel();
            _customerButtons.Clear();
            _customers.Clear();
            _bubbles.Clear();
            _walkers.Clear();
            _lastWho.Clear();
            _lastX.Clear();
            _shelfCells.Clear();
            _menu = null;
            _scheduler = null;
            _queueFresh = true;

            _resource = null;
            _stall = null;
            _clock = null;
            _codex = null;
            _wallet = null;
            _data = null;
            _ui = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshStatus();
            RefreshShelf();
            RefreshQueue();
            RefreshButtons();
            if (_menu.IsOpen)
            {
                _menu.Refresh();
            }
        }

        // 「닫기」 를 누르면 차림표 판부터 닫는다.
        public override bool CloseInner()
        {
            if (_menu.IsOpen == false)
            {
                return false;
            }

            _menu.Close();
            return true;
        }

        // 걷던 손님은 모두 멈추고, 다음에 열 때는 제 자리에 바로 서게 한다.
        public override void OnClosed()
        {
            _motions.Cancel();
            for (int i = 0; i < _walkers.Count; i++)
            {
                _walkers[i].Clear();
            }

            _queueFresh = true;
            _menu.Close();
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

            UiBindUtil.SetText(_status, "손님 " + _stall.ServedToday + "/" + _stall.CustomersPerDay + " · " + state);
        }

        // 종류별 수를 표 순서대로, 평범한 떡은 맨 뒤에 묶는다. 진열장 칸에는 앞에서부터 그림으로, 칠판에는 글자로.
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
            RefreshShelfCells(rows, counts, plain);
            RefreshBoard(rows, counts, plain, shelf.Count);
        }

        // 칠판 녹색 면 : 맨 윗줄 「진열 n/상한」, 그 아래 종류별 「팥떡 ×3」. 비었으면 만들러 가라는 안내.
        private void RefreshBoard(IReadOnlyList<RecipeRow> rows, Dictionary<string, int> counts, int plain, int total)
        {
            List<string> items = new List<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                int count;
                if (counts.TryGetValue(rows[i].Id, out count))
                {
                    items.Add(rows[i].Name + " ×" + count);
                }
            }

            if (plain > 0)
            {
                items.Add(PlainName + " ×" + plain);
            }

            List<string> lines = new List<string>();
            lines.Add("진열 " + total + "/" + _stall.ShelfMax);
            if (items.Count == 0)
            {
                lines.Add("비었어요");
                lines.Add("눌러서 떡 만들기");
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (i == BoardItemLineMax - 1 && items.Count > BoardItemLineMax)
                {
                    lines.Add("외 " + (items.Count - i) + "종");
                    break;
                }

                lines.Add(items[i]);
            }

            UiBindUtil.SetText(_boardBody, string.Join("\n", lines));
        }

        // 레시피표 순서로 한 종류에 접시 하나, 평범한 떡은 맨 뒤. 접시 여섯을 넘는 종류는 칠판 글자로만 본다. 남은 접시는 비운다.
        private void RefreshShelfCells(IReadOnlyList<RecipeRow> rows, Dictionary<string, int> counts, int plain)
        {
            int cellIndex = 0;
            for (int i = 0; i < rows.Count && cellIndex < _shelfCells.Count; i++)
            {
                int count;
                if (counts.TryGetValue(rows[i].Id, out count))
                {
                    _shelfCells[cellIndex].Set(_resource.GetSprite(SpriteKeys.CakeOne(rows[i].SpriteKey)), count);
                    cellIndex++;
                }
            }

            if (plain > 0 && cellIndex < _shelfCells.Count)
            {
                _shelfCells[cellIndex].Set(_resource.GetSprite(SpriteKeys.CakeOne(SpriteKeys.CakePlain)), plain);
                cellIndex++;
            }

            for (int i = cellIndex; i < _shelfCells.Count; i++)
            {
                _shelfCells[i].Hide();
            }
        }

        // 앞 손님 셋만 세우고, 넷째부터는 「뒤에 N명」 글자로 센다.
        // 칸을 바꾸기 전에 옛 줄을 베껴 두고, 칸마다 그 손님이 전에 어디 있었나를 보고 걷게 한다.
        private void RefreshQueue()
        {
            for (int i = 0; i < _walkers.Count; i++)
            {
                _lastWho[i] = _walkers[i].Who;
                _lastX[i] = _walkers[i].X;
            }

            IReadOnlyList<StallCustomer> queue = _stall.Queue;
            for (int i = 0; i < _customers.Count; i++)
            {
                if (i >= queue.Count)
                {
                    _customerButtons[i].interactable = false;
                    _customers[i].Hide();
                    _bubbles[i].Hide();
                    _walkers[i].Clear();
                    continue;
                }

                _customerButtons[i].interactable = _stall.Shelf.Count > 0;
                SetCustomer(i, queue[i]);
                MoveCustomer(i, queue[i]);
            }

            _queueFresh = false;

            int more = queue.Count - _customers.Count;
            UiBindUtil.SetActive(_queueMore.gameObject, more > 0);
            if (more > 0)
            {
                UiBindUtil.SetText(_queueMore, "뒤에 " + more + "명");
            }
        }

        // 화면을 처음 열 때는 제 자리에 세운다. 같은 손님이 이미 이 칸에 있으면(걷는 중이어도) 그대로 두고 말풍선만 머리에 다시 붙인다.
        // 다른 칸에 있던 손님이면 그 자리에서 당겨 오고, 처음 보는 손님이면 화면 왼쪽 밖에서 걸어 들어온다.
        private void MoveCustomer(int index, StallCustomer customer)
        {
            CustomerWalkView walker = _walkers[index];
            if (_queueFresh)
            {
                walker.Place(customer);
                return;
            }

            if (_lastWho[index] == customer)
            {
                walker.SnapBubble();
                return;
            }

            int from = _lastWho.IndexOf(customer);
            float fromX = StallLayout.QueueEnterX;
            if (from >= 0)
            {
                fromX = _lastX[from];
            }

            walker.Walk(customer, fromX, _scheduler, _motions);
        }

        // 손님 겹을 세우고 주문은 머리 위 말풍선에. 찾은 떡은 그림만, 아무거나 · 못 찾은 떡은 글자로.
        private void SetCustomer(int index, StallCustomer customer)
        {
            _customers[index].Apply(_resource, CustomerLook.FromSeed(customer.lookSeed), FaceOf(customer));

            Color color = BubbleCustomer;
            if (customer.isHint)
            {
                color = BubbleHint;
            }

            // 말풍선은 제 칸 자리에 먼저 그리고, 걷는 중이면 MoveCustomer 가 지금 자리로 옮긴다.
            float headX = StallLayout.QueueFootX[index] - StallLayout.HeadCenterOffset;
            float headTop = StallLayout.QueueFootY - StallLayout.HeadHeight;

            Sprite sprite = null;
            if (customer.HasOrder && _codex.IsRecipeOpen(customer.orderRecipeId))
            {
                sprite = _resource.GetSprite(_data.Recipes.Get(customer.orderRecipeId).SpriteKey);
            }

            if (sprite != null)
            {
                _bubbles[index].ShowIcon(sprite, color, headX, headTop);
                return;
            }

            _bubbles[index].ShowText(OrderLine(customer), color, headX, headTop);
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

        // 떡 그림이 없는 주문을 글자 한 줄로. 못 찾은 떡은 「? 팥+콩」처럼 재료 이름만 보인다.
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

        // 차림표 줄 : 그 자리에서 떡을 만들고 진열장 · 칠판 · 손님 표정을 같이 고친다. 판은 열린 채로 둔다.
        private void OnMenuPick(string recipeId)
        {
            _stall.Make(_data.Recipes.Get(recipeId).IngredientIds);
            Refresh();
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

        // 진열장 팝업. 팝업 자체가 확인이라 버튼 한 번에 판다. 비었으면 버튼을 끈다. 다시 그리기는 UiManager 가 한다.
        private void OnShelfTap()
        {
            int count = _stall.Shelf.Count;
            if (count == 0)
            {
                _ui.OpenActionPopup("진열대", "진열된 떡이 없어요", "비어 있음", false, null);
                return;
            }

            int percent = Mathf.RoundToInt(_data.Stalls.Get(DataKeys.DefaultRowId).ClearRate * 100f);
            string body = "떡 " + count + "개 · 정가 " + percent + "%";
            _ui.OpenActionPopup("진열대", body, "진열 비우기 +" + _stall.ClearShelfPrice() + "G", true, OnClearShelf);
        }

        private void OnClearShelf()
        {
            _stall.ClearShelf();
        }

        private void OnBuyClerk()
        {
            _stall.BuyClerk();
            Refresh();
        }
    }
}
