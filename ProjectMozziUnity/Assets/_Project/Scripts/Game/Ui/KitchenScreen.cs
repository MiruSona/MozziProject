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
        private const float FlashDuration = 0.4f;
        private const int MaxSelected = 3;

        // 서랍 칸 덧칠 : 칸 앞판이 서랍장 그림에 들어 있어 칸 Image 는 비워 두고(투명) 고른 칸 · 닫힌 칸에만 반투명 색을 덮는다.
        // 도구는 다음 차례만 제 색이고 나머지는 Dim 을 곱한다.
        private static readonly Color DrawerFront = new Color(1f, 1f, 1f, 0f);
        private static readonly Color DrawerOn = new Color(1f, 0.85f, 0.3f, 0.55f);
        private static readonly Color DrawerLocked = new Color(0.2f, 0.12f, 0.08f, 0.45f);
        private static readonly Color IconEmpty = new Color(0.35f, 0.35f, 0.35f, 1f);
        private const float Dim = 0.5f;

        private readonly List<Button> _drawerCells = new List<Button>();
        private readonly List<Image> _drawerIcons = new List<Image>();
        private readonly List<TMP_Text> _drawerCounts = new List<TMP_Text>();
        private readonly List<string> _drawerIds = new List<string>();
        private readonly List<Image> _bowlSlots = new List<Image>();
        private readonly List<string> _selected = new List<string>();

        private IResourceHub _resource;
        private IStallHub _stall;
        private ICodexHub _codex;
        private IWalletHub _wallet;
        private IInventoryHub _inventory;
        private IStaticDataHub _data;
        private IUiHub _ui;

        private KitchenFlowAgent _flow;
        private KitchenMortarView _mill;
        private Image _millImage;
        private KitchenMortarView _mortar;
        private Image _mortarImage;
        private Image _steamer;
        private Image _steamerLid;
        private KitchenLidView _lid;
        private Image _board;
        private KitchenOnceView _steam;
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
            _resultIcon = UiBindUtil.Find<Image>(_result.transform, UiNames.CakeIcon, PrefabName);
            _resultName = UiBindUtil.Find<TMP_Text>(_result.transform, UiNames.KitchenResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_result.transform, UiNames.KitchenResultNewText, PrefabName);
            _flash = UiBindUtil.Find<Image>(transform, UiNames.KitchenResultFlash, PrefabName);
            _hint = UiBindUtil.Find<TMP_Text>(transform, UiNames.KitchenHintText, PrefabName);

            UiBindUtil.BindButton(transform, UiNames.KitchenMenuButton, PrefabName, ToggleMenu);
            // 서랍장 틀(칸 밖)을 누르면 「재료 전체 팔기」 팝업. 칸 누름은 재료 고르기라 틀만 쓴다.
            UiBindUtil.BindButton(transform, UiNames.KitchenDrawerFrame, PrefabName, OnDrawerFrameTap);
            UiBindUtil.BindButton(transform, UiNames.StallRecipeCodexButton, PrefabName, _ui.OpenRecipeCodex);
            UiBindUtil.BindButton(transform, UiNames.KitchenDoorButton, PrefabName, _ui.OpenStall);

            _menu = new RecipeMenuView(transform, PrefabName, _resource, _data, _codex, _stall, OnMenuPick);
            ClearResult();
        }

        // 서랍 칸 열둘. 앞 칸부터 부재료 표 순서로 채우고, 남는 칸은 닫힌 「?」 칸(못 누름)이다.
        private void BindDrawers()
        {
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < UiNames.KitchenDrawerCellCount; i++)
            {
                string id = null;
                if (i < ids.Count)
                {
                    id = ids[i];
                }

                Button cell = UiBindUtil.BindButton(transform, UiNames.KitchenDrawerCellPrefix + i, PrefabName,
                    delegate { OnToggle(id); });
                Image icon = UiBindUtil.Find<Image>(cell.transform, UiNames.IngredientIcon, PrefabName);
                icon.sprite = null;
                if (id != null)
                {
                    icon.sprite = IngredientSprite(id);
                }

                UiBindUtil.SetActive(icon.gameObject, icon.sprite != null);
                _drawerCells.Add(cell);
                _drawerIcons.Add(icon);
                _drawerCounts.Add(UiBindUtil.Find<TMP_Text>(cell.transform, UiNames.KitchenTrayCountText, PrefabName));
                _drawerIds.Add(id);
            }
        }

        private void BindTools()
        {
            Button mill = UiBindUtil.BindButton(transform, UiNames.KitchenMill, PrefabName,
                delegate { OnTool(KitchenTool.Mill); });
            _millImage = mill.image;
            Sprite[][] millFrames = { LoadFrames(SpriteKeys.KitchenMillFrameCount, SpriteKeys.KitchenMill) };
            _mill = new KitchenMortarView(_millImage, millFrames, KitchenLayout.MillFrameSec, 0);
            _mill.SetIdle(0);

            Button mortar = UiBindUtil.BindButton(transform, UiNames.KitchenMortar, PrefabName,
                delegate { OnTool(KitchenTool.Mortar); });
            _mortarImage = mortar.image;
            Sprite[][] stages = new Sprite[SpriteKeys.KitchenPoundStageCount][];
            for (int s = 0; s < stages.Length; s++)
            {
                int stage = s;
                stages[s] = LoadFrames(SpriteKeys.KitchenPoundFrameCount, delegate(int i) { return SpriteKeys.KitchenPound(stage, i); });
            }

            _mortar = new KitchenMortarView(_mortarImage, stages, KitchenLayout.PoundFrameSec, KitchenLayout.PoundHitFrame);
            _mortar.SetIdle(0);

            _board = UiBindUtil.BindButton(transform, UiNames.KitchenBoard, PrefabName,
                delegate { OnTool(KitchenTool.Board); }).image;

            // 찜기는 누르지 않는다 (기다리기만) — 몸통 · 뚜껑은 그림만 찾는다.
            _steamer = UiBindUtil.Find<Image>(transform, UiNames.KitchenSteamer, PrefabName);
            _steamerLid = UiBindUtil.Find<Image>(transform, UiNames.KitchenSteamerLid, PrefabName);
            _lid = new KitchenLidView(_steamerLid.rectTransform);

            Sprite[] steam = LoadFrames(SpriteKeys.KitchenSteamFrameCount, SpriteKeys.KitchenSteam);
            _steam = new KitchenOnceView(UiBindUtil.Find<Image>(transform, UiNames.KitchenSteam, PrefabName), steam, KitchenLayout.SteamFrameSec);

            Sprite[] shape = LoadFrames(SpriteKeys.KitchenShapeFrameCount, SpriteKeys.KitchenShape);
            _shape = new KitchenOnceView(UiBindUtil.Find<Image>(transform, UiNames.KitchenShape, PrefabName), shape, KitchenLayout.ShapeFrameSec);

            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                _bowlSlots.Add(UiBindUtil.Find<Image>(transform, UiNames.KitchenMortarSlotPrefix + i, PrefabName));
            }
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
            _drawerCells.Clear();
            _drawerIcons.Clear();
            _drawerCounts.Clear();
            _drawerIds.Clear();
            _bowlSlots.Clear();
            _selected.Clear();
            _flashing = false;
            _bounceRect = null;
            _flow = null;
            _mill = null;
            _millImage = null;
            _mortar = null;
            _mortarImage = null;
            _steamer = null;
            _steamerLid = null;
            _lid = null;
            _board = null;
            _steam = null;
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
            RefreshTools();
            RefreshHint();
            if (_menu.IsOpen)
            {
                _menu.Refresh();
            }
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
                _ui.OpenActionPopup("재료 서랍", "팔 재료가 없어요", "비어 있음", false, null);
                return;
            }

            int count = 0;
            IReadOnlyList<string> ids = _inventory.IngredientIds;
            for (int i = 0; i < ids.Count; i++)
            {
                count += _inventory.Count(ids[i]);
            }

            _ui.OpenActionPopup("재료 서랍", "재료 " + count + "개를 모두 싸게 팔아요", "재료 전체 팔기 +" + price + "G", true, OnSellAll);
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
            // 찌기 시간은 실제 초로 잰다 (배속 · 일시정지와 상관없이 2.5초).
            if (_flow.Tick(Time.realtimeSinceStartup) == KitchenFlowResult.Steamed)
            {
                _steam.Stop();
                MakeFromFlow(KitchenLayout.LidStepSec);
            }

            _mill.Process(deltaTime);
            _mortar.Process(deltaTime);
            _steam.Process(deltaTime);
            _shape.Process(deltaTime);
            if (_lid.Process(deltaTime) && _resultPending)
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
            _mill.Stop();
            _mortar.SetIdle(0);
            _mortar.Stop();
            _steam.Stop();
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
            for (int i = 0; i < _drawerCells.Count; i++)
            {
                Button cell = _drawerCells[i];
                string id = _drawerIds[i];
                if (id == null)
                {
                    cell.image.color = DrawerLocked;
                    cell.interactable = false;
                    UiBindUtil.SetText(_drawerCounts[i], LockedMark);
                    continue;
                }

                int count = _inventory.Count(id);
                bool on = _selected.Contains(id);
                cell.image.color = DrawerFront;
                if (on)
                {
                    cell.image.color = DrawerOn;
                }

                cell.interactable = _flow.IsRunning == false && (on || (count > 0 && full == false));
                _drawerIcons[i].color = Color.white;
                if (count <= 0)
                {
                    _drawerIcons[i].color = IconEmpty;
                }

                UiBindUtil.SetText(_drawerCounts[i], count.ToString());
            }
        }

        // 그릇 위에 고른 재료 그림을 차례로.
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

        // 다음에 누를 도구만 제 색, 나머지는 어둡게 (「여기 눌러」). 찜기는 찌는 중 · 뚜껑이 열린 동안 제 색이다.
        private void RefreshTools()
        {
            KitchenTool next = _flow.NextTool(CanStart());
            _millImage.color = Lit(Color.white, next == KitchenTool.Mill);
            _mortarImage.color = Lit(Color.white, next == KitchenTool.Mortar);
            _board.color = Lit(Color.white, next == KitchenTool.Board);
            bool steaming = _flow.Step == KitchenStep.Steam || _lid.IsOpen;
            _steamer.color = Lit(Color.white, steaming);
            _steamerLid.color = _steamer.color;
        }

        private static Color Lit(Color color, bool on)
        {
            if (on)
            {
                return color;
            }

            return new Color(color.r * Dim, color.g * Dim, color.b * Dim, color.a);
        }

        private void RefreshHint()
        {
            if (_flow.Step == KitchenStep.Grind)
            {
                UiBindUtil.SetText(_hint, "맷돌을 돌려 가세요 " + _flow.GrindTaps + "/" + _flow.TotalTaps);
                return;
            }

            if (_flow.Step == KitchenStep.Pound)
            {
                UiBindUtil.SetText(_hint, "절구를 치세요 " + _flow.PoundTaps + "/" + _flow.TotalTaps);
                return;
            }

            if (_flow.Step == KitchenStep.Shape)
            {
                UiBindUtil.SetText(_hint, "빚기판을 눌러 빚으세요");
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

            UiBindUtil.SetText(_hint, PickInfo());
        }

        // 고르기 단계 안내. 고른 게 있으면 「→ 찾음 / 해 본 조합 / 새 조합!」 뒤에 맷돌 안내를 붙인다.
        private string PickInfo()
        {
            if (_stall.Shelf.Count >= _stall.ShelfMax)
            {
                return "진열대가 가득 찼어요. 가판대에서 팔고 오세요";
            }

            if (_selected.Count == 0)
            {
                return "서랍에서 재료를 1~3개 고르세요";
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
                return head + " → " + recipe.Name + " (찾음) · 맷돌을 누르세요";
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return head + " → 해 본 조합 · 맷돌을 누르세요";
            }

            return head + " → 새 조합! 맷돌을 돌려 가세요";
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

            _makeFailed = false;
            ClearResult();
            Refresh();
        }

        // 도구 누름 하나. 판정은 흐름이, 그림(맷돌 · 떡메 · 빚기 · 김)과 Make 는 여기서.
        private void OnTool(KitchenTool tool)
        {
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

            PlayTool(tool);

            // 해 본 조합 · 연타 건너뛰기 : 바로 만들고 뚜껑을 짧게 연다.
            if (result == KitchenFlowResult.Make || result == KitchenFlowResult.QuickMake)
            {
                MakeFromFlow(KitchenLayout.LidQuickStepSec);
                return;
            }

            // 빚었으면 찌기 시작 — 김을 되풀이하며 기다린다 (Process 가 실제 시간으로 잰다).
            if (result == KitchenFlowResult.Shaped)
            {
                _steam.PlayLoop();
            }

            Refresh();
        }

        private void PlayTool(KitchenTool tool)
        {
            if (tool == KitchenTool.Mill)
            {
                _mill.Play(0, 0);
                return;
            }

            // 흐름이 이미 이번 탭을 셌다 : k 번째 타는 (k-1) 단계에서 쳐서 맞는 순간 k 단계가 된다.
            if (tool == KitchenTool.Mortar)
            {
                int taps = _flow.PoundTaps;
                _mortar.Play(KitchenLayout.PoundStageAfter(taps - 1), KitchenLayout.PoundStageAfter(taps));
                return;
            }

            if (tool == KitchenTool.Board)
            {
                StartBounce(_board.rectTransform);
                _shape.Play();
            }
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
            _mortar.SetIdle(0);
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
                _lid.Open(lidStepSec);
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

            _flashing = made.IsNewRecipe;
            _flashTime = 0f;
            float alpha = 0f;
            if (_flashing)
            {
                alpha = 1f;
            }

            _flash.color = new Color(1f, 1f, 1f, alpha);
            RefreshTools();
        }

        // 결과를 지우고 찜기 뚜껑을 닫는다. 빚는 중이면 빚기도 거둔다.
        private void ClearResult()
        {
            UiBindUtil.SetActive(_result, false);
            _shape.Stop();
            _resultPending = false;
            _lid.Close();
        }
    }
}
