using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 진열장 접시 칸 하나 : 겹쳐 쌓는 떡 그림 셋(Cake_0~2). 개수는 글자 대신 보이는 떡 수로 알린다 (정확한 수는 칠판 글자).
    public sealed class ShelfCellView
    {
        private readonly GameObject _root;
        private readonly Image[] _cakes = new Image[UiNames.StallShelfCakeCount];

        public ShelfCellView(Transform cellRoot, string prefabName)
        {
            _root = cellRoot.gameObject;
            for (int i = 0; i < _cakes.Length; i++)
            {
                _cakes[i] = UiBindUtil.Find<Image>(cellRoot, UiNames.StallShelfCakePrefix + i, prefabName);
            }
        }

        // count 개를 떡 1~3개 그림으로 쌓아 보인다. 문턱 · 쌓는 자리는 StallLayout 이 쥔다. 아직 안 구운 그림(null)이면 칸을 끈다.
        public void Set(Sprite sprite, int count)
        {
            int shown = 0;
            for (int i = 0; i < StallLayout.PlateStackThresholds.Length; i++)
            {
                if (count >= StallLayout.PlateStackThresholds[i])
                {
                    shown = i + 1;
                }
            }

            if (sprite == null || shown == 0)
            {
                Hide();
                return;
            }

            for (int i = 0; i < _cakes.Length; i++)
            {
                bool on = i < shown;
                UiBindUtil.SetActive(_cakes[i].gameObject, on);
                if (on)
                {
                    _cakes[i].sprite = sprite;
                    _cakes[i].rectTransform.anchoredPosition = new Vector2(StallLayout.PlateStackX[shown - 1][i], StallLayout.PlateStackY[shown - 1][i]);
                }
            }

            UiBindUtil.SetActive(_root, true);
        }

        public void Hide()
        {
            UiBindUtil.SetActive(_root, false);
        }
    }
}
