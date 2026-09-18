using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Stall
{
    // 가판대. 진열대 · 제작(레시피 판정 · 떡 값) · 열림/닫힘 · 손님 도착 · 줄 · 주문 · 판매 · 점원 · 오프라인 판매.
    // 값은 전부 Stall 표(id default)에서 읽는다. 하루 손님 N 을 다 팔면 닫고 밤을 청한다 (가판대 문서 6절).
    public sealed class StallManager : IManager, IProcess, ISavable, IStallHub
    {
        private const string Key = "Stall";
        private const string RowId = DataKeys.DefaultRowId;

        // 오프라인은 「다음 사건(도착 · 점원)까지 남은 초」 걸음으로 실시간과 같은 함수를 돌린다 (가판대 문서 8절).
        // 사건 사이는 시계가 직선으로 줄어드니 결과는 1초 걸음과 같다. 상한 60초 · 하한 0.001초.
        private const double OfflineMaxStepSec = 60;
        private const double OfflineMinStepSec = 0.001;

        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private readonly List<string> _makeIds = new List<string>();
        private readonly List<RecipeRow> _candidates = new List<RecipeRow>();

        private StallState _state = new StallState();
        private StallRow _row;
        private StallOfflineReport _offlineReport;

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IClockHub _clock;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private ICodexHub _codex;

        public string SaveKey
        {
            get { return Key; }
        }

        public bool IsOpen
        {
            get { return _state.isOpen; }
        }

        public bool HasClerk
        {
            get { return _state.hasClerk; }
        }

        public int ServedToday
        {
            get { return _state.servedToday; }
        }

        public int CustomersPerDay
        {
            get { return _row.customersPerDay; }
        }

        public int QueueMax
        {
            get { return _row.queueMax; }
        }

        public int ShelfMax
        {
            get { return _row.shelfMax; }
        }

        public int ClerkPrice
        {
            get { return _row.clerkPrice; }
        }

        public IReadOnlyList<ShelfItem> Shelf
        {
            get { return _state.shelf; }
        }

        public IReadOnlyList<StallCustomer> Queue
        {
            get { return _state.queue; }
        }

        public StallOfflineReport OfflineReport
        {
            get { return _offlineReport; }
        }

        public void Init()
        {
        }

        // 복원 → 표에 없는 것 정리 → 오프라인 판매 → 구독 순서다.
        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _clock = InGameHub.Get<IClockHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _codex = InGameHub.Get<ICodexHub>();

            _row = _data.Stalls.Get(RowId);
            _save.Register(this);
            Normalize();
            _offlineReport = ApplyOffline(_clock.OfflineSeconds);

            _subscriptions.Add(EventBus.Subscribe<DayStartedEvent>(this, OnDayStarted));

            Debug.Log("[Stall] 열림 " + _state.isOpen + " · 점원 " + _state.hasClerk + " · 진열 " + _state.shelf.Count
                + "/" + _row.shelfMax + " · 줄 " + _state.queue.Count + " · 오늘 " + _state.servedToday + "/" + _row.customersPerDay
                + " · " + _offlineReport.Describe());
        }

        public void Process(float deltaTime)
        {
            if (_state.isOpen == false || _clock.IsNight)
            {
                return;
            }

            StallCustomer customer = StepArrival(deltaTime);
            if (customer != null)
            {
                EventBus.Publish(new CustomerArrivedEvent(_state.queue.Count - 1, customer.orderRecipeId, customer.isHint));
            }

            if (StepClerk(deltaTime))
            {
                Serve(0);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        public bool Open()
        {
            if (_state.isOpen || _clock.IsNight)
            {
                return false;
            }

            _state.isOpen = true;
            _state.nextCustomerSec = _row.customerGapSec;
            _state.clerkSec = _row.clerkSpeedSec;
            EventBus.Publish(new StallOpenedEvent(_clock.Day));
            return true;
        }

        public bool CanMake(IReadOnlyList<string> ingredientIds)
        {
            if (IsValidCombo(ingredientIds) == false || _state.shelf.Count >= _row.shelfMax)
            {
                return false;
            }

            for (int i = 0; i < ingredientIds.Count; i++)
            {
                if (_inventory.Has(ingredientIds[i], 1) == false)
                {
                    return false;
                }
            }

            return true;
        }

        // 재료를 빼고 → 진열 → 도감 → 이벤트. 재료 목록은 복사해 두고 쓴다 (호출 쪽 목록이 이벤트 중 바뀌어도 안전).
        public MakeResult Make(IReadOnlyList<string> ingredientIds)
        {
            if (CanMake(ingredientIds) == false)
            {
                return MakeResult.Failed;
            }

            _makeIds.Clear();
            _makeIds.AddRange(ingredientIds);

            RecipeRow recipe = RecipeMatchUtil.FindRecipe(_data.Recipes, _makeIds);
            int price = PriceOf(_makeIds, recipe);
            string recipeId = "";
            if (recipe != null)
            {
                recipeId = recipe.id;
            }

            _state.shelf.Add(new ShelfItem(recipeId, price));
            _codex.MarkTriedCombo(RecipeMatchUtil.ComboKey(_makeIds));

            for (int i = 0; i < _makeIds.Count; i++)
            {
                _inventory.Take(_makeIds[i], 1);
            }

            bool isNew = false;
            if (recipe != null)
            {
                isNew = _codex.DiscoverRecipe(recipe.id);
            }

            EventBus.Publish(new RiceCakeMadeEvent(recipeId, price, isNew));
            return new MakeResult(true, recipeId, price, isNew);
        }

        public int PreviewPrice(IReadOnlyList<string> ingredientIds)
        {
            if (IsValidCombo(ingredientIds) == false)
            {
                return 0;
            }

            return PriceOf(ingredientIds, RecipeMatchUtil.FindRecipe(_data.Recipes, ingredientIds));
        }

        // 상태를 다 바꾼 뒤에 골드 · 이벤트를 낸다 — 이벤트 안에서 화면이 Refresh 로 되읽기 때문이다.
        public bool Serve(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= _state.queue.Count || _state.shelf.Count == 0)
            {
                return false;
            }

            ShelfItem item;
            bool wasOrder;
            int gold = Sell(queueIndex, out item, out wasOrder);
            bool closes = CloseIfDayDone();

            _wallet.Earn(gold);
            EventBus.Publish(new CustomerServedEvent(gold, item.recipeId, wasOrder, _state.servedToday));

            if (closes)
            {
                _clock.FallNight();
            }

            return true;
        }

        public bool BuyClerk()
        {
            if (_state.hasClerk || _wallet.Pay(_row.clerkPrice) == false)
            {
                return false;
            }

            _state.hasClerk = true;
            _state.clerkSec = _row.clerkSpeedSec;
            return true;
        }

        public void Skip(double seconds)
        {
            _offlineReport = ApplyOffline(seconds);
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(_state);
        }

        public void RestoreJson(string json)
        {
            StallState loaded = JsonUtility.FromJson<StallState>(json);
            if (loaded == null || loaded.shelf == null || loaded.queue == null)
            {
                return;
            }

            _state = loaded;
        }

        // 새 날 : 손님 수 · 줄을 비우고 닫힌 상태로. 핸들러라 이벤트는 안 낸다.
        private void OnDayStarted(DayStartedEvent evt)
        {
            _state.isOpen = false;
            _state.arrivedToday = 0;
            _state.servedToday = 0;
            _state.queue.Clear();
        }

        // 표에 없는 레시피가 든 진열 · 주문은 버린다 (표가 바뀐 옛 세이브). 진열 상한도 다시 맞춘다.
        private void Normalize()
        {
            _state.shelf.RemoveAll(IsUnknownShelfItem);
            while (_state.shelf.Count > _row.shelfMax)
            {
                _state.shelf.RemoveAt(_state.shelf.Count - 1);
            }

            _state.queue.RemoveAll(IsUnknownCustomer);
            while (_state.queue.Count > _row.queueMax)
            {
                _state.queue.RemoveAt(_state.queue.Count - 1);
            }

            if (_state.servedToday >= _row.customersPerDay)
            {
                _state.isOpen = false;
            }
        }

        private bool IsUnknownShelfItem(ShelfItem item)
        {
            RecipeRow row;
            return item.IsPlain == false && _data.Recipes.TryGet(item.recipeId, out row) == false;
        }

        private bool IsUnknownCustomer(StallCustomer customer)
        {
            RecipeRow row;
            return customer.HasOrder && _data.Recipes.TryGet(customer.orderRecipeId, out row) == false;
        }

        // 도착 시계 한 걸음. 이번 걸음에 온 손님을 주고, 안 왔으면 null. 실시간·오프라인이 같이 쓴다.
        private StallCustomer StepArrival(double deltaTime)
        {
            _state.nextCustomerSec -= deltaTime;
            if (_state.nextCustomerSec > 0)
            {
                return null;
            }

            if (CanArrive() == false)
            {
                _state.nextCustomerSec = 0;
                return null;
            }

            _state.nextCustomerSec += _row.customerGapSec;
            return Arrive();
        }

        // 점원 시계 한 걸음. 떡이 있고 줄이 있을 때만 돌고, 이번 걸음에 팔 차례면 true.
        private bool StepClerk(double deltaTime)
        {
            if (_state.hasClerk == false || _state.queue.Count == 0 || _state.shelf.Count == 0)
            {
                _state.clerkSec = _row.clerkSpeedSec;
                return false;
            }

            _state.clerkSec -= deltaTime;
            if (_state.clerkSec > 0)
            {
                return false;
            }

            _state.clerkSec = _row.clerkSpeedSec;
            return true;
        }

        // 줄의 그 손님에게 판 값. 진열·줄·오늘 수만 바꾸고 골드·이벤트는 안 낸다.
        private int Sell(int queueIndex, out ShelfItem item, out bool wasOrder)
        {
            StallCustomer customer = _state.queue[queueIndex];
            int itemIndex = FindShelfIndex(customer.orderRecipeId);
            wasOrder = itemIndex >= 0;
            if (wasOrder == false)
            {
                itemIndex = FindPriciestIndex();
            }

            item = _state.shelf[itemIndex];
            int gold = item.price;
            if (wasOrder)
            {
                gold = PriceUtil.OrderPrice(item.price, _row);
            }

            _state.shelf.RemoveAt(itemIndex);
            _state.queue.RemoveAt(queueIndex);
            _state.servedToday++;
            return gold;
        }

        // 오늘 N 명을 다 팔았으면 닫고 줄을 비운다. 닫혔으면 true.
        private bool CloseIfDayDone()
        {
            if (IsDayDone() == false)
            {
                return false;
            }

            _state.isOpen = false;
            _state.queue.Clear();
            return true;
        }

        private bool CanArrive()
        {
            return _state.queue.Count < _row.queueMax && _state.arrivedToday < _row.customersPerDay;
        }

        // 오늘 n번째 손님이 hintEvery 의 배수면 힌트 손님 후보. 힌트감이 없으면 보통 주문이다.
        private StallCustomer Arrive()
        {
            _state.arrivedToday++;
            bool hintTurn = _row.hintEvery > 0 && _state.arrivedToday % _row.hintEvery == 0;

            StallCustomer customer = null;
            if (hintTurn)
            {
                customer = RollHintOrder();
            }

            if (customer == null)
            {
                customer = RollNormalOrder();
            }

            _state.queue.Add(customer);
            return customer;
        }

        // 못 찾은 레시피 가운데 재료를 지금 창고에 다 갖고 있는 것 하나.
        private StallCustomer RollHintOrder()
        {
            _candidates.Clear();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (_codex.IsRecipeOpen(rows[i].id) == false && HasAllIngredients(rows[i]))
                {
                    _candidates.Add(rows[i]);
                }
            }

            if (_candidates.Count == 0)
            {
                return null;
            }

            return new StallCustomer(_candidates[Random.Range(0, _candidates.Count)].id, true);
        }

        // 찾은 떡 중 무작위. 하나도 없으면 아무거나.
        private StallCustomer RollNormalOrder()
        {
            _candidates.Clear();
            IReadOnlyList<RecipeRow> rows = _data.Recipes.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (_codex.IsRecipeOpen(rows[i].id))
                {
                    _candidates.Add(rows[i]);
                }
            }

            if (_candidates.Count == 0)
            {
                return new StallCustomer("", false);
            }

            return new StallCustomer(_candidates[Random.Range(0, _candidates.Count)].id, false);
        }

        private bool HasAllIngredients(RecipeRow recipe)
        {
            for (int i = 0; i < recipe.ingredientIds.Count; i++)
            {
                if (_inventory.Has(recipe.ingredientIds[i], 1) == false)
                {
                    return false;
                }
            }

            return true;
        }

        // 1~3개 · 겹침 없음 · 표에 있는 재료.
        private bool IsValidCombo(IReadOnlyList<string> ingredientIds)
        {
            if (ingredientIds == null || ingredientIds.Count < 1 || ingredientIds.Count > 3)
            {
                return false;
            }

            for (int i = 0; i < ingredientIds.Count; i++)
            {
                IngredientRow row;
                if (_data.Ingredients.TryGet(ingredientIds[i], out row) == false)
                {
                    return false;
                }

                for (int j = i + 1; j < ingredientIds.Count; j++)
                {
                    if (ingredientIds[i] == ingredientIds[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private int PriceOf(IReadOnlyList<string> ingredientIds, RecipeRow recipe)
        {
            int sum = 0;
            for (int i = 0; i < ingredientIds.Count; i++)
            {
                sum += _data.Ingredients.Get(ingredientIds[i]).value;
            }

            return PriceUtil.CakePrice(sum, ingredientIds.Count, recipe != null, _row);
        }

        // 주문 떡이 진열대에 있으면 그 자리. 주문이 없거나 떡이 없으면 -1.
        private int FindShelfIndex(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                return -1;
            }

            for (int i = 0; i < _state.shelf.Count; i++)
            {
                if (_state.shelf[i].recipeId == recipeId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindPriciestIndex()
        {
            int best = 0;
            for (int i = 1; i < _state.shelf.Count; i++)
            {
                if (_state.shelf[i].price > _state.shelf[best].price)
                {
                    best = i;
                }
            }

            return best;
        }

        // 열려 있으면 다음 사건까지 걸음으로 Process 와 같은 도착·점원 함수를 돌린다 (손님은 점원 없이도 줄까지 찬다).
        // 이벤트 없이 상태만 바꾸고 골드는 한 번에 넣는다. N 명이면 닫고 밤.
        private StallOfflineReport ApplyOffline(double seconds)
        {
            if (seconds <= 0 || _state.isOpen == false || _clock.IsNight)
            {
                return new StallOfflineReport(seconds, 0, 0, false);
            }

            int served = 0;
            int gold = 0;
            double remaining = seconds;

            while (remaining > 0 && IsDayDone() == false)
            {
                double step = System.Math.Min(NextEventSec(), remaining);
                remaining -= step;

                StepArrival(step);
                if (StepClerk(step) == false)
                {
                    continue;
                }

                ShelfItem item;
                bool wasOrder;
                gold += Sell(0, out item, out wasOrder);
                served++;
            }

            bool closes = CloseIfDayDone();
            if (gold > 0)
            {
                _wallet.Earn(gold);
            }

            if (closes)
            {
                _clock.FallNight();
            }

            return new StallOfflineReport(seconds, served, gold, closes);
        }

        // 다음 도착 · 점원 판매까지 남은 초 중 최소. 줄이 못 받거나 점원이 안 돌면 그 시계는 안 본다.
        private double NextEventSec()
        {
            double next = OfflineMaxStepSec;
            if (CanArrive())
            {
                next = System.Math.Min(next, _state.nextCustomerSec);
            }

            bool clerkRuns = _state.hasClerk && _state.queue.Count > 0 && _state.shelf.Count > 0;
            if (clerkRuns)
            {
                next = System.Math.Min(next, _state.clerkSec);
            }

            return System.Math.Max(next, OfflineMinStepSec);
        }

        private bool IsDayDone()
        {
            return _state.servedToday >= _row.customersPerDay;
        }
    }
}
