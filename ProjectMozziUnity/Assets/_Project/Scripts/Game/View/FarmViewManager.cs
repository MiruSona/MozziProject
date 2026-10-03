using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Farm;
using Mozzi.Game.Stall;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Ui;
using Mozzi.Hubs;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Mozzi.Game.View
{
    // 농장 화면의 월드 쪽. IFarmHub 의 찹쌀이 · 더미를 Object 와 맞추고 건물 둘을 놓고 탭을 받는다.
    // 찹쌀이 · 더미는 매 프레임 목록 대조로 Rent · Return 하고, 건물 알림 점만 이벤트로 갱신한다.
    // 찹쌀이 탭·드래그 판정은 MozziDragAgent 가 하고, 놓았을 때 할 일(보관함 넣기 · 농장에 놓기)은 여기서 한다.
    public sealed class FarmViewManager : IManager, IProcess, IFarmViewHub
    {
        private const int GridColumns = 5;
        // 보관함에서 끌어낸 임시 마리의 id. 농장 id 는 1부터라 안 겹친다.
        private const int GhostMozziId = -1;
        private const float GridJitter = 0.35f;
        // 더미 줄이 담장 안쪽 바닥에서 뜨는 높이. 그림 아래 수량 글자가 앞 담장에 안 걸리게 잡았다.
        private const float PileRowUnits = 1.8f;
        // 변종 테두리(1.2배)까지 안 겹치게 반지름 합에 이만큼 곱한다.
        private const float SeparateMargin = LayoutConst.ShinyBorderScale;

        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();

        private readonly Dictionary<int, MozziObject> _mozziObjects = new Dictionary<int, MozziObject>();
        private readonly Dictionary<string, PileObject> _pileObjects = new Dictionary<string, PileObject>();
        private readonly List<MozziObject> _mozziList = new List<MozziObject>();
        private readonly List<int> _removeIds = new List<int>();
        private readonly List<string> _removePiles = new List<string>();
        private readonly Dictionary<string, Vector2> _pileSpots = new Dictionary<string, Vector2>();

        // 종 id → 레이어 그림 묶음. 24종이 스프라이트를 찾는 일을 한 번씩만 하게 담아 둔다.
        private readonly Dictionary<string, MozziSpriteSet> _spriteSets = new Dictionary<string, MozziSpriteSet>();

        private IResourceHub _resource;
        private IFarmHub _farm;
        private IStaticDataHub _data;
        private IUiHub _ui;
        private ISynthesisHub _synthesis;
        private IStallHub _stall;
        private IStorageHub _storage;

        private Transform _root;
        private Camera _camera;
        private int _fitWidth;
        private int _fitHeight;
        private Rect _fitSafeArea;
        private MozziDragAgent _drag;
        private bool _pressWasDown;
        private MozziObject _ghost;
        // 다음에 새로 놓이는 마리가 설 자리. 드래그로 놓은 것이 그 자리에 나오게.
        private bool _hasPendingStart;
        private Vector2 _pendingStart;
        private IFarmGroundPainter _groundPainter;
        private BuildingObject _synthesisBuilding;
        private BuildingObject _stallBuilding;
        private int _placedCount;

        public void Init()
        {
            _root = new GameObject("FarmView").transform;
            _camera = Camera.main;
            FitCamera();
            _resource = GlobalHub.Get<IResourceHub>();
            _drag = new MozziDragAgent(this, _camera);

            _groundPainter = new TilemapFarmGroundPainter();
            _groundPainter.Paint(_root, _resource);
        }

        public void PostInit()
        {
            _farm = InGameHub.Get<IFarmHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _synthesis = InGameHub.Get<ISynthesisHub>();
            _stall = InGameHub.Get<IStallHub>();
            _storage = InGameHub.Get<IStorageHub>();

            LayoutPileSpots();
            PlaceBuildings();
            SyncMozzis();
            SyncPiles();
            RefreshBadges();

            _subscriptions.Add(EventBus.Subscribe<SynthesisDoneEvent>(this, OnSynthesisDone));
            _subscriptions.Add(EventBus.Subscribe<SynthesisTakenEvent>(this, OnSynthesisTaken));
            _subscriptions.Add(EventBus.Subscribe<StallOpenedEvent>(this, OnStallOpened));
            _subscriptions.Add(EventBus.Subscribe<CustomerArrivedEvent>(this, OnCustomerArrived));
            _subscriptions.Add(EventBus.Subscribe<CustomerServedEvent>(this, OnCustomerServed));
            _subscriptions.Add(EventBus.Subscribe<NightFellEvent>(this, OnNightFell));
            _subscriptions.Add(EventBus.Subscribe<DayStartedEvent>(this, OnDayStarted));
        }

        public void Process(float deltaTime)
        {
            if (Screen.width != _fitWidth || Screen.height != _fitHeight || ScreenFitUtil.SafeArea() != _fitSafeArea)
            {
                FitCamera();
            }

            SyncMozzis();
            SyncPiles();

            for (int i = 0; i < _mozziList.Count; i++)
            {
                _mozziList[i].Step(deltaTime);
            }

            Separate();

            foreach (PileObject pile in _pileObjects.Values)
            {
                pile.Step(deltaTime);
            }

            if (_drag.IsActive)
            {
                _drag.Step();
            }
            else
            {
                ReadTap();
            }
        }

        // 플레이를 멈출 때는 Unity 가 먼저 부수므로 이미 없는 것은 반납하지 않는다.
        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();

            if (_drag != null)
            {
                _drag.Cancel();
            }

            ReturnIfAlive(_ghost);
            _ghost = null;

            foreach (MozziObject mozzi in _mozziObjects.Values)
            {
                ReturnIfAlive(mozzi);
            }

            foreach (PileObject pile in _pileObjects.Values)
            {
                ReturnIfAlive(pile);
            }

            _mozziObjects.Clear();
            _pileObjects.Clear();
            _mozziList.Clear();
            _spriteSets.Clear();

            ReturnIfAlive(_synthesisBuilding);
            ReturnIfAlive(_stallBuilding);

            if (_groundPainter != null)
            {
                _groundPainter.Clear();
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        // 카메라를 캔버스와 같은 배율로 맞춘다. 월드 540 폭이 내용 칸 폭과 같고, 월드 위끝이 내용 칸 위끝에 온다.
        // 그래서 자원바 · 건물 띠 · 농장이 위에서부터 붙고, 길쭉한 폰에서 남는 자리는 농장 아래로 간다.
        private void FitCamera()
        {
            _fitWidth = Screen.width;
            _fitHeight = Screen.height;
            _fitSafeArea = ScreenFitUtil.SafeArea();

            // 화면 px 하나의 월드 길이 (유닛).
            float unitsPerPx = LayoutConst.UiToWorldPx / (ScreenFitUtil.CanvasScale() * LayoutConst.PixelsPerUnit);
            Rect content = ScreenFitUtil.ContentScreenRect();

            _camera.orthographicSize = Screen.height * unitsPerPx * 0.5f;

            float x = (Screen.width * 0.5f - content.center.x) * unitsPerPx;
            float y = LayoutConst.HalfHeight - (content.yMax - Screen.height * 0.5f) * unitsPerPx;
            _camera.transform.position = new Vector3(x, y, _camera.transform.position.z);
        }

        private void ReturnIfAlive<T>(T obj) where T : Component
        {
            if (obj == null)
            {
                return;
            }

            _resource.Return(obj);
        }

        private void PlaceBuildings()
        {
            float x = LayoutConst.BuildingCenterX;
            float y = LayoutConst.BuildingCenterY;

            _synthesisBuilding = _resource.Rent<BuildingObject>(PrefabKeys.Building, _root);
            _synthesisBuilding.Setup(BuildingKind.Synthesis, _resource.GetSprite(SpriteKeys.BuildingSynthesis), new Vector2(-x, y));

            _stallBuilding = _resource.Rent<BuildingObject>(PrefabKeys.Building, _root);
            _stallBuilding.Setup(BuildingKind.Stall, _resource.GetSprite(SpriteKeys.BuildingStall), new Vector2(x, y));
        }

        // 합성소는 Done 칸이 하나라도 있으면, 가판대는 열려 있고 줄이 있으면 점을 켠다.
        private void RefreshBadges()
        {
            bool anyDone = false;
            for (int i = 0; i < _synthesis.SlotCount; i++)
            {
                if (_synthesis.GetSlot(i).status == SynthesisSlotStatus.Done)
                {
                    anyDone = true;
                    break;
                }
            }

            _synthesisBuilding.SetBadge(anyDone);
            _stallBuilding.SetBadge(_stall.IsOpen && _stall.Queue.Count >= 1);
        }

        private void OnSynthesisDone(SynthesisDoneEvent evt)
        {
            RefreshBadges();
        }

        private void OnSynthesisTaken(SynthesisTakenEvent evt)
        {
            RefreshBadges();
        }

        private void OnStallOpened(StallOpenedEvent evt)
        {
            RefreshBadges();
        }

        private void OnCustomerArrived(CustomerArrivedEvent evt)
        {
            RefreshBadges();
        }

        private void OnCustomerServed(CustomerServedEvent evt)
        {
            RefreshBadges();
        }

        private void OnNightFell(NightFellEvent evt)
        {
            RefreshBadges();
        }

        private void OnDayStarted(DayStartedEvent evt)
        {
            RefreshBadges();
        }

        // 더미 자리는 부재료마다 고정. 담장 아래쪽 한 줄이다.
        private void LayoutPileSpots()
        {
            IReadOnlyList<IngredientRow> rows = _data.Ingredients.All;
            Rect area = LayoutConst.FarmRect;
            float y = area.yMin + PileRowUnits;
            float step = area.width / (rows.Count + 1);

            for (int i = 0; i < rows.Count; i++)
            {
                float x = area.xMin + step * (i + 1);
                _pileSpots[rows[i].Id] = new Vector2(x, y);
            }
        }

        private void SyncMozzis()
        {
            IReadOnlyList<FarmMozzi> mozzis = _farm.Mozzis;
            for (int i = 0; i < mozzis.Count; i++)
            {
                FarmMozzi mozzi = mozzis[i];
                if (_mozziObjects.ContainsKey(mozzi.Id))
                {
                    continue;
                }

                MozziSpeciesRow species = _data.Species.Get(mozzi.SpeciesId);
                MozziObject obj = _resource.Rent<MozziObject>(PrefabKeys.Mozzi, _root);
                obj.Setup(mozzi.Id, species, SpriteSetOf(species), WanderRect(), TakeStart());
                _mozziObjects[mozzi.Id] = obj;
                _mozziList.Add(obj);
            }

            if (_mozziObjects.Count == mozzis.Count)
            {
                return;
            }

            _removeIds.Clear();
            foreach (int id in _mozziObjects.Keys)
            {
                if (HasMozzi(mozzis, id) == false)
                {
                    _removeIds.Add(id);
                }
            }

            for (int i = 0; i < _removeIds.Count; i++)
            {
                MozziObject obj = _mozziObjects[_removeIds[i]];
                _mozziObjects.Remove(_removeIds[i]);
                _mozziList.Remove(obj);
                _resource.Return(obj);
            }
        }

        private MozziSpriteSet SpriteSetOf(MozziSpeciesRow species)
        {
            MozziSpriteSet set;
            if (_spriteSets.TryGetValue(species.Id, out set))
            {
                return set;
            }

            set = MozziSpriteSet.Load(_resource, species);
            _spriteSets[species.Id] = set;
            return set;
        }

        private static bool HasMozzi(IReadOnlyList<FarmMozzi> mozzis, int id)
        {
            for (int i = 0; i < mozzis.Count; i++)
            {
                if (mozzis[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        // 담장 안에서 담장 두께와 더미 줄을 뺀 자리다. 옆 담장은 찹쌀이 앞이라 좌우를 더 뺀다.
        private static Rect WanderRect()
        {
            Rect area = LayoutConst.FarmRect;
            float side = LayoutConst.FenceUnits * 0.75f;
            float top = LayoutConst.FenceUnits * 0.5f;
            return Rect.MinMaxRect(
                area.xMin + side,
                area.yMin + LayoutConst.FenceUnits + 1.3f,
                area.xMax - side,
                area.yMax - top);
        }

        // 드래그로 놓은 자리가 있으면 그것을 한 번 쓰고, 없으면 격자 자리다.
        private Vector2 TakeStart()
        {
            if (_hasPendingStart == false)
            {
                return NextStart();
            }

            _hasPendingStart = false;
            return _pendingStart;
        }

        // 격자 + 흔들림. 20마리가 처음부터 겹치지 않게 놓는다.
        private Vector2 NextStart()
        {
            Rect area = WanderRect();
            int col = _placedCount % GridColumns;
            int row = (_placedCount / GridColumns) % 4;
            _placedCount++;

            float cellW = area.width / GridColumns;
            float cellH = area.height / 4f;
            float x = area.xMin + cellW * (col + 0.5f) + Random.Range(-GridJitter, GridJitter);
            float y = area.yMax - cellH * (row + 0.5f) + Random.Range(-GridJitter, GridJitter);
            return new Vector2(x, y);
        }

        private void SyncPiles()
        {
            foreach (KeyValuePair<string, int> pair in _farm.Piles)
            {
                PileObject pile;
                bool has = _pileObjects.TryGetValue(pair.Key, out pile);

                if (pair.Value <= 0)
                {
                    continue;
                }

                if (has == false)
                {
                    IngredientRow row = _data.Ingredients.Get(pair.Key);
                    pile = _resource.Rent<PileObject>(PrefabKeys.IngredientPile, _root);
                    pile.Setup(row, _resource.GetSprite(row.SpriteKey), _pileSpots[pair.Key]);
                    _pileObjects[pair.Key] = pile;
                }

                pile.SetCount(pair.Value);
            }

            _removePiles.Clear();
            foreach (KeyValuePair<string, PileObject> pair in _pileObjects)
            {
                if (_farm.PileOf(pair.Key) <= 0 && pair.Value.IsAnimating == false)
                {
                    _removePiles.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removePiles.Count; i++)
            {
                _resource.Return(_pileObjects[_removePiles[i]]);
                _pileObjects.Remove(_removePiles[i]);
            }
        }

        // 반지름 합 × SeparateMargin 보다 가까우면 서로 반씩 밀어낸다. 20마리라 n² 이어도 가볍다.
        private void Separate()
        {
            for (int i = 0; i < _mozziList.Count; i++)
            {
                for (int j = i + 1; j < _mozziList.Count; j++)
                {
                    MozziObject a = _mozziList[i];
                    MozziObject b = _mozziList[j];
                    if (a.IsDragging || b.IsDragging)
                    {
                        continue;
                    }

                    Vector2 diff = (Vector2)b.transform.position - (Vector2)a.transform.position;
                    float minDist = (a.Radius + b.Radius) * SeparateMargin;
                    float dist = diff.magnitude;
                    if (dist >= minDist)
                    {
                        continue;
                    }

                    Vector2 push;
                    if (dist < 0.001f)
                    {
                        push = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    }
                    else
                    {
                        push = diff / dist;
                    }

                    float overlap = (minDist - dist) * 0.5f;
                    a.Nudge(-push * overlap);
                    b.Nudge(push * overlap);
                }
            }
        }

        // 마우스 · 터치 둘 다 Pointer 로 받는다. UI 위 탭은 EventSystem 이 먹는다.
        // 누름 가장자리는 isPressed 를 지난 프레임과 견줘 직접 잡는다 — wasPressedThisFrame 은 에디터에서 입력 갱신이
        // 한 프레임에 두 번 돌면(파이프라인 simulate_pointer 등) Process 가 보기 전에 꺼진다.
        private void ReadTap()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            bool down = pointer.press.isPressed;
            bool justPressed = down && _pressWasDown == false;
            _pressWasDown = down;
            if (justPressed == false)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 screen = pointer.position.ReadValue();
            TapAtScreen(screen);
        }

        // 디버그·시험에서도 부를 수 있게 화면 좌표를 받는다.
        public void TapAtScreen(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            TapAtWorld(new Vector2(world.x, world.y));
        }

        public void TapAtWorld(Vector2 world)
        {
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null)
            {
                return;
            }

            PileObject pile = hit.GetComponent<PileObject>();
            if (pile != null)
            {
                OnPileTapped(pile);
                return;
            }

            BuildingObject building = hit.GetComponent<BuildingObject>();
            if (building != null)
            {
                OnBuildingTapped(building);
                return;
            }

            MozziObject mozzi = hit.GetComponent<MozziObject>();
            if (mozzi != null)
            {
                _drag.Press(mozzi, _camera.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)));
            }
        }

        // 누르고 안 움직이고 뗐다. 표정이 잠깐 바뀌고 팝업이 뜬다 (피드백 5 · 팝업은 그대로).
        public void OnMozziTapped(MozziObject mozzi)
        {
            if (mozzi == null || mozzi.IsDead)
            {
                return;
            }

            mozzi.ShowRandomFace(LayoutConst.FaceHoldSec);
            _ui.OpenMozziPopup(mozzi.MozziId);
        }

        // 농장 찹쌀이를 놓았다. 보관함 버튼 위면 넣고, 아니면 담장 안 그 자리에 둔다 (피드백 6·7).
        public void OnFarmMozziDropped(MozziObject mozzi, Vector2 screen)
        {
            if (mozzi == null || mozzi.IsDead)
            {
                return;
            }

            mozzi.EndDrag();
            if (_ui.IsOverStorageDrop(screen))
            {
                _storage.TakeFromFarm(mozzi.MozziId);
            }
        }

        // 보관함에서 끌어낸 임시 마리를 놓았다. 담장 안이면 그 자리에 진짜 마리가 놓이고, 밖이면 보관함 화면으로 돌아간다.
        public void OnPlaceGhostDropped(MozziObject ghost, string speciesId, Vector2 world)
        {
            ReturnGhost(ghost);

            bool placed = false;
            if (WanderRect().Contains(world))
            {
                _hasPendingStart = true;
                _pendingStart = world;
                placed = _storage.PlaceToFarm(speciesId);
                _hasPendingStart = false;
            }

            if (placed)
            {
                SyncMozzis();
                return;
            }

            _ui.OpenStorage();
        }

        public void OnPlaceGhostCancelled(MozziObject ghost, string speciesId)
        {
            ReturnGhost(ghost);
            _ui.OpenStorage();
        }

        // IFarmViewHub. 보관함 화면이 자기를 닫은 뒤 부른다. 포인터 자리에 임시 마리를 띄워 바로 든다.
        public void BeginPlaceDrag(string speciesId)
        {
            if (_drag.IsActive || _ghost != null)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));

            MozziSpeciesRow species = _data.Species.Get(speciesId);
            _ghost = _resource.Rent<MozziObject>(PrefabKeys.Mozzi, _root);
            _ghost.Setup(GhostMozziId, species, SpriteSetOf(species), WanderRect(), new Vector2(world.x, world.y));
            _drag.BeginPlace(_ghost, speciesId);
        }

        private void ReturnGhost(MozziObject ghost)
        {
            if (ghost != null && ghost.IsDead == false)
            {
                _resource.Return(ghost);
            }

            _ghost = null;
        }

        // 건물마다 제 화면을 연다.
        public void OnBuildingTapped(BuildingObject building)
        {
            if (building.Kind == BuildingKind.Synthesis)
            {
                _ui.OpenSynthesis();
                return;
            }

            _ui.OpenStall();
        }

        public void OnPileTapped(PileObject pile)
        {
            if (pile.IsAnimating)
            {
                return;
            }

            int moved = _farm.PickUp(pile.IngredientId);
            if (moved > 0)
            {
                pile.PlayPicked();
            }
        }
    }
}
