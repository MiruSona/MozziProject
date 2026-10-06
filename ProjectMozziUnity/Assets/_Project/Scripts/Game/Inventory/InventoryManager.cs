using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Inventory
{
    // 부재료 창고. Ingredient 표의 모든 id 를 0 으로 깔고, 상한(cap + 창고 넓히기)에서 자른다.
    // 창고 넓히기 단계는 창고 자신의 것이라 여기 둔다 — 농장이 창고를 보지, 창고가 농장을 보지 않는다.
    public sealed class InventoryManager : IManager, ISavable, IInventoryHub
    {
        private const string Key = "Inventory";

        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<string> _ids = new List<string>();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IWalletHub _wallet;
        private int _storageLevel;
        private int _maxStorageLevel;

        public string SaveKey
        {
            get { return Key; }
        }

        public IReadOnlyList<string> IngredientIds
        {
            get { return _ids; }
        }

        public int StorageLevel
        {
            get { return _storageLevel; }
        }

        public int MaxStorageLevel
        {
            get { return _maxStorageLevel; }
        }

        public int NextStoragePrice
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindStorage, _storageLevel + 1);
                if (row == null)
                {
                    return -1;
                }

                return row.Price;
            }
        }

        // storage 행의 rate 는 누적값이라 지금 단계 한 행만 더한다.
        public int StorageBonus
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindStorage, _storageLevel);
                if (row == null)
                {
                    return 0;
                }

                return (int)row.Rate;
            }
        }

        public void Init()
        {
        }

        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _maxStorageLevel = GrowthUtil.MaxLevel(_data.Growths, GrowthUtil.KindStorage);

            IReadOnlyList<IngredientRow> rows = _data.Ingredients.All;
            for (int i = 0; i < rows.Count; i++)
            {
                _ids.Add(rows[i].Id);
                _counts[rows[i].Id] = 0;
            }

            _save.Register(this);
            Debug.Log("[Inventory] 종 " + _ids.Count + " · " + Describe());
        }

        public void Dispose()
        {
        }

        // 모르는 id(빈 문자열 포함)는 0 이다.
        public int Count(string ingredientId)
        {
            int count;
            _counts.TryGetValue(ingredientId, out count);
            return count;
        }

        public int Cap(string ingredientId)
        {
            IngredientRow row;
            if (ingredientId == null || _data.Ingredients.TryGet(ingredientId, out row) == false)
            {
                return 0;
            }

            return row.Cap + StorageBonus;
        }

        public bool IsFull(string ingredientId)
        {
            return Count(ingredientId) >= Cap(ingredientId);
        }

        public int Add(string ingredientId, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            int room = Cap(ingredientId) - Count(ingredientId);
            int added = Math.Min(room, amount);
            if (added <= 0)
            {
                return 0;
            }

            _counts[ingredientId] += added;
            EventBus.Publish(new IngredientChangedEvent(ingredientId, _counts[ingredientId], added));
            return added;
        }

        public bool BuyStorage()
        {
            int price = NextStoragePrice;
            if (price < 0 || _wallet.Pay(price) == false)
            {
                return false;
            }

            _storageLevel++;
            EventBus.Publish(new StorageUpgradedEvent(_storageLevel));
            return true;
        }

        public bool Has(string ingredientId, int amount)
        {
            return Count(ingredientId) >= amount;
        }

        public bool Take(string ingredientId, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Has(ingredientId, amount) == false)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            _counts[ingredientId] -= amount;
            EventBus.Publish(new IngredientChangedEvent(ingredientId, _counts[ingredientId], -amount));
            return true;
        }

        // 수량을 다 뺀 뒤에 골드를 넣는다 — 골드 이벤트에서 화면이 되읽을 때 창고가 이미 비어 있게.
        public int SellAll()
        {
            int gold = SellAllPrice();
            for (int i = 0; i < _ids.Count; i++)
            {
                string id = _ids[i];
                int count = _counts[id];
                if (count <= 0)
                {
                    continue;
                }

                _counts[id] = 0;
                EventBus.Publish(new IngredientChangedEvent(id, 0, -count));
            }

            if (gold > 0)
            {
                _wallet.Earn(gold);
            }

            return gold;
        }

        public int SellAllPrice()
        {
            int gold = 0;
            for (int i = 0; i < _ids.Count; i++)
            {
                gold += _counts[_ids[i]] * _data.Ingredients.Get(_ids[i]).SellPrice;
            }

            return gold;
        }

        public string CaptureJson()
        {
            InventoryState state = new InventoryState();
            for (int i = 0; i < _ids.Count; i++)
            {
                state.counts.Add(new CountEntry(_ids[i], _counts[_ids[i]]));
            }

            state.storageLevel = _storageLevel;

            return JsonUtility.ToJson(state);
        }

        // 표에 없는 id 는 버린다 (표가 바뀐 옛 세이브).
        public void RestoreJson(string json)
        {
            InventoryState state = JsonUtility.FromJson<InventoryState>(json);
            if (state == null || state.counts == null)
            {
                return;
            }

            _storageLevel = Mathf.Clamp(state.storageLevel, 0, _maxStorageLevel);
            for (int i = 0; i < state.counts.Count; i++)
            {
                CountEntry entry = state.counts[i];
                if (_counts.ContainsKey(entry.id) == false)
                {
                    continue;
                }

                _counts[entry.id] = Math.Max(0, entry.count);
            }
        }

        private string Describe()
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < _ids.Count; i++)
            {
                parts.Add(_ids[i] + " " + _counts[_ids[i]]);
            }

            return string.Join(", ", parts);
        }
    }
}
