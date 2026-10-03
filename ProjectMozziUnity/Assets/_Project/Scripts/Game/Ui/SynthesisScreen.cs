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
    // 합성소 화면 (세로조각1판설계 3-3). 기계 한 대 · 칸 탭 4개 · 찹쌀이 고르기 목록 · 결과 연출.
    // 기계와 아래 판은 「보고 있는 칸」 하나만 보여 주고, 탭을 누르면 그 칸으로 바뀐다.
    // 고른 재료는 화면이 종 id 로만 기억하고, 「시작」 때 농장 것이면 보관함으로 먼저 옮긴 뒤 Manager 에 넘긴다.
    public sealed class SynthesisScreen : OverlayScreen
    {
        private const string Unknown = UiText.Unknown;
        private const float ResultDuration = 0.9f;
        private const float ResultPeakScale = 1.6f;

        private static readonly Color TabOff = new Color(0.35f, 0.38f, 0.55f, 1f);
        private static readonly Color TabOn = new Color(0.9f, 0.7f, 0.25f, 1f);

        private readonly List<SynthesisSlotView> _slots = new List<SynthesisSlotView>();
        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<TMP_Text> _tabLabels = new List<TMP_Text>();
        // 칸마다 마지막으로 적은 남은 초. 초가 바뀔 때만 글자를 고친다.
        private readonly List<int> _shownSec = new List<int>();
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

        private SynthesisMachineView _machine;
        // 기계와 아래 판이 보여 주는 칸.
        private int _viewSlot;

        // 기계 입구 누름 칸 둘과 빈 입구 표시(+).
        private Button _inletA;
        private Button _inletB;
        private GameObject _inletPlusA;
        private GameObject _inletPlusB;

        private TMP_Text _shortcutText;
        private Button _shortcutBuyButton;
        private GameObject _pickPanel;
        private TMP_Text _pickTitle;
        private GameObject _resultPanel;
        private RectTransform _resultBox;
        private Image _resultBoxImage;
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
            _resource = GlobalHub.Get<IResourceHub>();

            Transform scene = UiBindUtil.FindObject(transform, UiNames.Scene, PrefabName).transform;
            UiBindUtil.Find<Image>(scene, UiNames.SceneBg, PrefabName).sprite = _resource.GetSprite(SpriteKeys.ScreenSynthesisBg);
            _machine = new SynthesisMachineView(scene, PrefabName, _resource, InGameHub.Get<IVfxHub>());
            _viewSlot = 0;

            _inletA = UiBindUtil.BindButton(scene, UiNames.SynthesisInletAButton, PrefabName, delegate { OnInlet(true); });
            _inletB = UiBindUtil.BindButton(scene, UiNames.SynthesisInletBButton, PrefabName, delegate { OnInlet(false); });
            _inletPlusA = UiBindUtil.FindObject(_inletA.transform, UiNames.SynthesisInletPlus, PrefabName);
            _inletPlusB = UiBindUtil.FindObject(_inletB.transform, UiNames.SynthesisInletPlus, PrefabName);

            _shortcutText = UiBindUtil.Find<TMP_Text>(transform, UiNames.SynthesisShortcutText, PrefabName);
            _shortcutBuyButton = UiBindUtil.BindButton(transform, UiNames.SynthesisShortcutBuyButton, PrefabName, OnBuyShortcut);

            for (int i = 0; i < _synthesis.MaxSlotCount; i++)
            {
                int index = i;
                Transform slotRoot = UiBindUtil.FindObject(transform, UiNames.SynthesisSlotPrefix + i, PrefabName).transform;
                _slots.Add(new SynthesisSlotView(slotRoot, PrefabName,
                    OnBuySlot,
                    delegate { OpenPick(index, true); },
                    delegate { OpenPick(index, false); },
                    delegate { OnStart(index); },
                    delegate { OnTake(index); }));
                Button tab = UiBindUtil.BindButton(transform, UiNames.SynthesisSlotTabPrefix + i, PrefabName,
                    delegate { SelectSlot(index); });
                _tabs.Add(tab);
                _tabLabels.Add(UiBindUtil.Find<TMP_Text>(tab.transform, UiNames.ButtonLabel, PrefabName));
                _shownSec.Add(-1);
                _pickA.Add(null);
                _pickB.Add(null);
            }

            _pickPanel = UiBindUtil.FindObject(transform, UiNames.SynthesisPickPanel, PrefabName);
            _pickTitle = UiBindUtil.Find<TMP_Text>(_pickPanel.transform, UiNames.SynthesisPickTitleText, PrefabName);
            UiBindUtil.BindButton(_pickPanel.transform, UiNames.SynthesisPickCloseButton, PrefabName, ClosePick);
            for (int i = 0; i < UiNames.SynthesisPickRowCount; i++)
            {
                int index = i;
                Button row = UiBindUtil.BindButton(_pickPanel.transform, UiNames.SynthesisPickRowPrefix + i, PrefabName,
                    delegate { OnPickRow(index); });
                _pickRows.Add(row);
                _pickRowSpecies.Add(null);
            }

            _resultPanel = UiBindUtil.FindObject(transform, UiNames.SynthesisResultPanel, PrefabName);
            _resultBoxImage = UiBindUtil.Find<Image>(_resultPanel.transform, UiNames.SynthesisResultBox, PrefabName);
            _resultBox = _resultBoxImage.rectTransform;
            _resultName = UiBindUtil.Find<TMP_Text>(_resultPanel.transform, UiNames.SynthesisResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_resultPanel.transform, UiNames.SynthesisResultNewText, PrefabName);
            _resultPanel.GetComponent<Button>().onClick.RemoveAllListeners();
            _resultPanel.GetComponent<Button>().onClick.AddListener(CloseResult);

            UiBindUtil.SetActive(_pickPanel, false);
            UiBindUtil.SetActive(_resultPanel, false);
        }

        protected override void UnbindChildren()
        {
            _machine.Clear();
            _machine = null;
            _slots.Clear();
            _tabs.Clear();
            _tabLabels.Clear();
            _shownSec.Clear();
            _inletA = null;
            _inletB = null;
            _inletPlusA = null;
            _inletPlusB = null;
            _pickA.Clear();
            _pickB.Clear();
            _pickRows.Clear();
            _pickRowSpecies.Clear();
            _pickSlot = -1;
            _resultPlaying = false;

            _synthesis = null;
            _storage = null;
            _farm = null;
            _codex = null;
            _wallet = null;
            _data = null;
            _resource = null;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshShortcut();
            for (int i = 0; i < _slots.Count; i++)
            {
                RefreshSlot(i);
                RefreshTab(i);
                UiBindUtil.SetActive(_slots[i].Root, i == _viewSlot);
            }

            RefreshMachine();
            RefreshInlets();

            if (_pickPanel.activeSelf)
            {
                RefreshPickRows();
            }
        }

        public override void Process(float deltaTime)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                SynthesisSlot slot = _synthesis.GetSlot(i);
                if (slot.status != SynthesisSlotStatus.Running)
                {
                    continue;
                }

                int sec = CeilSec(slot.remainingSec);
                if (sec == _shownSec[i])
                {
                    continue;
                }

                _shownSec[i] = sec;
                UiBindUtil.SetText(_slots[i].RemainText, "남은 시간 " + FormatSec(slot.remainingSec));
                RefreshTab(i);
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
            _machine.Clear();
            ClosePick();
            UiBindUtil.SetActive(_resultPanel, false);
            _resultPlaying = false;
        }

        // 「받기」 뒤 결과 연출. 새 찹쌀이 색 네모가 커졌다 작아지며 이름, 도감 새 칸이면 NEW.
        public void PlayResult(string speciesId, bool isNew)
        {
            MozziSpeciesRow species = _data.Species.Get(speciesId);
            _resultBoxImage.color = View.MozziArtKeys.BodyColorOf(species);
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
            string text = "단축 " + level + "/" + _synthesis.MaxShortcutLevel + " · 대기 " + Percent(_synthesis.ShortcutRate);
            if (price < 0)
            {
                UiBindUtil.SetText(_shortcutText, text + " (최대)");
                _shortcutBuyButton.interactable = false;
                UiBindUtil.SetLabel(_shortcutBuyButton, "최대");
                return;
            }

            UiBindUtil.SetText(_shortcutText, text + " → " + Percent(_synthesis.NextShortcutRate));
            UiBindUtil.SetLabel(_shortcutBuyButton, "단축 사기 " + price + "G");
            _shortcutBuyButton.interactable = _wallet.CanPay(price);
        }

        // 탭을 누르면 기계와 아래 판이 그 칸을 보여 준다. 잠긴 칸도 고를 수 있다 (「칸 사기」가 거기 있다).
        private void SelectSlot(int index)
        {
            _viewSlot = index;
            Refresh();
        }

        // 탭 글자는 「1 · 비었음」처럼 칸 번호와 상태 한 마디다. 보고 있는 칸은 색으로 표시한다.
        private void RefreshTab(int index)
        {
            SynthesisSlot slot = _synthesis.GetSlot(index);
            string state = "비었음";
            if (slot.status == SynthesisSlotStatus.Locked)
            {
                state = "잠김";
            }
            else if (slot.status == SynthesisSlotStatus.Running)
            {
                state = FormatSec(slot.remainingSec);
            }
            else if (slot.status == SynthesisSlotStatus.Done)
            {
                state = "다 됨!";
            }

            UiBindUtil.SetText(_tabLabels[index], (index + 1) + " · " + state);

            Color color = TabOff;
            if (index == _viewSlot)
            {
                color = TabOn;
            }

            _tabs[index].image.color = color;
        }

        // 기계는 보고 있는 칸의 상태를 그린다. 시작 연출은 OnStart 가 따로 건다.
        private void RefreshMachine()
        {
            SynthesisSlot slot = _synthesis.GetSlot(_viewSlot);
            if (slot.status == SynthesisSlotStatus.Running)
            {
                _machine.ShowRunning(_viewSlot, _data.Species.Get(slot.speciesA), _data.Species.Get(slot.speciesB));
                return;
            }

            if (slot.status == SynthesisSlotStatus.Done)
            {
                // 무엇이 나왔는지는 받을 때 알린다 — 그림자는 재료 모양을 빌린다.
                _machine.ShowDone(_viewSlot, _data.Species.Get(slot.speciesA));
                return;
            }

            if (slot.status == SynthesisSlotStatus.Locked)
            {
                _machine.ShowIdle(_viewSlot, null, null);
                return;
            }

            _machine.ShowIdle(_viewSlot, SpeciesOrNull(_pickA[_viewSlot]), SpeciesOrNull(_pickB[_viewSlot]));
        }

        // 입구 누름 칸은 재료를 고를 수 있는 빈 칸에서만 켠다. 아직 안 고른 쪽에는 + 를 띄운다.
        private void RefreshInlets()
        {
            bool empty = _synthesis.GetSlot(_viewSlot).status == SynthesisSlotStatus.Empty;
            UiBindUtil.SetActive(_inletA.gameObject, empty);
            UiBindUtil.SetActive(_inletB.gameObject, empty);
            UiBindUtil.SetActive(_inletPlusA, _pickA[_viewSlot] == null);
            UiBindUtil.SetActive(_inletPlusB, _pickB[_viewSlot] == null);
        }

        // 기계 입구를 눌렀을 때. 아래 판의 재료 버튼과 같은 일을 보고 있는 칸에 한다.
        private void OnInlet(bool first)
        {
            if (_synthesis.GetSlot(_viewSlot).status != SynthesisSlotStatus.Empty)
            {
                return;
            }

            OpenPick(_viewSlot, first);
        }

        private MozziSpeciesRow SpeciesOrNull(string speciesId)
        {
            if (speciesId == null)
            {
                return null;
            }

            return _data.Species.Get(speciesId);
        }

        private void RefreshSlot(int index)
        {
            SynthesisSlotView view = _slots[index];
            SynthesisSlot slot = _synthesis.GetSlot(index);

            if (slot.status == SynthesisSlotStatus.Locked)
            {
                view.ShowGroup(view.LockedGroup);
                RefreshLocked(index, view);
                return;
            }

            if (slot.status == SynthesisSlotStatus.Running)
            {
                view.ShowGroup(view.RunningGroup);
                UiBindUtil.SetText(view.RemainText, "남은 시간 " + FormatSec(slot.remainingSec));
                return;
            }

            if (slot.status == SynthesisSlotStatus.Done)
            {
                view.ShowGroup(view.DoneGroup);
                UiBindUtil.SetLabel(view.TakeButton, "받기");
                return;
            }

            view.ShowGroup(view.EmptyGroup);
            RefreshEmpty(index, view);
        }

        // 다음 칸만 살 수 있다. 그 뒤 칸은 「잠김」.
        private void RefreshLocked(int index, SynthesisSlotView view)
        {
            bool isNext = index == _synthesis.SlotCount;
            int price = _synthesis.NextSlotPrice;
            if (isNext == false || price < 0)
            {
                UiBindUtil.SetLabel(view.BuySlotButton, "잠김");
                view.BuySlotButton.interactable = false;
                return;
            }

            UiBindUtil.SetLabel(view.BuySlotButton, "칸 사기 " + price + "G");
            view.BuySlotButton.interactable = _wallet.CanPay(price);
        }

        private void RefreshEmpty(int index, SynthesisSlotView view)
        {
            string a = _pickA[index];
            string b = _pickB[index];
            UiBindUtil.SetLabel(view.MaterialAButton, MaterialLabel(a));
            UiBindUtil.SetLabel(view.MaterialBButton, MaterialLabel(b));

            if (a == null || b == null)
            {
                UiBindUtil.SetText(view.PreviewText, "재료 2마리를 고르세요");
                UiBindUtil.SetLabel(view.StartButton, "시작");
                view.StartButton.interactable = false;
                return;
            }

            SynthesisPreview preview = _synthesis.Preview(a, b);
            UiBindUtil.SetText(view.PreviewText, PreviewText(preview));
            UiBindUtil.SetLabel(view.StartButton, "시작 " + preview.Gold + "G");
            view.StartButton.interactable = _wallet.CanPay(preview.Gold) && HasEnough(a, b);
        }

        private string MaterialLabel(string speciesId)
        {
            if (speciesId == null)
            {
                return "고르기";
            }

            return _data.Species.Get(speciesId).Name;
        }

        private string PreviewText(SynthesisPreview preview)
        {
            List<string> lines = new List<string>();
            if (preview.FixedSpeciesId != null)
            {
                lines.Add(NameOrUnknown(preview.FixedSpeciesId) + " " + Percent(preview.PFixed));
            }

            List<string> others = new List<string>();
            for (int i = 0; i < preview.OtherFamilyIds.Count; i++)
            {
                others.Add(NameOrUnknown(preview.OtherFamilyIds[i]));
            }

            if (others.Count > 0)
            {
                lines.Add("다른 계열(" + string.Join("/", others) + ") " + Percent(preview.POtherFamily));
            }

            if (preview.ShinySpeciesId != null)
            {
                lines.Add("변종 " + NameOrUnknown(preview.ShinySpeciesId) + " " + Percent(preview.PShiny));
            }

            lines.Add("대기 " + FormatSec(preview.WaitSec));
            return string.Join("\n", lines);
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
                if (index == _viewSlot)
                {
                    _machine.PlayMerge(index, _data.Species.Get(a), _data.Species.Get(b));
                }
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
            string which = "둘째";
            if (first)
            {
                which = "첫째";
            }

            UiBindUtil.SetText(_pickTitle, "칸 " + (slotIndex + 1) + " · " + which + " 재료 고르기");
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
