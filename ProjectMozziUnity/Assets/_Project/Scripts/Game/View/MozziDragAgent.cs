using Mozzi.Core;
using Mozzi.Game.Ui;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mozzi.Game.View
{
    // 찹쌀이 탭·드래그 판정 (반복 01 피드백 5·6·7). 포인터를 매 프레임 직접 읽는다 — UI 가 닫혀도 끊기지 않게.
    // 누른 채 DragThresholdPx 를 넘기면 드래그, 안 넘기고 떼면 탭이다. 놓을 때 할 일은 Owner 에게 넘긴다.
    public sealed class MozziDragAgent : AgentBase<FarmViewManager>
    {
        private readonly Camera _camera;

        private MozziDragPhase _phase;
        private MozziObject _target;
        private Vector2 _pressScreen;
        private string _placeSpeciesId;

        public MozziDragAgent(FarmViewManager owner, Camera camera)
            : base(owner)
        {
            _camera = camera;
        }

        public MozziDragPhase Phase
        {
            get { return _phase; }
        }

        public bool IsActive
        {
            get { return _phase != MozziDragPhase.None; }
        }

        // 농장 찹쌀이를 눌렀다. 탭이 될지 드래그가 될지는 Step 이 정한다.
        public void Press(MozziObject mozzi, Vector2 screen)
        {
            _phase = MozziDragPhase.Pressed;
            _target = mozzi;
            _pressScreen = screen;
        }

        // 보관함 줄에서 끌어내기 시작했다. ghost 는 Owner 가 빌려 준 임시 마리다.
        public void BeginPlace(MozziObject ghost, string speciesId)
        {
            _phase = MozziDragPhase.DragPlace;
            _target = ghost;
            _placeSpeciesId = speciesId;
            ghost.BeginDrag();
        }

        public void Step()
        {
            if (_phase == MozziDragPhase.None)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || _target == null || _target.IsDead)
            {
                Cancel();
                return;
            }

            Vector2 screen = pointer.position.ReadValue();
            Vector2 world = ToWorld(screen);
            bool pressed = pointer.press.isPressed;

            if (_phase == MozziDragPhase.Pressed)
            {
                if (pressed == false)
                {
                    MozziObject tapped = _target;
                    Clear();
                    Owner.OnMozziTapped(tapped);
                    return;
                }

                if (Vector2.Distance(screen, _pressScreen) < ThresholdScreenPx())
                {
                    return;
                }

                _phase = MozziDragPhase.DragFarm;
                _target.BeginDrag();
            }

            if (pressed)
            {
                _target.DragTo(world);
                return;
            }

            MozziObject dropped = _target;
            MozziDragPhase phase = _phase;
            string speciesId = _placeSpeciesId;
            Clear();

            if (phase == MozziDragPhase.DragFarm)
            {
                Owner.OnFarmMozziDropped(dropped, screen);
                return;
            }

            Owner.OnPlaceGhostDropped(dropped, speciesId, world);
        }

        // 들고 있던 것을 제자리로. 포인터가 사라지거나 대상이 풀로 돌아갔을 때.
        public void Cancel()
        {
            MozziObject target = _target;
            MozziDragPhase phase = _phase;
            string speciesId = _placeSpeciesId;
            Clear();

            if (target == null || target.IsDead)
            {
                return;
            }

            if (phase == MozziDragPhase.DragFarm)
            {
                target.EndDrag();
                return;
            }

            if (phase == MozziDragPhase.DragPlace)
            {
                Owner.OnPlaceGhostCancelled(target, speciesId);
            }
        }

        private void Clear()
        {
            _phase = MozziDragPhase.None;
            _target = null;
            _placeSpeciesId = null;
        }

        private Vector2 ToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            return new Vector2(world.x, world.y);
        }

        // UI px 기준 문턱을 실제 화면 px 로. 캔버스와 같은 배율을 곱한다.
        private static float ThresholdScreenPx()
        {
            return LayoutConstUi.DragThresholdPx * ScreenFitUtil.CanvasScale();
        }
    }
}
