using UnityEngine;

namespace Mozzi.Game.Ui
{
    // 찜기 뚜껑 (반복 08). 몸통과 같은 자리에 겹친 그림을 위로 올려 연다 — KitchenLayout.LidOpenRise 칸을 한 칸씩 (0 → 6 → 14 → 24 px).
    // 열기를 다 마친 그 틀에 Process 가 true 를 돌려준다 (화면은 그때 결과를 보인다). 닫기는 바로 제자리.
    public sealed class KitchenLidView
    {
        private readonly RectTransform _rect;
        private readonly Vector2 _home;

        private float _stepSec;
        private float _time;
        private int _step;
        private bool _opening;
        private bool _open;

        public KitchenLidView(RectTransform rect)
        {
            _rect = rect;
            _home = rect.anchoredPosition;
        }

        // 열렸거나 여는 중.
        public bool IsOpen
        {
            get { return _open || _opening; }
        }

        public void Open(float stepSec)
        {
            _stepSec = stepSec;
            _time = 0f;
            _step = 0;
            _opening = true;
            _open = false;
            Show(0);
        }

        public void Close()
        {
            _opening = false;
            _open = false;
            Show(0);
        }

        public bool Process(float deltaTime)
        {
            if (_opening == false)
            {
                return false;
            }

            _time += deltaTime;
            while (_opening && _time >= _stepSec)
            {
                _time -= _stepSec;
                _step++;
                Show(_step);
                if (_step >= KitchenLayout.LidOpenRise.Length - 1)
                {
                    _opening = false;
                    _open = true;
                    return true;
                }
            }

            return false;
        }

        // UI 는 위가 + 라 그림 좌표(위가 −)의 올림을 그대로 더한다.
        private void Show(int step)
        {
            _rect.anchoredPosition = _home + new Vector2(0f, KitchenLayout.LidOpenRise[step]);
        }
    }
}
