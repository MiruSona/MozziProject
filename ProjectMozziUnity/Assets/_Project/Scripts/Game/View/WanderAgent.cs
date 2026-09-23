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

        // speed 는 MozziObject 가 씨앗으로 뽑아 준다.
        public void Reset(Rect area, Vector2 start, float speed)
        {
            _speed = speed;
            _area = area;
            _target = start;
            _rest = Random.Range(0f, RestMax);
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
