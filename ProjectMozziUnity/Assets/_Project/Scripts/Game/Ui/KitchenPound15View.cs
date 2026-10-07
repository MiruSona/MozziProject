using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 떡메 pound15 재생기. 겹(아래→위) : 절구 뒤 · 반죽/떡메 · 절구 앞 · 넘친 반죽. 절구 둘은 고정 그림이라 여기서 안 만진다.
    // 한 번 누름 = 지금 단계 8장 → (마지막 단계가 아니면) 넘어감 4장 → 다음 단계 0장에서 쉰다. 마지막 단계는 7장에서 쉰다.
    // 묻힘 장(KitchenLayout.Pound15DoughOnTop)은 반죽 Image 를 떡메 위로 올린다. 그림이 하나라도 없으면 아무것도 안 바꾼다.
    public sealed class KitchenPound15View
    {
        private readonly Image _dough;
        private readonly Image _mallet;
        private readonly Image _spill;
        private readonly Sprite[][] _stageMallet;
        private readonly Sprite[][] _stageDough;
        private readonly Sprite[][] _stageSpill;
        private readonly Sprite[][] _turnMallet;
        private readonly Sprite[][] _turnDough;
        private readonly int _lowIndex;
        private readonly bool _ready;

        private int _stage;
        private bool _inTurn;
        private int _frame;
        private float _time;
        private bool _playing;

        public KitchenPound15View(Image dough, Image mallet, Image spill,
            Sprite[][] stageMallet, Sprite[][] stageDough, Sprite[][] stageSpill, Sprite[][] turnMallet, Sprite[][] turnDough)
        {
            _dough = dough;
            _mallet = mallet;
            _spill = spill;
            _stageMallet = stageMallet;
            _stageDough = stageDough;
            _stageSpill = stageSpill;
            _turnMallet = turnMallet;
            _turnDough = turnDough;
            _lowIndex = Mathf.Min(dough.transform.GetSiblingIndex(), mallet.transform.GetSiblingIndex());
            _ready = AllLoaded(stageMallet) && AllLoaded(stageDough) && AllLoaded(stageSpill) && AllLoaded(turnMallet) && AllLoaded(turnDough);
        }

        // 움직이는 중이면 true. 화면이 이 동안 누름을 안 받는다.
        public bool IsPlaying
        {
            get { return _playing; }
        }

        // 지금 단계(0~2)를 한 번 친다. 다 치면 다음 단계 0장(마지막이면 그 단계 7장)에 선다.
        public void Play(int stage)
        {
            _stage = Mathf.Clamp(stage, 0, _stageMallet.Length - 1);
            _inTurn = false;
            _frame = 0;
            _time = 0f;
            _playing = true;
            Show();
        }

        // 1단계 쉬는 장으로 되돌린다 (화면 열기 · 닫기).
        public void Reset()
        {
            _stage = 0;
            _inTurn = false;
            _frame = 0;
            _playing = false;
            Show();
        }

        // 반죽 · 넘친 반죽만 물들인다. 절구 · 떡메는 흰색 그대로.
        public void SetDoughColor(Color color)
        {
            _dough.color = color;
            _spill.color = color;
        }

        public void Process(float deltaTime)
        {
            if (_playing == false)
            {
                return;
            }

            _time += deltaTime;
            while (_playing && _time >= CurrentSec())
            {
                _time -= CurrentSec();
                Advance();
                Show();
            }
        }

        private float CurrentSec()
        {
            return _inTurn ? KitchenLayout.Pound15TurnSec[_frame] : KitchenLayout.Pound15StageSec[_frame];
        }

        // 다음 장으로. 단계 끝 → 넘어감(또는 마지막 단계면 7장에서 멈춤), 넘어감 끝 → 다음 단계 0장에서 멈춤.
        private void Advance()
        {
            _frame++;
            if (_inTurn)
            {
                if (_frame >= KitchenLayout.Pound15TurnSec.Length)
                {
                    _inTurn = false;
                    _stage++;
                    _frame = 0;
                    _playing = false;
                }

                return;
            }

            if (_frame < KitchenLayout.Pound15StageSec.Length)
            {
                return;
            }

            if (_stage < _stageMallet.Length - 1)
            {
                _inTurn = true;
                _frame = 0;
                return;
            }

            _frame = KitchenLayout.Pound15StageSec.Length - 1;
            _playing = false;
        }

        private void Show()
        {
            if (_ready == false)
            {
                return;
            }

            if (_inTurn)
            {
                _mallet.sprite = _turnMallet[_stage][_frame];
                _dough.sprite = _turnDough[_stage][_frame];
                _spill.enabled = false;
            }
            else
            {
                _mallet.sprite = _stageMallet[_stage][_frame];
                _dough.sprite = _stageDough[_stage][_frame];
                _spill.sprite = _stageSpill[_stage][_frame];
                _spill.enabled = true;
            }

            bool doughOnTop = KitchenLayout.Pound15DoughOnTop(_inTurn, _frame);
            Image below = doughOnTop ? _mallet : _dough;
            Image above = doughOnTop ? _dough : _mallet;
            below.transform.SetSiblingIndex(_lowIndex);
            above.transform.SetSiblingIndex(_lowIndex + 1);
        }

        private static bool AllLoaded(Sprite[][] groups)
        {
            for (int g = 0; g < groups.Length; g++)
            {
                for (int i = 0; i < groups[g].Length; i++)
                {
                    if (groups[g][i] == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
