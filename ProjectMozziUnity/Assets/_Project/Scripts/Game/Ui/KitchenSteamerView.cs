using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 1단 유리 찜기 (반복 09, steamer2). 겹 그림(뚜껑 둘 · 떡 · 김 서림)과 꼭지 김을 Process 의 수동 타이머로 움직인다.
    // Begin : 반죽이 들어온 순간부터 4초 연출 (뚜껑 닫힘 → 김 서림 → 김 되풀이 · 덜컹 · 떡 익음/부풂 → 김 서림 빠짐).
    // Open : 뚜껑을 열고 김을 한 번 낸 뒤, 기다림이 끝난 틀에 Process 가 true (화면은 그때 결과를 보인다). 시간 표는 README · KitchenLayout.
    public sealed class KitchenSteamerView
    {
        private readonly Image _lidClosed;
        private readonly Image _lidOpen;
        private readonly Image _dough;
        private readonly Image _fog;
        private readonly RectTransform _steamRect;
        private readonly KitchenOnceView _steam;
        private readonly Vector2 _lidClosedPos;
        private readonly Vector2 _lidOpenPos;
        private readonly Vector2 _steamOpenPos;
        private Color _doughStart = Color.white;
        private float _time;
        private float _waitSec;
        private bool _cooking;
        private bool _opening;
        private bool _open;
        private bool _steamStarted;

        public KitchenSteamerView(Image lidClosed, Image lidOpen, Image dough, Image fog, Image steam, Sprite[] steamFrames)
        {
            _lidClosed = lidClosed;
            _lidOpen = lidOpen;
            _dough = dough;
            _fog = fog;
            _steamRect = steam.rectTransform;
            _steam = new KitchenOnceView(steam, steamFrames, KitchenLayout.Steam9FrameSec);
            _lidClosedPos = lidClosed.rectTransform.anchoredPosition;
            _lidOpenPos = lidOpen.rectTransform.anchoredPosition;
            _steamOpenPos = _steamRect.anchoredPosition;
            // 떡은 꽉 찬 캔버스 겹이라, 부풂이 떡 아래 가운데를 기준으로 되게 pivot 만 옮긴다 (stretch 라 자리는 안 바뀜).
            _dough.rectTransform.pivot = KitchenLayout.Steamer2DoughPivot;
            Reset();
        }

        // 결과를 보였거나 여는 중 (결과를 보인 뒤 다음 반죽이 들어오면 풀린다).
        public bool IsOpen
        {
            get { return _open || _opening; }
        }

        public bool IsPlaying
        {
            get { return _cooking || _opening; }
        }

        // 시작 반죽색. 빚기 · 떡메와 같은 식으로 화면이 준다.
        public void SetDough(Color tint)
        {
            _doughStart = tint;
            _dough.color = tint;
        }

        // 반죽을 넣는 동안 : 뚜껑 열림, 떡은 아직 안 보임.
        public void ShowOpenBody()
        {
            Reset();
        }

        // 반죽이 찜기에 닿은 순간 : 떡 겹을 켜고 4초 연출을 시작한다.
        public void Begin()
        {
            Reset();
            _dough.color = _doughStart;
            _dough.gameObject.SetActive(true);
            _cooking = true;
        }

        public void Open(float stepSec)
        {
            _cooking = false;
            _waitSec = stepSec * (KitchenLayout.LidOpenRise.Length - 1);
            _time = 0f;
            _opening = true;
            _open = false;
            SetFog(0f, 0f);
            ShowLid(true, 0f);
            _steamRect.anchoredPosition = _steamOpenPos;
            _steamRect.gameObject.SetActive(true);
            _steam.Play();
        }

        public void Reset()
        {
            _cooking = false;
            _opening = false;
            _open = false;
            _steamStarted = false;
            _time = 0f;
            _steam.Stop();
            _steamRect.gameObject.SetActive(false);
            _steamRect.anchoredPosition = _steamOpenPos;
            ShowLid(true, 0f);
            SetFog(0f, 0f);
            _dough.rectTransform.localScale = Vector3.one;
            _dough.gameObject.SetActive(false);
        }

        public bool Process(float deltaTime)
        {
            _steam.Process(deltaTime);
            if (_cooking)
            {
                _time += deltaTime;
                ProcessCook(_time);
                return false;
            }

            if (_opening == false)
            {
                return false;
            }

            _time += deltaTime;
            if (_time < _waitSec)
            {
                return false;
            }

            _opening = false;
            _open = true;
            _dough.gameObject.SetActive(false);
            return true;
        }

        private void ProcessCook(float t)
        {
            ShowLid(t < KitchenLayout.Steamer2LidCloseSec, RattleUp(t));

            float fill = Ramp(t, KitchenLayout.Steamer2FogStartSec, KitchenLayout.Steamer2FogFullSec);
            float fade = 1f - Ramp(t, KitchenLayout.Steamer2FogOutStartSec, KitchenLayout.SteamSec);
            SetFog(fill, fill * fade);

            if (_steamStarted == false && t >= KitchenLayout.Steamer2SteamStartSec)
            {
                _steamStarted = true;
                _steamRect.anchoredPosition = _steamOpenPos + new Vector2(0f, KitchenLayout.Steam9Bottom - KitchenLayout.Steam9ClosedBottom);
                _steamRect.gameObject.SetActive(true);
                _steam.PlayLoop();
            }

            // 완성색 = 시작색에 (255,241,206) 을 곱한 것 — 물들인 색은 남기고 노릇하게.
            Color done = _doughStart * KitchenLayout.Steamer2CookedColor;
            done.a = _doughStart.a;
            _dough.color = Color.Lerp(_doughStart, done, Ramp(t, KitchenLayout.Steamer2CookStartSec, KitchenLayout.Steamer2CookEndSec));
            _dough.rectTransform.localScale = t >= KitchenLayout.Steamer2PuffSec ? KitchenLayout.Steamer2PuffScale : Vector3.one;
        }

        // 덜컹 시각마다 RattleLen 동안 위로 RattleUp px.
        private static float RattleUp(float t)
        {
            float[] at = KitchenLayout.Steamer2RattleSec;
            for (int i = 0; i < at.Length; i++)
            {
                if (t >= at[i] && t < at[i] + KitchenLayout.Steamer2RattleLen)
                {
                    return KitchenLayout.Steamer2RattleUp;
                }
            }

            return 0f;
        }

        private static float Ramp(float t, float from, float to)
        {
            return Mathf.Clamp01((t - from) / (to - from));
        }

        private void ShowLid(bool open, float up)
        {
            _lidOpen.gameObject.SetActive(open);
            _lidClosed.gameObject.SetActive(open == false);
            _lidOpen.rectTransform.anchoredPosition = _lidOpenPos + new Vector2(0f, up);
            _lidClosed.rectTransform.anchoredPosition = _lidClosedPos + new Vector2(0f, up);
        }

        private void SetFog(float fill, float alpha)
        {
            _fog.fillAmount = fill;
            Color c = _fog.color;
            c.a = alpha;
            _fog.color = c;
        }
    }
}
