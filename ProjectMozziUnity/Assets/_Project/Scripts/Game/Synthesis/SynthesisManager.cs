using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Storage;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Synthesis
{
    // 합성소. 칸마다 재료 2 · 결과 · 남은 초를 갖고, 결과는 시작 때 굴려 저장한다 (오프라인 진행이 쉽다).
    // 비용·대기는 SynthesisRule 표의 재료 단계(높은 쪽) 행, 칸·단축 값은 Growth 표다.
    public sealed class SynthesisManager : IManager, IProcess, ISavable, ISynthesisHub
    {
        private const string Key = "Synthesis";

        private readonly List<MozziSpeciesRow> _candidates = new List<MozziSpeciesRow>();
        private readonly List<string> _candidateIds = new List<string>();
        private readonly List<int> _justDone = new List<int>();

        private SynthesisState _state = new SynthesisState();

        private IStaticDataHub _data;
        private ISaveHub _save;
        private IClockHub _clock;
        private IWalletHub _wallet;
        private IStorageHub _storage;
        private ICodexHub _codex;

        private int _maxSlotCount;
        private int _maxShortcutLevel;
        private int _offlineDoneCount;

        public string SaveKey
        {
            get { return Key; }
        }

        public int SlotCount
        {
            get { return _state.slotCount; }
        }

        public int MaxSlotCount
        {
            get { return _maxSlotCount; }
        }

        public int ShortcutLevel
        {
            get { return _state.shortcutLevel; }
        }

        public int MaxShortcutLevel
        {
            get { return _maxShortcutLevel; }
        }

        public float ShortcutRate
        {
            get { return RateOf(_state.shortcutLevel); }
        }

        public int NextSlotPrice
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindSlot, _state.slotCount + 1);
                if (row == null)
                {
                    return -1;
                }

                return row.Price;
            }
        }

        public int NextShortcutPrice
        {
            get
            {
                GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindShortcut, _state.shortcutLevel + 1);
                if (row == null)
                {
                    return -1;
                }

                return row.Price;
            }
        }

        public float NextShortcutRate
        {
            get { return RateOf(_state.shortcutLevel + 1); }
        }

        public int OfflineDoneCount
        {
            get { return _offlineDoneCount; }
        }

        public void Init()
        {
        }

        // 복원 → 칸 목록 채우기 → 오프라인 진행 순서다.
        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _save = InGameHub.Get<ISaveHub>();
            _clock = InGameHub.Get<IClockHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _storage = InGameHub.Get<IStorageHub>();
            _codex = InGameHub.Get<ICodexHub>();

            _maxSlotCount = GrowthUtil.MaxLevel(_data.Growths, GrowthUtil.KindSlot);
            if (_maxSlotCount < 1)
            {
                _maxSlotCount = 1;
            }

            _maxShortcutLevel = GrowthUtil.MaxLevel(_data.Growths, GrowthUtil.KindShortcut);

            _save.Register(this);
            NormalizeSlots();
            _offlineDoneCount = ApplyOffline(_clock.OfflineSeconds);
            Debug.Log("[Synthesis] 칸 " + _state.slotCount + "/" + _maxSlotCount
                + " · 단축 " + _state.shortcutLevel + "/" + _maxShortcutLevel + " · " + Describe());
        }

        public void Process(float deltaTime)
        {
            for (int i = 0; i < _state.slots.Count; i++)
            {
                SynthesisSlot slot = _state.slots[i];
                if (slot.status != SynthesisSlotStatus.Running)
                {
                    continue;
                }

                slot.remainingSec -= deltaTime;
                if (slot.remainingSec > 0)
                {
                    continue;
                }

                slot.remainingSec = 0;
                slot.status = SynthesisSlotStatus.Done;
                EventBus.Publish(new SynthesisDoneEvent(i));
            }
        }

        public void Dispose()
        {
        }

        // 범위 밖이면 잠긴 빈 칸을 준다.
        public SynthesisSlot GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _state.slots.Count)
            {
                return new SynthesisSlot();
            }

            return _state.slots[slotIndex];
        }

        // 버튼에서 오므로(핸들러 아님) 이번에 새로 끝난 칸만 이벤트를 낸다.
        public void Skip(double seconds)
        {
            _offlineDoneCount = ApplyOffline(seconds);
            for (int i = 0; i < _justDone.Count; i++)
            {
                EventBus.Publish(new SynthesisDoneEvent(_justDone[i]));
            }
        }

        public SynthesisPreview Preview(string speciesA, string speciesB)
        {
            MozziSpeciesRow a = _data.Species.Get(speciesA);
            MozziSpeciesRow b = _data.Species.Get(speciesB);
            SynthesisRuleRow rule = RuleOf(a, b);

            MozziSpeciesRow fixedRow = SynthesisRollUtil.FindFixed(_data.Species, a, b);
            MozziSpeciesRow shinyRow = ShinyTargetOf(fixedRow, a);

            SynthesisRollUtil.CollectOtherFamily(_data.Species, a, b, _codex.IsFamilyOpen(SynthesisRollUtil.FamilySwamp), _candidates);
            _candidateIds.Clear();
            for (int i = 0; i < _candidates.Count; i++)
            {
                _candidateIds.Add(_candidates[i].Id);
            }

            string fixedId = null;
            float pFixed = 0f;
            float pOther = rule.POtherFamilyTop;
            float pShiny = rule.PShinyTop;
            if (fixedRow != null)
            {
                fixedId = fixedRow.Id;
                pFixed = rule.PFixed;
                pOther = rule.POtherFamily;
                pShiny = rule.PShiny;
            }

            string shinyId = null;
            if (shinyRow != null)
            {
                shinyId = shinyRow.Id;
            }

            return new SynthesisPreview(fixedId, new List<string>(_candidateIds), shinyId,
                pFixed, pOther, pShiny, rule.Gold, rule.WaitSec * ShortcutRate);
        }

        public bool Start(int slotIndex, string speciesA, string speciesB)
        {
            if (slotIndex < 0 || slotIndex >= _state.slotCount)
            {
                return false;
            }

            SynthesisSlot slot = _state.slots[slotIndex];
            if (slot.status != SynthesisSlotStatus.Empty)
            {
                return false;
            }

            MozziSpeciesRow a = _data.Species.Get(speciesA);
            MozziSpeciesRow b = _data.Species.Get(speciesB);
            if (HasMaterials(speciesA, speciesB) == false)
            {
                return false;
            }

            SynthesisRuleRow rule = RuleOf(a, b);
            if (_wallet.Pay(rule.Gold) == false)
            {
                return false;
            }

            _storage.Take(speciesA);
            _storage.Take(speciesB);

            slot.status = SynthesisSlotStatus.Running;
            slot.speciesA = speciesA;
            slot.speciesB = speciesB;
            slot.resultSpeciesId = Roll(rule, a, b).Id;
            slot.remainingSec = rule.WaitSec * ShortcutRate;

            EventBus.Publish(new SynthesisStartedEvent(slotIndex, slot.remainingSec));
            return true;
        }

        public string Take(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _state.slots.Count)
            {
                return null;
            }

            SynthesisSlot slot = _state.slots[slotIndex];
            if (slot.status != SynthesisSlotStatus.Done)
            {
                return null;
            }

            string result = slot.resultSpeciesId;
            slot.Clear(SynthesisSlotStatus.Empty);

            _storage.Add(result, 1);
            bool isNew = _codex.Discover(result);
            EventBus.Publish(new SynthesisTakenEvent(slotIndex, result, isNew));
            return result;
        }

        public bool BuySlot()
        {
            int price = NextSlotPrice;
            if (price < 0 || _wallet.Pay(price) == false)
            {
                return false;
            }

            _state.slotCount++;
            _state.slots[_state.slotCount - 1].Clear(SynthesisSlotStatus.Empty);
            return true;
        }

        // 단축은 돌고 있는 칸에도 걸린다 — 남은 초를 새 배율 ÷ 옛 배율로 줄인다.
        public bool BuyShortcut()
        {
            int price = NextShortcutPrice;
            if (price < 0 || _wallet.Pay(price) == false)
            {
                return false;
            }

            float oldRate = ShortcutRate;
            _state.shortcutLevel++;
            float scale = ShortcutRate / oldRate;

            for (int i = 0; i < _state.slots.Count; i++)
            {
                if (_state.slots[i].status == SynthesisSlotStatus.Running)
                {
                    _state.slots[i].remainingSec *= scale;
                }
            }

            return true;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(_state);
        }

        public void RestoreJson(string json)
        {
            SynthesisState loaded = JsonUtility.FromJson<SynthesisState>(json);
            if (loaded == null || loaded.slots == null)
            {
                return;
            }

            _state = loaded;
        }

        // 칸 목록을 최대 칸 수로 맞추고 잠김/빈 상태를 slotCount 에 맞춘다. 표에 없는 종이 든 칸은 비운다.
        private void NormalizeSlots()
        {
            _state.slotCount = Mathf.Clamp(_state.slotCount, 1, _maxSlotCount);
            _state.shortcutLevel = Mathf.Clamp(_state.shortcutLevel, 0, _maxShortcutLevel);

            while (_state.slots.Count < _maxSlotCount)
            {
                _state.slots.Add(new SynthesisSlot());
            }

            while (_state.slots.Count > _maxSlotCount)
            {
                _state.slots.RemoveAt(_state.slots.Count - 1);
            }

            for (int i = 0; i < _state.slots.Count; i++)
            {
                SynthesisSlot slot = _state.slots[i];
                if (i >= _state.slotCount)
                {
                    slot.Clear(SynthesisSlotStatus.Locked);
                    continue;
                }

                if (slot.status == SynthesisSlotStatus.Locked)
                {
                    slot.Clear(SynthesisSlotStatus.Empty);
                    continue;
                }

                MozziSpeciesRow row;
                bool hasResult = _data.Species.TryGet(slot.resultSpeciesId, out row);
                if (slot.status != SynthesisSlotStatus.Empty && hasResult == false)
                {
                    slot.Clear(SynthesisSlotStatus.Empty);
                }
            }
        }

        // 이벤트 없이 남은 초만 깎고 이번에 새로 끝난 칸을 _justDone 에 모아 그 수를 준다.
        private int ApplyOffline(double seconds)
        {
            _justDone.Clear();
            if (seconds <= 0)
            {
                return 0;
            }

            for (int i = 0; i < _state.slots.Count; i++)
            {
                SynthesisSlot slot = _state.slots[i];
                if (slot.status != SynthesisSlotStatus.Running)
                {
                    continue;
                }

                slot.remainingSec -= seconds;
                if (slot.remainingSec <= 0)
                {
                    slot.remainingSec = 0;
                    slot.status = SynthesisSlotStatus.Done;
                    _justDone.Add(i);
                }
            }

            return _justDone.Count;
        }

        // 같은 종 둘이면 2마리가 있어야 한다.
        private bool HasMaterials(string speciesA, string speciesB)
        {
            if (speciesA == speciesB)
            {
                return _storage.Count(speciesA) >= 2;
            }

            return _storage.Count(speciesA) >= 1 && _storage.Count(speciesB) >= 1;
        }

        private SynthesisRuleRow RuleOf(MozziSpeciesRow a, MozziSpeciesRow b)
        {
            int tier = SynthesisRollUtil.MaterialTier(a, b);
            IReadOnlyList<SynthesisRuleRow> rows = _data.SynthesisRules.All;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Tier == tier)
                {
                    return rows[i];
                }
            }

            throw new InvalidOperationException("SynthesisRule 표에 tier " + tier + " 행이 없다");
        }

        private float RateOf(int level)
        {
            if (level <= 0)
            {
                return 1f;
            }

            GrowthRow row = GrowthUtil.Find(_data.Growths, GrowthUtil.KindShortcut, level);
            if (row == null)
            {
                return RateOf(level - 1);
            }

            return row.Rate;
        }

        // ③ 변종은 ①과 같은 종. ①이 없으면(3단계 2마리) 첫 슬롯 종의 변종이다 — 「변종 뽑기」.
        private MozziSpeciesRow ShinyTargetOf(MozziSpeciesRow fixedRow, MozziSpeciesRow a)
        {
            MozziSpeciesRow target = fixedRow;
            if (target == null)
            {
                target = SynthesisRollUtil.FindBase(_data.Species, a.Family, a.Tier);
            }

            if (target == null)
            {
                return null;
            }

            MozziSpeciesRow shiny = SynthesisRollUtil.FindShiny(_data.Species, target.Family, target.Tier);
            if (shiny == null)
            {
                return target;
            }

            return shiny;
        }

        private MozziSpeciesRow Roll(SynthesisRuleRow rule, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            MozziSpeciesRow fixedRow = SynthesisRollUtil.FindFixed(_data.Species, a, b);
            SynthesisBranch branch = SynthesisRollUtil.PickBranch(rule, fixedRow != null, UnityEngine.Random.value);

            if (branch == SynthesisBranch.OtherFamily)
            {
                SynthesisRollUtil.CollectOtherFamily(_data.Species, a, b,
                    _codex.IsFamilyOpen(SynthesisRollUtil.FamilySwamp), _candidates);
                if (_candidates.Count > 0)
                {
                    return _candidates[UnityEngine.Random.Range(0, _candidates.Count)];
                }

                // 후보가 없으면 ①로, 그것도 없으면 ③으로 미룬다.
                if (fixedRow != null)
                {
                    return fixedRow;
                }

                branch = SynthesisBranch.Shiny;
            }

            if (branch == SynthesisBranch.Shiny)
            {
                MozziSpeciesRow shiny = ShinyTargetOf(fixedRow, a);
                if (shiny != null)
                {
                    return shiny;
                }
            }

            if (fixedRow != null)
            {
                return fixedRow;
            }

            throw new InvalidOperationException("합성 결과를 정할 수 없다 : " + a.Id + " + " + b.Id);
        }

        private string Describe()
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < _state.slots.Count; i++)
            {
                SynthesisSlot slot = _state.slots[i];
                parts.Add(i + ":" + slot.status + "(" + slot.remainingSec.ToString("F0") + "s)");
            }

            return string.Join(" ", parts);
        }
    }
}
