using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 합성소 · 가판대 자리표. 큰 네모 + 이름 + 오른쪽 위 알림 점. 탭은 FarmViewManager 가 받아 화면을 연다.
    // 점은 프리팹 자식 "Badge" 이고 켜고 끄기만 한다 (합성 완료 · 손님 줄).
    public sealed class BuildingObject : ObjectBase
    {
        private const string BadgeName = "Badge";

        private SpriteRenderer _renderer;
        private TextMesh _label;
        private GameObject _badge;

        public BuildingKind Kind { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer.sprite == null)
            {
                _renderer.sprite = SpriteUtil.White;
            }

            _label = GetComponentInChildren<TextMesh>();
            if (_label != null && _label.font == null)
            {
                _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _label.GetComponent<MeshRenderer>().material = _label.font.material;
            }

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

        public void Setup(BuildingKind kind, Vector2 position)
        {
            Kind = kind;
            transform.position = new Vector3(position.x, position.y, 0f);
            gameObject.name = "Building_" + kind;

            float w = LayoutConst.ToUnits(LayoutConst.BuildingWidthPx);
            float h = LayoutConst.ToUnits(LayoutConst.BuildingHeightPx);
            transform.localScale = new Vector3(w, h, 1f);

            if (kind == BuildingKind.Synthesis)
            {
                _renderer.color = new Color(0.55f, 0.45f, 0.75f);
            }
            else
            {
                _renderer.color = new Color(0.8f, 0.6f, 0.35f);
            }

            if (_label != null)
            {
                _label.text = NameOf(kind);
                // 부모 scale 이 글자를 늘리니 되돌린다.
                _label.transform.localScale = new Vector3(1f / w, 1f / h, 1f);
            }

            if (_badge != null)
            {
                // 점도 부모 scale 을 되돌려 px 크기로 둔다.
                float badge = LayoutConst.ToUnits(LayoutConst.BadgePx);
                _badge.transform.localScale = new Vector3(badge / w, badge / h, 1f);
                _badge.transform.localPosition = new Vector3(0.5f - badge / w * 0.5f, 0.5f - badge / h * 0.5f, 0f);
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
