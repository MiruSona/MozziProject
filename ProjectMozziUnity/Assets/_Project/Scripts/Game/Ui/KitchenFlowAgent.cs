using System.Collections.Generic;
using Mozzi.Data;

namespace Mozzi.Game.Ui
{
    // 주방 네 단계 흐름의 규칙만 든다 (순수 C#, 시험은 KitchenFlowAgentTests). 고름 → 찜 → 침 → 빚음 → 끝.
    // 그림 · 버튼 · StallManager.Make 호출은 KitchenScreen 몫이다. 과정은 결과를 안 바꾼다 — 떡은 재료로만 정해진다.
    //  - 고르기 : 화면이 서랍 칸으로 고른다. 고른 게 있고 만들 수 있으면(canStart) 다음 도구는 찜기다.
    //  - 찌기 : 찜기 한 번. 해 본 조합 · 찾은 조합(known)이면 여기서 과정을 건너뛰고 QuickMake.
    //  - 치기 : 절구 탭을 PoundAgent 가 센다. 첫 탭도 1타다.
    //  - 빚기 : 빚기판 한 번 → Make.
    // 다음 도구가 아닌 도구는 무시한다. 단, 흐름이 시작된 뒤 앞 누름에서 doubleTapMs 안에 또 누르면 어느 도구든 남은 과정을 건너뛰고 Make.
    public sealed class KitchenFlowAgent
    {
        private readonly PoundAgent _pound;
        private readonly float _doubleTapSec;

        private KitchenStep _step;
        private float _lastPressTime;

        public KitchenFlowAgent(StallRow row)
        {
            _pound = new PoundAgent(this, row);
            _doubleTapSec = row.DoubleTapMs / 1000f;
            Reset();
        }

        public KitchenStep Step
        {
            get { return _step; }
        }

        // 찜기를 누른 뒤 ~ 만들기 전. 이 동안은 재료를 다시 못 고른다.
        public bool IsRunning
        {
            get { return _step != KitchenStep.Pick; }
        }

        public int PoundTaps
        {
            get { return _pound.Taps; }
        }

        public int TotalTaps
        {
            get { return _pound.TotalTaps; }
        }

        // 찜기를 누를 때 베껴 둔 재료. Make · QuickMake 를 받은 뒤 화면이 이것으로 만든다.
        public IReadOnlyList<string> IngredientIds
        {
            get { return _pound.IngredientIds; }
        }

        // 지금 「여기 눌러」 로 밝힐 도구.
        public KitchenTool NextTool(bool canStart)
        {
            if (_step == KitchenStep.Pound)
            {
                return KitchenTool.Mortar;
            }

            if (_step == KitchenStep.Shape)
            {
                return KitchenTool.Board;
            }

            if (canStart)
            {
                return KitchenTool.Steamer;
            }

            return KitchenTool.None;
        }

        // 처음(고르기)으로. 만든 뒤 · 화면을 닫을 때 부른다. 재료는 Make 때만 빠지니 잃는 것이 없다.
        public void Reset()
        {
            _step = KitchenStep.Pick;
            _pound.Cancel();
            _lastPressTime = float.NegativeInfinity;
        }

        // now 는 실제 초(Time.realtimeSinceStartup). selected · canStart · known 은 고르기 단계에서만 본다.
        public KitchenFlowResult Press(KitchenTool tool, float now, IReadOnlyList<string> selected, bool canStart, bool known)
        {
            if (_step != KitchenStep.Pick && now - _lastPressTime < _doubleTapSec)
            {
                Reset();
                return KitchenFlowResult.Make;
            }

            if (tool == KitchenTool.None || tool != NextTool(canStart))
            {
                return KitchenFlowResult.Ignored;
            }

            if (_step == KitchenStep.Pick)
            {
                return Steam(now, selected, known);
            }

            if (_step == KitchenStep.Pound)
            {
                return Pound(now);
            }

            Reset();
            return KitchenFlowResult.Make;
        }

        private KitchenFlowResult Steam(float now, IReadOnlyList<string> selected, bool known)
        {
            _pound.Begin(selected);
            if (known)
            {
                Reset();
                return KitchenFlowResult.QuickMake;
            }

            _step = KitchenStep.Pound;
            _lastPressTime = now;
            return KitchenFlowResult.Steamed;
        }

        private KitchenFlowResult Pound(float now)
        {
            PoundTapResult result = _pound.Tap(now);
            if (result == PoundTapResult.Ignored)
            {
                return KitchenFlowResult.Ignored;
            }

            _lastPressTime = now;
            if (result == PoundTapResult.Counted)
            {
                return KitchenFlowResult.Pounded;
            }

            _step = KitchenStep.Shape;
            return KitchenFlowResult.PoundDone;
        }
    }
}
