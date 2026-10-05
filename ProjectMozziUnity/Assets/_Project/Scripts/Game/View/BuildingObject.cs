using Mozzi.Core;
using TMPro;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 메인 화면 건물 하나 (합성 기계 · 가게 · 보관함 궤짝). 그림(없으면 색 네모) + 이름표 + 알림 점.
    // 자리는 BuildingLayout 이 정본이다. 탭 · 끌어다 놓기 판정은 FarmViewManager 가 받는다.
    public sealed class BuildingObject : ObjectBase
    {
        private const string BadgeName = "Badge";

        // TMP 는 월드에서 fontSize 10 이 1유닛이다. px 로 잡은 글자 크기를 이 값으로 바꿔 준다.
        private const float TmpPointsPerUnit = 10f;

        private static readonly Color LabelColor = new Color(0.29f, 0.18f, 0.13f, 1f);

        private SpriteRenderer _renderer;
        private TMP_Text _label;
        private GameObject _badge;
        private SpriteRenderer _badgeRenderer;
        // 표식 그림이 없을 때 쓰는 색 네모의 색 (프리팹에 적힌 빨강).
        private Color _badgeFallbackColor;
        private BoxCollider2D _collider;
        private Rect _hitWorld;

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
                _badgeRenderer = _badge.GetComponent<SpriteRenderer>();
                _badgeFallbackColor = _badgeRenderer.color;
            }
        }

        // sprite 가 null 이면 누르는 네모 크기의 색 네모다. 그림을 찾아 주는 것은 Manager 몫이다.
        // badgeSprite 는 알림 표식 그림(16px)이고, null 이면 같은 크기의 빨간 네모로 대신한다.
        public void Setup(BuildingKind kind, Sprite sprite, Sprite badgeSprite, string label)
        {
            Kind = kind;
            gameObject.name = "Building_" + kind;

            BuildingLayout layout = BuildingLayout.For(kind);
            _hitWorld = layout.HitWorld;

            // 그림은 피벗이 아래 가운데라 발끝에 둔다. 흰 네모(1×1, 가운데 피벗)는 네모 가운데에 두고 늘린다.
            Vector2 rootPos = layout.FootWorld;
            Vector2 scale = Vector2.one;
            if (sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
                _renderer.color = FallbackColor(kind);
                rootPos = _hitWorld.center;
                scale = _hitWorld.size;
            }
            else
            {
                _renderer.sprite = sprite;
                _renderer.color = Color.white;
            }

            transform.position = new Vector3(rootPos.x, rootPos.y, 0f);
            transform.localScale = new Vector3(scale.x, scale.y, 1f);

            // 자식 · 콜라이더는 부모 scale 을 타므로 월드 값을 local 로 바꿔 쓴다.
            if (_collider != null)
            {
                _collider.size = Divide(_hitWorld.size, scale);
                _collider.offset = Divide(_hitWorld.center - rootPos, scale);
            }

            if (_label != null)
            {
                _label.text = label;
                _label.color = LabelColor;
                _label.fontSize = LayoutConst.ToUnits(LayoutConst.BuildingLabelPx) * TmpPointsPerUnit;
                _label.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
                _label.transform.localPosition = Divide(layout.LabelWorld - rootPos, scale);
            }

            if (_badge != null)
            {
                // 표식 그림은 제 크기(×1)로 찍고, 흰 네모(1×1)는 표식 크기만큼 늘린다.
                float badge = 1f;
                _badgeRenderer.sprite = badgeSprite;
                _badgeRenderer.color = Color.white;
                if (badgeSprite == null)
                {
                    badge = LayoutConst.ToUnits(LayoutConst.BadgePx);
                    _badgeRenderer.sprite = SpriteUtil.White;
                    _badgeRenderer.color = _badgeFallbackColor;
                }

                _badge.transform.localScale = new Vector3(badge / scale.x, badge / scale.y, 1f);
                _badge.transform.localPosition = Divide(layout.BadgeWorld - rootPos, scale);
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

        // 월드 점이 누르는 네모 안인가. 찹쌀이를 끌어다 놓을 자리 판정이다.
        public bool ContainsWorld(Vector2 world)
        {
            return _hitWorld.Contains(world);
        }

        private static Vector3 Divide(Vector2 value, Vector2 scale)
        {
            return new Vector3(value.x / scale.x, value.y / scale.y, 0f);
        }

        private static Color FallbackColor(BuildingKind kind)
        {
            if (kind == BuildingKind.Synthesis)
            {
                return new Color(0.55f, 0.45f, 0.75f);
            }

            if (kind == BuildingKind.Stall)
            {
                return new Color(0.8f, 0.6f, 0.35f);
            }

            return new Color(0.6f, 0.42f, 0.3f);
        }
    }
}
