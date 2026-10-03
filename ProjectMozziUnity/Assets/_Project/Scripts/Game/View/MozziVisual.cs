using System;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 찹쌀이 프리팹의 "Visual" 에 붙어 레이어 그림을 끼우고 애니를 몬다.
    // 시계는 코드가 하나만 갖는다 : Animator 는 speed 0 으로 세워 두고 Play(hash, 0, t) 로 프레임을 직접 고른다.
    // 클립은 스프라이트를 안 물고 _bodyFrame 숫자와 Offset 자리만 흔든다 — 그래서 12종이 클립 한 벌을 쓴다.
    public sealed class MozziVisual : MonoBehaviour
    {
        // 볼은 몸 그림에서 잘라 낸 11×11 이라 기준 자리를 코드가 놓는다.
        // 64 캔버스에서 왼볼 x14~24 · 오른볼 x39~49 · y34~44 이고 몸은 x32 를 축으로 좌우 대칭이다.
        private const float CheekCenterXPx = 12.5f;
        private const float CheekCenterYPx = 7.5f;

        private const float BlinkPeriodSec = 0.8f;
        private const float BlinkAlphaMin = 0.25f;
        private const float BlinkAlphaMax = 1f;

        // 클립이 흔드는 「몇 번째 몸 그림인가」. LateUpdate 가 읽어 Body 에 끼운다.
        [SerializeField]
        private int _bodyFrame;

        private Animator _animator;
        private SpriteRenderer _border;
        private SpriteRenderer _body;
        private SpriteRenderer _cheekL;
        private SpriteRenderer _cheekR;
        private SpriteRenderer _face;
        private SpriteRenderer _acc;
        private Transform _cheekAnchorL;
        private Transform _cheekAnchorR;
        private Transform _cheekOffsetL;
        private Transform _cheekOffsetR;
        private Transform _faceOffset;
        private Transform _accOffset;

        private Sprite[] _bodyHops;
        private bool _hasBody;
        private int _idleHash;
        private int _hopHash;

        // 지금 Body 에 끼워 둔 프레임. 같은 값이면 LateUpdate 가 건너뛴다.
        private int _shownFrame = -1;
        private float _blinkTime;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                throw new InvalidOperationException(ViewNames.MozziVisual + " 에 Animator 가 없다");
            }

            _animator.speed = 0f;
            _idleHash = Animator.StringToHash(ViewNames.AnimIdle);
            _hopHash = Animator.StringToHash(ViewNames.AnimHop);

            _border = FindRenderer(ViewNames.MozziBorder);
            _body = FindRenderer(ViewNames.MozziBody);

            string cheekL = ViewNames.MozziCheekAnchorL + "/" + ViewNames.MozziCheekOffsetL;
            string cheekR = ViewNames.MozziCheekAnchorR + "/" + ViewNames.MozziCheekOffsetR;
            string face = ViewNames.MozziFaceAnchor + "/" + ViewNames.MozziFaceOffset;
            string acc = ViewNames.MozziAccAnchor + "/" + ViewNames.MozziAccOffset;

            _cheekL = FindRenderer(cheekL + "/" + ViewNames.MozziCheek);
            _cheekR = FindRenderer(cheekR + "/" + ViewNames.MozziCheek);
            _face = FindRenderer(face + "/" + ViewNames.MozziFace);
            _acc = FindRenderer(acc + "/" + ViewNames.MozziAcc);

            _cheekAnchorL = FindChild(ViewNames.MozziCheekAnchorL);
            _cheekAnchorR = FindChild(ViewNames.MozziCheekAnchorR);
            _cheekOffsetL = FindChild(cheekL);
            _cheekOffsetR = FindChild(cheekR);
            _faceOffset = FindChild(face);
            _accOffset = FindChild(acc);
        }

        // 클립이 숫자를 옮긴 뒤 여기서 그림으로 바꾼다. Animator 는 LateUpdate 앞에 돌아간다.
        private void LateUpdate()
        {
            if (_bodyFrame == _shownFrame)
            {
                return;
            }

            _shownFrame = _bodyFrame;
            if (_bodyHops == null || _bodyHops.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp(_bodyFrame, 0, _bodyHops.Length - 1);
            Sprite sprite = _bodyHops[index];
            if (sprite == null)
            {
                sprite = SpriteUtil.White;
            }

            _body.sprite = sprite;
        }

        // 몸은 전 종 공용 그림에 종 색을 입히고, 볼·표정·악세사리는 흰색 그대로 얹는다.
        public void Apply(MozziSpriteSet set, Color bodyColor, int faceIndex)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            _bodyHops = set.BodyHops;
            _hasBody = set.HasBody;
            _bodyFrame = 0;
            _shownFrame = -1;
            _body.color = bodyColor;
            _body.enabled = true;

            // 몸 그림이 없으면 흰 네모 폴백이라 볼·표정·악세사리를 안 얹는다 — 얹을 자리가 없다.
            if (set.HasBody == false)
            {
                SetLayer(_cheekL, null);
                SetLayer(_cheekR, null);
                SetLayer(_face, null);
                SetLayer(_acc, null);
                return;
            }

            SetLayer(_cheekL, set.Cheek);
            SetLayer(_cheekR, set.Cheek);
            SetLayer(_face, set.FaceOf(faceIndex));
            SetLayer(_acc, set.Accessory);
        }

        // 표정만 갈아 끼운다. 흰 네모 폴백이면 얹을 자리가 없어 그대로 꺼 둔다.
        public void SetFace(Sprite sprite)
        {
            if (_hasBody == false)
            {
                return;
            }

            SetLayer(_face, sprite);
        }

        // 볼 기준 자리. Animator 는 Anchor 를 안 건드리니 여기서 한 번 놓으면 된다.
        public void SetAnchors()
        {
            float x = LayoutConst.ToUnits(CheekCenterXPx);
            float y = LayoutConst.ToUnits(-CheekCenterYPx);
            _cheekAnchorL.localPosition = new Vector3(-x, y, 0f);
            _cheekAnchorR.localPosition = new Vector3(x, y, 0f);
        }

        public void PlayIdle(float normalized)
        {
            PlayState(_idleHash, Mathf.Repeat(normalized, 1f));
        }

        public void PlayHop(float normalized)
        {
            PlayState(_hopHash, Mathf.Clamp01(normalized));
        }

        // 뜀 높이. 루트가 아니라 Visual 만 올려 판정을 건드리지 않는다.
        public void SetLift(float units)
        {
            transform.localPosition = new Vector3(0f, units, 0f);
        }

        // 변종 테두리. 부모 scale 안에서 배율만 키운다.
        public void SetShiny(bool on, float scale)
        {
            _blinkTime = 0f;
            _border.gameObject.SetActive(on);
            if (on == false)
            {
                return;
            }

            if (_border.sprite == null)
            {
                _border.sprite = SpriteUtil.White;
            }

            _border.transform.localScale = new Vector3(scale, scale, 1f);
            _border.transform.localPosition = Vector3.zero;
        }

        // 변종 테두리 알파 깜박임. 매 프레임 MozziObject 가 불러 준다.
        public void StepBlink(float deltaTime)
        {
            if (_border.gameObject.activeSelf == false)
            {
                return;
            }

            _blinkTime += deltaTime;
            float k = (Mathf.Sin(_blinkTime / BlinkPeriodSec * Mathf.PI * 2f) + 1f) * 0.5f;
            Color c = _border.color;
            c.a = Mathf.Lerp(BlinkAlphaMin, BlinkAlphaMax, k);
            _border.color = c;
        }

        // 풀에 되돌릴 때. 다음에 빌려 줄 마리에 지난 자리가 남지 않게 한다.
        public void ResetState()
        {
            _bodyFrame = 0;
            _shownFrame = -1;
            _cheekOffsetL.localPosition = Vector3.zero;
            _cheekOffsetR.localPosition = Vector3.zero;
            _faceOffset.localPosition = Vector3.zero;
            _accOffset.localPosition = Vector3.zero;
            SetShiny(false, 1f);
            SetLift(0f);
            PlayIdle(0f);
        }

        // Play 만으로는 그 프레임에 값이 안 들어온다 — Update(0) 로 한 번 굴려야 반영된다 (실측 2026-09-20).
        // 꺼진 마리(디버그 · 캡처 연출로 SetActive(false) 한 것)는 목록에 남아 Step 을 계속 받는다.
        // 꺼진 Animator 에 Play · Update 를 부르면 Unity 가 매 프레임 경고를 찍으니 건너뛴다.
        // 다시 켜지면 다음 Step 이 상태를 새로 고르니 따로 되살릴 것이 없다.
        private void PlayState(int stateHash, float normalized)
        {
            if (_animator.isActiveAndEnabled == false)
            {
                return;
            }

            _animator.Play(stateHash, 0, normalized);
            _animator.Update(0f);
        }

        private static void SetLayer(SpriteRenderer renderer, Sprite sprite)
        {
            if (sprite == null)
            {
                renderer.enabled = false;
                return;
            }

            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.enabled = true;
        }

        private Transform FindChild(string path)
        {
            Transform child = transform.Find(path);
            if (child == null)
            {
                throw new InvalidOperationException(ViewNames.MozziVisual + " 아래 " + path + " 자식이 없다");
            }

            return child;
        }

        private SpriteRenderer FindRenderer(string path)
        {
            SpriteRenderer renderer = FindChild(path).GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(path + " 에 SpriteRenderer 가 없다");
            }

            return renderer;
        }
    }
}
