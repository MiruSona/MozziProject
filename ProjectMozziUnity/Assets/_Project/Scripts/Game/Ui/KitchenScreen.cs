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
    // 주방 화면 — 네 단계 (반복 06 상자 판 → 반복 07 정식 그림). 서랍장에서 재료 1~3개를 고르고 찜기 → 절구 3탭 → 빚기판 순서로 누르면 만든다.
    // 연출 : 찜기를 누르면 김 한 바퀴 · 칠 때마다 반죽이 쌀 → 거친 반죽 → 매끈한 떡 · 빚기판에서 빚는 떡 3장 뒤 접시에 결과.
    // 단계 규칙(다음 도구 · 연타 건너뛰기 · 해 본 조합 건너뛰기)은 KitchenFlowAgent, 여기는 고르기 · 그림 · Make 호출만 든다.
    // 과정은 결과를 안 바꾼다 — StallManager.Make 는 그대로 부르고 부르는 때만 마지막 단계 끝이다.
    // 차림표 판에서는 찾은 떡을 한 번에 만든다. 결과는 빚기판 위 접시에 보인다. 배치는 KitchenScreenSetup · KitchenLayout.
    public sealed class KitchenScreen : OverlayScreen
    {
        private const string StallRowId = DataKeys.DefaultRowId;
        private const string PlainName = "평범한 떡";
        private const string LockedMark = "?";
        private const float FlashDuration = 0.4f;
        private const int MaxSelected = 3;

        // 서랍 칸 그림에 곱하는 색 : 기본(그림 그대로) · 고른 칸(노랑) · 닫힌 칸(어둡게). 도구는 다음 차례만 제 색이고 나머지는 Dim 을 곱한다.
        private static readonly Color DrawerFront = Color.white;
        private static readonly Color DrawerOn = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color DrawerLocked = new Color(0.45f, 0.36f, 0.28f, 1f);
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
        private KitchenMortarView _mortar;
        private Image _mortarImage;
        private Image _steamer;
        private Image _board;
        private KitchenOnceView _steam;
        private KitchenOnceView _shape;
        private GameObject _plate;
        private MakeResult _shapedResult;
        private bool _shapedPending;
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
            _flow = new KitchenFlowAgent(_data.Stalls.Get(StallRowId));

            BindDrawers();
            BindTools();

            _result = UiBindUtil.FindObject(transform, UiNames.KitchenResult, PrefabName);
            _resultIcon = UiBindUtil.Find<Image>(_result.transform, UiNames.CakeIcon, PrefabName);
            _resultName = UiBindUtil.Find<TMP_Text>(_result.transform, UiNames.KitchenResultNameText, PrefabName);
            _resultNew = UiBindUtil.FindObject(_result.transform, UiNames.KitchenResultNewText, PrefabName);
            _flash = UiBindUtil.Find<Image>(transform, UiNames.KitchenResultFlash, PrefabName);
            _hint = UiBindUtil.Find<TMP_Text>(transform, UiNames.KitchenHintText, PrefabName);

            UiBindUtil.BindButton(transform, UiNames.KitchenMenuButton, PrefabName, ToggleMenu);
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
            _steamer = UiBindUtil.BindButton(transform, UiNames.KitchenSteamer, PrefabName,
                delegate { OnTool(KitchenTool.Steamer); }).image;
            _board = UiBindUtil.BindButton(transform, UiNames.KitchenBoard, PrefabName,
                delegate { OnTool(KitchenTool.Board); }).image;

            Button mortar = UiBindUtil.BindButton(transform, UiNames.KitchenMortar, PrefabName,
                delegate { OnTool(KitchenTool.Mortar); });
            _mortarImage = mortar.image;
            Sprite[][] stages = new Sprite[SpriteKeys.KitchenPoundStageCount][];
            for (int s = 0; s < stages.Length; s++)
            {
                stages[s] = new Sprite[SpriteKeys.KitchenPoundFrameCount];
                for (int i = 0; i < SpriteKeys.KitchenPoundFrameCount; i++)
                {
                    stages[s][i] = _resource.GetSprite(SpriteKeys.KitchenPound(s, i));
                }
            }

            _mortar = new KitchenMortarView(_mortarImage, stages, KitchenLayout.PoundFrameSec);
            _mortar.SetIdle(0);

            Sprite[] steam = new Sprite[SpriteKeys.KitchenSteamFrameCount];
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = _resource.GetSprite(SpriteKeys.KitchenSteam(i));
            }

            _steam = new KitchenOnceView(UiBindUtil.Find<Image>(transform, UiNames.KitchenSteam, PrefabName), steam, KitchenLayout.SteamFrameSec);

            Sprite[] shape = new Sprite[SpriteKeys.KitchenShapeFrameCount];
            for (int i = 0; i < shape.Length; i++)
            {
                shape[i] = _resource.GetSprite(SpriteKeys.KitchenShape(i));
            }

            _shape = new KitchenOnceView(UiBindUtil.Find<Image>(transform, UiNames.KitchenShape, PrefabName), shape, KitchenLayout.ShapeFrameSec);
            _plate = UiBindUtil.FindObject(transform, UiNames.KitchenPlate, PrefabName);

            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                _bowlSlots.Add(UiBindUtil.Find<Image>(transform, UiNames.KitchenMortarSlotPrefix + i, PrefabName));
            }
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
            _mortar = null;
            _mortarImage = null;
            _steamer = null;
            _board = null;
            _steam = null;
            _shape = null;
            _plate = null;
            _shapedPending = false;
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

        public override void Process(float deltaTime)
        {
            _mortar.Process(deltaTime);
            _steam.Process(deltaTime);
            if (_shape.Process(deltaTime))
            {
                ShowShaped();
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

        // 도중에 닫으면 흐름을 처음으로 (재료는 Make 때만 빠지니 잃는 것이 없다).
        public override void OnClosed()
        {
            _flow.Reset();
            _mortar.SetIdle(0);
            _mortar.Stop();
            _steam.Stop();
            StopBounce();
            _selected.Clear();
            _flashing = false;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            ClearResult();
            _menu.Close();
        }

        // 고른 칸은 노랗게, 남은 수가 0 이면 그림을 어둡게. 다 골랐거나(3개) 과정 중이면 안 고른 칸은 못 누른다.
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

        // 다음에 누를 도구만 제 색, 나머지는 어둡게 (「여기 눌러」).
        private void RefreshTools()
        {
            KitchenTool next = _flow.NextTool(CanStart());
            _steamer.color = Lit(Color.white, next == KitchenTool.Steamer);
            _board.color = Lit(Color.white, next == KitchenTool.Board);
            _mortarImage.color = Lit(Color.white, next == KitchenTool.Mortar);
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

            UiBindUtil.SetText(_hint, PickInfo());
        }

        // 고르기 단계 안내. 고른 게 있으면 「→ 찾음 / 해 본 조합 / 새 조합!」 뒤에 찜기 안내를 붙인다.
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
                return head + " → " + recipe.Name + " (찾음) · 찜기를 누르세요";
            }

            if (_codex.HasTriedCombo(RecipeMatchUtil.ComboKey(_selected)))
            {
                return head + " → 해 본 조합 · 찜기를 누르세요";
            }

            return head + " → 새 조합! 찜기를 눌러 찌세요";
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

        // 과정 중에는 고르기를 받지 않는다. 닫힌 칸(id 없음)은 버튼이 꺼져 있어 여기 안 온다.
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

            ClearResult();
            Refresh();
        }

        // 도구 누름 하나. 판정은 흐름이, 그림(통통 · 김 · 떡메 · 빚기)과 Make 는 여기서.
        private void OnTool(KitchenTool tool)
        {
            bool starting = _flow.IsRunning == false;
            KitchenFlowResult result = _flow.Press(tool, Time.realtimeSinceStartup, _selected, CanStart(), starting && IsKnown());
            if (result == KitchenFlowResult.Ignored)
            {
                return;
            }

            PlayTool(tool);
            if (result == KitchenFlowResult.Steamed)
            {
                ClearResult();
            }

            // 해 본 조합 : 떡메를 한 번만 — 쌀에서 맞는 순간 바로 매끈한 떡이 된다. 결과는 빚기 없이 곧바로.
            if (result == KitchenFlowResult.QuickMake)
            {
                _mortar.Play(0, KitchenLayout.PoundStageLast);
            }

            if (result == KitchenFlowResult.Make || result == KitchenFlowResult.QuickMake)
            {
                List<string> ids = new List<string>(_flow.IngredientIds);
                _selected.Clear();
                _mortar.SetIdle(0);
                MakeAndShow(ids, result == KitchenFlowResult.Make);
                return;
            }

            Refresh();
        }

        private void PlayTool(KitchenTool tool)
        {
            // 흐름이 이미 이번 탭을 셌다 : k 번째 타는 (k-1) 단계에서 쳐서 맞는 순간 k 단계가 된다.
            if (tool == KitchenTool.Mortar)
            {
                int taps = _flow.PoundTaps;
                _mortar.Play(KitchenLayout.PoundStageAfter(taps - 1), KitchenLayout.PoundStageAfter(taps));
                return;
            }

            if (tool == KitchenTool.Steamer)
            {
                StartBounce(_steamer.rectTransform);
                _steam.Play();
                return;
            }

            if (tool == KitchenTool.Board)
            {
                StartBounce(_board.rectTransform);
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

        // 차림표 줄. 만드는 과정 중에는 받지 않는다 (고른 재료와 섞이지 않게).
        private void OnMenuPick(string recipeId)
        {
            if (_flow.IsRunning)
            {
                return;
            }

            MakeAndShow(_data.Recipes.Get(recipeId).IngredientIds, false);
        }

        // 만들기는 바로 하고(재료 · 진열대가 곧 바뀐다), shaping 이면 빚는 떡 3장이 다 돈 뒤에 접시 · 결과를 보인다.
        private void MakeAndShow(IReadOnlyList<string> ids, bool shaping)
        {
            ClearResult();
            MakeResult made = _stall.Make(ids);
            Refresh();
            if (made.Succeeded == false)
            {
                return;
            }

            if (shaping == false)
            {
                ShowResult(made);
                return;
            }

            _shapedResult = made;
            _shapedPending = true;
            UiBindUtil.SetActive(_plate, false);
            _shape.Play();
        }

        private void ShowShaped()
        {
            UiBindUtil.SetActive(_plate, true);
            if (_shapedPending)
            {
                ShowResult(_shapedResult);
            }

            _shapedPending = false;
        }

        // 접시 위에 떡 그림 · 이름 · 값. 새 레시피면 「NEW」 와 흰 번쩍(알파 1→0).
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
        }

        // 빚는 중이면 빚기도 거둔다 (접시는 다시 보인다).
        private void ClearResult()
        {
            UiBindUtil.SetActive(_result, false);
            _shape.Stop();
            _shapedPending = false;
            UiBindUtil.SetActive(_plate, true);
        }
    }
}
