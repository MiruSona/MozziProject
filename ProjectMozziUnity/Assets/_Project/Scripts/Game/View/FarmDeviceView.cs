using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 농장 장치 하나(수집기 · 먹이 공급기). 누를 수 없는 월드 그림이라 FarmDecorView 처럼 SpriteRenderer 하나에 y 정렬이다.
    // 0 장이 쉼이고 Play 하면 1 ~ 끝 장을 한 바퀴 돌고 쉼으로 돌아온다. 시간은 Process 의 deltaTime(게임 시간)을 쓴다.
    // 뿌리는 그림 아래 가운데에 둔다 — 눌렀다 펴기(squash)가 바닥에 붙은 채로 일어나게.
    public sealed class FarmDeviceView
    {
        private Sprite[] _frames;
        private float _frameSec;
        private Vector2 _squash;

        private GameObject _root;
        private SpriteRenderer _renderer;
        private bool _playing;
        private float _time;

        public bool IsShown
        {
            get { return _root != null && _root.activeSelf; }
        }

        // 한 바퀴 도는 중이면 true. 끝나면 0 장(쉼)이다.
        public bool IsPlaying
        {
            get { return _playing; }
        }

        // 그림 왼쪽 아래 끝 = (leftPx, 위에서 잰 bottomPx). 처음에는 숨겨 둔다. squash 가 (1,1) 이면 크기는 안 바뀐다.
        public void Build(Transform parent, IResourceHub resource, string name, string spritePrefix, int frameCount,
            float leftPx, float widthPx, float bottomPx, float frameSec, Vector2 squash)
        {
            Clear();
            _frameSec = frameSec;
            _squash = squash;
            _frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                _frames[i] = resource.GetSprite(spritePrefix + i);
            }

            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);
            float bottomY = LayoutConst.YFromTop(bottomPx);
            _root.transform.localPosition = new Vector3(LayoutConst.XFromLeft(leftPx + widthPx * 0.5f), bottomY, 0f);

            GameObject art = new GameObject(name + "Art");
            art.transform.SetParent(_root.transform, false);
            _renderer = art.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames[0];
            // 찹쌀이 · front 장식과 같은 식이다 — 발 y 가 장치 아래끝보다 낮은 마리는 장치 앞에 선다.
            _renderer.sortingOrder = LayoutConst.MozziSortBase - Mathf.RoundToInt(bottomY * LayoutConst.PixelsPerUnit);
            if (_frames[0] != null)
            {
                Bounds bounds = _frames[0].bounds;
                art.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);
            }

            _root.SetActive(false);
        }

        public void SetShown(bool shown)
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(shown);
            if (shown == false)
            {
                Stop();
            }
        }

        // 한 바퀴 돈다. 이미 도는 중이면 처음부터 다시 하지 않는다 (재료 여럿을 한 번에 걷어도 한 바퀴).
        public void Play()
        {
            if (IsShown == false || _playing)
            {
                return;
            }

            _playing = true;
            _time = 0f;
        }

        public void Step(float deltaTime)
        {
            if (_playing == false)
            {
                return;
            }

            _time += deltaTime;
            int loopFrames = _frames.Length - 1;
            float duration = loopFrames * _frameSec;
            if (_time >= duration)
            {
                Stop();
                return;
            }

            int frame = 1 + Mathf.Min(loopFrames - 1, Mathf.FloorToInt(_time / _frameSec));
            ApplyFrame(frame);

            float s = Mathf.Sin(_time / duration * Mathf.PI);
            _root.transform.localScale = new Vector3(Mathf.Lerp(1f, _squash.x, s), Mathf.Lerp(1f, _squash.y, s), 1f);
        }

        public void Clear()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _renderer = null;
            _playing = false;
        }

        private void Stop()
        {
            _playing = false;
            _time = 0f;
            ApplyFrame(0);
            _root.transform.localScale = Vector3.one;
        }

        // 그림이 빠진 장은 앞 장을 그대로 둔다.
        private void ApplyFrame(int frame)
        {
            if (_frames[frame] != null)
            {
                _renderer.sprite = _frames[frame];
            }
        }
    }
}
