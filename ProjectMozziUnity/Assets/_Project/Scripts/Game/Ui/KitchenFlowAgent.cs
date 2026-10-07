using System.Collections.Generic;
using Mozzi.Data;

namespace Mozzi.Game.Ui
{
    // 주방 다섯 단계 흐름의 규칙만 든다 (순수 C#, 시험은 KitchenFlowAgentTests). 고름 → 갊 → 침 → 빚음 → 찜 → 끝 (반복 08).
    // 그림 · 버튼 · StallManager.Make 호출은 KitchenScreen 몫이다. 과정은 결과를 안 바꾼다 — 떡은 재료로만 정해진다.
    //  - 고르기 : 화면이 서랍 칸으로 고른다. 고른 게 있고 만들 수 있으면(canStart) 다음 도구는 맷돌이다.
    //    맷돌 첫 누름이 흐름의 시작이다. 해 본 조합 · 찾은 조합(known)이면 여기서 과정을 건너뛰고 QuickMake.
    //  - 갈기 : 맷돌 탭을 _grind 가 센다 (첫 누름이 1타). 치기와 같은 규칙 · 같은 수(PoundTaps).
    //  - 치기 : 절구 탭을 _pound 가 센다.
    //  - 빚기 : 빚기판을 ShapeTotalTaps 번.
    //  - 찌기 : 누를 것 없이 steamSec 동안 기다린다 (실제 시간). 화면이 Tick 으로 재고, 다 되면 Steamed → 그때 Make.
    // 갈기 · 치기 · 빚기 모두 마지막 누름은 Ground · Pounded · ShapeTapped 로 세기만 하고 「한 번 더」(IsWaitingNext) 로 선다.
    // 같은 도구를 한 번 더 누르면 그때 GrindDone · PoundDone · Shaped 로 넘어간다 (반복 09 — 마지막 움직임을 끝까지 보이려고).
    // 다음 도구가 아닌 도구 · 찌는 중 누름은 Ignored. 연타로 과정을 건너뛰는 규칙은 없다 (반복 09 에서 뺐다).
    public sealed class KitchenFlowAgent
    {
        private readonly PoundAgent _grind;
        private readonly PoundAgent _pound;
        private readonly float _steamSec;
        private readonly List<string> _ingredientIds = new List<string>();

        private KitchenStep _step;
        private float _steamStart;
        private int _shapeTaps;
        private bool _waitNext;

        public KitchenFlowAgent(StallRow row, float steamSec)
        {
            _grind = new PoundAgent(this, row);
            _pound = new PoundAgent(this, row);
            _steamSec = steamSec;
            Reset();
        }

        public KitchenStep Step
        {
            get { return _step; }
        }

        // 맷돌을 처음 누른 뒤 ~ 만들기 전. 이 동안은 재료를 다시 못 고른다.
        public bool IsRunning
        {
            get { return _step != KitchenStep.Pick; }
        }

        public int GrindTaps
        {
            get { return _grind.Taps; }
        }

        public int PoundTaps
        {
            get { return _pound.Taps; }
        }

        // 갈기 · 치기 둘 다 같은 수 (stall 표 PoundTaps).
        public int TotalTaps
        {
            get { return _pound.TotalTaps; }
        }

        // 빚기판 누른 수 (0 ~ ShapeTotalTaps).
        public int ShapeTaps
        {
            get { return _shapeTaps; }
        }

        public int ShapeTotalTaps
        {
            get { return KitchenLayout.ShapeTapCount; }
        }

        // 갈기 · 치기 · 빚기의 마지막 누름을 다 하고 「한 번 더」 누름을 기다리는 중.
        public bool IsWaitingNext
        {
            get { return _waitNext; }
        }

        // 맷돌 첫 누름 때 베껴 둔 재료. Steamed · QuickMake 를 받은 뒤 화면이 이것으로 만든다. Reset 해도 남는다.
        public IReadOnlyList<string> IngredientIds
        {
            get { return _ingredientIds; }
        }

