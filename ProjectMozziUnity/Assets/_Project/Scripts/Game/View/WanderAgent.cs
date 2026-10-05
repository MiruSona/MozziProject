using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 담장 안에서 목표점을 골라 천천히 걷고, 닿으면 잠깐 쉬었다가 다시 고른다.
    // 걷는 속도는 개체마다 다르다 — 같으면 같이 출발한 애들이 같은 속도로 미끄러져 보인다.
    public sealed class WanderAgent : AgentBase<MozziObject>
    {
        // 개체 속도 범위. 기준 0.6 을 가운데 두고 흩는다.
        public const float SpeedMin = 0.5f;
        public const float SpeedMax = 0.75f;
        private const float RestMin = 1f;
        private const float RestMax = 3f;

        private float _speed = SpeedMin;
        private Rect _area;
        private Vector2 _target;
        private float _rest;

        public WanderAgent(MozziObject owner) : base(owner)
        {
        }

        // 지금 걷고 있나. 쉬는 중이면 false 다 — 뜀(MozziHopAgent)이 이 값을 보고 통통 뛴다.
        public bool IsMoving { get; private set; }

        public float Speed
        {
            get { return _speed; }
        }

        // 처음 쉼을 이 범위에서 뽑는다 (초). 쉬는 쪽 마리가 한꺼번에 일어나지 않게 흩는다.
        public const float FirstRestMin = 0.5f;
        public const float FirstRestMax = 3f;

        // 처음부터 걷는 채로 시작할 확률. 나머지는 FirstRest 만큼 섰다가 걷는다.
        public const float StartWalkingChance = 0.5f;

        // speed · firstRest 는 MozziObject 가 씨앗으로 뽑아 준다. firstRest 가 0 이면 걷는 채로 시작한다.
        // 처음 목표를 제자리로 두면 모두 쉼만 끝나고 같이 출발해 보여서, 바로 새 목표를 고른다.
        public void Reset(Rect area, float speed, float firstRest)
        {
            _speed = speed;
            _area = area;
            _target = PickTarget();
            _rest = firstRest;
            IsMoving = firstRest <= 0f;
        }

        // 드래그로 옮겨진 뒤. 목표를 지금 자리로 바꾸고 잠깐 쉰다 — 안 그러면 옛 목표로 미끄러진다.
        public void Relocate(Vector2 position)
        {
            _target = position;
            _rest = Random.Range(RestMin, RestMax);
            IsMoving = false;
        }

        public Vector2 Step(Vector2 position, float deltaTime)
        {
            if (_rest > 0f)
            {
                _rest -= deltaTime;
                IsMoving = false;
                return position;
            }

            IsMoving = true;
            Vector2 next = Vector2.MoveTowards(position, _target, _speed * deltaTime);
            if (Vector2.Distance(next, _target) < 0.05f)
            {
                _rest = Random.Range(RestMin, RestMax);
                _target = PickTarget();
            }

            return next;
        }

        private Vector2 PickTarget()
        {
            float x = Random.Range(_area.xMin, _area.xMax);
            float y = Random.Range(_area.yMin, _area.yMax);
            return new Vector2(x, y);
        }
    }
}
