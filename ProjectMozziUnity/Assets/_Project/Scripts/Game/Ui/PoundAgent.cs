using Mozzi.Core;
using Mozzi.Data;

namespace Mozzi.Game.Ui
{
    // 누름 N번의 규칙만 든다 (세로조각1판설계 4-4). 「갈기」(맷돌) · 「치기」(절구) 단계 안에서 KitchenFlowAgent 가 하나씩 쓴다.
    // 순수 탭 카운터다 — 재료 목록 · 그림 · Make 호출은 흐름 · 화면 몫이다.
    // 앞 탭에서 poundGapMs 안이면 무시, 그 뒤면 한 번 센다. Begin 뒤 첫 탭은 늘 1타로 센다. 연타로 건너뛰기는 없다 (반복 09).
    public sealed class PoundAgent : AgentBase<KitchenFlowAgent>
    {
        private readonly int _totalTaps;
        private readonly float _gapSec;

        private int _taps;
        private float _lastTapTime;
        private bool _running;

        public PoundAgent(KitchenFlowAgent owner, StallRow row) : base(owner)
        {
            _totalTaps = row.PoundTaps;
            if (_totalTaps < 1)
            {
                _totalTaps = 1;
            }

            _gapSec = row.PoundGapMs / 1000f;
        }

        public bool IsRunning
        {
            get { return _running; }
        }

        public int Taps
        {
            get { return _taps; }
        }

        public int TotalTaps
        {
            get { return _totalTaps; }
        }

        public void Begin()
        {
            _taps = 0;
            _lastTapTime = float.NegativeInfinity;
            _running = true;
        }

        public void Cancel()
        {
            _running = false;
            _taps = 0;
        }

        // now 는 실제 초(Time.realtimeSinceStartup). 배속과 상관없이 손가락 리듬을 본다.
        public PoundTapResult Tap(float now)
        {
            if (_running == false)
            {
                return PoundTapResult.Ignored;
            }

            float since = now - _lastTapTime;
            if (_taps > 0 && since < _gapSec)
            {
                return PoundTapResult.Ignored;
            }

            _taps++;
            _lastTapTime = now;
            if (_taps < _totalTaps)
            {
                return PoundTapResult.Counted;
            }

            _running = false;
            return PoundTapResult.Finished;
        }
    }
}
