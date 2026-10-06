using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 절구 + 떡메 조각 · 맷돌을 프레임으로 돌린다. Play 한 번에 0→끝을 한 바퀴 돌고 쉬는 장(0)에서 멈춘다.
    // 떡메는 반죽 단계(가루 · 거친 반죽 · 매끈한 떡)마다 7장 묶음이 따로 있다 — 맞는 장(hitFrame) 앞은 from 묶음, 맞는 장부터는 to 묶음 (KitchenLayout.FrameStage).
    // 맷돌은 묶음 하나(5장)라 from · to 가 늘 0 이다.
    // 쉬는 장은 _idleStage 묶음의 0번이다. 그림이 하나라도 없으면 아무것도 안 바꾼다 (자리표 그림 그대로).
    public sealed class KitchenMortarView
    {
        private readonly Image _image;
        private readonly Sprite[][] _stages;
        private readonly float[] _frameSec;
        private readonly int _hitFrame;
        private readonly bool _ready;

        private int _frame;
        private float _time;
        private bool _playing;
        private int _from;
        private int _to;
        private int _idleStage;

        public KitchenMortarView(Image image, Sprite[][] stages, float[] frameSec, int hitFrame)
        {
            _hitFrame = hitFrame;
            _image = image;
            _stages = stages;
            _frameSec = frameSec;
            _ready = stages.Length > 0;
            for (int s = 0; s < stages.Length; s++)
            {
                Sprite[] frames = stages[s];
                if (frames.Length != frameSec.Length)
                {
                    _ready = false;
                    continue;
                }

                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i] == null)
                    {
                        _ready = false;
                    }
                }
            }
        }

        // 한 번 치기. 치는 중에 또 누르면 처음부터 다시 친다. 다 치면 to 단계의 쉬는 장에 선다.
        public void Play(int from, int to)
        {
            _from = from;
            _to = to;
            _idleStage = to;
            _frame = 0;
            _time = 0f;
            _playing = true;
            Show(_from, 0);
        }

        // 쉬는 장의 단계를 바꾼다. 치는 중이면 다 친 뒤에 그 단계로 선다.
        public void SetIdle(int stage)
        {
            _idleStage = stage;
            if (_playing == false)
            {
                Show(stage, 0);
            }
        }

        public void Stop()
        {
            _playing = false;
            _frame = 0;
            Show(_idleStage, 0);
        }

        public void Process(float deltaTime)
        {
            if (_playing == false)
            {
                return;
            }

            _time += deltaTime;
            while (_playing && _time >= _frameSec[_frame])
            {
                _time -= _frameSec[_frame];
                _frame++;
                if (_frame >= _frameSec.Length)
                {
                    Stop();
                    return;
                }

                Show(KitchenLayout.FrameStage(_frame, _hitFrame, _from, _to), _frame);
            }
        }

        private void Show(int stage, int frame)
        {
            if (_ready == false)
            {
                return;
            }

            _image.sprite = _stages[stage][frame];
        }
    }
}
