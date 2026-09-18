using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Events;
using Mozzi.Game.Farm;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Storage
{
    // 농장에 못 넣은 찹쌀이가 머무는 곳. 재료를 안 뱉고 번식도 안 한다. 상한 없음.
    // 팔기 값은 Farm 표 storageSellPrice 다.
    public sealed class StorageManager : IManager, ISavable, IStorageHub
    {
        private const string Key = "Storage";
        private const string FarmRowId = DataKeys.DefaultRowId;

        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<string> _order = new List<string>();
        private readonly List<CountEntry> _entries = new List<CountEntry>();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IFarmHub _farm;
        private IWalletHub _wallet;
        private FarmRow _row;

        public string SaveKey
        {
            get { return Key; }
        }

        public IReadOnlyList<CountEntry> Entries
        {
            get { return _entries; }
        }

        public int Total
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _entries.Count; i++)
                {
                    total += _entries[i].count;
                }

                return total;
            }
        }

        public int SellPrice
        {
            get { return _row.storageSellPrice; }
        }

        public void Init()
        {
        }

        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _farm = InGameHub.Get<IFarmHub>();
            _wallet = InGameHub.Get<IWalletHub>();

            _row = _data.Farms.Get(FarmRowId);
            _save.Register(this);
            Debug.Log("[Storage] 마릿수 " + Total);
        }

        public void Dispose()
        {
        }

        public int Count(string speciesId)
        {
            int count;
            _counts.TryGetValue(speciesId, out count);
            return count;
        }

        public void Add(string speciesId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _data.Species.Get(speciesId);
            if (_counts.ContainsKey(speciesId) == false)
            {
                _counts[speciesId] = 0;
                _order.Add(speciesId);
            }

            _counts[speciesId] += amount;
            RebuildEntries();
            EventBus.Publish(new StorageChangedEvent(speciesId, _counts[speciesId], amount));
        }

        public bool Take(string speciesId)
        {
            if (Count(speciesId) <= 0)
            {
                return false;
            }

            _counts[speciesId]--;
            RebuildEntries();
            EventBus.Publish(new StorageChangedEvent(speciesId, _counts[speciesId], -1));
            return true;
        }

        // 농장에 먼저 놓아 보고, 됐을 때만 보관함에서 뺀다.
        public bool PlaceToFarm(string speciesId)
        {
            if (Count(speciesId) <= 0)
            {
                return false;
            }

            if (_farm.Place(speciesId) == false)
            {
                return false;
            }

            Take(speciesId);
            return true;
        }

        public bool TakeFromFarm(int mozziId)
        {
            string speciesId = FindFarmSpecies(mozziId);
            if (speciesId == null)
            {
                return false;
            }

            if (_farm.Remove(mozziId) == false)
            {
                return false;
            }

            Add(speciesId, 1);
            return true;
        }

        public bool Sell(string speciesId)
        {
            if (Take(speciesId) == false)
            {
                return false;
            }

            _wallet.Earn(_row.storageSellPrice);
            return true;
        }

        public int SellAll(string speciesId)
        {
            int count = Count(speciesId);
            if (count <= 0)
            {
                return 0;
            }

            _counts[speciesId] = 0;
            RebuildEntries();
            EventBus.Publish(new StorageChangedEvent(speciesId, 0, -count));
            _wallet.Earn(_row.storageSellPrice * count);
            return count;
        }

        public string CaptureJson()
        {
            StorageState state = new StorageState();
            for (int i = 0; i < _entries.Count; i++)
            {
                state.counts.Add(new CountEntry(_entries[i].id, _entries[i].count));
            }

            return JsonUtility.ToJson(state);
        }

        // 표에 없는 종은 버린다 (표가 바뀐 옛 세이브).
        public void RestoreJson(string json)
        {
            StorageState state = JsonUtility.FromJson<StorageState>(json);
            if (state == null || state.counts == null)
            {
                return;
            }

            for (int i = 0; i < state.counts.Count; i++)
            {
                CountEntry entry = state.counts[i];
                MozziSpeciesRow row;
                if (_data.Species.TryGet(entry.id, out row) == false || entry.count <= 0)
                {
                    continue;
                }

                if (_counts.ContainsKey(entry.id) == false)
                {
                    _order.Add(entry.id);
                }

                _counts[entry.id] = entry.count;
            }

            RebuildEntries();
        }

        private string FindFarmSpecies(int mozziId)
        {
            IReadOnlyList<FarmMozzi> mozzis = _farm.Mozzis;
            for (int i = 0; i < mozzis.Count; i++)
            {
                if (mozzis[i].Id == mozziId)
                {
                    return mozzis[i].SpeciesId;
                }
            }

            return null;
        }

        private void RebuildEntries()
        {
            _entries.Clear();
            for (int i = 0; i < _order.Count; i++)
            {
                int count = _counts[_order[i]];
                if (count > 0)
                {
                    _entries.Add(new CountEntry(_order[i], count));
                }
            }
        }
    }
}
