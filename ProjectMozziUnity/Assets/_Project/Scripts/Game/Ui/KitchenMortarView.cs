using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 절구 + 떡메 조각을 프레임으로 돌린다. Play 한 번에 0→끝을 한 바퀴 돌고 0 에서 멈춘다.
    // 그림이 하나라도 없으면 아무것도 안 바꾼다 (자리표 그림 그대로).
    public sealed class KitchenMortarView
    {
        private readonly Image _image;
        private readonly Sprite[] _frames;
        private readonly float[] _frameSec;
        private readonly bool _ready;

        private int _frame;
        private float _time;
        private bool _playing;

        public KitchenMortarView(Image image, Sprite[] frames, float[] frameSec)
        {
            _image = image;
            _frames = frames;
            _frameSec = frameSec;
            _ready = frames.Length > 0 && frames.Length == frameSec.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    _ready = false;
                }
            }
        }

        // 치는 중에 또 누르면 처음부터 다시 친다.
        public void Play()
        {
            _frame = 0;
            _time = 0f;
            _playing = true;
            Show(0);
        }

        public void Stop()
        {
            _playing = false;
            _frame = 0;
            Show(0);
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

                Show(_frame);
            }
        }

        private void Show(int frame)
        {
            if (_ready == false)
            {
                return;
            }

            _image.sprite = _frames[frame];
        }
    }
}