        // 지금 「여기 눌러」 로 밝힐 도구. 찌는 중에는 없다.
        public KitchenTool NextTool(bool canStart)
        {
            if (_step == KitchenStep.Grind)
            {
                return KitchenTool.Mill;
            }

            if (_step == KitchenStep.Pound)
            {
                return KitchenTool.Mortar;
            }

            if (_step == KitchenStep.Shape)
            {
                return KitchenTool.Board;
            }

            if (_step == KitchenStep.Pick && canStart)
            {
                return KitchenTool.Mill;
            }

            return KitchenTool.None;
        }

        // 처음(고르기)으로. 만든 뒤 · 화면을 닫을 때 부른다. 재료는 Make 때만 빠지니 잃는 것이 없다.
        public void Reset()
        {
            _step = KitchenStep.Pick;
            _shapeTaps = 0;
            _waitNext = false;
            _grind.Cancel();
            _pound.Cancel();
        }

        // now 는 실제 초(Time.realtimeSinceStartup). selected · canStart · known 은 고르기 단계에서만 본다.
        public KitchenFlowResult Press(KitchenTool tool, float now, IReadOnlyList<string> selected, bool canStart, bool known)
        {
            if (_step == KitchenStep.Steam)
            {
                return KitchenFlowResult.Ignored;
            }

            if (tool == KitchenTool.None || tool != NextTool(canStart))
            {
                return KitchenFlowResult.Ignored;
            }

            if (_step == KitchenStep.Pick)
            {
                return Start(now, selected, known);
            }

            if (_step == KitchenStep.Grind)
            {
                return Grind(now);
            }

            if (_step == KitchenStep.Pound)
            {
                return Pound(now);
            }

            // 빚기 : ShapeTotalTaps 번 다 빚으면 「한 번 더」 를 기다리고, 그 누름에 찌기 시작.
            if (_waitNext)
            {
                _waitNext = false;
                _step = KitchenStep.Steam;
                _steamStart = now;
                return KitchenFlowResult.Shaped;
            }

            _shapeTaps++;
            if (_shapeTaps >= ShapeTotalTaps)
            {
                _waitNext = true;
            }

            return KitchenFlowResult.ShapeTapped;
        }

        // 반죽이 찜기에 닿은 순간부터 다시 잰다 — 날아가는 연출 동안은 안 센다.
        public void RestartSteam(float now)
        {
            if (_step == KitchenStep.Steam)
            {
                _steamStart = now;
            }
        }

        // 찌는 시간 재기. 화면이 틀마다 실제 초로 부른다. 다 찐 그 한 번만 Steamed (흐름은 고르기로 돌아간다).
        public KitchenFlowResult Tick(float now)
        {
            if (_step != KitchenStep.Steam || now - _steamStart < _steamSec)
            {
                return KitchenFlowResult.Ignored;
            }

            Reset();
            return KitchenFlowResult.Steamed;
        }

        private KitchenFlowResult Start(float now, IReadOnlyList<string> selected, bool known)
        {
            _ingredientIds.Clear();
            _ingredientIds.AddRange(selected);
            if (known)
            {
                Reset();
                return KitchenFlowResult.QuickMake;
            }

            _step = KitchenStep.Grind;
            _grind.Begin();
            return Grind(now);
        }

        private KitchenFlowResult Grind(float now)
        {
            if (_waitNext)
            {
                _waitNext = false;
                _step = KitchenStep.Pound;
                _pound.Begin();
                return KitchenFlowResult.GrindDone;
            }

            PoundTapResult result = _grind.Tap(now);
            if (result == PoundTapResult.Ignored)
            {
                return KitchenFlowResult.Ignored;
            }

            _waitNext = result == PoundTapResult.Finished;
            return KitchenFlowResult.Ground;
        }

        private KitchenFlowResult Pound(float now)
        {
            if (_waitNext)
            {
                _waitNext = false;
                _step = KitchenStep.Shape;
                _shapeTaps = 0;
                return KitchenFlowResult.PoundDone;
            }

            PoundTapResult result = _pound.Tap(now);
            if (result == PoundTapResult.Ignored)
            {
                return KitchenFlowResult.Ignored;
            }

            _waitNext = result == PoundTapResult.Finished;
            return KitchenFlowResult.Pounded;
        }
    }
}
