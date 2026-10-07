using LitMotion;
using Mozzi.Game.Vfx;
using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using Mozzi.Hubs;
using Mozzi.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 주방 화면 — 다섯 단계 (반복 08). 서랍장에서 재료 1~3개를 고르고 맷돌 3탭 → 절구 3탭 → 빚기판 → 찜기(기다리기)면 만든다.
    // 연출 : 맷돌이 한 바퀴씩 돌고 · 칠 때마다 반죽이 가루 → 거친 반죽 → 매끈한 떡 · 빚기판에서 빚는 떡 3장 ·
    //        찌는 동안 김이 되풀이되고, 다 찌면 찜기 뚜껑이 열리며 찜기 위에 결과.
    // 단계 규칙(다음 도구 · 연타 건너뛰기 · 해 본 조합 건너뛰기 · 찌기 시간)은 KitchenFlowAgent, 여기는 고르기 · 그림 · Make 호출만 든다.
    // 과정은 결과를 안 바꾼다 — StallManager.Make 는 그대로 부르고 부르는 때만 찌기가 끝나는 순간이다.
    // 차림표 판 · 해 본 조합 · 연타 건너뛰기는 바로 만들고 뚜껑을 짧게 연다. 배치는 KitchenScreenSetup · KitchenLayout.
    public sealed class KitchenScreen : OverlayScreen
    {
        private const string StallRowId = DataKeys.DefaultRowId;
        private const string PlainName = "평범한 떡";
        private const string LockedMark = "?";
        private const string MakeFailedNotice = "진열대가 가득 차서 떡을 못 만들었어요. 가판대에서 팔고 오세요";
        private const string WaitNextNotice = "한 번 더 눌러 다음으로";
        private const float FlashDuration = 0.4f;
        private const int MaxSelected = 3;

        // 서랍 칸 덧칠 : 칸 앞판이 서랍장 그림에 들어 있어 칸 Image 는 비워 두고(투명) 고른 칸 · 닫힌 칸에만 반투명 색을 덮는다.
        private static readonly Color DrawerFront = new Color(1f, 1f, 1f, 0f);
        private static readonly Color DrawerOn = new Color(1f, 0.85f, 0.3f, 0.55f);
        private static readonly Color DrawerLocked = new Color(0.2f, 0.12f, 0.08f, 0.45f);
        private static readonly Color IconEmpty = new Color(0.35f, 0.35f, 0.35f, 1f);

        private readonly List<KitchenJarItem> _jarItems = new List<KitchenJarItem>();
        private readonly List<Image> _bowlSlots = new List<Image>();
        private KitchenShelfView _shelf;
        private KitchenPanSlider _slider;
        // 결과 떡을 보인 뒤 아무 데나 한 번 누르면 칸1 로 돌아간다 (사용자 결정 — 자동 복귀 아님).
        private bool _backToPrep;
        private Image _cakeSpot;
        private GameObject _shapeDough;
        private RectTransform _doughRect;
        private Image _doughImage;
        private Vector2 _doughHome;
        private MotionHandle _fly;
        // 빈 MotionHandle 에 IsActive 를 묻지 않으려고 걸었는지 따로 쥔다.
        private bool _hasFly;
        private readonly List<Button> _finishTaps = new List<Button>();
        private readonly List<string> _selected = new List<string>();

        private IResourceHub _resource;
        private IStallHub _stall;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private KitchenFlowAgent _flow;
        private KitchenBlenderView _blender;
        // 「한 번 더」 안내를 기구 움직임이 끝난 순간에 바꾸려고 앞 프레임의 움직임 여부를 둔다.
        private bool _wasToolAnimating;
        private KitchenPound15View _mortar;
        private Image _mortarImage;
        private KitchenSteamerView _steamer;
        private Image _board;
        private KitchenOnceView _shape;
        private MakeResult _pendingResult;
        private bool _resultPending;
        private bool _makeFailed;
        private TMP_Text _hint;

        private RectTransform _bounceRect;
        private float _bounceTime;

        private GameObject _result;
        private Image _resultIcon;
        private TMP_Text _resultName;
        private GameObject _resultNew;
        private Image _flash;
        private float _flashTime;
        private bool _flashing;

        private RecipeMenuView _menu;

        protected override string PrefabName
        {
            get { return "KitchenScreen"; }
        }

        protected override string Title
        {
            get { return "주방"; }
        }

        protected override void BindChildren()
        {
            _stall = InGameHub.Get<IStallHub>();
            _codex = InGameHub.Get<ICodexHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _inventory = InGameHub.Get<IInventoryHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _resource = GlobalHub.Get<IResourceHub>();
            _flow = new KitchenFlowAgent(_data.Stalls.Get(StallRowId), KitchenLayout.SteamSec);

            BindDrawers();
            BindTools();

            _result = UiBindUtil.FindObject(transform, UiNames.KitchenResult, PrefabName);
            BindFinishTap(_result); // 결과 판도 「다음 누름」을 받는다 (BindTools 보다 늦게 찾으므로 여기서 붙인다).
            _resultIcon = UiBindUtil.Find<Image>(_result.transform, UiNames.CakeIcon, PrefabName);
            _resultName = UiBindUtil.Find<TMP_Text>(_result.transform, UiNames.KitchenResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_result.transform, UiNames.KitchenResultNewText, PrefabName);
            _flash = UiBindUtil.Find<Image>(transform, UiNames.KitchenResultFlash, PrefabName);
            _hint = UiBindUtil.Find<TMP_Text>(transform, UiNames.KitchenHintText, PrefabName);

            UiBindUtil.BindButton(transform, UiNames.KitchenMenuButton, PrefabName, ToggleMenu);
            // 아래 줄 「재료 팔기」 버튼 → 팔기 팝업.
            UiBindUtil.BindButton(transform, UiNames.KitchenSellAllButton, PrefabName, OnDrawerFrameTap);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);

            RectTransform strip = UiBindUtil.Find<RectTransform>(transform, UiNames.KitchenStrip, PrefabName);
            _slider = new KitchenPanSlider(strip, InGameHub.Get<IVfxHub>().Ui);
            _slider.SnapTo(KitchenLayout.PanPrep);

            _menu = new RecipeMenuView(transform, PrefabName, _resource, _data, _codex, _stall, OnMenuPick);
            ClearResult();
        }

        // 선반 병 열여덟 칸 한 쪽. 부재료 표 순서로 채우고, 열여덟을 넘으면 뷰가 쪽을 넘긴다. 병 누름 → 쪽×18+병 번째 재료.
        private void BindDrawers()
        {
            int n = UiNames.KitchenJarCount;
            Button[] jars = new Button[n];
            Image[] icons = new Image[n];
            TMP_Text[] counts = new TMP_Text[n];
            for (int i = 0; i < n; i++)
            {
                jars[i] = UiBindUtil.Find<Button>(transform, UiNames.KitchenJarPrefix + i, PrefabName);
                icons[i] = UiBindUtil.Find<Image>(jars[i].transform, UiNames.IngredientIcon, PrefabName);
                counts[i] = UiBindUtil.Find<TMP_Text>(jars[i].transform, UiNames.KitchenTrayCountText, PrefabName);
            }

            Image[] dots = new Image[UiNames.KitchenPageDotCount];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = UiBindUtil.Find<Image>(transform, UiNames.KitchenPageDotPrefix + i, PrefabName);
            }

            Button left = UiBindUtil.Find<Button>(transform, UiNames.KitchenArrowLeft, PrefabName);
            Button right = UiBindUtil.Find<Button>(transform, UiNames.KitchenArrowRight, PrefabName);
            _shelf = new KitchenShelfView(jars, icons, counts, left, right, dots,
                _resource.GetSprite(SpriteKeys.ScreenKitchenDotOn), _resource.GetSprite(SpriteKeys.ScreenKitchenDotOff), OnJar);
        }

        private void OnJar(int index)
        {
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            if (index < ids.Count)
            {
                OnToggle(ids[index]);
            }
        }

        private void BindTools()
        {
            // 믹서기 blender10 : 몸통(누름) 위 자식 겹에 재료 색 내용물을 돌린다. 쉬는 장 0, 누름마다 KitchenLayout.BlenderTapFrames.
            Button blender = EnsureButton(UiBindUtil.Find<Image>(transform, UiNames.KitchenBlender, PrefabName).gameObject);
            blender.onClick.AddListener(delegate { OnTool(KitchenTool.Mill); });
            Image blenderFill = UiBindUtil.Find<Image>(transform, UiNames.KitchenBlenderFill, PrefabName);
            Image[] blenderLayers = new Image[UiNames.KitchenBlenderLayerMax];
            Sprite[,,] layerSprites = new Sprite[SpriteKeys.KitchenBlenderLayerFrameCount, 2, UiNames.KitchenBlenderLayerMax];
            for (int layer = 0; layer < blenderLayers.Length; layer++)
            {
                blenderLayers[layer] = UiBindUtil.Find<Image>(transform, UiNames.KitchenBlenderLayerPrefix + layer, PrefabName);
                for (int frame = 0; frame < SpriteKeys.KitchenBlenderLayerFrameCount; frame++)
                {
                    for (int count = 2; count <= UiNames.KitchenBlenderLayerMax; count++)
                    {
                        layerSprites[frame, count - 2, layer] = layer < count ? _resource.GetSprite(SpriteKeys.KitchenBlenderLayer(frame, count, layer)) : null;
                    }
                }
            }

            Sprite[] fills = LoadFrames(SpriteKeys.KitchenBlenderFillCount, SpriteKeys.KitchenBlenderFill);
            _blender = new KitchenBlenderView(blenderFill, blenderLayers, fills, layerSprites);
            _blender.Stop();

            // 치기 : pound15 겹 다섯. 누르는 몸통은 Pound(투명 판) 이고 Button 도 거기 단다.
            _mortarImage = UiBindUtil.Find<Image>(transform, UiNames.KitchenPound, PrefabName);
            Button pound = EnsureButton(_mortarImage.gameObject);
            pound.onClick.AddListener(delegate { OnTool(KitchenTool.Mortar); });
            _mortar = new KitchenPound15View(
                UiBindUtil.Find<Image>(_mortarImage.transform, UiNames.KitchenPoundDough, PrefabName),
                UiBindUtil.Find<Image>(_mortarImage.transform, UiNames.KitchenPoundMallet, PrefabName),
                UiBindUtil.Find<Image>(_mortarImage.transform, UiNames.KitchenPoundDoughSpill, PrefabName),
                LoadPound15(SpriteKeys.KitchenPound15LayerMallet, false),
                LoadPound15(SpriteKeys.KitchenPound15LayerDough, false),
                LoadPound15(SpriteKeys.KitchenPound15LayerSpill, false),
                LoadPound15(SpriteKeys.KitchenPound15LayerMallet, true),
                LoadPound15(SpriteKeys.KitchenPound15LayerDough, true));
            _mortar.Reset();

            // 빚기 판. 결과를 본 뒤의 「다음 누름」도 여기서 받는다 (칸1 로 돌아가기).
            _board = UiBindUtil.Find<Image>(transform, UiNames.KitchenBoard9, PrefabName);
            Button board = EnsureButton(_board.gameObject);
            board.onClick.AddListener(OnBoardTap);

            // 찜기는 누르지 않는다 (기다리기만). 겹 7개 + 꼭지 김의 4초 연출은 KitchenSteamerView 가 한다.
            Transform steamerRoot = UiBindUtil.Find<RectTransform>(transform, UiNames.KitchenSteamer2, PrefabName);
            Image steamerBase = UiBindUtil.Find<Image>(steamerRoot, UiNames.KitchenSteamerBase, PrefabName);
            Image steam9 = UiBindUtil.Find<Image>(transform, UiNames.KitchenSteam9, PrefabName);
            Sprite[] steam = LoadFrames(SpriteKeys.KitchenSteam9FrameCount, SpriteKeys.KitchenSteam9);
            _steamer = new KitchenSteamerView(
                UiBindUtil.Find<Image>(steamerRoot, UiNames.KitchenSteamerLidClosed, PrefabName),
                UiBindUtil.Find<Image>(steamerRoot, UiNames.KitchenSteamerLidOpen, PrefabName),
                UiBindUtil.Find<Image>(steamerRoot, UiNames.KitchenSteamerDough, PrefabName),
                UiBindUtil.Find<Image>(steamerRoot, UiNames.KitchenSteamerFog, PrefabName),
                steam9, steam);

            Image dough = UiBindUtil.Find<Image>(transform, UiNames.KitchenShapeDough, PrefabName);
            _shapeDough = dough.gameObject;
            _doughRect = dough.rectTransform;
            _doughImage = dough;
            _doughHome = _doughRect.anchoredPosition;
            Sprite[] shape = LoadFrames(SpriteKeys.KitchenShape9FrameCount, SpriteKeys.KitchenShape9);
            _shape = new KitchenOnceView(dough, shape, KitchenLayout.Shape9FrameSec);

            _cakeSpot = UiBindUtil.Find<Image>(transform, UiNames.KitchenCakeSpot, PrefabName);

            // 결과를 본 뒤의 「다음 누름」은 칸3 어디를 눌러도 받는다 (배경 · 결과 떡 · 결과 판 · 찜기).
            Transform finishPan = UiBindUtil.Find<RectTransform>(transform, UiNames.KitchenPanPrefix + KitchenLayout.PanFinish, PrefabName);
            BindFinishTap(UiBindUtil.FindObject(finishPan, UiNames.KitchenPanBg, PrefabName));
            BindFinishTap(_cakeSpot.gameObject);
            BindFinishTap(steamerBase.gameObject);

            // 쟁반 위 고른 재료 자리 (옛 그릇 칸 이름 그대로 쟁반 안에 둔다).
            Transform tray = UiBindUtil.Find<RectTransform>(transform, UiNames.KitchenTray, PrefabName);
            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                _bowlSlots.Add(UiBindUtil.Find<Image>(tray, UiNames.KitchenMortarSlotPrefix + i, PrefabName));
            }
        }

        private void OnBoardTap()
        {
            if (_backToPrep)
            {
                // 결과 뒤의 누름은 빚기 판을 눌러도 같은 복귀다.
                OnFinishTap();
                return;
            }

            OnTool(KitchenTool.Board);
        }

        // 그림(Graphic)이 있는 칸3 조각에 누름을 붙인다. 결과가 떠 있을 때만 칸1 로 돌아가고, 그 밖에는 무시한다.
        private void BindFinishTap(GameObject go)
        {
            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic == null)
            {
                return;
            }

            graphic.raycastTarget = true;
            Button button = EnsureButton(go);
            button.onClick.AddListener(OnFinishTap);
            // 벽 · 찜기는 결과가 떠 있을 때만 누름을 받는다 (그 밖에 눌러도 아무 반응이 없게).
            button.interactable = false;
            _finishTaps.Add(button);
        }

        private void SetFinishTaps(bool on)
        {
            for (int i = 0; i < _finishTaps.Count; i++)
            {
                if (_finishTaps[i] != null)
                {
                    _finishTaps[i].interactable = on;
                }
            }
        }

        private void OnFinishTap()
        {
            if (_backToPrep == false)
            {
                return;
            }

            _backToPrep = false;
            // 찜기를 쉬는 모습으로 — IsOpen 이 남아 있으면 칸3 에 머문다.
            _steamer.Reset();
            ClearResult();
            // 결과 뒤 칸1 로 돌아오면 믹서기를 쉬는 장 0 으로 (8장 「다 갈린 모습」에 멈춰 있었다).
            SetIngredients(_selected);
            _blender.Stop();
            Refresh();
        }

        // 새 기구는 그림(Image)만 프리팹에 있다 — 누름은 여기서 붙인다 (배선은 코드에).
        private static Button EnsureButton(GameObject go)
        {
            Button button = go.GetComponent<Button>();
            if (button == null)
            {
                button = go.AddComponent<Button>();
            }

            // 눌려도 색이 변하지 않게 (기구의 눌림 반응은 통통 튀기가 맡는다).
            button.transition = Selectable.Transition.None;

            // 누름을 붙인 그림은 레이캐스트를 받아야 진짜 터치가 닿는다 (프리팹에서 꺼져 있어도 여기서 켠다).
            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }

            button.onClick.RemoveAllListeners();
            return button;
        }

        // 떡메 한 겹의 장 묶음. turn 이면 넘어감 둘(t12 · t23) × 4장, 아니면 단계 셋 × 8장.
        private Sprite[][] LoadPound15(string layer, bool turn)
        {
            int groups = turn ? SpriteKeys.KitchenPound15StageCount - 1 : SpriteKeys.KitchenPound15StageCount;
            int count = turn ? SpriteKeys.KitchenPound15TurnFrameCount : SpriteKeys.KitchenPound15StageFrameCount;
            Sprite[][] result = new Sprite[groups][];
            for (int g = 0; g < groups; g++)
            {
                int group = g;
                result[g] = LoadFrames(count, delegate(int i)
                {
                    return turn ? SpriteKeys.KitchenPound15Turn(layer, group, i) : SpriteKeys.KitchenPound15Stage(layer, group, i);
                });
            }

            return result;
        }

        private Sprite[] LoadFrames(int count, System.Func<int, string> key)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = _resource.GetSprite(key(i));
            }

            return frames;
        }

        protected override void UnbindChildren()
        {
            _jarItems.Clear();
            _shelf = null;
            if (_slider != null)
            {
                _slider.Cancel();
            }

            _slider = null;
            _cakeSpot = null;
            _shapeDough = null;
            _doughImage = null;
            _backToPrep = false;
            _bowlSlots.Clear();
            _selected.Clear();
            _flashing = false;
            _bounceRect = null;
            _flow = null;
            _blender = null;
            _wasToolAnimating = false;
            _mortar = null;
            _mortarImage = null;
            _steamer = null;
            _board = null;
            _shape = null;
            _resultPending = false;
            _makeFailed = false;
            _menu = null;

            _resource = null;
            _stall = null;
            _codex = null;
            _wallet = null;
            _inventory = null;
            _data = null;
            _ui = null;
        }

        // 아래 줄 「차림표」는 열려 있으면 닫는다 (판이 가림막을 거의 다 덮는다). 만드는 과정 중에는 안 연다.
        private void ToggleMenu()
        {
            if (_menu.IsOpen)
            {
                _menu.Close();
                return;
            }

            if (_flow.IsRunning)
            {
                return;
            }

            _menu.Open();
        }

        // 「닫기」 를 누르면 차림표 판부터 닫는다.
        public override bool CloseInner()
        {
            if (_menu.IsOpen == false)
            {
                return false;
            }

            _menu.Close();
            return true;
        }

        public override void Refresh()
        {
            SetGold(_wallet.Gold);
            RefreshDrawers();
            RefreshBowl();
            RefreshHint();
            if (_slider.Current != TargetPan())
            {
                _slider.SlideTo(TargetPan());
            }

            // 빚기 단계가 시작되면(칸3 도착) 첫 누름 전부터 반죽 덩어리(0장)를 판 위에 놓아 둔다.
            if (_flow.Step == KitchenStep.Shape && _flow.ShapeTaps == 0 && _shapeDough != null && !_shapeDough.activeSelf)
            {
                _shape.PlayHold(0, 0);
            }

            if (_menu.IsOpen)
            {
                _menu.Refresh();
            }
        }

        // 지금 단계가 보일 칸. 결과를 본 뒤에는 「다음 누름」까지 칸3 에 머문다.
        private int TargetPan()
        {
            if (_backToPrep || _resultPending || _steamer.IsOpen)
            {
                return KitchenLayout.PanFinish;
            }

            KitchenStep step = _flow.Step;
            if (step == KitchenStep.Pound)
            {
                return KitchenLayout.PanPound;
            }

            if (step == KitchenStep.Shape || step == KitchenStep.Steam)
            {
                return KitchenLayout.PanFinish;
            }

            return KitchenLayout.PanPrep;
        }

        // 재료 팔기 팝업. 만드는 중에는 안 띄우고, 팔 재료가 없으면 버튼을 끈다. 팝업 자체가 확인이라 한 번에 판다.
        private void OnDrawerFrameTap()
        {
            if (_flow.IsRunning)
            {
                return;
            }

            int price = _inventory.SellAllPrice();
            if (price <= 0)
            {
                _ui.OpenActionPopup("재료 선반", "팔 재료가 없어요", "비어 있음", false, null);
                return;
            }

            int count = 0;
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                count += _inventory.Count(ids[i]);
            }

            _ui.OpenActionPopup("재료 선반", "재료 " + count + "개를 모두 싸게 팔아요", "재료 전체 팔기 +" + price + "G", true, OnSellAll);
        }

        // 고르던 재료도 풀린다. 다시 그리기는 UiManager 가 한다.
        private void OnSellAll()
        {
            if (_flow.IsRunning)
            {
                return;
            }

            _inventory.SellAll();
            _selected.Clear();
        }

        public override void Process(float deltaTime)
        {
            // 찌기 시간은 실제 초로 잰다 (배속 · 일시정지와 상관없이 SteamSec 초).
            if (_flow.Tick(Time.realtimeSinceStartup) == KitchenFlowResult.Steamed)
            {
                MakeFromFlow(KitchenLayout.LidStepSec);
            }

            _blender.Process(deltaTime);
            _mortar.Process(deltaTime);
            _shape.Process(deltaTime);
            bool animating = IsToolAnimating();
            if (_wasToolAnimating && animating == false)
            {
                RefreshHint();
            }

            _wasToolAnimating = animating;
            // 찜기 연출은 흐름의 찌기 시간과 같은 실제 초로 돈다
            if (_steamer.Process(Time.unscaledDeltaTime) && _resultPending)
            {
                _resultPending = false;
                ShowResult(_pendingResult);
            }

            ProcessBounce(deltaTime);
            if (_flashing == false)
            {
                return;
            }

            _flashTime += deltaTime;
            float alpha = 1f - Mathf.Clamp01(_flashTime / FlashDuration);
            _flash.color = new Color(1f, 1f, 1f, alpha);
            if (alpha <= 0f)
            {
                _flashing = false;
            }
        }

        // 도중에 닫으면 흐름을 처음으로 (재료는 Make 때만 빠지니 잃는 것이 없다 — 찌는 중이어도 떡은 안 만들어진다).
        public override void OnClosed()
        {
            _flow.Reset();
            _slider.SnapTo(KitchenLayout.PanPrep);
            SetIngredients(new string[0]);
            _blender.Stop();
            _mortar.Reset();
            _steamer.Reset();
            StopBounce();
            _selected.Clear();
            _makeFailed = false;
            _flashing = false;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            ClearResult();
            _menu.Close();
        }

        // 고른 칸은 노랗게 덮고, 남은 수가 0 이면 그림을 어둡게. 다 골랐거나(3개) 과정 중이면 안 고른 칸은 못 누른다.
        private void RefreshDrawers()
        {
            bool full = _selected.Count >= MaxSelected;
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            _jarItems.Clear();
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                int count = _inventory.Count(id);
                bool on = _selected.Contains(id);
                KitchenJarItem item = new KitchenJarItem();
                item.Icon = IngredientSprite(id);
                item.CountText = count.ToString();
                item.Empty = count <= 0;
                item.Selected = on;
                item.Interactable = _flow.IsRunning == false && (on || (count > 0 && full == false));
                _jarItems.Add(item);
            }

            // 남는 칸은 잠긴 「?」 병 (못 누름) — 쪽을 다 채울 때까지.
            int fill = (ids.Count + UiNames.KitchenJarCount - 1) / UiNames.KitchenJarCount * UiNames.KitchenJarCount;
            for (int i = ids.Count; i < Mathf.Max(fill, UiNames.KitchenJarCount); i++)
            {
                KitchenJarItem locked = new KitchenJarItem();
                locked.CountText = LockedMark;
                locked.Locked = true;
                _jarItems.Add(locked);
            }

            _shelf.SetItems(_jarItems);
        }

        // 쟁반 위에 고른 재료 그림을 차례로.
        private void RefreshBowl()
        {
            for (int i = 0; i < _bowlSlots.Count; i++)
            {
                bool has = i < _selected.Count;
                UiBindUtil.SetActive(_bowlSlots[i].gameObject, has);
                if (has)
                {
                    _bowlSlots[i].sprite = IngredientSprite(_selected[i]);
                }
            }
        }

        private void RefreshHint()
        {
            // 3번째 움직임이 끝난 뒤에야 「한 번 더」 안내로 바꾼다. 도는 동안은 단계 안내(n/3)를 그대로 둔다.
            if (_flow.IsWaitingNext && IsToolAnimating() == false)
            {
                UiBindUtil.SetText(_hint, WaitNextNotice);
                return;
            }

            if (_flow.Step == KitchenStep.Grind)
            {
                UiBindUtil.SetText(_hint, "믹서기를 돌려 가세요 " + _flow.GrindTaps + "/" + _flow.TotalTaps);
                return;
            }

            if (_flow.Step == KitchenStep.Pound)
            {
                UiBindUtil.SetText(_hint, "절구를 치세요 " + _flow.PoundTaps + "/" + _flow.TotalTaps);
                return;
            }

            if (_flow.Step == KitchenStep.Shape)
            {
                UiBindUtil.SetText(_hint, "빚기판을 눌러 빚으세요 " + _flow.ShapeTaps + "/" + _flow.ShapeTotalTaps);
                return;
            }

            if (_flow.Step == KitchenStep.Steam)
            {
                UiBindUtil.SetText(_hint, "찜기에서 찌는 중이에요…");
                return;
            }

            if (_makeFailed)
            {
                UiBindUtil.SetText(_hint, MakeFailedNotice);
                return;
            }

            if (_backToPrep)
            {
                UiBindUtil.SetText(_hint, "화면을 누르면 다시 만들어요");
                return;
            }

            UiBindUtil.SetText(_hint, PickInfo());
        }

        // 고르기 단계 안내. 고른 게 있으면 「→ 찾음 / 해 본 조합 / 새 조합!」 뒤에 믹서기 안내를 붙인다.
        private string PickInfo()
        {
            if (_stall.Shelf.Count >= _stall.ShelfMax)
            {
                return "진열대가 가득 찼어요. 가판대에서 팔고 오세요";
            }

            if (_selected.Count == 0)
            {
                return "선반에서 재료를 1~3개 고르세요";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < _selected.Count; i++)
            {
                names.Add(_data.Ingredients.Get(_selected[i]).Name);
            }

            string head = string.Join("+", names);
            RecipeRow recipe = RecipeMatchUtil.FindRecipe(_data.Recipes, _selected);
            if (recipe != null && _codex.IsRecipeOpen(recipe.Id))
            {
                return head + " → " + recipe.Name + " (찾음) · 믹서기를 누르세요";
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return head + " → 해 본 조합 · 믹서기를 누르세요";
            }

            return head + " → 새 조합! 믹서기를 돌려 가세요";
        }

        private bool CanStart()
        {
            return _selected.Count > 0 && _stall.CanMake(_selected);
        }

        // 해 본 조합 · 찾은 조합이면 과정을 건너뛴다.
        private bool IsKnown()
        {
            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return true;
            }

            RecipeRow recipe = RecipeMatchUtil.FindRecipe(_data.Recipes, _selected);
            return recipe != null && _codex.IsRecipeOpen(recipe.Id);
        }

        private Sprite IngredientSprite(string ingredientId)
        {
            return _resource.GetSprite(_data.Ingredients.Get(ingredientId).SpriteKey);
        }

        // 과정 중에는 고르기를 받지 않는다. 닫힌 칸(id 없음)은 버튼이 꺼져 있어 여기 안 온다. 고르면 찜기 뚜껑이 닫히고 결과가 지워진다.
        private void OnToggle(string ingredientId)
        {
            if (ingredientId == null || _flow.IsRunning)
            {
                return;
            }

            if (_selected.Remove(ingredientId) == false && _selected.Count < MaxSelected)
            {
                _selected.Add(ingredientId);
            }

            // 쟁반에 올린 재료를 쉬는 장 0 의 층으로 바로 보여 준다 (칸3 반죽 색도 여기서 정해진다).
            SetIngredients(_selected);

            _makeFailed = false;
            ClearResult();
            Refresh();
        }

        // 도구 누름 하나. 판정은 흐름이, 그림(믹서기 · 떡메 · 빚기 · 김)과 Make 는 여기서.
        private void OnTool(KitchenTool tool)
        {
            // 기구 움직임 · 칸 밀림 · 반죽 날아가기가 도는 동안은 누름을 안 받는다 (반복 09).
            if (IsToolBusy())
            {
                return;
            }

            bool starting = _flow.IsRunning == false;
            KitchenFlowResult result = _flow.Press(tool, Time.realtimeSinceStartup, _selected, CanStart(), starting && IsKnown());
            if (result == KitchenFlowResult.Ignored)
            {
                return;
            }

            if (starting)
            {
                _makeFailed = false;
                ClearResult();
            }

            // 센 누름만 기구를 움직인다. 「한 번 더」 누름(GrindDone · PoundDone · Shaped)은 마지막 모습 그대로 다음으로 넘어간다.
            if (result == KitchenFlowResult.Ground || result == KitchenFlowResult.Pounded || result == KitchenFlowResult.ShapeTapped)
            {
                PlayTool(tool);
            }

            // 해 본 조합 : 바로 만들고 뚜껑을 짧게 연다.
            if (result == KitchenFlowResult.QuickMake)
            {
                PlayTool(tool);
                MakeFromFlow(KitchenLayout.LidQuickStepSec);
                return;
            }

            // 빚었으면 반죽을 찜기에 넣는 연출 뒤 김을 되풀이하며 기다린다 (찌는 시간은 반죽이 닿은 뒤부터 흐름이 실제 시간으로 잰다).
            if (result == KitchenFlowResult.Shaped)
            {
                StartFly();
            }

            Refresh();
        }

        private bool IsToolBusy()
        {
            return IsToolAnimating() || _slider.IsSliding || _hasFly;
        }

        // 기구(믹서기 · 떡메 · 빚기) 움직임만. 안내 글을 움직임 끝에 바꾸는 데 쓴다.
        private bool IsToolAnimating()
        {
            return _blender.IsPlaying || _mortar.IsPlaying || _shape.IsPlaying || _steamer.IsPlaying;
        }

        private void PlayTool(KitchenTool tool)
        {
            if (tool == KitchenTool.Mill)
            {
                // 흐름이 이미 이번 누름을 셌다 : GrindTaps 1~3 번째 묶음.
                _blender.Play(_flow.GrindTaps);
                return;
            }

            // 흐름이 이미 이번 탭을 셌다 : k 번째 타는 (k-1) 단계에서 쳐서 맞는 순간 k 단계가 된다.
            if (tool == KitchenTool.Mortar)
            {
                _mortar.Play(_flow.PoundTaps - 1);
                return;
            }

            if (tool == KitchenTool.Board)
            {
                // 누름 k 번째에 반죽 (k-1)*2 → (k-1)*2+1 장. 마지막 장에 멈춰 판 위에 남는다.
                StartBounce(_board.rectTransform);
                int from = (_flow.ShapeTaps - 1) * KitchenLayout.ShapeFramesPerTap;
                _shape.PlayHold(from, from + KitchenLayout.ShapeFramesPerTap - 1);
            }
        }

        // 빚은 반죽(마지막 장)을 판 위에서 찜 칸으로 포물선으로 던져 넣는다. 그동안 찜기는 열린 그림, 닿으면 닫고 김을 낸다.
        private void StartFly()
        {
            StopFly();
            _steamer.ShowOpenBody();
            Vector2 from = _doughHome;
            Vector2 delta = new Vector2(KitchenLayout.ShapeFlyToX - KitchenLayout.Shape9X, KitchenLayout.Shape9Bottom - KitchenLayout.ShapeFlyToY);
            _fly = LMotion.Create(0f, 1f, KitchenLayout.ShapeFlySec)
                .WithScheduler(InGameHub.Get<IVfxHub>().Ui)
                .WithOnComplete(OnFlyDone)
                .Bind(t =>
                {
                    float arc = 4f * KitchenLayout.ShapeFlyArc * t * (1f - t);
                    _doughRect.anchoredPosition = from + new Vector2(delta.x * t, delta.y * t + arc);
                    float scale = Mathf.Lerp(1f, KitchenLayout.ShapeFlyEndScale, t);
                    _doughRect.localScale = new Vector3(scale, scale, 1f);
                });
            _hasFly = true;
        }

        private void OnFlyDone()
        {
            _hasFly = false;
            ResetDough();
            UiBindUtil.SetActive(_shapeDough, false);
            _flow.RestartSteam(Time.realtimeSinceStartup);
            _steamer.Begin();
        }

        // 화면이 닫히거나 다시 시작할 때 : 날던 반죽을 끊고 자리 · 크기를 되돌린다.
        private void StopFly()
        {
            if (_hasFly && _fly.IsActive())
            {
                _fly.Cancel();
            }

            _hasFly = false;
            ResetDough();
        }

        // 믹서기 내용물과 칸3 반죽(놓임 · 빚기 · 날기 모두 같은 Image) 색을 같은 재료 목록으로 맞춘다.
        // 반죽 = lerp(흰색, 재료 평균색, DoughTintStrength). 재료가 없으면 흰색 그대로 (결과 뒤 칸1 복귀 · 닫기).
        private void SetIngredients(IList<string> ids)
        {
            _blender.SetIngredients(ids);
            Color dough = ids.Count == 0
                ? Color.white
                : Color.Lerp(Color.white, KitchenBlenderView.MixedColor(ids), KitchenLayout.DoughTintStrength);
            if (_mortar != null)
            {
                _mortar.SetDoughColor(dough);
            }

            if (_steamer != null)
            {
                _steamer.SetDough(dough);
            }

            if (_doughImage == null)
            {
                return;
            }

            _doughImage.color = dough;
        }

        private void ResetDough()
        {
            if (_doughRect == null)
            {
                return;
            }

            _doughRect.anchoredPosition = _doughHome;
            _doughRect.localScale = Vector3.one;
        }


        // 상자 한 번 통통 : scale 1 → BounceScale → 1 (BounceSec 동안 삼각).
        private void StartBounce(RectTransform rect)
        {
            StopBounce();
            _bounceRect = rect;
            _bounceTime = 0f;
        }

        private void StopBounce()
        {
            if (_bounceRect != null)
            {
                _bounceRect.localScale = Vector3.one;
            }

            _bounceRect = null;
        }

        private void ProcessBounce(float deltaTime)
        {
            if (_bounceRect == null)
            {
                return;
            }

            _bounceTime += deltaTime;
            float t = Mathf.Clamp01(_bounceTime / KitchenLayout.BounceSec);
            if (t >= 1f)
            {
                StopBounce();
                return;
            }

            float peak = 1f - Mathf.Abs(t * 2f - 1f);
            float scale = Mathf.Lerp(1f, KitchenLayout.BounceScale, peak);
            _bounceRect.localScale = new Vector3(scale, scale, 1f);
        }

        // 차림표 줄. 만드는 과정 중에는 받지 않는다 (고른 재료와 섞이지 않게). 결과는 찜기 위 (뚜껑 짧게).
        private void OnMenuPick(string recipeId)
        {
            if (_flow.IsRunning)
            {
                return;
            }

            _makeFailed = false;
            MakeAndShow(_data.Recipes.Get(recipeId).IngredientIds, KitchenLayout.LidQuickStepSec);
        }

        // 흐름이 끝났다 (다 쪘음 · 건너뛰기) : 흐름이 베껴 둔 재료로 만들고 절구를 처음 모습으로.
        private void MakeFromFlow(float lidStepSec)
        {
            List<string> ids = new List<string>(_flow.IngredientIds);
            _selected.Clear();
            _mortar.Reset();
            MakeAndShow(ids, lidStepSec);
        }

        // 만들기는 바로 하고(재료 · 진열대가 곧 바뀐다), 찜기 뚜껑이 다 열린 뒤 결과를 보인다.
        // 못 만들었으면(진열대 가득 등) 안내 글로 알리고 뚜껑은 안 연다.
        private void MakeAndShow(IReadOnlyList<string> ids, float lidStepSec)
        {
            ClearResult();
            MakeResult made = _stall.Make(ids);
            _makeFailed = made.Succeeded == false;
            if (made.Succeeded)
            {
                _pendingResult = made;
                _resultPending = true;
                _steamer.Open(lidStepSec);
            }

            Refresh();
        }

        // 찜기 위에 떡 그림 · 이름 · 값. 새 레시피면 「NEW」 와 흰 번쩍(알파 1→0).
        private void ShowResult(MakeResult made)
        {
            string name = PlainName;
            string spriteKey = SpriteKeys.CakePlain;
            if (made.IsPlain == false)
            {
                RecipeRow recipe = _data.Recipes.Get(made.RecipeId);
                name = recipe.Name;
                spriteKey = recipe.SpriteKey;
            }

            Sprite sprite = _resource.GetSprite(spriteKey);
            _resultIcon.sprite = sprite;
            UiBindUtil.SetActive(_resultIcon.gameObject, sprite != null);
            UiBindUtil.SetActive(_resultNew, made.IsNewRecipe);
            UiBindUtil.SetText(_resultName, name + "\n" + made.Price + "G");
            UiBindUtil.SetActive(_result, true);

            // 떡 그림은 결과 판 안에 하나만 (벽의 큰 결과 떡 CakeSpot 은 꺼 둔다 — 두 번 보이던 것을 뺐다). 다음 누름에 칸1 로 돌아간다.
            UiBindUtil.SetActive(_cakeSpot.gameObject, false);
            UiBindUtil.SetActive(_shapeDough, false);
            _backToPrep = true;
            SetFinishTaps(true);

            _flashing = made.IsNewRecipe;
            _flashTime = 0f;
            float alpha = 0f;
            if (_flashing)
            {
                alpha = 1f;
            }

            _flash.color = new Color(1f, 1f, 1f, alpha);
            // 결과가 뜬 뒤의 안내(「화면을 누르면 다시 만들어요」)는 _backToPrep 을 켠 다음에 다시 써야 맞는다.
            RefreshHint();
        }

        // 결과를 지우고 찜기 뚜껑을 닫는다. 빚는 중이면 빚기도 거둔다.
        private void ClearResult()
        {
            UiBindUtil.SetActive(_result, false);
            UiBindUtil.SetActive(_cakeSpot.gameObject, false);
            UiBindUtil.SetActive(_shapeDough, false);
            StopFly();
            _backToPrep = false;
            SetFinishTaps(false);
            _shape.Stop();
            _resultPending = false;
        }
    }
}
