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
            if (_renderer.sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }

            _label = GetComponentInChildren<TMP_Text>();
        }

        public void Setup(IngredientRow ingredient, Vector2 position)
        {
            IngredientId = ingredient.id;
            _basePosition = new Vector3(position.x, position.y, 0f);
            transform.position = _basePosition;
            gameObject.name = "Pile_" + ingredient.id;

            float size = LayoutConst.ToUnits(LayoutConst.PilePx);
            transform.localScale = new Vector3(size, size, 1f);
            _renderer.color = SpriteUtil.ParseHex(ingredient.colorHex, Color.gray);
            if (_label != null)
            {
                // 부모 scale 이 글자를 줄이니 되돌린다.
                _label.transform.localScale = new Vector3(1f / size, 1f / size, 1f);
            }

            ResetVisual();
        }

        public void SetCount(int count)
        {
            if (_label != null)
            {
                _label.text = count.ToString();
            }
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
