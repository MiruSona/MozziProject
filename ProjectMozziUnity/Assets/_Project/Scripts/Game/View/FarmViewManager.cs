using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game.Clock;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Ui;
using Mozzi.Hubs;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mozzi.Game.View
{
    // 농장 화면의 월드 쪽. IFarmHub 의 찹쌀이를 Object 와 맞추고 건물 셋을 놓고 누르기를 받는다.
    // 찹쌀이는 매 프레임 목록 대조로 Rent · Return 하고, 건물 알림 점만 이벤트로 갱신한다.
    // 바닥 재료는 FloorItemsAgent, 누르기 · 훑기는 FarmSweepAgent, 찹쌀이 탭·드래그는 MozziDragAgent 가 맡고
    // 놓았을 때 할 일(보관함 넣기 · 농장에 놓기)은 여기서 한다.
    public sealed class FarmViewManager : IManager, IProcess, IFarmViewHub
    {
        // 보관함에서 끌어낸 임시 마리의 id. 농장 id 는 1부터라 안 겹친다.
        private const int GhostMozziId = -1;
        // 시작 자리를 뽑을 때 다른 마리와 이만큼(반지름 합 배율) 떨어뜨린다. 이 횟수 안에 못 찾으면 마지막 자리를 쓴다.
        private const float StartGapMargin = 1.2f;
        private const int StartPickTries = 20;
        // 찹쌀이가 걷는 자리를 울타리 안쪽에서 이만큼 줄인다 (유닛). 옆은 앞 울타리에 반쯤 걸치게.
        // 아래 0.6 은 발(루트 −18px)이 아래 울타리 줄 바로 위에 닿는 값이다.
        private const float WanderBottomUnits = 0.6f;
        private const float WanderSideUnits = 0.3f;
        private const float WanderTopUnits = 0.1f;
        // 변종 테두리(1.2배)까지 안 겹치게 반지름 합에 이만큼 곱한다.
        private const float SeparateMargin = LayoutConst.ShinyBorderScale;

        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();

        private readonly Dictionary<int, MozziObject> _mozziObjects = new Dictionary<int, MozziObject>();
        private readonly List<MozziObject> _mozziList = new List<MozziObject>();
        // 보관함 궤짝으로 뛰어 들어가는 중인 마리. 데이터는 이미 빠져 목록 대조에서 떼어 두고 연출이 끝나면 반납한다.
        private readonly List<MozziObject> _leaving = new List<MozziObject>();
        private readonly List<int> _removeIds = new List<int>();

        // 종 id → 레이어 그림 묶음. 24종이 스프라이트를 찾는 일을 한 번씩만 하게 담아 둔다.
        private readonly Dictionary<string, MozziSpriteSet> _spriteSets = new Dictionary<string, MozziSpriteSet>();

        private IResourceHub _resource;
        private IFarmHub _farm;
        private IStaticDataHub _data;
        private IUiHub _ui;
        private ISynthesisHub _synthesis;
        private IStallHub _stall;
        private IStorageHub _storage;
        private IInventoryHub _inventory;

        private Transform _root;
        private Camera _camera;
        private int _fitWidth;
        private int _fitHeight;
        private Rect _fitSafeArea;
        private MozziDragAgent _drag;
        private FarmSweepAgent _sweep;
        private FloorItemsAgent _floor;
        private MozziObject _ghost;
        // 다음에 새로 놓이는 마리가 설 자리. 드래그로 놓은 것이 그 자리에 나오게.
        private bool _hasPendingStart;
        private Vector2 _pendingStart;
        private IFarmGroundPainter _groundPainter;
        private FarmDecorView _decor;
        private SkyView _sky;
        private BuildingObject _synthesisBuilding;
        private BuildingObject _stallBuilding;
        private BuildingObject _storageBuilding;

        public void Init()
        {
            _root = new GameObject("FarmView").transform;
            _camera = Camera.main;
            FitCamera();
            _resource = GlobalHub.Get<IResourceHub>();
            _drag = new MozziDragAgent(this, _camera);
            _sweep = new FarmSweepAgent(this, _camera);

            _groundPainter = new SpriteFarmGroundPainter();
            _groundPainter.Paint(_root, _resource);

            _sky = new SkyView();
            _sky.Build(_root, _resource, _camera);
        }

        public void PostInit()
        {
            _farm = InGameHub.Get<IFarmHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _ui = InGameHub.Get<IUiHub>();
            _synthesis = InGameHub.Get<ISynthesisHub>();
            _stall = InGameHub.Get<IStallHub>();
            _storage = InGameHub.Get<IStorageHub>();
            _inventory = InGameHub.Get<IInventoryHub>();

            // 장식은 표가 있어야 놓을 수 있어 땅(Init)과 달리 여기서 깐다.
            _decor = new FarmDecorView();
            _decor.Build(_root, _resource, _data.FarmDecors.All);

            _floor = new FloorItemsAgent(this, _resource, _farm, _data, _root, FloorRect());

            PlaceBuildings();
            SyncMozzis();
            RefreshBadges();

            _subscriptions.Add(EventBus.Subscribe<SynthesisDoneEvent>(this, OnSynthesisDone));
            _subscriptions.Add(EventBus.Subscribe<SynthesisTakenEvent>(this, OnSynthesisTaken));
            _subscriptions.Add(EventBus.Subscribe<StallOpenedEvent>(this, OnStallOpened));
            _subscriptions.Add(EventBus.Subscribe<CustomerArrivedEvent>(this, OnCustomerArrived));
            _subscriptions.Add(EventBus.Subscribe<CustomerServedEvent>(this, OnCustomerServed));
            _subscriptions.Add(EventBus.Subscribe<NightFellEvent>(this, OnNightFell));
            _subscriptions.Add(EventBus.Subscribe<DayStartedEvent>(this, OnDayStarted));
            _subscriptions.Add(EventBus.Subscribe<IngredientDroppedEvent>(this, OnIngredientDropped));
        }

        public void Process(float deltaTime)
        {
            if (Screen.width != _fitWidth || Screen.height != _fitHeight || ScreenFitUtil.SafeArea() != _fitSafeArea)
            {
                FitCamera();
            }

            SyncMozzis();

            for (int i = 0; i < _mozziList.Count; i++)
            {
                _mozziList[i].Step(deltaTime);
            }

            Separate();
            StepLeaving(deltaTime);
            _storageBuilding.Step(deltaTime);
            _floor.Step(deltaTime);

            if (_drag.IsActive)
            {
                _drag.Step();
            }
            else
            {
                _sweep.Step();
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

            for (int i = 0; i < _leaving.Count; i++)
            {
                ReturnIfAlive(_leaving[i]);
            }

            _leaving.Clear();

            if (_sweep != null)
            {
                _sweep.Cancel();
            }

            if (_floor != null)
            {
                _floor.Clear();
            }

            _mozziObjects.Clear();
            _mozziList.Clear();
            _spriteSets.Clear();

            ReturnIfAlive(_synthesisBuilding);
            ReturnIfAlive(_stallBuilding);
            ReturnIfAlive(_storageBuilding);

            if (_groundPainter != null)
            {
                _groundPainter.Clear();
            }

            if (_decor != null)
            {
                _decor.Clear();
            }

            if (_sky != null)
            {
                _sky.Clear();
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        // 카메라를 캔버스와 같은 배율로 맞춘다. 월드 540 폭이 내용 칸 폭과 같고, 월드 아래끝이 내용 칸 아래끝에 온다.
        // 그래서 농장 · 길 · 건물이 아래에서부터 붙고, 길쭉한 폰에서 남는 자리는 월드 위(하늘, SkyView)로 간다.
        // 9:16 에서는 내용 칸 높이가 월드 960 과 같아 위끝 맞춤과 똑같다.
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
            float y = -LayoutConst.HalfHeight + (Screen.height * 0.5f - content.yMin) * unitsPerPx;
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

        // 건물 셋은 BuildingLayout 의 자리에 선다.
        private void PlaceBuildings()
        {
            _synthesisBuilding = RentBuilding(BuildingKind.Synthesis, SpriteKeys.BuildingSynthesis, "합성소");
            _stallBuilding = RentBuilding(BuildingKind.Stall, SpriteKeys.BuildingStall, "가판대");
            _storageBuilding = RentBuilding(BuildingKind.Storage, SpriteKeys.BuildingStorage, "보관함");
        }

        private BuildingObject RentBuilding(BuildingKind kind, string spriteKey, string label)
        {
            BuildingObject building = _resource.Rent<BuildingObject>(PrefabKeys.Building, _root);
            building.Setup(kind, _resource.GetSprite(spriteKey), _resource.GetSprite(SpriteKeys.UiBadgeAlert), label);
            return building;
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

        // 찹쌀이가 재료를 떨궜다. 그 종 마리 하나의 발밑에서 튀어 나오게 한다.
        private void OnIngredientDropped(IngredientDroppedEvent evt)
        {
            _floor.OnDropped(evt.IngredientId, evt.SpeciesId, _mozziList);
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
                bool dropped = _hasPendingStart;
                obj.Setup(mozzi.Id, species, SpriteSetOf(species), WanderRect(), TakeStart());
                if (dropped)
                {
                    obj.SettleAfterDrop();
                }

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

        // 찹쌀이가 걷는 칸. 보관함에서 끌어 놓을 때도 이 안이어야 놓인다.
        private static Rect WanderRect()
        {
            Rect area = LayoutConst.FarmRect;
            return Rect.MinMaxRect(
                area.xMin + WanderSideUnits,
                area.yMin + WanderBottomUnits,
                area.xMax - WanderSideUnits,
                area.yMax - WanderTopUnits);
        }

        // 드래그로 놓은 자리가 있으면 그것을 한 번 쓰고, 없으면 걷는 칸 안 무작위 자리다.
        private Vector2 TakeStart()
        {
            if (_hasPendingStart == false)
            {
                return NextStart();
            }

            _hasPendingStart = false;
            return _pendingStart;
        }

        // 걷는 칸 안 무작위. 이미 놓인 마리와 겹치지 않는 자리를 몇 번 다시 뽑는다 — 줄 맞춰 선 것처럼 안 보이게.
        // 다 실패해도 그대로 쓴다. 밀어내기(Separate)가 다음 프레임부터 떼어 놓는다.
        private Vector2 NextStart()
        {
            Rect area = WanderRect();
            float minDist = LayoutConst.ToUnits(LayoutConst.MozziSizePx) * StartGapMargin;
            Vector2 pick = area.center;

            for (int i = 0; i < StartPickTries; i++)
            {
                pick = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                if (IsNearOther(pick, minDist) == false)
                {
                    break;
                }
            }

            return pick;
        }

        private bool IsNearOther(Vector2 point, float minDist)
        {
            for (int i = 0; i < _mozziList.Count; i++)
            {
                if (Vector2.Distance(point, _mozziList[i].transform.position) < minDist)
                {
                    return true;
                }
            }

            return false;
        }

        // 바닥 재료 가운데가 놓일 수 있는 자리. 울타리 안쪽에서 재료 그림 반쪽(14px)씩 줄여 울타리에 안 걸리게 한다.
        private static Rect FloorRect()
        {
            Rect area = LayoutConst.FarmRect;
            float half = LayoutConst.ToUnits(LayoutConst.PilePx * 0.5f);
            return Rect.MinMaxRect(area.xMin + half, area.yMin + half, area.xMax - half, area.yMax - half);
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
                    if (a.IsDragging || b.IsDragging || a.IsSliding || b.IsSliding)
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

        // 궤짝에 다 들어간 마리는 풀에 돌리고 궤짝을 들썩인다.
        private void StepLeaving(float deltaTime)
        {
            for (int i = _leaving.Count - 1; i >= 0; i--)
            {
                MozziObject mozzi = _leaving[i];
                if (mozzi.StepLeaving(deltaTime) == false)
                {
                    continue;
                }

                _leaving.RemoveAt(i);
                _resource.Return(mozzi);
                _storageBuilding.PlayBump();
            }
        }

        // 디버그·시험에서도 부를 수 있게 화면 좌표를 받는다. 판정 순서는 FarmSweepAgent.PressAt 이 정본이다.
        public void TapAtScreen(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            TapAtWorld(new Vector2(world.x, world.y));
        }

        public void TapAtWorld(Vector2 world)
        {
            _sweep.PressAt(world);
        }

        // FarmSweepAgent 가 찹쌀이 위 누르기를 넘긴다. 탭이 될지 드래그가 될지는 MozziDragAgent 가 정한다.
        public void PressMozzi(MozziObject mozzi, Vector2 world)
        {
            _drag.Press(mozzi, _camera.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)));
        }

        // 그 재료를 지금 하나라도 주울 수 있나 (창고 상한이 안 찼나). 꽉 찼으면 FarmSweepAgent 가 누름 순서를 뒤로 민다.
        public bool HasRoomFor(FloorItemObject item)
        {
            return _inventory.IsFull(item.IngredientId) == false;
        }

        public void PickFloorItem(FloorItemObject item)
        {
            _floor.Pick(item);
        }

        public void SweepFloor(Vector2 from, Vector2 to)
        {
            _floor.PickAlong(from, to);
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

        // 농장 찹쌀이를 놓았다. 보관함 궤짝 위면 넣고 입구로 뛰어 들어가는 연출을 튼다.
        // 궤짝 밖이거나 못 넣었으면 담장 안 가장 가까운 자리로 돌아온다. 합성 기계 · 가게 위에 놓는 판정은 아직 없다.
        // 궤짝 판정을 EndDrag 보다 먼저 한다 — EndDrag 가 걷는 칸으로 끌어내린 뒤 지우면 아래로 순간이동해 보인다.
        public void OnFarmMozziDropped(MozziObject mozzi, Vector2 screen)
        {
            if (mozzi == null || mozzi.IsDead)
            {
                return;
            }

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            bool overStorage = _storageBuilding.ContainsWorld(new Vector2(world.x, world.y));
            if (overStorage == false || _storage.TakeFromFarm(mozzi.MozziId) == false)
            {
                mozzi.EndDrag();
                return;
            }

            // 데이터는 이미 보관함으로 갔다. 목록 대조가 바로 반납하지 않게 떼어 연출 목록으로 옮긴다.
            _mozziObjects.Remove(mozzi.MozziId);
            _mozziList.Remove(mozzi);
            _leaving.Add(mozzi);
            mozzi.PlayStore(BuildingLayout.For(BuildingKind.Storage).MouthWorld);
        }

        // 보관함에서 끌어낸 임시 마리를 놓았다. 담장 안이면 그 자리에 진짜 마리가 놓이고, 밖이면 보관함 화면으로 돌아간다.
        public void OnPlaceGhostDropped(MozziObject ghost, string speciesId, Vector2 world)
        {
            ReturnGhost(ghost);

            // 놓은 자리는 SyncMozzis 가 새 마리를 만들 때 TakeStart 로 한 번 쓴다. 그 뒤에 지워야 한다.
            bool placed = false;
            if (WanderRect().Contains(world))
            {
                _hasPendingStart = true;
                _pendingStart = world;
                placed = _storage.PlaceToFarm(speciesId);
            }

            if (placed)
            {
                SyncMozzis();
            }

            _hasPendingStart = false;
            if (placed)
            {
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

            if (building.Kind == BuildingKind.Storage)
            {
                _ui.OpenStorage();
                return;
            }

            _ui.OpenStall();
        }
    }
}
