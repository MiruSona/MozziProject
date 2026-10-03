using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 메인 화면의 하늘. 아래끝을 지평선(SkyBottomPx)에 맞춰 하늘 그림(없으면 단색)을 깔고, 카메라 배경색을 하늘 맨 윗줄 색으로 둔다.
    // 하늘은 월드 안 건물 뒤까지 내려오고, 길쭉한 폰에서 월드 위끝 위로 남는 자리도 같이 덮는다.
    public sealed class SkyView
    {
        private GameObject _root;

        public void Build(Transform parent, IResourceHub resource, Camera camera)
        {
            Clear();

            _root = new GameObject(ViewNames.SkyRoot);
            _root.transform.SetParent(parent, false);

            camera.backgroundColor = LayoutConst.SkyTopColor;

            float skyBottomY = LayoutConst.YFromTop(LayoutConst.SkyBottomPx);
            Sprite sky = resource.GetSprite(SpriteKeys.MainSky);
            if (sky != null)
            {
                SpriteRenderer art = AddRenderer(ViewNames.SkyArt, sky, Color.white);
                art.transform.localPosition = new Vector3(0f, skyBottomY, 0f);
            }
            else
            {
                AddBlock(ViewNames.SkyArt, LayoutConst.SkyTopColor, skyBottomY, LayoutConst.SkyFallbackHeightPx);
            }
        }

        public void Clear()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        // 아래끝 월드 y 에서 위로 heightPx 만큼, 폭은 월드 540 그대로인 단색 네모.
        private void AddBlock(string name, Color color, float bottomY, float heightPx)
        {
            SpriteRenderer block = AddRenderer(name, SpriteUtil.White, color);
            float width = LayoutConst.ToUnits(LayoutConst.ScreenWidthPx);
            float height = LayoutConst.ToUnits(heightPx);
            block.transform.localScale = new Vector3(width, height, 1f);
            block.transform.localPosition = new Vector3(0f, bottomY + height * 0.5f, 0f);
        }

        private SpriteRenderer AddRenderer(string name, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = LayoutConst.SkySortOrder;
            return renderer;
        }
    }
}
