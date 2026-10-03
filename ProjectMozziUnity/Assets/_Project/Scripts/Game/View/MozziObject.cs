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
        private const string WhiteSpeciesId = "grass1";

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

        public int MozziId { get; private set; }

        public string SpeciesId { get; private set; }

        // 겹침 밀어내기에 쓰는 반지름 (유닛).
        public float Radius { get; private set; }

        public bool IsDragging
        {
            get { return _dragging; }
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
            _faceIndex = MozziArtKeys.BaseFaceIndex;
            _faceHoldSec = 0f;

            float sizePx = SizePxOf(species.Tier);
            float size = LayoutConst.ToUnits(sizePx);
            Radius = size * 0.5f;

            float divisor = DivisorOf(set);
            float scale = size / divisor;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(start.x, start.y, 0f);
            gameObject.name = "Mozzi_" + mozziId + "_" + species.Id;

            _visual.Apply(set, ColorOf(species), MozziArtKeys.BaseFaceIndex);
            _visual.SetAnchors();
            // 변종 네모 배경은 지금 꺼 둔다. Border 자식과 SetShiny 는 남겨 다음 표시 방법에 다시 쓴다.
            bool shiny = LayoutConst.MozziShinyBorderVisible && species.IsShiny;
            _visual.SetShiny(shiny, LayoutConst.ShinyBorderScale * divisor);
            _visual.SetLift(0f);
            _visual.PlayIdle(0f);

            // 움직임 개성은 mozziId 를 씨앗으로 뽑는다 — 재생·풀 재사용 뒤에도 같은 마리는 같은 걸음이다.
            System.Random rng = new System.Random(mozziId);
            _wander.Reset(area, start, Range(rng, WanderAgent.SpeedMin, WanderAgent.SpeedMax));
            _hop.Reset(
                Range(rng, MozziHopAgent.JumpSecMin, MozziHopAgent.JumpSecMax),
                Range(rng, MozziHopAgent.HopRestSecMin, MozziHopAgent.HopRestSecMax),
                Range(rng, MozziHopAgent.FirstDelaySecMin, MozziHopAgent.FirstDelaySecMax));
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

            Vector2 next = _wander.Step(transform.position, deltaTime);
            transform.position = new Vector3(next.x, next.y, 0f);

            _hop.Step(deltaTime, _wander.IsMoving);
            _visual.SetLift(_hop.LiftUnits);
            if (_hop.IsHopping)
            {
                _visual.PlayHop(_hop.Progress);
            }
            else
            {
                _visual.PlayIdle(0f);
            }

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
            SetFace(MozziArtKeys.DragFaceIndex);
            _faceHoldSec = float.PositiveInfinity;
            _hop.Reset();
            _visual.SetLift(LayoutConst.ToUnits(LayoutConst.MozziDragLiftPx));
            _visual.PlayIdle(0f);
            _sorting.sortingOrder = LayoutConst.MozziDragSortOrder;
        }

        // 담장 밖(아래 띠의 보관함 버튼까지)으로도 끌 수 있어 여기서는 안 가둔다.
        public void DragTo(Vector2 world)
        {
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        // 놓는다. 담장 안으로 되돌리고 표정을 바로 기본으로 돌린 뒤 그 자리에서 다시 걷기 시작한다.
        public void EndDrag()
        {
            _dragging = false;
            Vector2 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _area.xMin, _area.xMax);
            pos.y = Mathf.Clamp(pos.y, _area.yMin, _area.yMax);
            transform.position = new Vector3(pos.x, pos.y, 0f);

            _wander.Relocate(pos);
            _visual.SetLift(0f);
            SetFace(MozziArtKeys.BaseFaceIndex);
            _faceHoldSec = 0f;
            ApplySort();
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

        // 아래쪽 마리가 앞에 오게 발 자리(루트 y) 를 빼서 쓴다. 뜀 높이는 Visual 만 올라가니 안 섞인다.
        private void ApplySort()
        {
            float y = transform.position.y;
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

        private static float SizePxOf(int tier)
        {
            if (tier >= 3)
            {
                return LayoutConst.MozziTier3Px;
            }

            if (tier == 2)
            {
                return LayoutConst.MozziTier2Px;
            }

            return LayoutConst.MozziTier1Px;
        }

        // 계열 색 · 단계 밝기는 표의 colorHex 가 정본이다. 흰찹쌀이만 흰색이라 그림 색이 그대로 나온다.
        private static Color ColorOf(MozziSpeciesRow species)
        {
            if (species.Id == WhiteSpeciesId)
            {
                return Color.white;
            }

            return SpriteUtil.ParseHex(species.ColorHex, Color.gray);
        }
    }
}
