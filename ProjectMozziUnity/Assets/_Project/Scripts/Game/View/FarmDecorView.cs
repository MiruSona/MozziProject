using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Game.View
{
    // 울타리 안 장식 조각(풀포기 · 꽃 · 돌 · 버섯)을 FarmDecor 표대로 놓는다. 자리는 표에 고정이라 날마다 같다.
    // back 겹은 바탕 바로 위(찹쌀이 발밑), front 겹은 찹쌀이와 같은 y 정렬이라 아래쪽에 있는 쪽이 앞에 온다.
    public sealed class FarmDecorView
    {
        private GameObject _root;

        public void Build(Transform parent, IResourceHub resource, IReadOnlyList<FarmDecorRow> rows)
        {
            Clear();
            FarmDecorLayout.Validate(rows, LayoutConst.DecorCols, LayoutConst.DecorRows);

            _root = new GameObject(ViewNames.GroundDecor);
            _root.transform.SetParent(parent, false);

            Rect area = LayoutConst.FarmRect;
            for (int i = 0; i < rows.Count; i++)
            {
                FarmDecorRow row = rows[i];
                Sprite sprite = resource.GetSprite(row.SpriteKey);
                if (sprite == null)
                {
                    continue;
                }

                Vector2 pos = FarmDecorLayout.CellBottomCenter(area, row.Col, row.Row);
                GameObject go = new GameObject(row.Id);
                go.transform.SetParent(_root.transform, false);
                go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.flipX = row.FlipX;
                renderer.sortingOrder = SortOrderOf(row.Layer, pos.y);
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

        // front 는 MozziObject.ApplySort 와 같은 식이다 — 발 y 가 낮을수록 앞이다.
        private static int SortOrderOf(DecorLayer layer, float y)
        {
            if (layer == DecorLayer.Front)
            {
                return LayoutConst.MozziSortBase - Mathf.RoundToInt(y * LayoutConst.PixelsPerUnit);
            }

            return LayoutConst.DecorBackSortOrder;
        }
    }
}
