using Mozzi.Core;
using Mozzi.Data;
using TMPro;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 바닥에 떨어진 재료 하나. 양 1 이면 낱개(그림만), 2 이상이면 꾸러미(그림 + 수량 글자)다.
    // 자리는 화면만의 것이고 세이브에 안 들어간다 — 모델은 IFarmHub 의 재료별 더미 수 하나다 (FloorItemsAgent 가 맞춘다).
    // 반응 셋을 스스로 돈다 : 발밑에서 톡 튀어 나오기 · 주우면 튀고 사라지기 · 창고가 차서 못 주우면 흔들기.
    public sealed class FloorItemObject : ObjectBase
    {
        private const float PickDuration = 0.35f;
        private const float PickJump = 0.7f;

        private const float DropDuration = 0.35f;
        private const float DropArcPx = 16f;

        private const float ShakeDuration = 0.25f;
        private const float ShakePx = 2f;
        private const float ShakeTurns = 2f;

        // 다섯 자리부터는 만 단위로 줄여 쓴다. 안 줄이면 글자가 옆 꾸러미까지 뻗는다.
        private const int CountShortenFrom = 10000;

        // 수량 글자 외곽선. 풀밭과 옅은 재료 그림 위에서도 흰 글씨가 읽히게 어둡게 두른다.
        // 모바일 SDF 셰이더는 키워드를 켜야 외곽선을 그린다. 10px 남짓한 글자라 외곽선이 흰 속을 갉아먹으니 Dilate 로 되살린다.
        private const string OutlineKeyword = "OUTLINE_ON";
        private const float LabelOutlineWidth = 0.6f;
        private const float LabelFaceDilate = 0.3f;
        private static readonly Color LabelOutlineColor = new Color(0.12f, 0.1f, 0.08f, 1f);

        private SpriteRenderer _renderer;
        private TMP_Text _label;
        private Renderer _labelRenderer;
        private Vector3 _basePosition;

        private bool _dropping;
        private float _dropTime;
        private Vector3 _dropFrom;

        private bool _picking;
        private float _pickTime;

        private bool _shaking;
        private float _shakeTime;

        public string IngredientId { get; private set; }

        public int Amount { get; private set; }

        // 주워서 사라지는 중이면 false. 이때는 더 줍지 않고 화면 합계에도 안 센다.
        public bool IsPickable
        {
            get { return _picking == false && IsDead == false; }
        }

        // 사라지는 반응까지 끝났다. Agent 가 풀에 돌린다.
        public bool IsGone { get; private set; }

        public Vector2 Position
        {
            get { return _basePosition; }
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _label = GetComponentInChildren<TMP_Text>();
            if (_label != null)
            {
                _labelRenderer = _label.GetComponent<Renderer>();
            }
        }

        // 외곽선은 이 오브젝트만의 인스턴스 머티리얼에 건다. 공유 머티리얼을 고치면 같은 폰트를 쓰는 다른 글씨까지 바뀐다.
        // 바닥 물건은 30 이하라 인스턴스도 그만큼이고, 풀에서 다시 빌려도 같은 인스턴스를 그대로 쓴다.
        private void ApplyLabelOutline()
        {
            Material material = _label.fontMaterial;
            material.EnableKeyword(OutlineKeyword);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, LabelOutlineWidth);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, LabelFaceDilate);
            material.SetColor(ShaderUtilities.ID_OutlineColor, LabelOutlineColor);

            // 두꺼워진 테두리가 글자 상자 밖에서 잘리지 않게 여백을 다시 잰다.
            _label.extraPadding = true;
            _label.UpdateMeshPadding();
        }

        // sprite 가 null 이면 재료 색 흰 네모다. 그림을 찾아 주는 것은 Agent 몫이다.
        public void Setup(IngredientRow ingredient, Sprite sprite, Vector2 position, int amount)
        {
            IngredientId = ingredient.Id;
            _basePosition = new Vector3(position.x, position.y, 0f);
            transform.position = _basePosition;
            gameObject.name = "FloorItem_" + ingredient.Id;
            _dropping = false;
            _picking = false;
            _shaking = false;
            IsGone = false;

            _renderer.sprite = sprite;
            _renderer.color = SpriteUtil.ParseHex(ingredient.ColorHex, Color.gray);
            if (sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }
            else
            {
                _renderer.color = Color.white;
            }

            // 찹쌀이(132~)보다 뒤, back 장식(-8)보다 앞에 고정한다.
            _renderer.sortingOrder = LayoutConst.FloorItemSortOrder;

            // 그림 원래 크기로 나눠 목표 28px 을 맞춘다. 흰 네모는 1유닛이라 예전과 같다.
            float size = LayoutConst.ToUnits(LayoutConst.PilePx);
            float scale = size / DivisorOf(_renderer.sprite);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_label != null)
            {
                // 부모 scale 이 글자를 줄이니 되돌린다.
                _label.transform.localScale = new Vector3(1f / scale, 1f / scale, 1f);

                // 그림을 덮지 않게 아래로 내린다. local 값도 부모 scale 을 타므로 나눠 준다.
                float below = LayoutConst.ToUnits(LayoutConst.PileLabelBelowPx) / scale;
                _label.transform.localPosition = new Vector3(0f, -below, 0f);

                if (_labelRenderer != null)
                {
                    _labelRenderer.sortingOrder = LayoutConst.FloorItemLabelSortOrder;
                }

                ApplyLabelOutline();
            }

            SetAmount(amount);
            SetAlpha(1f);
        }

        // 1 이면 글자를 숨긴다 — 낱개는 그림만 보인다.
        public void SetAmount(int amount)
        {
            Amount = amount;
            if (_label == null)
            {
                return;
            }

            _label.enabled = amount >= 2;
            _label.text = CountText(amount);
        }

        // 네 자리까지는 그대로, 다섯 자리부터는 「2.7만」 처럼 줄인다.
        private static string CountText(int count)
        {
            if (count < CountShortenFrom)
            {
                return count.ToString();
            }

            return (count / (float)CountShortenFrom).ToString("0.0") + "만";
        }

        // from(찹쌀이 발밑)에서 Setup 자리까지 낮은 포물선으로 톡 튀어 나온다.
        public void PlayDrop(Vector2 from)
        {
            _dropping = true;
            _dropTime = 0f;
            _dropFrom = new Vector3(from.x, from.y, 0f);
            transform.position = _dropFrom;
        }

        // 주웠다. 지금 자리에서 튀어 오르며 흐려지고, 끝나면 IsGone 이다.
        public void PlayPicked()
        {
            _basePosition = transform.position;
            _dropping = false;
            _shaking = false;
            _picking = true;
            _pickTime = 0f;
        }

        // 창고가 차서 못 주웠다. 훑는 손가락이 매 프레임 불러도 처음부터 다시 흔들지 않는다.
        public void PlayShake()
        {
            if (_shaking || _picking)
            {
                return;
            }

            _shaking = true;
            _shakeTime = 0f;
        }

        public void Step(float deltaTime)
        {
            if (_picking)
            {
                StepPick(deltaTime);
                return;
            }

            Vector3 pos = _basePosition;
            if (_dropping)
            {
                _dropTime += deltaTime;
                float t = Mathf.Clamp01(_dropTime / DropDuration);
                pos = Vector3.Lerp(_dropFrom, _basePosition, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * LayoutConst.ToUnits(DropArcPx);
                if (t >= 1f)
                {
                    _dropping = false;
                }
            }

            if (_shaking)
            {
                _shakeTime += deltaTime;
                float t = Mathf.Clamp01(_shakeTime / ShakeDuration);
                pos.x += Mathf.Sin(t * Mathf.PI * 2f * ShakeTurns) * LayoutConst.ToUnits(ShakePx) * (1f - t);
                if (t >= 1f)
                {
                    _shaking = false;
                }
            }

            transform.position = pos;
        }

        private void StepPick(float deltaTime)
        {
            _pickTime += deltaTime;
            float t = Mathf.Clamp01(_pickTime / PickDuration);
            float jump = Mathf.Sin(t * Mathf.PI) * PickJump;
            transform.position = _basePosition + new Vector3(0f, jump, 0f);
            SetAlpha(1f - t);

            if (t >= 1f)
            {
                IsGone = true;
            }
        }

        public override void OnReturn()
        {
            base.OnReturn();
            _dropping = false;
            _picking = false;
            _shaking = false;
            IsGone = false;
            IngredientId = null;
            Amount = 0;
        }

        // 세로를 기준으로 맞춘다. 0 이면 1유닛으로 본다.
        private static float DivisorOf(Sprite sprite)
        {
            if (sprite == null)
            {
                return 1f;
            }

            float height = sprite.bounds.size.y;
            if (height <= 0f)
            {
                return 1f;
            }

            return height;
        }

        private void SetAlpha(float alpha)
        {
            Color c = _renderer.color;
            c.a = alpha;
            _renderer.color = c;

            if (_label != null)
            {
                Color lc = _label.color;
                lc.a = alpha;
                _label.color = lc;
            }
        }
    }
}
