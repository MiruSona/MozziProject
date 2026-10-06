using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 메인 화면 땅을 SpriteRenderer 로 깐다. 잔디 바탕 한 장(Tiled) · 길 · 통나무 울타리다. Tilemap 은 안 쓴다.
    // 울타리 겹은 위 줄이 찹쌀이 뒤(FenceBackSortOrder), 옆 · 아래 줄이 찹쌀이 앞(FenceFrontSortOrder)이다.
    public sealed class SpriteFarmGroundPainter : IFarmGroundPainter
    {
        // 잔디를 월드 아래끝 밑으로 더 까는 높이 (px). 화면 맞춤 반올림으로 맨 아래 한 줄이 비지 않게.
        private const float GrassBelowPx = 64f;

        // 잔디 그림이 없을 때 쓰는 풀색.
        private static readonly Color GrassFallbackColor = new Color(0.36f, 0.55f, 0.3f);

        private GameObject _root;

        // 옆 세로 칸 renderer (줄 번호 = 칸 row, 0 · rows−1 은 비어 있다). 장치 자리 칸을 끄고 켠다.
        private readonly SpriteRenderer[] _leftSide = new SpriteRenderer[LayoutConst.FarmTileRows];
        private readonly SpriteRenderer[] _rightSide = new SpriteRenderer[LayoutConst.FarmTileRows];

        public void Paint(Transform parent, IResourceHub resource)
        {
            Clear();

            _root = new GameObject(ViewNames.GroundRoot);
            _root.transform.SetParent(parent, false);

            PaintGrass(resource.GetSprite(SpriteKeys.FarmGrass));
            PaintPath(resource.GetSprite(SpriteKeys.MainPath));
            PaintFence(resource);
        }

        public void Clear()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            System.Array.Clear(_leftSide, 0, _leftSide.Length);
            System.Array.Clear(_rightSide, 0, _rightSide.Length);
        }

        public void SetSideGap(FenceSide side, int fromRow, int toRow, bool open)
        {
            SpriteRenderer[] pieces = side == FenceSide.Left ? _leftSide : _rightSide;
            for (int row = Mathf.Max(0, fromRow); row <= toRow && row < pieces.Length; row++)
            {
                if (pieces[row] != null)
                {
                    pieces[row].enabled = open == false;
                }
            }
        }

        // 지평선(하늘 아래끝)부터 월드 아래끝까지 540 폭 전체를 한 장으로 덮는다. 256 그림이 네 변으로 이어진다.
        private void PaintGrass(Sprite grass)
        {
            float topY = LayoutConst.YFromTop(LayoutConst.SkyBottomPx);
            float bottomY = LayoutConst.YFromTop(LayoutConst.ScreenHeightPx + GrassBelowPx);
            float width = LayoutConst.ToUnits(LayoutConst.ScreenWidthPx);
            float height = topY - bottomY;
            Vector3 center = new Vector3(0f, (topY + bottomY) * 0.5f, 0f);

            if (grass == null)
            {
                SpriteRenderer block = AddRenderer(ViewNames.GroundGrass, SpriteUtil.White, LayoutConst.GroundSortOrder);
                block.color = GrassFallbackColor;
                block.transform.localScale = new Vector3(width, height, 1f);
                block.transform.localPosition = center;
                return;
            }

            SpriteRenderer renderer = AddRenderer(ViewNames.GroundGrass, grass, LayoutConst.GroundSortOrder);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(width, height);
            renderer.transform.localPosition = center;
        }

        // 길 그림은 아래 가운데 피벗이라 아래끝(농장 위끝)에 바로 놓는다.
        private void PaintPath(Sprite path)
        {
            if (path == null)
            {
                return;
            }

            SpriteRenderer renderer = AddRenderer(ViewNames.GroundPath, path, LayoutConst.PathSortOrder);
            renderer.transform.localPosition = new Vector3(0f, LayoutConst.YFromTop(LayoutConst.FarmTopPx), 0f);
        }

        // 위 · 아래 가로 줄과 양옆 세로 줄. 세로 줄은 위아래 울타리 줄을 뺀 칸 1 ~ rows-2 에만 놓는다.
        private void PaintFence(IResourceHub resource)
        {
            Sprite rail = resource.GetSprite(SpriteKeys.FarmFenceH);
            Sprite side = resource.GetSprite(SpriteKeys.FarmFenceV);
            Sprite post = resource.GetSprite(SpriteKeys.FarmFencePost);

            float cell = LayoutConst.FarmCellPx;
            float leftPx = LayoutConst.FarmGridLeftPx;
            float rightColPx = leftPx + (LayoutConst.FarmTileCols - 1) * cell;

            PaintRail(rail, post, LayoutConst.FarmTopPx + cell, LayoutConst.FenceBackSortOrder);
            PaintRail(rail, post, LayoutConst.FarmBottomPx, LayoutConst.FenceFrontSortOrder);

            for (int row = 1; row < LayoutConst.FarmTileRows - 1; row++)
            {
                float bottomPx = LayoutConst.FarmBottomPx - row * cell;
                _leftSide[row] = AddPiece(side, leftPx, bottomPx, LayoutConst.FenceFrontSortOrder);
                _rightSide[row] = AddPiece(side, rightColPx, bottomPx, LayoutConst.FenceFrontSortOrder);
            }
        }

        // 가로 한 줄. 왼쪽 끝 기둥 → 가로 마디를 FenceSegmentPx 마다 → 오른쪽 끝 칸에 모서리 기둥을 한 번 더 덮는다.
        // 마디 간격을 64 로 바꿔도 오른쪽 기둥은 세로 줄과 같은 칸에 선다.
        private void PaintRail(Sprite rail, Sprite post, float bottomPx, int sortOrder)
        {
            float leftPx = LayoutConst.FarmGridLeftPx;
            float rightColPx = leftPx + (LayoutConst.FarmTileCols - 1) * LayoutConst.FarmCellPx;

            AddPiece(post, leftPx, bottomPx, sortOrder);
            for (float x = leftPx + LayoutConst.FenceSegmentPx; x <= rightColPx; x += LayoutConst.FenceSegmentPx)
            {
                AddPiece(rail, x, bottomPx, sortOrder);
            }

            AddPiece(post, rightColPx, bottomPx, sortOrder + 1);
        }

        // 그림의 왼쪽 아래를 (leftPx, 위에서 잰 bottomPx) 에 맞춘다. 피벗이 어디든 bounds 로 맞추니 상관없다.
        // 높이 36 그림(가로 마디 · 기둥)은 칸 위로 4px 솟는다.
        private SpriteRenderer AddPiece(Sprite sprite, float leftPx, float bottomPx, int sortOrder)
        {
            if (sprite == null)
            {
                return null;
            }

            SpriteRenderer renderer = AddRenderer(ViewNames.GroundFence, sprite, sortOrder);
            float x = LayoutConst.XFromLeft(leftPx) - sprite.bounds.min.x;
            float y = LayoutConst.YFromTop(bottomPx) - sprite.bounds.min.y;
            renderer.transform.localPosition = new Vector3(x, y, 0f);
            return renderer;
        }

        private SpriteRenderer AddRenderer(string name, Sprite sprite, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortOrder;
            return renderer;
        }
    }
}
