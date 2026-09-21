using Mozzi.Core;
using TMPro;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 합성소 · 가판대. 건물 그림(없으면 색 네모) + 이름 + 오른쪽 위 알림 점. 탭은 FarmViewManager 가 받아 화면을 연다.
    // 점은 프리팹 자식 "Badge" 이고 켜고 끄기만 한다 (합성 완료 · 손님 줄).
    public sealed class BuildingObject : ObjectBase
    {
        private const string BadgeName = "Badge";

        // TMP 는 월드에서 fontSize 10 이 1유닛이다. px 로 잡은 글자 크기를 이 값으로 바꿔 준다.
        private const float TmpPointsPerUnit = 10f;

        private SpriteRenderer _renderer;
        private TMP_Text _label;
        private GameObject _badge;
        private BoxCollider2D _collider;

        public BuildingKind Kind { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer.sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }

            _label = GetComponentInChildren<TMP_Text>();
            _collider = GetComponent<BoxCollider2D>();

            Transform badge = transform.Find(BadgeName);
            if (badge != null)
            {
                _badge = badge.gameObject;
                SpriteRenderer badgeRenderer = _badge.GetComponent<SpriteRenderer>();
                if (badgeRenderer != null && badgeRenderer.sprite == null)
                {
                    badgeRenderer.sprite = SpriteUtil.White;
                }
            }
        }

        // sprite 가 null 이면 예전처럼 건물 색 네모다. 그림을 찾아 주는 것은 Manager 몫이다.
        public void Setup(BuildingKind kind, Sprite sprite, Vector2 position)
        {
            Kind = kind;
            transform.position = new Vector3(position.x, position.y, 0f);
            gameObject.name = "Building_" + kind;

            // 몸은 어느 쪽이든 월드에서 140×64px 이다.
            float w = LayoutConst.ToUnits(LayoutConst.BuildingWidthPx);
            float h = LayoutConst.ToUnits(LayoutConst.BuildingHeightPx);

            // 그림은 PPU 32 라 그것만으로 이미 4.375×2.0 유닛이다. 흰 네모는 1×1 이라 그만큼 늘린다.
            float scaleX = 1f;
            float scaleY = 1f;
            if (sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
                scaleX = w;
                scaleY = h;
                if (kind == BuildingKind.Synthesis)
                {
                    _renderer.color = new Color(0.55f, 0.45f, 0.75f);
                }
                else
                {
                    _renderer.color = new Color(0.8f, 0.6f, 0.35f);
                }
            }
            else
            {
                _renderer.sprite = sprite;
                _renderer.color = Color.white;
            }

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // 자식과 콜라이더는 부모 scale 을 타므로 몸 크기를 local 단위로 바꿔 쓴다.
            float localW = w / scaleX;
            float localH = h / scaleY;

            if (_collider != null)
            {
                _collider.size = new Vector2(localW, localH);
            }

            if (_label != null)
            {
                _label.text = NameOf(kind);
                // 부모 scale 이 글자를 늘리니 되돌린다.
                _label.transform.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);

                // 그림에는 아래쪽에 이름 띠가 있다. 네모일 때는 예전처럼 한가운데 둔다.
                float below = 0f;
                if (sprite != null)
                {
                    below = LayoutConst.ToUnits(LayoutConst.BuildingLabelBelowPx) / scaleY;
                    _label.fontSize = LayoutConst.ToUnits(LayoutConst.BuildingLabelPx) * TmpPointsPerUnit;
                }

                _label.transform.localPosition = new Vector3(0f, -below, 0f);
            }

            if (_badge != null)
            {
                // 점도 부모 scale 을 되돌려 14px 로 두고 오른쪽 위 구석에 붙인다.
                float badgeW = LayoutConst.ToUnits(LayoutConst.BadgePx) / scaleX;
                float badgeH = LayoutConst.ToUnits(LayoutConst.BadgePx) / scaleY;
                _badge.transform.localScale = new Vector3(badgeW, badgeH, 1f);
                _badge.transform.localPosition = new Vector3((localW - badgeW) * 0.5f, (localH - badgeH) * 0.5f, 0f);
            }

            SetBadge(false);
        }

        public void SetBadge(bool on)
        {
            if (_badge != null && _badge.activeSelf != on)
            {
                _badge.SetActive(on);
            }
        }

        private static string NameOf(BuildingKind kind)
        {
            if (kind == BuildingKind.Synthesis)
            {
                return "합성소";
            }

            return "가판대";
        }
    }
}
