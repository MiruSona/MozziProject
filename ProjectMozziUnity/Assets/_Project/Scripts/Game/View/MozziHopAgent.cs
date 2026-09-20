using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 걷는 동안 한 주기(뜀 0.35초 + 바닥 쉼 0.15초)로 통통 뛴다.
    // 뜨는 것은 Visual 자식의 y 뿐이라 루트 위치·콜라이더는 그대로다 — 줍기·탭·밀어내기 판정이 안 흔들린다.
    public sealed class MozziHopAgent : AgentBase<MozziObject>
    {
        private const float JumpSec = 0.35f;
        private const float HopRestSec = 0.15f;

        // 지금 단계(뜀 또는 쉼)에서 흐른 시간.
        private float _time;
        private bool _isHopping;

        public MozziHopAgent(MozziObject owner)
            : base(owner)
        {
        }

        public bool IsHopping
        {
            get { return _isHopping; }
        }

        // 뜀 진행도 0~1. 바닥에 있을 때는 0 이다.
        public float Progress
        {
            get
            {
                if (_isHopping == false)
                {
                    return 0f;
                }

                return Mathf.Clamp01(_time / JumpSec);
            }
        }

        // 지금 떠 있는 높이 (유닛).
        public float LiftUnits
        {
            get
            {
                if (_isHopping == false)
                {
                    return 0f;
                }

                float height = LayoutConst.ToUnits(LayoutConst.MozziHopHeightPx);
                return height * Mathf.Sin(Mathf.PI * Progress);
            }
        }

        public void Reset()
        {
            _time = 0f;
            _isHopping = false;
        }

        // isMoving 은 WanderAgent 가 걷는 중인가다. 멈춰도 떠 있는 뜀은 끝까지 마무리한다.
        public void Step(float deltaTime, bool isMoving)
        {
            if (_isHopping)
            {
                _time += deltaTime;
                if (_time < JumpSec)
                {
                    return;
                }

                _isHopping = false;
                _time = 0f;
                return;
            }

            if (isMoving == false)
            {
                _time = 0f;
                return;
            }

            _time += deltaTime;
            if (_time < HopRestSec)
            {
                return;
            }

            _isHopping = true;
            _time = 0f;
        }
    }
}
