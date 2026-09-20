using System.Collections.Generic;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mozzi.Game.View
{
    // Unity Tilemap 으로 농장 바닥 12×13 과 담장을 깐다. 층은 바닥 · 담장 뒤 · 담장 앞 셋이다.
    // Unity 의 Grid · Tilemap · Tile 은 이 파일 밖으로 안 나간다 — 그게 이음매의 전부다.
    public sealed class TilemapFarmGroundPainter : IFarmGroundPainter
    {
        private const int GroundVariantCount = 3;

        // 바닥 그림이 아직 없을 때 쓰는 풀색.
        private static readonly Color GroundFallbackColor = new Color(0.36f, 0.55f, 0.3f);
        private static readonly Matrix4x4 FlipX = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));

        // 런타임에 만든 Tile 은 에셋이 아니라 Clear 에서 같이 지운다.
        private readonly List<Tile> _tiles = new List<Tile>();

        private GameObject _gridObject;

        public void Paint(Transform parent, IResourceHub resource)
        {
            Clear();

            _gridObject = new GameObject("FarmGrid");
            _gridObject.transform.SetParent(parent);
            _gridObject.transform.localPosition = new Vector3(
                -LayoutConst.FarmTileCols * 0.5f,
                LayoutConst.YFromTop(LayoutConst.FarmBottomPx),
                0f);

            Grid grid = _gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            Tilemap ground = CreateLayer("FarmGround", LayoutConst.GroundSortOrder);
            Tilemap fenceBack = CreateLayer("FarmFenceBack", LayoutConst.FenceBackSortOrder);
            Tilemap fenceFront = CreateLayer("FarmFenceFront", LayoutConst.FenceFrontSortOrder);

            PaintGround(ground, resource);
            PaintFence(fenceBack, fenceFront, resource);
        }

        public void Clear()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                Object.Destroy(_tiles[i]);
            }

            _tiles.Clear();

            if (_gridObject != null)
            {
                Object.Destroy(_gridObject);
                _gridObject = null;
            }
        }

        // 담장이 없는 안쪽 칸은 Ground 를 준다.
        public static FarmTileKind FenceKindAt(int col, int row, int cols, int rows)
        {
            bool sideEdge = col == 0 || col == cols - 1;

            if (row == rows - 1)
            {
                if (sideEdge)
                {
                    return FarmTileKind.FenceCornerTop;
                }

                return FarmTileKind.FenceTop;
            }

            if (row == 0)
            {
                if (sideEdge)
                {
                    return FarmTileKind.FenceCornerBottom;
                }

                return FarmTileKind.FenceBottom;
            }

            if (sideEdge)
            {
                return FarmTileKind.FenceSide;
            }

            return FarmTileKind.Ground;
        }

        // 찹쌀이 앞에 그릴 담장인가. 위 가로 담장만 뒤에 남는다.
        public static bool IsFrontLayer(FarmTileKind kind)
        {
            if (kind == FarmTileKind.FenceTop)
            {
                return false;
            }

            return true;
        }

        // 0 기본 · 1 꽃 · 2 돌. 시드가 고정이라 캡처가 늘 같은 화면이다.
        public static int GroundVariantOf(double roll)
        {
            if (roll < LayoutConst.FarmGroundBaseRatio)
            {
                return 0;
            }

            if (roll < LayoutConst.FarmGroundBaseRatio + LayoutConst.FarmGroundFlowerRatio)
            {
                return 1;
            }

            return 2;
        }

        private Tilemap CreateLayer(string name, int sortingOrder)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(_gridObject.transform);
            layer.transform.localPosition = Vector3.zero;

            Tilemap map = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return map;
        }

        // 담장이 덮는 테두리 칸에도 바닥을 깐다. 담장 그림에 투명한 자리가 있으면 풀이 비친다.
        private void PaintGround(Tilemap map, IResourceHub resource)
        {
            Tile[] variants = new Tile[GroundVariantCount];
            for (int i = 0; i < GroundVariantCount; i++)
            {
                variants[i] = MakeTile(resource.GetSprite(SpriteKeys.FarmGroundPrefix + i));
            }

            if (variants[0] == null)
            {
                variants[0] = MakeTile(SpriteUtil.White);
                variants[0].color = GroundFallbackColor;
            }

            for (int i = 1; i < GroundVariantCount; i++)
            {
                if (variants[i] == null)
                {
                    variants[i] = variants[0];
                }
            }

            System.Random rng = new System.Random(LayoutConst.FarmGroundSeed);
            for (int row = 0; row < LayoutConst.FarmTileRows; row++)
            {
                for (int col = 0; col < LayoutConst.FarmTileCols; col++)
                {
                    int variant = GroundVariantOf(rng.NextDouble());
                    map.SetTile(new Vector3Int(col, row, 0), variants[variant]);
                }
            }
        }

        // 위 가로 담장만 뒤 층이고 나머지는 앞 층이다. 오른쪽 끝 칸은 왼쪽 그림을 뒤집어 쓴다.
        private void PaintFence(Tilemap back, Tilemap front, IResourceHub resource)
        {
            Tile[] byKind = new Tile[6];
            byKind[(int)FarmTileKind.FenceTop] = MakeTile(resource.GetSprite(SpriteKeys.FarmFenceTop));
            byKind[(int)FarmTileKind.FenceBottom] = MakeTile(resource.GetSprite(SpriteKeys.FarmFenceBottom));
            byKind[(int)FarmTileKind.FenceSide] = MakeTile(resource.GetSprite(SpriteKeys.FarmFenceSide));
            byKind[(int)FarmTileKind.FenceCornerTop] = MakeTile(resource.GetSprite(SpriteKeys.FarmFenceCornerTop));
            byKind[(int)FarmTileKind.FenceCornerBottom] = MakeTile(resource.GetSprite(SpriteKeys.FarmFenceCornerBottom));

            int cols = LayoutConst.FarmTileCols;
            int rows = LayoutConst.FarmTileRows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    FarmTileKind kind = FenceKindAt(col, row, cols, rows);
                    Tile tile = byKind[(int)kind];
                    if (tile == null)
                    {
                        continue;
                    }

                    Tilemap map = back;
                    if (IsFrontLayer(kind))
                    {
                        map = front;
                    }

                    Vector3Int pos = new Vector3Int(col, row, 0);
                    map.SetTile(pos, tile);

                    if (col == cols - 1)
                    {
                        map.SetTransformMatrix(pos, FlipX);
                    }
                }
            }
        }

        // 그림이 없는 종류는 null 을 주고 그 칸을 건너뛴다.
        private Tile MakeTile(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.flags = TileFlags.None;
            _tiles.Add(tile);
            return tile;
        }
    }
}
