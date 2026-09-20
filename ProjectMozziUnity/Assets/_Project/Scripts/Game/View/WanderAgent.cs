using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 담장 안에서 목표점을 골라 천천히 걷고, 닿으면 잠깐 쉬었다가 다시 고른다.
    public sealed class WanderAgent : AgentBase<MozziObject>
    {
        private const float Speed = 0.6f;
        private const float RestMin = 1f;
        private const float RestMax = 3f;

        private Rect _area;
        private Vector2 _target;
        private float _rest;

        public WanderAgent(MozziObject owner) : base(owner)
        {
        }

        // 지금 걷고 있나. 쉬는 중이면 false 다 — 뜀(MozziHopAgent)이 이 값을 보고 통통 뛴다.
        public bool IsMoving { get; private set; }

        public void Reset(Rect area, Vector2 start)
        {
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
            Vector2 next = Vector2.MoveTowards(position, _target, Speed * deltaTime);
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
