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

        private Canvas _canvas;
        private FarmScreen _farmScreen;
        private SynthesisScreen _synthesisScreen;
        private MozziCodexScreen _codexScreen;
        private StorageScreen _storageScreen;
        private StallScreen _stallScreen;
        private RecipeCodexScreen _recipeCodexScreen;
        private OverlayScreen _open;
        private OfflinePopup _offlinePopup;
#if UNITY_EDITOR || MOZZI_DEBUG
        private DebugPanel _debugPanel;
#endif

        public bool IsAnyOpen
        {
            get { return _open != null; }
        }

        public void Init()
        {
            _resource = GlobalHub.Get<IResourceHub>();
            _canvas = CreateCanvas();
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

            _farmScreen = _resource.Rent<FarmScreen>(PrefabKeys.UiFarmScreen, _canvas.transform);
            StretchToParent(_farmScreen.transform as RectTransform);
            _farmScreen.Bind(_inventory.IngredientIds, this, _clock.AdvanceDay);
            BindIngredientIcons();

            _synthesisScreen = RentOverlay<SynthesisScreen>(PrefabKeys.UiSynthesisScreen);
            _codexScreen = RentOverlay<MozziCodexScreen>(PrefabKeys.UiMozziCodexScreen);
            _storageScreen = RentOverlay<StorageScreen>(PrefabKeys.UiStorageScreen);
            _stallScreen = RentOverlay<StallScreen>(PrefabKeys.UiStallScreen);
            _recipeCodexScreen = RentOverlay<RecipeCodexScreen>(PrefabKeys.UiRecipeCodexScreen);

            _offlinePopup = _resource.Rent<OfflinePopup>(PrefabKeys.UiOfflinePopup, _canvas.transform);
            StretchToParent(_offlinePopup.transform as RectTransform);
            _offlinePopup.Bind(_offlinePopup.Hide);
            _offlinePopup.Hide();

#if UNITY_EDITOR || MOZZI_DEBUG
            _debugPanel = _resource.Rent<DebugPanel>(PrefabKeys.UiDebugPanel, _canvas.transform);
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
            _offlinePopup.transform.SetAsLastSibling();
#if UNITY_EDITOR || MOZZI_DEBUG
            _debugPanel.transform.SetAsLastSibling();
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

        // 덮개는 빌려서 Canvas 밑에 두고 처음엔 꺼 둔다.
        private T RentOverlay<T>(string key) where T : OverlayScreen
        {
            T screen = _resource.Rent<T>(key, _canvas.transform);
            StretchToParent(screen.transform as RectTransform);
            screen.Bind(this);
            screen.gameObject.SetActive(false);
            _overlays.Add(screen);
            return screen;
        }

        // 기준 360×640 · 세로 기준으로 맞춘다 (아트규칙 4번).
        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject(UiNames.CanvasRoot);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LayoutConstUi.ReferenceWidth, LayoutConstUi.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
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
                string key = _data.Ingredients.Get(ids[i]).spriteKey;
                _farmScreen.SetIngredientIcon(ids[i], _resource.GetSprite(key));
            }
        }

        private void RefreshAll()
        {
            _farmScreen.SetGold(_wallet.Gold, false);
            _farmScreen.SetDay(_clock.Day, _clock.IsNight);
            _farmScreen.SetFence(_farm.MozziCount, _farm.FenceMax);

            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                RefreshIngredient(ids[i], false);
            }
        }

        private void RefreshIngredient(string ingredientId, bool bump)
        {
            string label = _data.Ingredients.Get(ingredientId).name;
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
            RefreshOpen();
        }

        private void OnIngredientChanged(IngredientChangedEvent evt)
        {
            RefreshIngredient(evt.IngredientId, evt.Delta > 0);
        }

        private void OnMozziBorn(MozziBornEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
        }

        private void OnMozziPlaced(MozziPlacedEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
        }

        private void OnMozziRemoved(MozziRemovedEvent evt)
        {
            _farmScreen.SetFence(evt.FarmCount, _farm.FenceMax);
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
