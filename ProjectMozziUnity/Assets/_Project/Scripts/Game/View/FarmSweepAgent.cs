using Mozzi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Mozzi.Game.View
{
    // 농장 월드의 누르기와 훑기. MozziDragAgent 처럼 포인터를 매 프레임 직접 읽는다 (드래그 중에는 Owner 가 이걸 안 부른다).
    // 누른 자리에 겹친 것을 다 보고 우선순위로 고른다 : 주울 수 있는 바닥 재료 → 찹쌀이 → 건물 → 창고가 차서 못 줍는 재료 → 빈 땅.
    // 재료나 빈 땅에서 누르기 시작하면 떼기 전까지 훑기다 — 지난 프레임 자리 → 지금 자리 선분을 따라 줍는다.
    public sealed class FarmSweepAgent : AgentBase<FarmViewManager>
    {
        private readonly Camera _camera;

        private bool _pressWasDown;
        private bool _sweeping;
        private Vector2 _lastWorld;

        public FarmSweepAgent(FarmViewManager owner, Camera camera)
            : base(owner)
        {
            _camera = camera;
        }

        // 누름 가장자리는 isPressed 를 지난 프레임과 견줘 직접 잡는다 — wasPressedThisFrame 은 에디터에서 입력 갱신이
        // 한 프레임에 두 번 돌면(파이프라인 simulate_pointer 등) Process 가 보기 전에 꺼진다.
        public void Step()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                _sweeping = false;
                return;
            }

            bool down = pointer.press.isPressed;
            bool justPressed = down && _pressWasDown == false;
            _pressWasDown = down;

            if (_sweeping)
            {
                if (down == false)
                {
                    _sweeping = false;
                    return;
                }

                Vector2 world = ToWorld(pointer.position.ReadValue());
                Owner.SweepFloor(_lastWorld, world);
                _lastWorld = world;
                return;
            }

            if (justPressed == false)
            {
                return;
            }

            // UI 위 누르기는 EventSystem 이 먹는다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            PressAt(ToWorld(pointer.position.ReadValue()));
        }

        // 디버그 · 시험에서도 부를 수 있게 월드 자리를 받는다.
        // OverlapPoint 하나만 보면 찹쌀이에 덮인 재료를 못 줍고, 무엇이 잡힐지도 정해지지 않는다. 그래서 겹친 것을 다 본다.
        public void PressAt(Vector2 world)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(world);

            // 재료는 창고에 자리가 있는 것과 꽉 찬 것을 나눠 찾는다 — 꽉 찬 것은 순서가 맨 뒤다 (FloorItemMath.ChoosePressTarget).
            FloorItemObject roomItem = NearestFloorItem(hits, world, true);
            FloorItemObject fullItem = NearestFloorItem(hits, world, false);
            MozziObject mozzi = FrontMozzi(hits);
            BuildingObject building = FirstBuilding(hits);

            switch (FloorItemMath.ChoosePressTarget(roomItem != null, fullItem != null, mozzi != null, building != null))
            {
                case FarmPressTarget.FloorItem:
                    Owner.PickFloorItem(roomItem != null ? roomItem : fullItem);
                    BeginSweep(world);
                    break;
                case FarmPressTarget.Mozzi:
                    Owner.PressMozzi(mozzi, world);
                    break;
                case FarmPressTarget.Building:
                    Owner.OnBuildingTapped(building);
                    break;
                default:
                    BeginSweep(world);
                    break;
            }
        }

        public void Cancel()
        {
            _sweeping = false;
        }

        private void BeginSweep(Vector2 world)
        {
            _sweeping = true;
            _lastWorld = world;
        }

        // 겹친 재료가 여럿이면 누른 자리에 가장 가까운 것. hasRoom 이 true 면 창고에 자리가 있는 재료만, false 면 꽉 찬 재료만 본다.
        private FloorItemObject NearestFloorItem(Collider2D[] hits, Vector2 world, bool hasRoom)
        {
            FloorItemObject best = null;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                FloorItemObject item = hits[i].GetComponent<FloorItemObject>();
                if (item == null || item.IsPickable == false || Owner.HasRoomFor(item) != hasRoom)
                {
                    continue;
                }

                float dist = (item.Position - world).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = item;
                }
            }

            return best;
        }

        // 겹친 찹쌀이가 여럿이면 화면 맨 앞(발이 가장 아래)인 것 — 눈에 보이는 마리를 집게.
        private static MozziObject FrontMozzi(Collider2D[] hits)
        {
            MozziObject best = null;
            for (int i = 0; i < hits.Length; i++)
            {
                MozziObject mozzi = hits[i].GetComponent<MozziObject>();
                if (mozzi == null)
                {
                    continue;
                }

                if (best == null || mozzi.FootWorld.y < best.FootWorld.y)
                {
                    best = mozzi;
                }
            }

            return best;
        }

        private static BuildingObject FirstBuilding(Collider2D[] hits)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                BuildingObject building = hits[i].GetComponent<BuildingObject>();
                if (building != null)
                {
                    return building;
                }
            }

            return null;
        }

        private Vector2 ToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            return new Vector2(world.x, world.y);
        }
    }
}
