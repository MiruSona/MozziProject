using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data.Rows;

namespace Mozzi.Game.Ui
{
    // 떡메 3탭의 규칙만 든다 (세로조각1판설계 4-4). 그림 · Make 호출은 owner 인 StallScreen 몫이다.
    // doubleTapMs 안 연타는 남은 탭을 건너뛰고, 그보다 느리되 poundGapMs 안이면 무시, 그 뒤면 한 번 센다.
    public sealed class PoundAgent : AgentBase<StallScreen>
    {
        private readonly List<string> _ingredientIds = new List<string>();
        private readonly int _totalTaps;
        private readonly float _gapSec;
        private readonly float _doubleTapSec;

        private int _taps;
        private float _lastTapTime;
        private bool _running;

        public PoundAgent(StallScreen owner, StallRow row) : base(owner)
        {
            _totalTaps = row.poundTaps;
            if (_totalTaps < 1)
            {
                _totalTaps = 1;
            }

            _gapSec = row.poundGapMs / 1000f;
            _doubleTapSec = row.doubleTapMs / 1000f;
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

        public IReadOnlyList<string> IngredientIds
        {
            get { return _ingredientIds; }
        }

        public void Begin(IReadOnlyList<string> ingredientIds)
        {
            _ingredientIds.Clear();
            _ingredientIds.AddRange(ingredientIds);
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
            if (_taps > 0 && since < _doubleTapSec)
            {
                _taps = _totalTaps;
                _running = false;
                return PoundTapResult.Finished;
            }

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
