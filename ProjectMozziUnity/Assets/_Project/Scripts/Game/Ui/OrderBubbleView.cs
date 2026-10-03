using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 손님 머리 위 주문 말풍선 하나. 둥근 꼴은 프리팹의 네모 조각들이 만들고, 여기서는 켜고 끄기 · 색 · 폭만 다룬다.
    public sealed class OrderBubbleView
    {
        private readonly RectTransform _root;
        private readonly List<Image> _fills = new List<Image>();

        public OrderBubbleView(Transform cell, string prefabName)
        {
            _root = UiBindUtil.FindObject(cell, UiNames.StallOrderBubble, prefabName).transform as RectTransform;
            for (int i = 0; i < UiNames.StallBubbleFillCount; i++)
            {
                _fills.Add(UiBindUtil.Find<Image>(_root, UiNames.StallBubbleFillPrefix + i, prefabName));
            }
        }

        public void SetActive(bool active)
        {
            UiBindUtil.SetActive(_root.gameObject, active);
        }

        public void SetColor(Color color)
        {
            for (int i = 0; i < _fills.Count; i++)
            {
                _fills[i].color = color;
            }
        }

        // 글자가 들어가면 넓게, 떡 그림만이면 좁게.
        public void SetWide(bool wide)
        {
            float width = LayoutConstUi.StallBubbleWidthIcon;
            if (wide)
            {
                width = LayoutConstUi.StallBubbleWidthText;
            }

            _root.sizeDelta = new Vector2(width, _root.sizeDelta.y);
        }
    }
}
