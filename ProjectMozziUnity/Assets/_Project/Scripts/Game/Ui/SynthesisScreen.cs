using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Codex;
using Mozzi.Game.Farm;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Vfx;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using Mozzi.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 합성소 화면 (540 정식). 기계 넷이 칸 넷이고, 칸마다 상태(쉼 · 도는 중 · 다 됨 · 살 수 있음 · 잠김)를 그림으로 보인다.
    // 누르는 곳 : 깔때기 = 찹쌀이 넣기/빼기, 나무 표 = 시작(다 되면 받기), 서랍 = 받기, 빈 받침 = 칸 사기.
    // 고른 재료는 화면이 종 id 로만 기억하고, 시작 때 농장 것이면 보관함으로 먼저 옮긴 뒤 Manager 에 넘긴다.
    // 배치는 Editor/SynthesisScreenSetup · SynthesisRoomLayout 이 정본이다.
    public sealed class SynthesisScreen : OverlayScreen
    {
        private const string Unknown = UiText.Unknown;
        private const float ResultDuration = 0.9f;
        private const float ResultPeakScale = 1.6f;

        private static readonly Color PlateTextOn = new Color(1f, 0.96f, 0.86f, 1f);
        private static readonly Color PlateTextOff = new Color(1f, 0.96f, 0.86f, 0.5f);
        private static readonly Color BuyTextOn = new Color(0.29f, 0.18f, 0.13f, 1f);
        private static readonly Color BuyTextOff = new Color(0.29f, 0.18f, 0.13f, 0.45f);

        private readonly List<SynthesisSlotView> _slots = new List<SynthesisSlotView>();
        // 칸마다 마지막으로 적은 남은 초. 초가 바뀔 때만 글자를 고친다.
        private readonly List<int> _shownSec = new List<int>();
        // 칸마다 이번 합성의 전체 대기 초 (지금 단축률 기준). 창 안 두 반죽이 물드는 진행률을 낸다.
        private readonly List<double> _waitSec = new List<double>();
        private readonly List<string> _pickA = new List<string>();
        private readonly List<string> _pickB = new List<string>();
        private readonly List<Button> _pickRows = new List<Button>();
        private readonly List<string> _pickRowSpecies = new List<string>();

        private ISynthesisHub _synthesis;
        private IStorageHub _storage;
        private IFarmHub _farm;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IStaticDataHub _data;
        private IResourceHub _resource;
        private IUiHub _ui;

        // 기계 몸 그림. 도는 프레임은 고리로 돌리고, 없는 장은 쉼 그림으로 대신한다.
        private Sprite _idleSprite;
        private Sprite _doneSprite;
        private Sprite _buySprite;
        private Sprite _lockedSprite;
        private Sprite[] _runSprites;
        private float _runTime;
        private int _runFrame;

        private TMP_Text _shortcutText;
        private Button _shortcutBuyButton;
        private GameObject _pickPanel;
        private TMP_Text _pickTitle;
        private GameObject _resultPanel;
        private RectTransform _resultBox;
        private MozziIconView _resultMozzi;
        private TMP_Text _resultName;
        private GameObject _resultNew;

        private int _pickSlot = -1;
        private bool _pickFirst;
        private float _resultTime;
        private bool _resultPlaying;

        protected override string PrefabName
        {
            get { return "SynthesisScreen"; }
        }

        protected override string Title
        {
            get { return "합성소"; }
        }

        protected override void BindChildren()
        {
            _synthesis = InGameHub.Get<ISynthesisHub>();
            _storage = InGameHub.Get<IStorageHub>();
            _farm = InGameHub.Get<IFarmHub>();
            _codex = InGameHub.Get<ICodexHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _resource = GlobalHub.Get<IResourceHub>();

            BindSprites();
            BindMachines();

            _shortcutText = UiBindUtil.Find<TMP_Text>(transform, UiNames.SynthesisShortcutText, PrefabName);
            _shortcutBuyButton = UiBindUtil.BindButton(transform, UiNames.SynthesisShortcutBuyButton, PrefabName, OnBuyShortcut);
            UiBindUtil.BindButton(transform, UiNames.SynthesisMozziCodexButton, PrefabName, _ui.OpenMozziCodex);

            _pickPanel = UiBindUtil.FindObject(transform, UiNames.SynthesisPickPanel, PrefabName);
            _pickTitle = UiBindUtil.Find<TMP_Text>(_pickPanel.transform, UiNames.SynthesisPickTitleText, PrefabName);
            UiBindUtil.BindButton(_pickPanel.transform, UiNames.SynthesisPickCloseButton, PrefabName, ClosePick);
            UiBindUtil.BindButton(_pickPanel.transform, UiNames.SynthesisPickBlocker, PrefabName, ClosePick);
            for (int i = 0; i < UiNames.SynthesisPickRowCount; i++)
            {
                int index = i;
                Button row = UiBindUtil.BindButton(_pickPanel.transform, UiNames.SynthesisPickRowPrefix + i, PrefabName,
                    delegate { OnPickRow(index); });
                _pickRows.Add(row);
                _pickRowSpecies.Add(null);
            }

            _resultPanel = UiBindUtil.FindObject(transform, UiNames.SynthesisResultPanel, PrefabName);
            _resultBox = UiBindUtil.Find<RectTransform>(_resultPanel.transform, UiNames.SynthesisResultBox, PrefabName);
            _resultMozzi = new MozziIconView(UiBindUtil.FindObject(_resultBox, UiNames.SynthesisResultMozzi, PrefabName).transform, PrefabName);
            _resultName = UiBindUtil.Find<TMP_Text>(_resultPanel.transform, UiNames.SynthesisResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_resultPanel.transform, UiNames.SynthesisResultNewText, PrefabName);
            _resultPanel.GetComponent<Button>().onClick.RemoveAllListeners();
            _resultPanel.GetComponent<Button>().onClick.AddListener(CloseResult);

            UiBindUtil.SetActive(_pickPanel, false);
            UiBindUtil.SetActive(_resultPanel, false);
        }

        private void BindSprites()
        {
            _idleSprite = _resource.GetSprite(SpriteKeys.ScreenSynthesisMachine);
            _doneSprite = _resource.GetSprite(SpriteKeys.ScreenSynthesisMachineDone);
            _buySprite = _resource.GetSprite(SpriteKeys.ScreenSynthesisSlotBuy);
            _lockedSprite = _resource.GetSprite(SpriteKeys.ScreenSynthesisSlotLocked);
            _runSprites = new Sprite[SpriteKeys.SynthesisMachineRunFrameCount];
            for (int i = 0; i < _runSprites.Length; i++)
            {
                _runSprites[i] = _resource.GetSprite(SpriteKeys.SynthesisMachineRun(i));
                if (_runSprites[i] == null)
                {
                    _runSprites[i] = _idleSprite;
                }
            }
        }

        // 기계 넷. 같은 이름 자식이 넷씩이라 Stage 밑 기계 뿌리를 먼저 찾고 그 밑에서만 찾는다.
        private void BindMachines()
        {
            Transform stage = UiBindUtil.FindObject(transform, UiNames.Stage, PrefabName).transform;
            IVfxHub vfx = InGameHub.Get<IVfxHub>();
            for (int i = 0; i < _synthesis.MaxSlotCount; i++)
            {
                int index = i;
                Transform root = UiBindUtil.FindObject(stage, UiNames.SynthesisMachinePrefix + i, PrefabName).transform;
                SynthesisMachineView machine = new SynthesisMachineView(root, PrefabName, _resource, vfx);
                _slots.Add(new SynthesisSlotView(root, PrefabName, machine,
                    delegate { OnInlet(index, true); },
                    delegate { OnInlet(index, false); },
                    delegate { OnPlate(index); },
                    delegate { OnDrawer(index); },
                    OnBuySlot));
                _shownSec.Add(-1);
                _waitSec.Add(0);
                _pickA.Add(null);
                _pickB.Add(null);
            }
        }

        protected override void UnbindChildren()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Machine.Clear();
            }

            _slots.Clear();
            _shownSec.Clear();
            _waitSec.Clear();
            _pickA.Clear();
            _pickB.Clear();
            _pickRows.Clear();
            _pickRowSpecies.Clear();
            _pickSlot = -1;
            _resultPlaying = false;
            _runSprites = null;

            _synthesis = null;
            _storage = null;
            _farm = null;
            _codex = null;
            _wallet = null;
            _data = null;
            _resource = null;
            _ui = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshShortcut();
            for (int i = 0; i < _slots.Count; i++)
            {
                RefreshSlot(i);
            }

            if (_pickPanel.activeSelf)
            {
                RefreshPickRows();
            }
        }

        public override void Process(float deltaTime)
        {
            _runTime += deltaTime;
            int frame = (int)(_runTime / SynthesisRoomLayout.RunFrameSec) % _runSprites.Length;
            bool frameChanged = frame != _runFrame;
            _runFrame = frame;

            for (int i = 0; i < _slots.Count; i++)
            {
                SynthesisSlot slot = _synthesis.GetSlot(i);
                if (slot.status != SynthesisSlotStatus.Running)
                {
                    continue;
                }

                if (frameChanged)
                {
                    _slots[i].MachineArt.sprite = _runSprites[frame];
                }

                _slots[i].Machine.SetProgress(Progress(i, slot));

                int sec = CeilSec(slot.remainingSec);
                if (sec == _shownSec[i])
                {
                    continue;
                }

                _shownSec[i] = sec;
                UiBindUtil.SetText(_slots[i].PlateText, FormatSec(slot.remainingSec));
            }

            if (_resultPlaying == false)
            {
                return;
            }

            _resultTime += deltaTime;
            float k = Mathf.Clamp01(_resultTime / ResultDuration);
            float scale = Mathf.Lerp(0.3f, ResultPeakScale, Mathf.Sin(k * Mathf.PI));
            if (k >= 1f)
            {
                scale = 1f;
                _resultPlaying = false;
            }

            _resultBox.localScale = new Vector3(scale, scale, 1f);
        }

        public override void OnClosed()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Machine.Clear();
            }

            ClosePick();
            UiBindUtil.SetActive(_resultPanel, false);
            _resultPlaying = false;
        }

        // 「받기」 뒤 결과 연출. 새 찹쌀이 그림이 커졌다 작아지며 이름, 도감 새 칸이면 NEW.
        public void PlayResult(string speciesId, bool isNew)
        {
            MozziSpeciesRow species = _data.Species.Get(speciesId);
            _resultMozzi.Show(_resource, species);
            UiBindUtil.SetText(_resultName, species.Name);
            UiBindUtil.SetActive(_resultNew, isNew);
            _resultBox.localScale = new Vector3(0.3f, 0.3f, 1f);
            _resultTime = 0f;
            _resultPlaying = true;
            UiBindUtil.SetActive(_resultPanel, true);
        }

        private void RefreshShortcut()
        {
            int level = _synthesis.ShortcutLevel;
            int price = _synthesis.NextShortcutPrice;
            string text = "영구 단축 " + level + "/" + _synthesis.MaxShortcutLevel + " · 대기 " + Percent(_synthesis.ShortcutRate);
            if (price < 0)
            {
                UiBindUtil.SetText(_shortcutText, text + " (최대)");
                _shortcutBuyButton.interactable = false;
                UiBindUtil.SetLabel(_shortcutBuyButton, "단축 최대");
                return;
            }

            UiBindUtil.SetText(_shortcutText, text + " → " + Percent(_synthesis.NextShortcutRate));
            UiBindUtil.SetLabel(_shortcutBuyButton, "단축 사기 " + price + "G");
            _shortcutBuyButton.interactable = _wallet.CanPay(price);
        }

        // 칸 하나의 상태를 기계 그림 · 누를 곳 · 표 글자 · 연출로 옮긴다.
        private void RefreshSlot(int index)
        {
            SynthesisSlotView view = _slots[index];
            SynthesisSlot slot = _synthesis.GetSlot(index);

            if (slot.status == SynthesisSlotStatus.Locked)
            {
                view.Machine.ShowIdle(index, null, null);
                RefreshLocked(index, view);
                return;
            }

            if (slot.status == SynthesisSlotStatus.Running)
            {
                view.MachineArt.sprite = _runSprites[_runFrame];
                view.ShowControls(true, false, false, false);
                view.PlateText.color = PlateTextOn;
                _shownSec[index] = CeilSec(slot.remainingSec);
                UiBindUtil.SetText(view.PlateText, FormatSec(slot.remainingSec));
                // 남은 초는 단축을 사면 같은 비율로 줄어드니, 전체 초도 지금 단축률로 다시 잰다.
                _waitSec[index] = _synthesis.Preview(slot.speciesA, slot.speciesB).WaitSec;
                view.Machine.SetProgress(Progress(index, slot));
                view.Machine.ShowRunning(index, _data.Species.Get(slot.speciesA), _data.Species.Get(slot.speciesB));
                return;
            }

            if (slot.status == SynthesisSlotStatus.Done)
            {
                view.MachineArt.sprite = _doneSprite;
                view.ShowControls(true, true, false, false);
                view.PlateText.color = PlateTextOn;
                UiBindUtil.SetText(view.PlateText, "다 됨! 받기");
                // 무엇이 나왔는지는 받을 때 알린다 — 그림자는 재료 모양을 빌린다.
                view.Machine.ShowDone(index, _data.Species.Get(slot.speciesA));
                return;
            }

            view.MachineArt.sprite = _idleSprite;
            view.ShowControls(true, false, false, true);
            UiBindUtil.SetActive(view.PlusA, _pickA[index] == null);
            UiBindUtil.SetActive(view.PlusB, _pickB[index] == null);
            view.Machine.ShowIdle(index, SpeciesOrNull(_pickA[index]), SpeciesOrNull(_pickB[index]));
            RefreshEmptyPlate(index, view);
        }

        // 다음 칸만 살 수 있다 (빈 받침 + 간판). 그 뒤 칸은 덮개 + 자물쇠.
        private void RefreshLocked(int index, SynthesisSlotView view)
        {
            int price = _synthesis.NextSlotPrice;
            bool isNext = index == _synthesis.SlotCount && price >= 0;
            if (isNext == false)
            {
                view.MachineArt.sprite = _lockedSprite;
                view.ShowControls(false, false, false, false);
                return;
            }

            view.MachineArt.sprite = _buySprite;
            view.ShowControls(false, false, true, false);
            UiBindUtil.SetText(view.BuyText, "칸 사기\n" + price + "G");
            bool canPay = _wallet.CanPay(price);
            view.BuyStand.interactable = canPay;
            view.BuyText.color = BuyTextOff;
            if (canPay)
            {
                view.BuyText.color = BuyTextOn;
            }
        }

        // 빈 칸의 나무 표. 둘 다 넣으면 「시작 20G · 풀 75%」 (확정 종이 있으면 그 이름과 확률).
        private void RefreshEmptyPlate(int index, SynthesisSlotView view)
        {
            string a = _pickA[index];
            string b = _pickB[index];
            if (a == null || b == null)
            {
                view.PlateText.color = PlateTextOff;
                UiBindUtil.SetText(view.PlateText, "찹쌀이 2마리 넣기");
                return;
            }

            SynthesisPreview preview = _synthesis.Preview(a, b);
            string text = "시작 " + preview.Gold + "G";
            if (preview.FixedSpeciesId != null)
            {
                text += " · " + NameOrUnknown(preview.FixedSpeciesId) + " " + Percent(preview.PFixed);
            }

            UiBindUtil.SetText(view.PlateText, text);
            view.PlateText.color = PlateTextOff;
            if (_wallet.CanPay(preview.Gold) && HasEnough(a, b))
            {
                view.PlateText.color = PlateTextOn;
            }
        }

        // 깔때기를 눌렀을 때. 빈 깔때기면 고르기 목록을 열고, 이미 넣었으면 뺀다.
        private void OnInlet(int index, bool first)
        {
            if (_synthesis.GetSlot(index).status != SynthesisSlotStatus.Empty)
            {
                return;
            }

            if (first && _pickA[index] != null)
            {
                _pickA[index] = null;
                Refresh();
                return;
            }

            if (first == false && _pickB[index] != null)
            {
                _pickB[index] = null;
                Refresh();
                return;
            }

            OpenPick(index, first);
        }

        // 나무 표. 빈 칸이면 시작, 다 됐으면 받기 (서랍과 같다).
        private void OnPlate(int index)
        {
            SynthesisSlotStatus status = _synthesis.GetSlot(index).status;
            if (status == SynthesisSlotStatus.Empty)
            {
                OnStart(index);
                return;
            }

            if (status == SynthesisSlotStatus.Done)
            {
                OnTake(index);
            }
        }

        private void OnDrawer(int index)
        {
            if (_synthesis.GetSlot(index).status == SynthesisSlotStatus.Done)
            {
                OnTake(index);
            }
        }

        private MozziSpeciesRow SpeciesOrNull(string speciesId)
        {
            if (speciesId == null)
            {
                return null;
            }

            return _data.Species.Get(speciesId);
        }

        private string NameOrUnknown(string speciesId)
        {
            if (_codex.IsOpen(speciesId) == false)
            {
                return Unknown;
            }

            return _data.Species.Get(speciesId).Name;
        }

        // 보관함 + 농장을 합쳐 재료가 되는지.
        private bool HasEnough(string a, string b)
        {
            List<CountEntry> needs = CollectNeeds(a, b);
            for (int i = 0; i < needs.Count; i++)
            {
                if (_storage.Count(needs[i].id) + _farm.CountOf(needs[i].id) < needs[i].count)
                {
                    return false;
                }
            }

            return true;
        }

        // 종별 필요 마릿수. 옮기는 도중 이벤트로 Refresh 가 다시 들어오므로 필드가 아니라 새 목록으로 준다.
        private static List<CountEntry> CollectNeeds(string a, string b)
        {
            List<CountEntry> needs = new List<CountEntry>();
            if (a == b)
            {
                needs.Add(new CountEntry(a, 2));
                return needs;
            }

            needs.Add(new CountEntry(a, 1));
            needs.Add(new CountEntry(b, 1));
            return needs;
        }

        private void OnBuyShortcut()
        {
            _synthesis.BuyShortcut();
            Refresh();
        }

        private void OnBuySlot()
        {
            _synthesis.BuySlot();
            Refresh();
        }

        // 시작이 될지(칸 · 골드 · 재료) 먼저 보고, 보관함에 모자란 만큼 농장에서 옮긴 뒤 시작한다.
        // 그래도 Start 가 실패하면 옮긴 마리를 농장에 되돌린다 — 찹쌀이가 보관함에 남지 않게.
        private void OnStart(int index)
        {
            string a = _pickA[index];
            string b = _pickB[index];
            if (a == null || b == null)
            {
                return;
            }

            if (CanStart(index, a, b) == false)
            {
                Debug.LogWarning("[Synthesis] 칸 · 골드 · 재료가 안 맞아 시작 못 함 : " + a + " + " + b);
                Refresh();
                return;
            }

            List<string> moved = new List<string>();
            List<CountEntry> needs = CollectNeeds(a, b);
            for (int i = 0; i < needs.Count; i++)
            {
                string id = needs[i].id;
                if (MoveFromFarm(id, needs[i].count - _storage.Count(id), moved) == false)
                {
                    Debug.LogWarning("[Synthesis] 재료가 모자라 시작 못 함 : " + id);
                    ReturnToFarm(moved);
                    Refresh();
                    return;
                }
            }

            if (_synthesis.Start(index, a, b))
            {
                _pickA[index] = null;
                _pickB[index] = null;
                _slots[index].Machine.PlayMerge(index, _data.Species.Get(a), _data.Species.Get(b));
            }
            else
            {
                Debug.LogWarning("[Synthesis] Start 실패 · 옮긴 " + moved.Count + "마리를 농장에 되돌림");
                ReturnToFarm(moved);
            }

            Refresh();
        }

        // Manager 의 Start 가 보는 것과 같은 조건. 보관함 + 농장을 합쳐 재료를 센다.
        private bool CanStart(int index, string a, string b)
        {
            if (_synthesis.GetSlot(index).status != SynthesisSlotStatus.Empty)
            {
                return false;
            }

            if (_wallet.CanPay(_synthesis.Preview(a, b).Gold) == false)
            {
                return false;
            }

            return HasEnough(a, b);
        }

        private bool MoveFromFarm(string speciesId, int count, List<string> moved)
        {
            for (int i = 0; i < count; i++)
            {
                int mozziId = FindFarmMozzi(speciesId);
                if (mozziId < 0 || _storage.TakeFromFarm(mozziId) == false)
                {
                    return false;
                }

                moved.Add(speciesId);
            }

            return true;
        }

        private void ReturnToFarm(List<string> moved)
        {
            for (int i = 0; i < moved.Count; i++)
            {
                if (_storage.PlaceToFarm(moved[i]) == false)
                {
                    Debug.LogWarning("[Synthesis] 되돌리기 실패 · 보관함에 남음 : " + moved[i]);
                }
            }

            moved.Clear();
        }

        private int FindFarmMozzi(string speciesId)
        {
            IReadOnlyList<FarmMozzi> mozzis = _farm.Mozzis;
            for (int i = 0; i < mozzis.Count; i++)
            {
                if (mozzis[i].SpeciesId == speciesId)
                {
                    return mozzis[i].Id;
                }
            }

            return -1;
        }

        // 연출은 UiManager 가 SynthesisTakenEvent 를 받아 PlayResult 로 띄운다.
        private void OnTake(int index)
        {
            _synthesis.Take(index);
        }

        private void OpenPick(int slotIndex, bool first)
        {
            _pickSlot = slotIndex;
            _pickFirst = first;
            string which = "오른쪽";
            if (first)
            {
                which = "왼쪽";
            }

            UiBindUtil.SetText(_pickTitle, "칸 " + (slotIndex + 1) + " · " + which + " 깔때기에 넣을 찹쌀이");
            UiBindUtil.SetActive(_pickPanel, true);
            RefreshPickRows();
        }

        private void ClosePick()
        {
            _pickSlot = -1;
            UiBindUtil.SetActive(_pickPanel, false);
        }

        // 보관함 + 농장에 있는 종을 한 줄씩. 남는 줄은 숨긴다.
        private void RefreshPickRows()
        {
            List<string> ids = new List<string>();
            IReadOnlyList<CountEntry> entries = _storage.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ids.Add(entries[i].id);
            }

            IReadOnlyList<FarmMozzi> mozzis = _farm.Mozzis;
            for (int i = 0; i < mozzis.Count; i++)
            {
                if (ids.Contains(mozzis[i].SpeciesId) == false)
                {
                    ids.Add(mozzis[i].SpeciesId);
                }
            }

            for (int i = 0; i < _pickRows.Count; i++)
            {
                bool has = i < ids.Count;
                UiBindUtil.SetActive(_pickRows[i].gameObject, has);
                if (has == false)
                {
                    _pickRowSpecies[i] = null;
                    continue;
                }

                string id = ids[i];
                _pickRowSpecies[i] = id;
                UiBindUtil.SetLabel(_pickRows[i], _data.Species.Get(id).Name
                    + " · 보관함 " + _storage.Count(id) + " · 농장 " + _farm.CountOf(id));
            }
        }

        private void OnPickRow(int rowIndex)
        {
            string id = _pickRowSpecies[rowIndex];
            if (id == null || _pickSlot < 0)
            {
                return;
            }

            if (_pickFirst)
            {
                _pickA[_pickSlot] = id;
            }
            else
            {
                _pickB[_pickSlot] = id;
            }

            ClosePick();
            Refresh();
        }

        private void CloseResult()
        {
            if (_resultPlaying)
            {
                return;
            }

            UiBindUtil.SetActive(_resultPanel, false);
        }

        private static string Percent(float rate)
        {
            return Mathf.RoundToInt(rate * 100f) + "%";
        }

        // 대기 진행률 0 → 1. 전체 초를 아직 모르면 0.
        private float Progress(int index, SynthesisSlot slot)
        {
            double wait = _waitSec[index];
            if (wait <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(1.0 - slot.remainingSec / wait));
        }

        private static int CeilSec(double seconds)
        {
            return (int)System.Math.Ceiling(seconds);
        }

        private static string FormatSec(double seconds)
        {
            int total = CeilSec(seconds);
            int min = total / 60;
            int sec = total % 60;
            return min + ":" + sec.ToString("00");
        }
    }
}
