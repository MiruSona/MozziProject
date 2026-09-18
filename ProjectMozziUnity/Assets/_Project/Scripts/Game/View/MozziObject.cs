using Mozzi.Core;
using Mozzi.Data.Rows;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 농장의 찹쌀이 한 마리. 종 색(MozziSpecies 표 colorHex) + 단계 크기의 네모다. 걷기는 WanderAgent 가 맡는다.
    // 변종은 프리팹 자식 "Border"(밝은 테두리 네모)를 켜고 알파를 깜빡인다. 흰찹쌀이(grass1)만 흰색.
    public sealed class MozziObject : ObjectBase
    {
        private const string BorderName = "Border";
        private const string WhiteSpeciesId = "grass1";
        private const float BlinkPeriodSec = 0.8f;
        private const float BlinkAlphaMin = 0.25f;
        private const float BlinkAlphaMax = 1f;

        private SpriteRenderer _renderer;
        private SpriteRenderer _border;
        private WanderAgent _wander;
        private Rect _area;
        private float _blinkTime;

        public int MozziId { get; private set; }

        public string SpeciesId { get; private set; }

        // 겹침 밀어내기에 쓰는 반지름 (유닛).
        public float Radius { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer.sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }

            Transform border = transform.Find(BorderName);
            if (border != null)
            {
                _border = border.GetComponent<SpriteRenderer>();
                if (_border != null && _border.sprite == null)
                {
                    _border.sprite = SpriteUtil.White;
                }
            }

            _wander = new WanderAgent(this);
        }

        public void Setup(int mozziId, MozziSpeciesRow species, Rect area, Vector2 start)
        {
            MozziId = mozziId;
            SpeciesId = species.id;
            _area = area;

            float sizePx = SizePxOf(species.tier);
            float size = LayoutConst.ToUnits(sizePx);
            Radius = size * 0.5f;
            transform.localScale = new Vector3(size, size, 1f);
            transform.position = new Vector3(start.x, start.y, 0f);
            gameObject.name = "Mozzi_" + mozziId + "_" + species.id;

            _renderer.color = ColorOf(species);
            SetupBorder(species.isShiny);
            _wander.Reset(area, start);
        }

        public void Step(float deltaTime)
        {
            Vector2 next = _wander.Step(transform.position, deltaTime);
            transform.position = new Vector3(next.x, next.y, 0f);
            StepBlink(deltaTime);
        }

        // 밀어낸 뒤 담장 안으로 되돌린다.
        public void Nudge(Vector2 delta)
        {
            Vector2 pos = (Vector2)transform.position + delta;
            pos.x = Mathf.Clamp(pos.x, _area.xMin, _area.xMax);
            pos.y = Mathf.Clamp(pos.y, _area.yMin, _area.yMax);
            transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        public override void OnReturn()
        {
            base.OnReturn();
            MozziId = 0;
            SpeciesId = null;
        }

        // 테두리는 부모 scale 안에서 배율만 키운다. 변종이 아니면 꺼 둔다.
        private void SetupBorder(bool isShiny)
        {
            if (_border == null)
            {
                return;
            }

            _border.gameObject.SetActive(isShiny);
            if (isShiny == false)
            {
                return;
            }

            float scale = LayoutConst.ShinyBorderScale;
            _border.transform.localScale = new Vector3(scale, scale, 1f);
            _border.transform.localPosition = Vector3.zero;
            _blinkTime = 0f;
        }

        private void StepBlink(float deltaTime)
        {
            if (_border == null || _border.gameObject.activeSelf == false)
            {
                return;
            }

            _blinkTime += deltaTime;
            float k = (Mathf.Sin(_blinkTime / BlinkPeriodSec * Mathf.PI * 2f) + 1f) * 0.5f;
            Color c = _border.color;
            c.a = Mathf.Lerp(BlinkAlphaMin, BlinkAlphaMax, k);
            _border.color = c;
        }

        private static float SizePxOf(int tier)
        {
            if (tier >= 3)
            {
                return LayoutConst.MozziTier3Px;
            }

            if (tier == 2)
            {
                return LayoutConst.MozziTier2Px;
            }

            return LayoutConst.MozziTier1Px;
        }

        // 계열 색 · 단계 밝기는 표의 colorHex 가 정본이다. 흰찹쌀이만 흰색 (임시 그림 대신).
        private static Color ColorOf(MozziSpeciesRow species)
        {
            if (species.id == WhiteSpeciesId)
            {
                return Color.white;
            }

            return SpriteUtil.ParseHex(species.colorHex, Color.gray);
        }
    }
}
