using Mozzi.Core;
using Mozzi.Data.Rows;
using TMPro;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 바닥 재료 더미. 재료 색 네모 + 수량 글자. 탭하면 FarmViewManager 가 IFarmHub.PickUp 을 부른다.
    // 주우면 살짝 튀어 오르며 사라지는 반응 1종을 스스로 돈다.
    public sealed class PileObject : ObjectBase
    {
        private const float PickDuration = 0.35f;
        private const float PickJump = 0.7f;

        // 다섯 자리부터는 만 단위로 줄여 쓴다. 안 줄이면 글자가 옆 더미까지 뻗는다.
        private const int CountShortenFrom = 10000;

        // 수량 글자 외곽선. 풀밭과 옅은 재료 그림 위에서도 흰 글씨가 읽히게 어둡게 두른다.
        // 모바일 SDF 셰이더는 키워드를 켜야 외곽선을 그린다. 10px 남짓한 글자라 외곽선이 흰 속을 갉아먹으니 Dilate 로 되살린다.
        private const string OutlineKeyword = "OUTLINE_ON";
        private const float LabelOutlineWidth = 0.6f;
        private const float LabelFaceDilate = 0.3f;
        private static readonly Color LabelOutlineColor = new Color(0.12f, 0.1f, 0.08f, 1f);

        private SpriteRenderer _renderer;
        private TMP_Text _label;
        private Vector3 _basePosition;
        private float _pickTime;
        private bool _picking;

        public string IngredientId { get; private set; }

        public bool IsAnimating
        {
            get { return _picking; }
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _label = GetComponentInChildren<TMP_Text>();
        }

        // 외곽선은 이 오브젝트만의 인스턴스 머티리얼에 건다. 공유 머티리얼을 고치면 같은 폰트를 쓰는 다른 글씨까지 바뀐다.
        // 더미는 여섯이라 인스턴스 여섯 벌이면 끝이고, 풀에서 다시 빌려도 같은 인스턴스를 그대로 쓴다.
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

        // sprite 가 null 이면 예전처럼 재료 색 흰 네모다. 그림 묶음을 찾아 주는 것은 Manager 몫이다.
        public void Setup(IngredientRow ingredient, Sprite sprite, Vector2 position)
        {
            IngredientId = ingredient.id;
            _basePosition = new Vector3(position.x, position.y, 0f);
            transform.position = _basePosition;
            gameObject.name = "Pile_" + ingredient.id;

            _renderer.sprite = sprite;
            _renderer.color = SpriteUtil.ParseHex(ingredient.colorHex, Color.gray);
            if (sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }
            else
            {
                _renderer.color = Color.white;
            }

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

                ApplyLabelOutline();
            }

            ResetVisual();
        }

        public void SetCount(int count)
        {
            if (_label != null)
            {
                _label.text = CountText(count);
            }
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

        // 줍기 반응 시작. 끝나면 ResetVisual 로 돌아온다.
        public void PlayPicked()
        {
            _picking = true;
            _pickTime = 0f;
        }

        public void Step(float deltaTime)
        {
            if (_picking == false)
            {
                return;
            }

            _pickTime += deltaTime;
            float t = Mathf.Clamp01(_pickTime / PickDuration);
            float jump = Mathf.Sin(t * Mathf.PI) * PickJump;
            transform.position = _basePosition + new Vector3(0f, jump, 0f);
            SetAlpha(1f - t);

            if (t >= 1f)
            {
                _picking = false;
                ResetVisual();
            }
        }

        public override void OnReturn()
        {
            base.OnReturn();
            _picking = false;
            IngredientId = null;
        }

        // 세로를 기준으로 맞춘다. 0 이면 예전처럼 1유닛으로 본다.
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

        private void ResetVisual()
        {
            transform.position = _basePosition;
            SetAlpha(1f);
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
