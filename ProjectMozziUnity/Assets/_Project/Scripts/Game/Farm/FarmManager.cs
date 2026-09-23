using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Inventory;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Farm
{
    // 농장의 찹쌀이 목록 · 생산 · 바닥 더미 · 줍기 · 번식 · 오프라인 진행.
    // 생산·번식 시계는 종마다 하나씩 돈다 (프로토 q3 produce/breed 방식).
    public sealed class FarmManager : IManager, IProcess, ISavable, IFarmHub
    {
        private const string Key = "Farm";
        private const string FarmRowId = DataKeys.DefaultRowId;

        // 오프라인은 「다음 번식까지 남은 초」 걸음으로 실시간과 같은 함수를 돌린다 (농장 문서 8절).
        // 사건 사이는 시계가 직선으로 쌓이므로 결과는 1초 걸음과 같고, 걸음 수는 며칠이어도 수천 회다.
        private const double OfflineMaxStepSec = 60;
        private const double OfflineMinStepSec = 0.001;

        private readonly List<FarmMozzi> _mozzis = new List<FarmMozzi>();
        private readonly List<string> _speciesOrder = new List<string>();
        private readonly Dictionary<string, int> _countBySpecies = new Dictionary<string, int>();
        private readonly Dictionary<string, double> _produceAcc = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _breedClock = new Dictionary<string, double>();
        private readonly Dictionary<string, int> _piles = new Dictionary<string, int>();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IClockHub _clock;
        private IInventoryHub _inventory;
        private IWalletHub _wallet;

        private FarmRow _row;
        private int _nextMozziId = 1;
        private int _fenceLevel;
        private int _maxFenceLevel;
        private bool _restored;
        private OfflineReport _offlineReport;

        public string SaveKey
        {
            get { return Key; }
        }

        // 담장 단계의 rate 는 「더해지는 마리 수」라 누적값이다 — 단계마다 더하지 않고 현재 단계 한 행만 더한다.
        public int FenceMax
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindFence, _fenceLevel);
                if (row == null)
                {
                    return _row.fenceMax;
                }

                return _row.fenceMax + (int)row.rate;
            }
        }

        public int FenceLevel
        {
            get { return _fenceLevel; }
        }

        public int MaxFenceLevel
        {
            get { return _maxFenceLevel; }
        }

        public int NextFencePrice
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindFence, _fenceLevel + 1);
                if (row == null)
                {
                    return -1;
                }

                return row.price;
            }
        }

        public int MozziCount
        {
            get { return _mozzis.Count; }
        }

        public IReadOnlyList<FarmMozzi> Mozzis
        {
            get { return _mozzis; }
        }

        public IReadOnlyDictionary<string, int> Piles
        {
            get { return _piles; }
        }

        public OfflineReport OfflineReport
        {
            get { return _offlineReport; }
        }

        public void Init()
        {
        }

        // 순서가 중요하다 : 복원 → (없으면) 시작 상태 → 오프라인 진행.
        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _clock = InGameHub.Get<IClockHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _wallet = InGameHub.Get<IWalletHub>();

            _row = _data.Farms.Get(FarmRowId);
            _maxFenceLevel = GrowthUtil.MaxLevel(_data.Growths, GrowthUtil.KindFence);
            _offlineReport = new OfflineReport(0, new List<CountEntry>(), new List<CountEntry>());

            _save.Register(this);
            if (_restored == false)
            {
                SeedStart();
            }

            ApplyOffline(_clock.OfflineSeconds);
            Debug.Log("[Farm] 마릿수 " + _mozzis.Count + "/" + FenceMax + " · " + _offlineReport.Describe());
        }

        // 한 걸음은 생산 → 번식 순서다. 오프라인(ApplyOffline)도 같은 두 함수를 쓴다.
        public void Process(float deltaTime)
        {
            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                MozziSpeciesRow species = _data.Species.Get(_speciesOrder[i]);
                int dropped = StepProduce(species, deltaTime);
                int pile = PileOf(species.ingredientId);
                for (int d = dropped - 1; d >= 0; d--)
                {
                    EventBus.Publish(new IngredientDroppedEvent(species.ingredientId, pile - d));
                }
            }

            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                FarmMozzi born = StepBreed(_speciesOrder[i], deltaTime);
                if (born != null)
                {
                    EventBus.Publish(new MozziBornEvent(born.Id, born.SpeciesId, _mozzis.Count));
                }
            }
        }

        public void Dispose()
        {
        }

        public int CountOf(string speciesId)
        {
            int count;
            _countBySpecies.TryGetValue(speciesId, out count);
            return count;
        }

        public int PileOf(string ingredientId)
        {
            int count;
            _piles.TryGetValue(ingredientId, out count);
            return count;
        }

        public int PickUp(string ingredientId)
        {
            int pile = PileOf(ingredientId);
            if (pile <= 0)
            {
                return 0;
            }

            int moved = _inventory.Add(ingredientId, pile);
            _piles[ingredientId] = pile - moved;
            if (moved > 0)
            {
                EventBus.Publish(new IngredientPickedEvent(ingredientId, moved, pile - moved));
            }

            return moved;
        }

        public bool Place(string speciesId)
        {
            if (_mozzis.Count >= FenceMax)
            {
                return false;
            }

            FarmMozzi mozzi = AddMozzi(speciesId);
            EventBus.Publish(new MozziPlacedEvent(mozzi.Id, speciesId, _mozzis.Count));
            return true;
        }

        public bool Remove(int mozziId)
        {
            for (int i = 0; i < _mozzis.Count; i++)
            {
                if (_mozzis[i].Id != mozziId)
                {
                    continue;
                }

                string speciesId = _mozzis[i].SpeciesId;
                _mozzis.RemoveAt(i);
                _countBySpecies[speciesId]--;
                if (_countBySpecies[speciesId] <= 0)
                {
                    _produceAcc[speciesId] = 0;
                    _breedClock[speciesId] = 0;
                }

                EventBus.Publish(new MozziRemovedEvent(mozziId, speciesId, _mozzis.Count));
                return true;
            }

            return false;
        }

        public bool BuyFence()
        {
            int price = NextFencePrice;
            if (price < 0 || _wallet.Pay(price) == false)
            {
                return false;
            }

            _fenceLevel++;
            EventBus.Publish(new FenceUpgradedEvent(_fenceLevel, FenceMax));
            return true;
        }

        public void Skip(double seconds)
        {
            ApplyOffline(seconds);
        }

        public string CaptureJson()
        {
            FarmState state = new FarmState();
            state.nextMozziId = _nextMozziId;
            state.fenceLevel = _fenceLevel;

            for (int i = 0; i < _mozzis.Count; i++)
            {
                FarmMozziState m = new FarmMozziState();
                m.id = _mozzis[i].Id;
                m.speciesId = _mozzis[i].SpeciesId;
                state.mozzis.Add(m);
            }

            foreach (KeyValuePair<string, int> pair in _piles)
            {
                state.piles.Add(new CountEntry(pair.Key, pair.Value));
            }

            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                FarmSpeciesClockState c = new FarmSpeciesClockState();
                c.speciesId = _speciesOrder[i];
                c.produceAcc = _produceAcc[c.speciesId];
                c.breedClock = _breedClock[c.speciesId];
                state.clocks.Add(c);
            }

            return JsonUtility.ToJson(state);
        }

        // 표에 없는 종·부재료는 버린다 (표가 바뀐 옛 세이브).
        public void RestoreJson(string json)
        {
            FarmState state = JsonUtility.FromJson<FarmState>(json);
            if (state == null || state.mozzis == null)
            {
                return;
            }

            _restored = true;
            _nextMozziId = Math.Max(1, state.nextMozziId);
            // 표가 줄어든 옛 세이브도 표 안으로 맞춘다.
            _fenceLevel = Mathf.Clamp(state.fenceLevel, 0, _maxFenceLevel);

            for (int i = 0; i < state.mozzis.Count; i++)
            {
                FarmMozziState m = state.mozzis[i];
                MozziSpeciesRow row;
                if (_data.Species.TryGet(m.speciesId, out row) == false)
                {
                    continue;
                }

                _mozzis.Add(new FarmMozzi(m.id, m.speciesId));
                TrackSpecies(m.speciesId);
            }

            for (int i = 0; i < state.piles.Count; i++)
            {
                IngredientRow row;
                if (_data.Ingredients.TryGet(state.piles[i].id, out row) == false)
                {
                    continue;
                }

                _piles[state.piles[i].id] = Math.Max(0, state.piles[i].count);
            }

            for (int i = 0; i < state.clocks.Count; i++)
            {
                FarmSpeciesClockState c = state.clocks[i];
                if (_countBySpecies.ContainsKey(c.speciesId) == false)
                {
                    continue;
                }

                _produceAcc[c.speciesId] = c.produceAcc;
                _breedClock[c.speciesId] = c.breedClock;
            }
        }

        private void SeedStart()
        {
            for (int i = 0; i < _row.startCount; i++)
            {
                Place(_row.startSpeciesId);
            }
        }

        private FarmMozzi AddMozzi(string speciesId)
        {
            _data.Species.Get(speciesId);
            FarmMozzi mozzi = new FarmMozzi(_nextMozziId, speciesId);
            _nextMozziId++;
            _mozzis.Add(mozzi);
            TrackSpecies(speciesId);
            return mozzi;
        }

        private void TrackSpecies(string speciesId)
        {
            if (_countBySpecies.ContainsKey(speciesId) == false)
            {
                _speciesOrder.Add(speciesId);
                _countBySpecies[speciesId] = 0;
                _produceAcc[speciesId] = 0;
                _breedClock[speciesId] = 0;
            }

            _countBySpecies[speciesId]++;
        }

        private double ProducePeriodOf(MozziSpeciesRow species)
        {
            double mult = _row.tierMult1;
            if (species.tier == 2)
            {
                mult = _row.tierMult2;
            }
            else if (species.tier >= 3)
            {
                mult = _row.tierMult3;
            }

            return _row.producePeriodSec * mult;
        }

        // 생산 한 걸음. 이번 걸음에 더미로 떨어진 개수를 준다. 이벤트는 부르는 쪽 몫이다.
        // 창고가 찬 종은 시계가 0 으로 돌아가고 멈춘다 (농장 문서 5절). 부재료가 없는 종(늪)은 생산이 없다.
        private int StepProduce(MozziSpeciesRow species, double deltaTime)
        {
            int count = _countBySpecies[species.id];
            if (count <= 0 || string.IsNullOrEmpty(species.ingredientId))
            {
                return 0;
            }

            if (_inventory.IsFull(species.ingredientId))
            {
                _produceAcc[species.id] = 0;
                return 0;
            }

            double acc = _produceAcc[species.id] + count * deltaTime / ProducePeriodOf(species);
            int dropped = 0;
            while (acc >= 1)
            {
                acc -= 1;
                dropped++;
            }

            _produceAcc[species.id] = acc;
            if (dropped > 0)
            {
                _piles[species.ingredientId] = PileOf(species.ingredientId) + dropped;
            }

            return dropped;
        }

        // 번식 한 걸음. 태어났으면 그 마리, 아니면 null. 2마리 밑이거나 담장이 차면 시계가 0 으로 돌아간다 (농장 문서 6절).
        private FarmMozzi StepBreed(string speciesId, double deltaTime)
        {
            if (_countBySpecies[speciesId] < 2 || _mozzis.Count >= FenceMax)
            {
                _breedClock[speciesId] = 0;
                return null;
            }

            double clock = _breedClock[speciesId] + deltaTime;
            FarmMozzi born = null;
            if (clock >= _row.breedPeriodSec)
            {
                clock -= _row.breedPeriodSec;
                born = AddMozzi(speciesId);
            }

            _breedClock[speciesId] = clock;
            return born;
        }

        // 다음 사건까지 걸음으로 Process 와 같은 함수를 돌린다 (농장 문서 8절). 이벤트는 안 내고 OfflineReport 만 모은다.
        // 생산 보고는 부재료별로 합친다 (같은 부재료를 주는 종이 여럿이다).
        private void ApplyOffline(double seconds)
        {
            List<CountEntry> produced = new List<CountEntry>();
            List<CountEntry> born = new List<CountEntry>();
            List<string> producedOrder = new List<string>();
            Dictionary<string, int> producedSum = new Dictionary<string, int>();
            Dictionary<string, int> bornSum = new Dictionary<string, int>();

            if (seconds <= 0)
            {
                _offlineReport = new OfflineReport(0, produced, born);
                return;
            }

            double remaining = seconds;
            while (remaining > 0)
            {
                double step = Math.Min(NextEventSec(), remaining);
                remaining -= step;

                for (int i = 0; i < _speciesOrder.Count; i++)
                {
                    if (_countBySpecies[_speciesOrder[i]] <= 0)
                    {
                        continue;
                    }

                    MozziSpeciesRow species = _data.Species.Get(_speciesOrder[i]);
                    int dropped = StepProduce(species, step);
                    if (dropped > 0)
                    {
                        AddCount(producedSum, producedOrder, species.ingredientId, dropped);
                    }
                }

                for (int i = 0; i < _speciesOrder.Count; i++)
                {
                    if (_countBySpecies[_speciesOrder[i]] <= 0)
                    {
                        continue;
                    }

                    if (StepBreed(_speciesOrder[i], step) != null)
                    {
                        AddCount(bornSum, null, _speciesOrder[i], 1);
                    }
                }
            }

            for (int i = 0; i < producedOrder.Count; i++)
            {
                produced.Add(new CountEntry(producedOrder[i], producedSum[producedOrder[i]]));
            }

            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                int count;
                if (bornSum.TryGetValue(_speciesOrder[i], out count))
                {
                    born.Add(new CountEntry(_speciesOrder[i], count));
                }
            }

            _offlineReport = new OfflineReport(seconds, produced, born);
        }

        // 다음 번식까지 남은 초 중 최소. 상한 60초 · 하한 0.001초 (부동소수 오차로 0 이 되는 것을 막는다).
        // 생산은 사건이 아니다 — 안 켠 동안 창고는 안 바뀌고 더미는 상한이 없어 StepProduce 가 한 걸음에 여러 개를 떨군다.
        // 마릿수를 바꿔 생산 속도를 바꾸는 것은 번식뿐이라 번식 시각만 맞추면 결과가 1초 걸음과 같다.
        private double NextEventSec()
        {
            double next = OfflineMaxStepSec;
            if (_mozzis.Count >= FenceMax)
            {
                return next;
            }

            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                string id = _speciesOrder[i];
                if (_countBySpecies[id] < 2)
                {
                    continue;
                }

                next = Math.Min(next, _row.breedPeriodSec - _breedClock[id]);
            }

            return Math.Max(next, OfflineMinStepSec);
        }

        // 보고용 합계. order 를 주면 처음 보는 id 를 순서 목록에도 넣는다.
        private static void AddCount(Dictionary<string, int> sum, List<string> order, string id, int amount)
        {
            int current;
            if (sum.TryGetValue(id, out current) == false && order != null)
            {
                order.Add(id);
            }

            sum[id] = current + amount;
        }
    }
}
