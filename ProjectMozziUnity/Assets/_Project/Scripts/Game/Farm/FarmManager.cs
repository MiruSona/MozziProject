using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Inventory;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Farm
{
    // 농장의 찹쌀이 목록 · 생산 · 바닥 더미 · 줍기 · 번식 · 수집기 · 농장 강화 · 오프라인 진행.
    // 생산·번식 시계는 종마다 하나씩 돈다 (프로토 q3 produce/breed 방식). 수집기 시계는 농장에 하나다.
    public sealed class FarmManager : IManager, IProcess, ISavable, IFarmHub
    {
        private const string Key = "Farm";
        private const string FarmRowId = DataKeys.DefaultRowId;

        // 오프라인은 「다음 번식까지 남은 초」 걸음으로 실시간과 같은 함수를 돌린다 (농장 문서 8절).
        // 사건 사이는 시계가 직선으로 쌓이므로 결과는 1초 걸음과 같다. 더 바뀔 것이 없으면 남은 시간을 한 걸음에 돌린다.
        private const double OfflineMaxStepSec = 60;
        private const double OfflineMinStepSec = 0.001;

        private readonly List<FarmMozzi> _mozzis = new List<FarmMozzi>();
        private readonly List<string> _speciesOrder = new List<string>();
        private readonly Dictionary<string, int> _countBySpecies = new Dictionary<string, int>();
        private readonly Dictionary<string, double> _produceAcc = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _breedClock = new Dictionary<string, double>();
        private readonly Dictionary<string, int> _piles = new Dictionary<string, int>();
        private readonly List<string> _collectorSkip = new List<string>();

        // 농장 강화 넷(담장 · 바닥 넓히기 · 수집기 · 먹이 공급기)의 단계와 최대 단계.
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _maxLevels = new Dictionary<string, int>();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IClockHub _clock;
        private IInventoryHub _inventory;
        private IWalletHub _wallet;

        private FarmRow _row;
        private int _nextMozziId = 1;
        private double _collectorClock;
        private bool _restored;
        private OfflineReport _offlineReport;
        private EventSubscription _foregroundSub;

        public string SaveKey
        {
            get { return Key; }
        }

        // 담장 상한 = Farm 표 fenceMax + 담장 단계만큼 더한 수 (누적값 한 행).
        public int FenceMax
        {
            get { return _row.FenceMax + (int)RateOf(GrowthUtil.KindFence); }
        }

        // 바닥 재료 합계 한도 = Farm 표 floorMax + 바닥 넓히기 단계만큼 더한 수 (누적값 한 행).
        public int FloorMax
        {
            get { return _row.FloorMax + (int)RateOf(GrowthUtil.KindFloor); }
        }

        public int FloorTotal
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<string, int> pair in _piles)
                {
                    total += pair.Value;
                }

                return total;
            }
        }

        public int PileMax
        {
            get { return _row.PileMax; }
        }

        // 수집기가 걷는 주기 (초). 주기는 Farm 표 하나이고 단계는 걷는 수만 올린다. 수집기가 없으면 0.
        public double CollectorPeriodSec
        {
            get
            {
                if (CollectorAmount <= 0)
                {
                    return 0;
                }

                return _row.CollectorPeriodSec;
            }
        }

        public int CollectorAmount
        {
            get { return (int)RateOf(GrowthUtil.KindCollector); }
        }

        // 먹이 공급기가 더하는 생산 속도 비율 (0.1 = +10%).
        public double FeederBonus
        {
            get { return RateOf(GrowthUtil.KindFeeder); }
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
            AddUpgradeKind(GrowthUtil.KindFence);
            AddUpgradeKind(GrowthUtil.KindFloor);
            AddUpgradeKind(GrowthUtil.KindCollector);
            AddUpgradeKind(GrowthUtil.KindFeeder);
            _offlineReport = new OfflineReport(0, new List<CountEntry>(), new List<CountEntry>());

            _save.Register(this);
            if (_restored == false)
            {
                SeedStart();
            }

            ApplyOffline(_clock.OfflineSeconds);
            // 앱이 멈췄다 돌아오면 못 돈 시간을 오프라인과 같은 길로 따라잡는다.
            _foregroundSub = EventBus.Subscribe<AppForegroundEvent>(this, evt => Skip(evt.Seconds));
            Debug.Log("[Farm] 마릿수 " + _mozzis.Count + "/" + FenceMax + " · " + _offlineReport.Describe());
        }

        // 한 걸음은 생산 → 수집기 → 번식 순서다. 오프라인(ApplyOffline)도 같은 함수를 쓴다.
        public void Process(float deltaTime)
        {
            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                MozziSpeciesRow species = _data.Species.Get(_speciesOrder[i]);
                int dropped = StepProduce(species, deltaTime);
                int pile = PileOf(species.IngredientId);
                for (int d = dropped - 1; d >= 0; d--)
                {
                    EventBus.Publish(new IngredientDroppedEvent(species.IngredientId, pile - d, species.Id));
                }
            }

            StepCollector(deltaTime, true);

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
            if (_foregroundSub != null)
            {
                _foregroundSub.Dispose();
            }
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
            return PickUp(ingredientId, PileOf(ingredientId));
        }

        // 더미에서 amount 개까지만 창고로 옮긴다. 더미보다 많이 청하면 더미만큼, 창고가 차면 들어간 만큼이다.
        public int PickUp(string ingredientId, int amount)
        {
            int pile = PileOf(ingredientId);
            int want = Math.Min(pile, amount);
            if (want <= 0)
            {
                return 0;
            }

            int moved = _inventory.Add(ingredientId, want);
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

        // 담장 · 바닥 넓히기 · 수집기 · 먹이 공급기의 지금 단계. 그 밖의 kind 는 0.
        public int UpgradeLevel(string kind)
        {
            int level;
            _levels.TryGetValue(kind, out level);
            return level;
        }

        public int MaxUpgradeLevel(string kind)
        {
            int level;
            _maxLevels.TryGetValue(kind, out level);
            return level;
        }

        // 다음 단계 값. 최대이거나 농장 강화 kind 가 아니면 -1.
        public int NextUpgradePrice(string kind)
        {
            if (_levels.ContainsKey(kind) == false)
            {
                return -1;
            }

            GrowthRow row = GrowthUtil.Find(_data.Growths, kind, _levels[kind] + 1);
            if (row == null)
            {
                return -1;
            }

            return row.Price;
        }

        public bool BuyUpgrade(string kind)
        {
            int price = NextUpgradePrice(kind);
            if (price < 0 || _wallet.Pay(price) == false)
            {
                return false;
            }

            _levels[kind]++;
            EventBus.Publish(new FarmUpgradedEvent(kind, _levels[kind]));
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
            state.fenceLevel = _levels[GrowthUtil.KindFence];
            state.floorLevel = _levels[GrowthUtil.KindFloor];
            state.collectorLevel = _levels[GrowthUtil.KindCollector];
            state.feederLevel = _levels[GrowthUtil.KindFeeder];
            state.collectorClock = _collectorClock;

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
            _levels[GrowthUtil.KindFence] = Mathf.Clamp(state.fenceLevel, 0, _maxLevels[GrowthUtil.KindFence]);
            _levels[GrowthUtil.KindFloor] = Mathf.Clamp(state.floorLevel, 0, _maxLevels[GrowthUtil.KindFloor]);
            _levels[GrowthUtil.KindCollector] = Mathf.Clamp(state.collectorLevel, 0, _maxLevels[GrowthUtil.KindCollector]);
            _levels[GrowthUtil.KindFeeder] = Mathf.Clamp(state.feederLevel, 0, _maxLevels[GrowthUtil.KindFeeder]);
            _collectorClock = Math.Max(0, state.collectorClock);

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

            // 한도가 없던 옛 세이브는 바닥 재료가 한도를 넘을 수 있다. 비율대로 줄여 한도에 맞춘다.
            List<string> pileIds = new List<string>(_piles.Keys);
            List<int> pileCounts = new List<int>();
            for (int i = 0; i < pileIds.Count; i++)
            {
                pileCounts.Add(_piles[pileIds[i]]);
            }

            FarmUpgradeMath.TrimToMax(pileCounts, FloorMax);
            for (int i = 0; i < pileIds.Count; i++)
            {
                _piles[pileIds[i]] = pileCounts[i];
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
            for (int i = 0; i < _row.StartCount; i++)
            {
                Place(_row.StartSpeciesId);
            }
        }

        private void AddUpgradeKind(string kind)
        {
            _levels[kind] = 0;
            _maxLevels[kind] = GrowthUtil.MaxLevel(_data.Growths, kind);
        }

        // 지금 단계 한 행의 rate. 안 샀으면(0단계) 0 이다.
        private double RateOf(string kind)
        {
            GrowthRow row = GrowthUtil.Find(_data.Growths, kind, UpgradeLevel(kind));
            if (row == null)
            {
                return 0;
            }

            return row.Rate;
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
            double mult = _row.TierMult1;
            if (species.Tier == 2)
            {
                mult = _row.TierMult2;
            }
            else if (species.Tier >= 3)
            {
                mult = _row.TierMult3;
            }

            return FarmUpgradeMath.ProducePeriod(_row.ProducePeriodSec, mult, FeederBonus);
        }

        // 생산 한 걸음. 이번 걸음에 더미로 떨어진 개수를 준다. 이벤트는 부르는 쪽 몫이다.
        // 창고 + 바닥 그 재료가 창고 상한에 닿았거나 바닥 합계가 한도에 닿으면 시계가 0 으로 돌아가고 멈춘다 (농장 문서 5절).
        // 창고 상한까지만 떨궈 못 줍는 재료가 바닥에 남지 않게 한다. 늪처럼 부재료가 없는 종은 생산이 없다.
        private int StepProduce(MozziSpeciesRow species, double deltaTime)
        {
            int count = _countBySpecies[species.Id];
            if (count <= 0 || string.IsNullOrEmpty(species.IngredientId))
            {
                return 0;
            }

            int floorTotal = FloorTotal;
            int held = _inventory.Count(species.IngredientId) + PileOf(species.IngredientId);
            int cap = _inventory.Cap(species.IngredientId);
            if (held >= cap || floorTotal >= FloorMax)
            {
                _produceAcc[species.Id] = 0;
                return 0;
            }

            double acc = _produceAcc[species.Id] + count * deltaTime / ProducePeriodOf(species);
            int dropped = 0;
            while (acc >= 1)
            {
                acc -= 1;
                dropped++;
            }

            int fit = FarmUpgradeMath.ClampDrop(dropped, floorTotal, FloorMax);
            fit = FarmUpgradeMath.ClampDrop(fit, held, cap);
            if (fit < dropped)
            {
                acc = 0;
                dropped = fit;
            }

            _produceAcc[species.Id] = acc;
            if (dropped > 0)
            {
                _piles[species.IngredientId] = PileOf(species.IngredientId) + dropped;
            }

            return dropped;
        }

        // 수집기 한 걸음. 주기가 차면 바닥 재료를 CollectorAmount 개까지 많이 쌓인 재료부터 창고로 걷는다. 창고가 찬 재료는 건너뛴다.
        // publish 면 걷은 재료마다 CollectorSweptEvent 를 낸다 — 화면이 수집기를 돌린다. 오프라인 처리는 false 다.
        private void StepCollector(double deltaTime, bool publish)
        {
            double period = CollectorPeriodSec;
            if (period <= 0)
            {
                _collectorClock = 0;
                return;
            }

            double clock = _collectorClock + deltaTime;
            if (clock < period)
            {
                _collectorClock = clock;
                return;
            }

            _collectorClock = clock % period;
            // 창고가 차서 하나도 못 옮긴 재료는 _collectorSkip 에 넣어 다음 고르기에서 뺀다.
            _collectorSkip.Clear();
            int left = CollectorAmount;
            while (left > 0)
            {
                string id = FarmUpgradeMath.BiggestPile(_piles, _collectorSkip);
                if (id == null)
                {
                    return;
                }

                int moved = PickUp(id, left);
                if (moved <= 0)
                {
                    _collectorSkip.Add(id);
                }
                else if (publish)
                {
                    EventBus.Publish(new CollectorSweptEvent(id, moved));
                }

                left -= moved;
            }
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
            if (clock >= _row.BreedPeriodSec)
            {
                clock -= _row.BreedPeriodSec;
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
                if (Stalled())
                {
                    step = remaining;
                }

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
                        AddCount(producedSum, producedOrder, species.IngredientId, dropped);
                    }
                }

                StepCollector(step, false);

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

        // 다음 번식 · 수집기까지 남은 초 중 최소. 상한 60초 · 하한 0.001초 (부동소수 오차로 0 이 되는 것을 막는다).
        // 생산은 사건이 아니다 — StepProduce 가 한 걸음에 여러 개를 떨구고 바닥 한도에서 자른다. 바닥을 비우는 것은 수집기,
        // 생산 속도를 바꾸는 것은 번식뿐이라 이 둘의 시각만 맞추면 결과가 1초 걸음과 같다 (한도 앞에서 종 차례만 다르다).
        private double NextEventSec()
        {
            double next = OfflineMaxStepSec;
            double period = CollectorPeriodSec;
            if (period > 0)
            {
                next = Math.Min(next, period - _collectorClock);
            }

            if (_mozzis.Count >= FenceMax)
            {
                return Math.Max(next, OfflineMinStepSec);
            }

            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                string id = _speciesOrder[i];
                if (_countBySpecies[id] < 2)
                {
                    continue;
                }

                next = Math.Min(next, _row.BreedPeriodSec - _breedClock[id]);
            }

            return Math.Max(next, OfflineMinStepSec);
        }

        // 더 걸어도 바뀌는 것이 없나 : 번식할 종이 없고, 생산이 모두 막혔고(창고 상한 · 바닥 한도), 수집기가 옮길 것이 없다.
        // 그러면 남은 시간을 한 걸음에 돌려도 1초 걸음과 같다 — 막힌 생산 · 번식 시계는 0 이고 수집기 시계는 나머지(%)로 맞춰진다.
        private bool Stalled()
        {
            bool fenceFull = _mozzis.Count >= FenceMax;
            bool floorFull = FloorTotal >= FloorMax;
            for (int i = 0; i < _speciesOrder.Count; i++)
            {
                string id = _speciesOrder[i];
                if (_countBySpecies[id] >= 2 && fenceFull == false)
                {
                    return false;
                }

                string ingredientId = _data.Species.Get(id).IngredientId;
                bool producing = _countBySpecies[id] > 0 && string.IsNullOrEmpty(ingredientId) == false;
                if (producing && floorFull == false && _inventory.Count(ingredientId) + PileOf(ingredientId) < _inventory.Cap(ingredientId))
                {
                    return false;
                }
            }

            if (CollectorAmount <= 0)
            {
                return true;
            }

            foreach (KeyValuePair<string, int> pair in _piles)
            {
                if (pair.Value > 0 && _inventory.IsFull(pair.Key) == false)
                {
                    return false;
                }
            }

            return true;
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
