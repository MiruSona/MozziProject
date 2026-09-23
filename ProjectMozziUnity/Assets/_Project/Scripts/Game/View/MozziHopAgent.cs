using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 걷는 동안 한 주기(뜀 + 바닥 쉼)로 통통 뛴다. 주기는 개체마다 다르다 — 전부 같으면 한 박자로 뛰어 이상해 보인다.
    // 뜨는 것은 Visual 자식의 y 뿐이라 루트 위치·콜라이더는 그대로다 — 줍기·탭·밀어내기 판정이 안 흔들린다.
    public sealed class MozziHopAgent : AgentBase<MozziObject>
    {
        // 개체 값의 범위. 기준 0.35 / 0.15 초를 가운데 두고 흩는다 (아트규칙 5절).
        public const float JumpSecMin = 0.3f;
        public const float JumpSecMax = 0.42f;
        public const float HopRestSecMin = 0.1f;
        public const float HopRestSecMax = 0.25f;
        // 걷기 시작 뒤 첫 뜀까지 더 기다리는 시간. 같이 걷기 시작한 애들이 첫 뜀부터 어긋나게.
        public const float FirstDelaySecMin = 0f;
        public const float FirstDelaySecMax = 0.3f;

        private float _jumpSec = JumpSecMin;
        private float _hopRestSec = HopRestSecMin;
        private float _firstDelaySec;

        // 지금 단계(뜀 또는 쉼)에서 흐른 시간.
        private float _time;
        private bool _isHopping;
        // 걷기 시작 뒤 첫 뜀을 아직 안 했나. 첫 쉼에만 _firstDelaySec 을 더한다.
        private bool _firstHopPending;

        public MozziHopAgent(MozziObject owner)
            : base(owner)
        {
        }

        public bool IsHopping
        {
            get { return _isHopping; }
        }

        public float JumpSec
        {
            get { return _jumpSec; }
        }

        public float HopRestSec
        {
            get { return _hopRestSec; }
        }

        public float FirstDelaySec
        {
            get { return _firstDelaySec; }
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

                return Mathf.Clamp01(_time / _jumpSec);
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

        // 개체 값은 MozziObject 가 씨앗으로 뽑아 준다. 풀에서 다시 쓸 때도 Setup 이 새로 부른다.
        public void Reset(float jumpSec, float hopRestSec, float firstDelaySec)
        {
            _jumpSec = jumpSec;
            _hopRestSec = hopRestSec;
            _firstDelaySec = firstDelaySec;
            Reset();
        }

        public void Reset()
        {
            _time = 0f;
            _isHopping = false;
            _firstHopPending = true;
        }

        // isMoving 은 WanderAgent 가 걷는 중인가다. 멈춰도 떠 있는 뜀은 끝까지 마무리한다.
        public void Step(float deltaTime, bool isMoving)
        {
            if (_isHopping)
            {
                _time += deltaTime;
                if (_time < _jumpSec)
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
                _firstHopPending = true;
                return;
            }

            _time += deltaTime;
            float rest = _firstHopPending ? _hopRestSec + _firstDelaySec : _hopRestSec;
            if (_time < rest)
            {
                return;
            }

            _isHopping = true;
            _firstHopPending = false;
            _time = 0f;
        }
    }
}
