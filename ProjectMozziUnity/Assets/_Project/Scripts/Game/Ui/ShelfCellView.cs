using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 진열대 묶음 칸 하나 : 떡 그림(Icon) + 개수 글자(CountText). 둘 다 있어야 칸을 쓴다.
    public sealed class ShelfCellView
    {
        // Icon 과 CountText 둘 다 있나. 칸 자리를 아직 안 만든 프리팹에서도 화면이 뜨게 하려고 둔다.
        public readonly bool HasParts;

        private readonly GameObject _root;
        private readonly Image _icon;
        private readonly TMP_Text _count;

        public ShelfCellView(Transform cellRoot)
        {
            Image icon;
            TMP_Text count;
            bool hasIcon = UiBindUtil.TryFind<Image>(cellRoot, UiNames.CakeIcon, out icon);
            bool hasCount = UiBindUtil.TryFind<TMP_Text>(cellRoot, UiNames.StallShelfCountText, out count);

            _root = cellRoot.gameObject;
            _icon = icon;
            _count = count;
            HasParts = hasIcon && hasCount;
        }

        // 그림을 끼우고 「×count」 를 쓰고 켠다. 아직 안 구운 그림(null)이면 Icon 만 꺼지고 개수는 남는다.
        public void Set(Sprite sprite, int count)
        {
            if (HasParts == false)
            {
                return;
            }

            _icon.sprite = sprite;
            UiBindUtil.SetActive(_icon.gameObject, sprite != null);
            UiBindUtil.SetText(_count, "×" + count);
            UiBindUtil.SetActive(_root, true);
        }

        public void Hide()
        {
            if (HasParts == false)
            {
                return;
            }

            UiBindUtil.SetActive(_root, false);
        }
    }
}
