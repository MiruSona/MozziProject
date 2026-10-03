using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using Mozzi.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // Canvas 를 코드로 만들고 화면 프리팹을 빌려 붙인다. Manager → 화면은 EventBus 로 받는다.
    // 농장 화면이 바탕, 덮개는 한 번에 하나, 팝업 · 디버그 패널이 맨 위다 (세로조각1판설계 3절).
    // 화면·디버그 패널은 내용 칸(안전 영역 안 540 폭)에 붙이고, 팝업의 어두운 막은 화면 끝까지 덮는다.
    // 옛 360 기준 프리팹은 Legacy360 껍데기(×1.5) 안에 붙여 겉보기를 지킨다 — 540 으로 옮긴 화면(농장)은 껍데기 밖 내용 칸에 바로 붙인다.
    public sealed class UiManager : IManager, IProcess, IUiHub
    {
        // 이보다 짧게 비웠으면 팝업을 안 띄운다.
        private const double OfflinePopupMinSec = 30;

        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private readonly List<OverlayScreen> _overlays = new List<OverlayScreen>();

        private IResourceHub _resource;
        private IStaticDataHub _data;
        private IClockHub _clock;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IFarmHub _farm;
        private ISynthesisHub _synthesis;
        private IStallHub _stall;
        private ISaveHub _save;
        private IStorageHub _storage;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        // 내용 칸. 화면 크기 · 안전 영역이 바뀌면 FitToScreen 이 다시 맞춘다.
        private RectTransform _contentRoot;
        // 옛 360 기준 화면 · 팝업을 ×1.5 로 감싸는 껍데기. 크기는 FitToScreen 이 부모 크기 ÷ 1.5 로 맞춘다.
        private RectTransform _legacyContent;
        private RectTransform _legacyPopup;
        private int _fitWidth;
        private int _fitHeight;
        private Rect _fitSafeArea;
        private FarmScreen _farmScreen;
        private SynthesisScreen _synthesisScreen;
        private MozziCodexScreen _codexScreen;
        private StorageScreen _storageScreen;
        private StallScreen _stallScreen;
        private RecipeCodexScreen _recipeCodexScreen;
        private OverlayScreen _open;
        private OfflinePopup _offlinePopup;
        private MozziPopup _mozziPopup;
#if UNITY_EDITOR || MOZZI_DEBUG
        private DebugPanel _debugPanel;
        // 팝업보다 위에 두는 두 번째 내용 칸. 디버그 패널이 팝업에 안 가리게.
        private RectTransform _debugRoot;
        private RectTransform _legacyDebug;
#endif

        public bool IsAnyOpen
        {
            get { return _open != null; }
        }

        public void Init()
        {
            _resource = GlobalHub.Get<IResourceHub>();
            _canvas = CreateCanvas();
            _scaler = _canvas.GetComponent<CanvasScaler>();
            _contentRoot = CreateContentRoot(UiNames.ContentRoot);
            _legacyContent = CreateLegacyRoot(_contentRoot, UiNames.Legacy360Root);
            _legacyPopup = CreateLegacyRoot(_canvas.transform as RectTransform, UiNames.Legacy360PopupRoot);
            FitToScreen();
        }

        public void PostInit()
        {
            _data = InGameHub.Get<IStaticDataHub>();
            _clock = InGameHub.Get<IClockHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _farm = InGameHub.Get<IFarmHub>();
            _synthesis = InGameHub.Get<ISynthesisHub>();
            _stall = InGameHub.Get<IStallHub>();
            _save = InGameHub.Get<ISaveHub>();
            _storage = InGameHub.Get<IStorageHub>();

            // 농장 화면은 540 기준이라 껍데기 밖 내용 칸에 바로 붙인다. 덮개(껍데기 안)보다 뒤에 오게 맨 앞 형제로 둔다.
            _farmScreen = _resource.Rent<FarmScreen>(PrefabKeys.UiFarmScreen, _contentRoot);
            StretchToParent(_farmScreen.transform as RectTransform);
            _farmScreen.transform.SetAsFirstSibling();
            _farmScreen.Bind(_inventory.IngredientIds, this, _clock.AdvanceDay, OnBuyFence);
            BindIngredientIcons();

            _synthesisScreen = RentOverlay<SynthesisScreen>(PrefabKeys.UiSynthesisScreen);
            _codexScreen = RentOverlay<MozziCodexScreen>(PrefabKeys.UiMozziCodexScreen);
            _storageScreen = RentOverlay<StorageScreen>(PrefabKeys.UiStorageScreen);
            _stallScreen = RentOverlay<StallScreen>(PrefabKeys.UiStallScreen);
            _recipeCodexScreen = RentOverlay<RecipeCodexScreen>(PrefabKeys.UiRecipeCodexScreen);

            _offlinePopup = _resource.Rent<OfflinePopup>(PrefabKeys.UiOfflinePopup, _legacyPopup);
            StretchToParent(_offlinePopup.transform as RectTransform);
            _offlinePopup.Bind(_offlinePopup.Hide);
            _offlinePopup.Hide();

            _mozziPopup = _resource.Rent<MozziPopup>(PrefabKeys.UiMozziPopup, _legacyPopup);
            StretchToParent(_mozziPopup.transform as RectTransform);
            _mozziPopup.Bind(OnMozziPopupStore, _mozziPopup.Hide);
            _mozziPopup.Hide();

#if UNITY_EDITOR || MOZZI_DEBUG
            _debugRoot = CreateContentRoot(UiNames.DebugRoot);
            _legacyDebug = CreateLegacyRoot(_debugRoot, UiNames.Legacy360Root);
            FitToScreen();
            _debugPanel = _resource.Rent<DebugPanel>(PrefabKeys.UiDebugPanel, _legacyDebug);
            StretchToParent(_debugPanel.transform as RectTransform);
            _debugPanel.Bind(OnDebugSkipped);
#endif

            RefreshAll();
            _farmScreen.SetNight(_clock.IsNight, false);

            _subscriptions.Add(EventBus.Subscribe<GoldChangedEvent>(this, OnGoldChanged));
            _subscriptions.Add(EventBus.Subscribe<IngredientChangedEvent>(this, OnIngredientChanged));
            _subscriptions.Add(EventBus.Subscribe<MozziBornEvent>(this, OnMozziBorn));
            _subscriptions.Add(EventBus.Subscribe<MozziPlacedEvent>(this, OnMozziPlaced));
            _subscriptions.Add(EventBus.Subscribe<MozziRemovedEvent>(this, OnMozziRemoved));
            _subscriptions.Add(EventBus.Subscribe<FenceUpgradedEvent>(this, OnFenceUpgraded));
            _subscriptions.Add(EventBus.Subscribe<DayStartedEvent>(this, OnDayStarted));
            _subscriptions.Add(EventBus.Subscribe<NightFellEvent>(this, OnNightFell));
            _subscriptions.Add(EventBus.Subscribe<StorageChangedEvent>(this, OnStorageChanged));
            _subscriptions.Add(EventBus.Subscribe<SynthesisStartedEvent>(this, OnSynthesisStarted));
            _subscriptions.Add(EventBus.Subscribe<SynthesisDoneEvent>(this, OnSynthesisDone));
            _subscriptions.Add(EventBus.Subscribe<SynthesisTakenEvent>(this, OnSynthesisTaken));
            _subscriptions.Add(EventBus.Subscribe<MozziDiscoveredEvent>(this, OnMozziDiscovered));
            _subscriptions.Add(EventBus.Subscribe<StallOpenedEvent>(this, OnStallOpened));
            _subscriptions.Add(EventBus.Subscribe<CustomerArrivedEvent>(this, OnCustomerArrived));
            _subscriptions.Add(EventBus.Subscribe<CustomerServedEvent>(this, OnCustomerServed));
            _subscriptions.Add(EventBus.Subscribe<RiceCakeMadeEvent>(this, OnRiceCakeMade));
            _subscriptions.Add(EventBus.Subscribe<RecipeDiscoveredEvent>(this, OnRecipeDiscovered));

            // 맨 뒤 — 앞 Manager 들의 오프라인 진행이 다 끝난 결과를 읽는다 (세로조각1판설계 4-3).
            if (_save.HasSaveFile && _clock.OfflineSeconds >= OfflinePopupMinSec)
            {
                ShowOfflinePopup(_clock.OfflineSeconds);
            }
        }

        public void Process(float deltaTime)
        {
            if (Screen.width != _fitWidth || Screen.height != _fitHeight || ScreenFitUtil.SafeArea() != _fitSafeArea)
            {
                FitToScreen();
            }

            _farmScreen.Process(deltaTime);
            if (_open != null)
            {
                _open.Process(deltaTime);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();

            // 플레이를 멈출 때는 Unity 가 먼저 부수므로 이미 없는 것은 반납하지 않는다.
            if (_farmScreen != null)
            {
                _resource.Return(_farmScreen);
            }

            _farmScreen = null;

            for (int i = 0; i < _overlays.Count; i++)
            {
                if (_overlays[i] != null)
                {
                    _resource.Return(_overlays[i]);
                }
            }

            _overlays.Clear();
            _open = null;

            if (_offlinePopup != null)
            {
                _resource.Return(_offlinePopup);
            }

            _offlinePopup = null;

            if (_mozziPopup != null)
            {
                _resource.Return(_mozziPopup);
            }

            _mozziPopup = null;

#if UNITY_EDITOR || MOZZI_DEBUG
            if (_debugPanel != null)
            {
                _resource.Return(_debugPanel);
            }

            _debugPanel = null;
#endif

            if (_canvas != null)
            {
                Object.Destroy(_canvas.gameObject);
            }
        }

        public void OpenSynthesis()
        {
            Open(_synthesisScreen);
        }

        public void OpenMozziCodex()
        {
            Open(_codexScreen);
        }

        public void OpenStorage()
        {
            Open(_storageScreen);
        }

        public void OpenStall()
        {
            Open(_stallScreen);
        }

        // 가판대 화면의 「레시피 도감」 버튼에서도 온다 — 3-1 「패널끼리 안 옮김」의 유일한 예외다.
        public void OpenRecipeCodex()
        {
            Open(_recipeCodexScreen);
        }

        public void CloseAll()
        {
            if (_open == null)
            {
                return;
            }

            _open.OnClosed();
            _open.gameObject.SetActive(false);
            _open = null;
        }

        // 농장에 없는 id 면 안 연다 (탭과 사라짐이 같은 프레임에 겹칠 때).
        public void OpenMozziPopup(int mozziId)
        {
            string speciesId = FindFarmSpecies(mozziId);
            if (speciesId == null)
            {
                return;
            }

            _mozziPopup.Show(mozziId, _data.Species.Get(speciesId).Name);
            KeepTopmost();
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

        // 메인 화면 강화 판의 「담장 늘리기」. 값이 바뀌면 FenceUpgradedEvent · GoldChangedEvent 가 판을 되읽는다.
        private void OnBuyFence()
        {
            _farm.BuyFence();
            RefreshUpgrade();
        }

        private void RefreshUpgrade()
        {
            int price = _farm.NextFencePrice;
            _farmScreen.SetUpgradeFence(_farm.MozziCount, _farm.FenceMax, price, price >= 0 && _wallet.CanPay(price));
        }

        // 「보관함으로」. 옮기고 나면 팝업을 닫는다 — 농장 Object 는 FarmViewManager 가 목록 대조로 거둔다.
        private void OnMozziPopupStore(int mozziId)
        {
            _storage.TakeFromFarm(mozziId);
            _mozziPopup.Hide();
        }

        private void Open(OverlayScreen screen)
        {
            if (_open == screen)
            {
                return;
            }

            CloseAll();
            _open = screen;
            screen.gameObject.SetActive(true);
            screen.transform.SetAsLastSibling();
            screen.Refresh();
            KeepTopmost();
        }

        // 팝업 · 디버그 패널은 어느 덮개보다 위에 둔다.
        private void KeepTopmost()
        {
            _legacyPopup.SetAsLastSibling();
            _offlinePopup.transform.SetAsLastSibling();
            _mozziPopup.transform.SetAsLastSibling();
#if UNITY_EDITOR || MOZZI_DEBUG
            _debugRoot.SetAsLastSibling();
#endif
        }

        private void ShowOfflinePopup(double seconds)
        {
            string body = OfflineTextUtil.Body(_data, _farm.OfflineReport, _synthesis.OfflineDoneCount, _stall.OfflineReport);
            _offlinePopup.Show(OfflineTextUtil.TimeLine(seconds), body);
            KeepTopmost();
        }

#if UNITY_EDITOR || MOZZI_DEBUG
        // 디버그 「5분 건너뛰기」 뒤. 이벤트 없이 바뀐 더미 · 담장 수를 되읽고 팝업을 띄운다.
        private void OnDebugSkipped(double seconds)
        {
            RefreshAll();
            RefreshOpen();
            ShowOfflinePopup(seconds);
        }
#endif

        // 덮개는 빌려서 내용 칸 밑에 두고 처음엔 꺼 둔다.
        private T RentOverlay<T>(string key) where T : OverlayScreen
        {
            T screen = _resource.Rent<T>(key, _legacyContent);
            StretchToParent(screen.transform as RectTransform);
            screen.Bind(this);
            screen.gameObject.SetActive(false);
            _overlays.Add(screen);
            return screen;
        }

        // 배율은 CanvasScaler 의 자동 맞춤 대신 FitToScreen 이 직접 넣는다 — 안전 영역 기준이라야 해서다.
        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject(UiNames.CanvasRoot);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private RectTransform CreateContentRoot(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(_canvas.transform, false);
            return rect;
        }

        // 옛 360 기준 프리팹을 받는 껍데기. 자식은 부모 크기의 2/3 짜리 칸에 붙고 ×1.5 로 커져 부모를 꼭 채운다.
        private static RectTransform CreateLegacyRoot(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            return rect;
        }

        // 기준 540×960 이 안전 영역에 통째로 들어가는 배율을 넣고, 내용 칸 · 껍데기를 그 자리에 맞춘다.
        private void FitToScreen()
        {
            _fitWidth = Screen.width;
            _fitHeight = Screen.height;
            _fitSafeArea = ScreenFitUtil.SafeArea();

            _scaler.scaleFactor = ScreenFitUtil.CanvasScale();
            FitRoot(_contentRoot);
            FitLegacy(_legacyContent, ScreenFitUtil.ContentUiSize());
            // 팝업 껍데기는 캔버스 전체(어두운 막이 덮는 자리)에 맞춘다.
            FitLegacy(_legacyPopup, new Vector2(Screen.width, Screen.height) / ScreenFitUtil.CanvasScale());
#if UNITY_EDITOR || MOZZI_DEBUG
            if (_debugRoot != null)
            {
                FitRoot(_debugRoot);
                FitLegacy(_legacyDebug, ScreenFitUtil.ContentUiSize());
            }
#endif
        }

        // 부모(UI px 크기 parentSize) 가운데에 parentSize ÷ 1.5 칸을 두고 ×1.5 로 키운다.
        // 껍데기 안에서는 9:16 일 때 360×640, 길쭉한 폰이면 세로만 더 긴 옛 기준 칸이 된다.
        private static void FitLegacy(RectTransform rect, Vector2 parentSize)
        {
            float scale = LayoutConstUi.Legacy360Scale;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = parentSize / scale;
            rect.localScale = new Vector3(scale, scale, 1f);
        }

        // 앵커를 화면 비율로 주면 캔버스 배율과 상관없이 내용 칸 자리에 붙는다.
        private static void FitRoot(RectTransform rect)
        {
            Rect content = ScreenFitUtil.ContentScreenRect();
            rect.anchorMin = new Vector2(content.xMin / Screen.width, content.yMin / Screen.height);
            rect.anchorMax = new Vector2(content.xMax / Screen.width, content.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // 부재료 그림은 Resources 에 있으면 끼우고 없으면 null 이라 아이콘이 꺼진다.
        private void BindIngredientIcons()
        {
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                string key = _data.Ingredients.Get(ids[i]).SpriteKey;
                _farmScreen.SetIngredientIcon(ids[i], _resource.GetSprite(key));
            }
        }

        private void RefreshAll()
        {
            _farmScreen.SetGold(_wallet.Gold, false);
            _farmScreen.SetDay(_clock.Day, _clock.IsNight);
            _farmScreen.SetFence(_farm.MozziCount, _farm.FenceMax);
            RefreshUpgrade();

            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                RefreshIngredient(ids[i], false);
            }
        }

        private void RefreshIngredient(string ingredientId, bool bump)
        {
            string label = _data.Ingredients.Get(ingredientId).Name;
            _farmScreen.SetIngredient(ingredientId, label, _inventory.Count(ingredientId), _inventory.Cap(ingredientId), bump);
        }

        private void RefreshOpen()
        {
            if (_open != null)
            {
                _open.Refresh();
            }
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            _farmScreen.SetGold(evt.Gold, true);
            RefreshUpgrade();
            RefreshOpen();
        }

        private void OnIngredientChanged(IngredientChangedEvent evt)
        {
            RefreshIngredient(evt.IngredientId, evt.Delta > 0);
        }

        private void OnMozziBorn(MozziBornEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
            RefreshUpgrade();
        }

        private void OnMozziPlaced(MozziPlacedEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
            RefreshUpgrade();
        }

        private void OnMozziRemoved(MozziRemovedEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
            RefreshUpgrade();
        }

        private void OnFenceUpgraded(FenceUpgradedEvent evt)
        {
            _farmScreen.SetFence(_farm.MozziCount, evt.FenceMax);
            RefreshUpgrade();
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            _farmScreen.SetDay(_clock.Day, _clock.IsNight);
            _farmScreen.SetNight(false, true);
            RefreshOpen();
        }

        private void OnNightFell(NightFellEvent evt)
        {
            _farmScreen.SetDay(_clock.Day, _clock.IsNight);
            _farmScreen.SetNight(true, false);
            RefreshOpen();
        }

        private void OnStorageChanged(StorageChangedEvent evt)
        {
            RefreshOpen();
        }

        private void OnSynthesisStarted(SynthesisStartedEvent evt)
        {
            RefreshOpen();
        }

        private void OnSynthesisDone(SynthesisDoneEvent evt)
        {
            RefreshOpen();
        }

        // 받기 → 연출은 합성소 화면이 열려 있을 때만. 닫힌 채 받을 길은 없지만 안전하게 본다.
        private void OnSynthesisTaken(SynthesisTakenEvent evt)
        {
            RefreshOpen();
            if (_open == _synthesisScreen)
            {
                _synthesisScreen.PlayResult(evt.SpeciesId, evt.IsNew);
            }
        }

        private void OnMozziDiscovered(MozziDiscoveredEvent evt)
        {
            RefreshOpen();
        }

        private void OnStallOpened(StallOpenedEvent evt)
        {
            RefreshOpen();
        }

        private void OnCustomerArrived(CustomerArrivedEvent evt)
        {
            RefreshOpen();
        }

        private void OnCustomerServed(CustomerServedEvent evt)
        {
            RefreshOpen();
        }

        // 발견 연출은 가판대 화면이 Make 의 결과로 직접 띄운다 (떡메 흐름을 화면이 쥐고 있어서).
        private void OnRiceCakeMade(RiceCakeMadeEvent evt)
        {
            RefreshOpen();
        }

        private void OnRecipeDiscovered(RecipeDiscoveredEvent evt)
        {
            RefreshOpen();
        }
    }
}
