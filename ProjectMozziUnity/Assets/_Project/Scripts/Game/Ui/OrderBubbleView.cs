using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 손님 머리 위 주문 말풍선 하나 (Stage 밑 "Bubble_" + 번호). 판이 둘이다 — 늘어나는 9-slice 판(Frame, 꼬리 왼쪽 아래)과
    // 꼬리 가운데 작은 판(FrameSmall, 54×29 고정). 떡 그림 · 긴 글자는 Frame, 「아무거나」 처럼 짧은 글자는 FrameSmall.
    public sealed class OrderBubbleView
    {
        // 짧은 글자로 보는 기준(글자 수). 이보다 길면 늘어나는 판을 쓴다.
        private const int SmallTextMax = 4;
        private const float SmallWidth = 54f;
        private const float SmallHeight = 29f;
        private const float TextPad = 20f;

        private readonly RectTransform _root;
        private readonly Image _frame;
        private readonly Image _frameSmall;
        private readonly Image _icon;
        private readonly TMP_Text _label;

        public OrderBubbleView(Transform bubbleRoot, string prefabName)
        {
            _root = bubbleRoot as RectTransform;
            _frame = UiBindUtil.Find<Image>(bubbleRoot, UiNames.StallBubbleFrame, prefabName);
            _frameSmall = UiBindUtil.Find<Image>(bubbleRoot, UiNames.StallBubbleFrameSmall, prefabName);
            _icon = UiBindUtil.Find<Image>(bubbleRoot, UiNames.CakeIcon, prefabName);
            _label = UiBindUtil.Find<TMP_Text>(bubbleRoot, UiNames.ButtonLabel, prefabName);
        }

        public void Hide()
        {
            UiBindUtil.SetActive(_root.gameObject, false);
        }

        // 떡 그림 주문. headX · headTop 은 손님 머리 가운데 x 와 머리 꼭대기 y (세계 좌표).
        public void ShowIcon(Sprite sprite, Color color, float headX, float headTop, bool facesLeft)
        {
            UseWide(StallLayout.BubbleIconWidth, color, headX, headTop, facesLeft);
            _icon.sprite = sprite;
            UiBindUtil.SetActive(_icon.gameObject, true);
            UiBindUtil.SetActive(_label.gameObject, false);
            UiBindUtil.SetActive(_root.gameObject, true);
        }

        // 글자 주문. 짧으면 작은 판, 길면 글자 폭만큼 늘린 판 (넘치면 두 줄로 접고 글자를 줄인다).
        public void ShowText(string text, Color color, float headX, float headTop, bool facesLeft)
        {
            // 글자 폭을 재려면 켜져 있어야 해서 먼저 켠다.
            UiBindUtil.SetActive(_root.gameObject, true);
            UiBindUtil.SetText(_label, text);
            UiBindUtil.SetActive(_icon.gameObject, false);
            UiBindUtil.SetActive(_label.gameObject, true);

            if (text.Length <= SmallTextMax)
            {
                UseSmall(color, headX, headTop);
            }
            else
            {
                float width = _label.GetPreferredValues(text, 999f, 99f).x + TextPad;
                width = Mathf.Clamp(width, StallLayout.BubbleIconWidth, StallLayout.BubbleTextMaxWidth);
                UseWide(width, color, headX, headTop, facesLeft);
            }
        }

        // 꼬리가 왼쪽 아래인 판. 왼쪽을 보는 손님은 판만 좌우로 뒤집어 꼬리가 머리 쪽에 오게 한다 (글자는 안 뒤집는다).
        private void UseWide(float width, Color color, float headX, float headTop, bool facesLeft)
        {
            UiBindUtil.SetActive(_frame.gameObject, true);
            UiBindUtil.SetActive(_frameSmall.gameObject, false);
            _frame.color = color;
            _frame.rectTransform.localScale = new Vector3(1f, 1f, 1f);

            float x = headX - StallLayout.BubbleTailX;
            Vector2 pivot = new Vector2(0f, 0f);
            if (facesLeft)
            {
                x = headX + StallLayout.BubbleTailX;
                pivot = new Vector2(1f, 0f);
                _frame.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            Place(x, headTop, pivot, width, StallLayout.BubbleHeight);
            _label.rectTransform.offsetMin = new Vector2(8f, 18f);
            _label.rectTransform.offsetMax = new Vector2(-8f, -6f);
        }

        private void UseSmall(Color color, float headX, float headTop)
        {
            UiBindUtil.SetActive(_frame.gameObject, false);
            UiBindUtil.SetActive(_frameSmall.gameObject, true);
            _frameSmall.color = color;

            Place(headX, headTop, new Vector2(0.5f, 0f), SmallWidth, SmallHeight);
            _label.rectTransform.offsetMin = new Vector2(4f, 8f);
            _label.rectTransform.offsetMax = new Vector2(-4f, -3f);
        }

        // 꼬리 끝(pivot 점)을 머리 꼭대기 위 BubbleGap 에 둔다. 부모는 Stage(왼쪽 위 앵커)다.
        private void Place(float x, float headTop, Vector2 pivot, float width, float height)
        {
            _root.pivot = pivot;
            _root.anchoredPosition = new Vector2(x, -(headTop - StallLayout.BubbleGap));
            _root.sizeDelta = new Vector2(width, height);
        }
    }
}
