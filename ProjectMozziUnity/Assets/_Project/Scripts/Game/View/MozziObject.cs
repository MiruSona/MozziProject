using System;
using Mozzi.Core;
using Mozzi.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mozzi.Game.View
{
    // 농장의 찹쌀이 한 마리. 루트는 자리와 탭 판정(BoxCollider2D)만 갖고 그림은 자식 Visual 이 레이어로 그린다.
    // 걷기는 WanderAgent · 뜀은 MozziHopAgent 가 맡는다. 뜀은 Visual 만 올리니 판정이 안 흔들린다.
    // 들려 있는 동안(IsDragging)은 걷기·뜀을 멈추고 FarmViewManager 가 DragTo 로 자리를 준다.
    public sealed class MozziObject : ObjectBase
    {
        private MozziVisual _visual;
        private SortingGroup _sorting;
        private WanderAgent _wander;
        private MozziHopAgent _hop;
        private MozziSpriteSet _set;
        private Rect _area;

        // 지금 표정과 기본으로 돌아가기까지 남은 시간. 들려 있는 동안은 무한대라 안 돌아간다.
        private int _faceIndex = MozziArtKeys.BaseFaceIndex;
        private float _faceHoldSec;
        private bool _dragging;
        private bool _wasHopping;
        private Collider2D _collider;

        // 담장 밖에 놓았을 때 걷는 칸으로 미끄러져 돌아오는 중.
        private bool _sliding;
        private float _slideTime;
        private Vector2 _slideFrom;
        private Vector2 _slideTo;

        // 보관함 궤짝으로 뛰어 들어가는 걸음. 이때는 FarmViewManager 가 Step 대신 StepLeaving 을 부른다.
        private float _leaveTime;
        private Vector2 _leaveFrom;
        private Vector2 _leaveTo;
        private float _leaveBaseScale;

        public int MozziId { get; private set; }

        public string SpeciesId { get; private set; }

        // 겹침 밀어내기에 쓰는 반지름 (유닛).
        public float Radius { get; private set; }

        public bool IsDragging
        {
            get { return _dragging; }
        }

        // 발 자리 (월드). 루트는 몸 가운데라 발까지 내린다 — 재료를 떨굴 자리 · 앞뒤 정렬의 기준이다.
        public Vector2 FootWorld
        {
            get { return new Vector2(transform.position.x, transform.position.y - LayoutConst.MozziFootBelowUnits); }
        }

        // 놓은 뒤 걷는 칸으로 돌아오는 중. 밀어내기가 담장 안으로 끌어당기면 순간이동처럼 보여 건너뛴다.
        public bool IsSliding
        {
            get { return _sliding; }
        }

        public int FaceIndex
        {
            get { return _faceIndex; }
        }

        private void Awake()
        {
            Transform visual = transform.Find(ViewNames.MozziVisual);
            if (visual == null)
            {
                throw new InvalidOperationException("Mozzi 프리팹에 " + ViewNames.MozziVisual + " 자식이 없다");
            }

            _visual = visual.GetComponent<MozziVisual>();
            if (_visual == null)
            {
                throw new InvalidOperationException(ViewNames.MozziVisual + " 에 MozziVisual 이 없다");
            }

            _sorting = GetComponent<SortingGroup>();
            if (_sorting == null)
            {
                throw new InvalidOperationException("Mozzi 프리팹 루트에 SortingGroup 이 없다");
            }

            _collider = GetComponent<Collider2D>();

            _wander = new WanderAgent(this);
            _hop = new MozziHopAgent(this);
        }

        // 몸 그림이 없는 종은 예전처럼 흰 네모에 종 색을 입힌다. 그림 묶음을 찾아 주는 것은 Manager 몫이다.
        public void Setup(int mozziId, MozziSpeciesRow species, MozziSpriteSet set, Rect area, Vector2 start)
        {
            MozziId = mozziId;
            SpeciesId = species.Id;
            _set = set;
            _area = area;
            _dragging = false;
            _sliding = false;
            _faceIndex = MozziArtKeys.BaseFaceIndex;
            _faceHoldSec = 0f;
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            float size = LayoutConst.ToUnits(LayoutConst.MozziSizePx);
            Radius = size * 0.5f;

            float divisor = DivisorOf(set);
            float scale = size / divisor;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(start.x, start.y, 0f);
            gameObject.name = "Mozzi_" + mozziId + "_" + species.Id;

            _visual.Apply(set, MozziArtKeys.BodyColorOf(species), MozziArtKeys.BaseFaceIndex);
            _visual.SetAnchors();
            // 변종 네모 배경은 지금 꺼 둔다. Border 자식과 SetShiny 는 남겨 다음 표시 방법에 다시 쓴다.
            bool shiny = LayoutConst.MozziShinyBorderVisible && species.IsShiny;
            _visual.SetShiny(shiny, LayoutConst.ShinyBorderScale * divisor);
            _visual.SetLift(0f);
            _visual.PlayIdle(0f);
            _wasHopping = false;

            // 움직임 개성은 mozziId 를 씨앗으로 뽑는다 — 재생·풀 재사용 뒤에도 같은 마리는 같은 걸음이다.
            // 시작 때 걷는 채인지 섰는지도 여기서 뽑는다 — 절반쯤은 바로 걷고 나머지는 잠깐 섰다가 걷는다.
            System.Random rng = new System.Random(mozziId);
            float speed = Range(rng, WanderAgent.SpeedMin, WanderAgent.SpeedMax);
            _hop.Reset(
                Range(rng, MozziHopAgent.JumpSecMin, MozziHopAgent.JumpSecMax),
                Range(rng, MozziHopAgent.HopRestSecMin, MozziHopAgent.HopRestSecMax),
                Range(rng, MozziHopAgent.FirstDelaySecMin, MozziHopAgent.FirstDelaySecMax));

            float firstRest = 0f;
            if (rng.NextDouble() >= WanderAgent.StartWalkingChance)
            {
                firstRest = Range(rng, WanderAgent.FirstRestMin, WanderAgent.FirstRestMax);
            }

            _wander.Reset(area, speed, firstRest);
            ApplySort();
        }

        public void Step(float deltaTime)
        {
            StepFace(deltaTime);
            _visual.StepBlink(deltaTime);

            if (_dragging)
            {
                return;
            }

            if (_sliding)
            {
                StepSlide(deltaTime);
                ApplySort();
                return;
            }

            Vector2 next = _wander.Step(transform.position, deltaTime);
            transform.position = new Vector3(next.x, next.y, 0f);

            _hop.Step(deltaTime, _wander.IsMoving);
            _visual.SetLift(_hop.LiftUnits);
            // 서 있는 동안은 Animator 를 안 건드린다. 뜀이 끝난 프레임에만 서기로 돌린다.
            bool hopping = _hop.IsHopping;
            if (hopping)
            {
                _visual.PlayHop(_hop.Progress);
            }
            else if (_wasHopping)
            {
                _visual.PlayIdle(0f);
            }

            _wasHopping = hopping;

            ApplySort();
        }

        // 탭했을 때. holdSec 뒤 기본 표정으로 돌아간다.
        public void ShowRandomFace(float holdSec)
        {
            SetFace(MozziArtKeys.RandomFaceIndex(_faceIndex));
            _faceHoldSec = holdSec;
        }

        // 들어 올린다. 놀란 표정으로 고정하고 앞 담장보다 앞에 그린다. 자리는 DragTo 가 준다.
        public void BeginDrag()
        {
            _dragging = true;
            _sliding = false;
            SetFace(MozziArtKeys.DragFaceIndex);
            _faceHoldSec = float.PositiveInfinity;
            _hop.Reset();
            _visual.SetLift(LayoutConst.ToUnits(LayoutConst.MozziDragLiftPx));
            _visual.PlayIdle(0f);
            _wasHopping = false;
            _sorting.sortingOrder = LayoutConst.MozziDragSortOrder;
        }

        // 담장 밖(길 위 보관함 궤짝까지)으로도 끌 수 있어 여기서는 안 가둔다.
        public void DragTo(Vector2 world)
        {
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        // 놓는다. 표정을 바로 기본으로 돌리고, 걷는 칸 밖이면 가장 가까운 칸 안 자리로 미끄러져 돌아온 뒤 다시 걷는다.
        public void EndDrag()
        {
            _dragging = false;
            Vector2 from = transform.position;
            Vector2 pos = from;
            pos.x = Mathf.Clamp(pos.x, _area.xMin, _area.xMax);
            pos.y = Mathf.Clamp(pos.y, _area.yMin, _area.yMax);

            if (pos != from)
            {
                _sliding = true;
                _slideTime = 0f;
                _slideFrom = from;
                _slideTo = pos;
            }

            _wander.Relocate(pos);
            _visual.SetLift(0f);
            SetFace(MozziArtKeys.BaseFaceIndex);
            _faceHoldSec = 0f;
            ApplySort();
        }

        // 보관함 데이터는 이미 옮겼다. 놀란 표정 그대로 입구(mouth)로 폴짝 뛰며 작아진다. 끝나면 Manager 가 풀에 돌린다.
        public void PlayStore(Vector2 mouth)
        {
            _dragging = false;
            _sliding = false;
            _leaveTime = 0f;

            // 들린 높이(Visual lift)를 루트로 옮겨 시작 자리가 안 튀게 한다.
            _leaveFrom = _visual.transform.position;
            _leaveTo = mouth;
            _leaveBaseScale = transform.localScale.x;
            transform.position = new Vector3(_leaveFrom.x, _leaveFrom.y, 0f);
            _visual.SetLift(0f);
            _sorting.sortingOrder = LayoutConst.MozziStoreSortOrder;

            // 0.3초 사이에 궤짝을 누르면 찹쌀이가 판정을 가로챈다.
            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        // 다 들어갔으면 true. LitMotion 없이 손으로 돈다 — 풀 반납과 핸들 정리가 엇갈리지 않게.
        public bool StepLeaving(float deltaTime)
        {
            _leaveTime += deltaTime;
            float t = Mathf.Clamp01(_leaveTime / LayoutConst.MozziStoreSec);

            Vector2 pos = Vector2.Lerp(_leaveFrom, _leaveTo, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * LayoutConst.ToUnits(LayoutConst.MozziStoreJumpPx);
            transform.position = new Vector3(pos.x, pos.y, 0f);

            // 뛰는 동안은 잘 보이다가 입구에 닿을 무렵 확 줄게 t² 로 줄인다.
            float scale = _leaveBaseScale * Mathf.Lerp(1f, LayoutConst.MozziStoreEndScale, t * t);
            transform.localScale = new Vector3(scale, scale, 1f);

            return t >= 1f;
        }

        private void StepSlide(float deltaTime)
        {
            _slideTime += deltaTime;
            float t = Mathf.Clamp01(_slideTime / LayoutConst.MozziDropSlideSec);
            Vector2 pos = Vector2.Lerp(_slideFrom, _slideTo, t);
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (t >= 1f)
            {
                _sliding = false;
            }
        }

        // 보관함에서 끌어 놓은 마리. 놓은 자리에서 잠깐 쉬고 걷는다 — 시작 때처럼 바로 걸어가면 놓은 느낌이 없다.
        public void SettleAfterDrop()
        {
            _wander.Relocate(transform.position);
        }

        private static float Range(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        // 밀어낸 뒤 담장 안으로 되돌린다.
        public void Nudge(Vector2 delta)
        {
            Vector2 pos = (Vector2)transform.position + delta;
            pos.x = Mathf.Clamp(pos.x, _area.xMin, _area.xMax);
            pos.y = Mathf.Clamp(pos.y, _area.yMin, _area.yMax);
            transform.position = new Vector3(pos.x, pos.y, 0f);
            ApplySort();
        }

        public override void OnReturn()
        {
            base.OnReturn();
            _visual.ResetState();
            _hop.Reset();
            _dragging = false;
            _sliding = false;
            _set = null;
            MozziId = 0;
            SpeciesId = null;
        }

        private void StepFace(float deltaTime)
        {
            if (_faceIndex == MozziArtKeys.BaseFaceIndex)
            {
                return;
            }

            if (float.IsPositiveInfinity(_faceHoldSec))
            {
                return;
            }

            _faceHoldSec -= deltaTime;
            if (_faceHoldSec <= 0f)
            {
                SetFace(MozziArtKeys.BaseFaceIndex);
            }
        }

        private void SetFace(int index)
        {
            _faceIndex = index;
            if (_set != null)
            {
                _visual.SetFace(_set.FaceOf(index));
            }
        }

        // 아래쪽 마리가 앞에 오게 발 y 를 빼서 쓴다. 루트는 몸 가운데라 발까지 내려 장식(칸 바닥 기준)과 맞춘다.
        // 뜀 높이는 Visual 만 올라가니 안 섞인다.
        private void ApplySort()
        {
            float y = transform.position.y - LayoutConst.MozziFootBelowUnits;
            _sorting.sortingOrder = LayoutConst.MozziSortBase - Mathf.RoundToInt(y * LayoutConst.PixelsPerUnit);
        }

        // 그림 원래 높이로 나눠 목표 크기를 맞춘다. 레이어 그림은 64 캔버스에 여백이 있어 몸 높이 비를 곱한다
        // — 그러면 목표 크기가 곧 몸 높이가 된다. 흰 네모 폴백은 1유닛이라 예전과 같다.
        private static float DivisorOf(MozziSpriteSet set)
        {
            if (set.HasBody == false)
            {
                return 1f;
            }

            float height = set.BodyHops[0].bounds.size.y;
            if (height <= 0f)
            {
                return 1f;
            }

            return height * (LayoutConst.MozziBodyHeightPx / LayoutConst.MozziCanvasPx);
        }
    }
}
