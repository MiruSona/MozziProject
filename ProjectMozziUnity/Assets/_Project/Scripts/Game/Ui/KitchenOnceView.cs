using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 그림 몇 장을 한 바퀴만 돌리고 숨는 작은 연출 (주방 빚기). 쉴 때는 꺼 둔다.
    // PlayLoop 로 틀면 Stop 할 때까지 되풀이한다 (찌는 동안의 김, 반복 08).
    // 그림이 하나라도 없으면 Play 해도 안 켜지고, 시간만 흘러 같은 때에 끝난다.
    public sealed class KitchenOnceView
    {
        private readonly Image _image;
        private readonly Sprite[] _frames;
        private readonly float[] _frameSec;
        private readonly bool _ready;

        private int _frame;
        private float _time;
        private bool _playing;
        private bool _loop;

        public KitchenOnceView(Image image, Sprite[] frames, float[] frameSec)
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

            Hide();
        }

        public bool IsPlaying
        {
            get { return _playing; }
        }

        // 도는 중에 또 부르면 처음부터 다시 돈다.
        public void Play()
        {
            Begin(false);
        }

        public void PlayLoop()
        {
            Begin(true);
        }

        private void Begin(bool loop)
        {
            _loop = loop;
            _frame = 0;
            _time = 0f;
            _playing = true;
            if (_ready)
            {
                _image.sprite = _frames[0];
                _image.gameObject.SetActive(true);
            }
        }

        public void Stop()
        {
            _playing = false;
            Hide();
        }

        // 한 바퀴를 다 돈 그 틀에만 true. 되풀이 중이면 늘 false.
        public bool Process(float deltaTime)
        {
            if (_playing == false)
            {
                return false;
            }

            _time += deltaTime;
            while (_time >= _frameSec[_frame])
            {
                _time -= _frameSec[_frame];
                _frame++;
                if (_frame >= _frameSec.Length && _loop)
                {
                    _frame = 0;
                }
                else if (_frame >= _frameSec.Length)
                {
                    Stop();
                    return true;
                }

                if (_ready)
                {
                    _image.sprite = _frames[_frame];
                }
            }

            return false;
        }

        private void Hide()
        {
            _image.gameObject.SetActive(false);
        }
    }
}
