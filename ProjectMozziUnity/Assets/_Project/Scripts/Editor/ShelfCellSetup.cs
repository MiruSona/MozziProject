using Mozzi.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 가판대 진열대를 글자 한 줄에서 떡 묶음 칸 24개(8×3)로 바꾸는 일회성 손질기 (진열대칸설계 4-3).
    // 부르는 법 : 메뉴 「Mozzi/진열대 칸 만들기」 또는 unity command eval 로 ShelfCellSetup.Apply().
    public static class ShelfCellSetup
    {
        private const string StallScreenPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";

        // 손님 줄 칸 수. CustomerLayerSetup 이 안 내린 QueueCell 을 이번에 같이 내린다.
        private const int QueueCellCount = 5;

        private static readonly Color CellBackground = new Color(0.3f, 0.3f, 0.38f, 1f);
        private static readonly Color CountColor = Color.white;

        // 칸이 들어간 만큼 아래로 내릴 위젯들. ButtonStrip 은 화면 바닥에 붙어 있어 건드리지 않는다.
        private static readonly string[] ShiftNames =
        {
            "MakeTitleText",
            "ExpTitleText",
            UiNames.StallExpInfoText,
            UiNames.StallExpMakeButton,
        };

        private static readonly string[] ShiftToggleIds =
        {
            "redbean", "bean", "mugwort", "sesame", "pinenut", "jujube",
        };

        [MenuItem("Mozzi/진열대 칸 만들기")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StallScreenPath);

            Transform shelfText = FindDeep(root.transform, UiNames.StallShelfText);
            if (shelfText == null)
            {
                Debug.LogError("[ShelfCellSetup] ShelfText 가 없다");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            // 첫 칸이 이미 있으면 아래 위젯을 또 내리지 않는다 (두 번 돌려도 같은 자리).
            bool alreadyMade = FindDeep(root.transform, UiNames.StallShelfCellPrefix + 0) != null;

            TMP_Text sample = shelfText.GetComponent<TMP_Text>();
            Transform parent = shelfText.parent;
            int firstSibling = shelfText.GetSiblingIndex() + 1;
            for (int i = 0; i < UiNames.StallShelfCellCount; i++)
            {
                MakeCell(parent, i, firstSibling + i, sample);
            }

            shelfText.gameObject.SetActive(false);

            if (alreadyMade == false)
            {
                ShiftLowerWidgets(root.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(root, StallScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[ShelfCellSetup] 진열대 칸 " + UiNames.StallShelfCellCount + " · 아래 위젯 내림 " + (alreadyMade == false));
        }

        // 칸 뿌리(Image) + Icon + CountText. 있으면 자리만 다시 맞추고 없으면 만든다.
        private static void MakeCell(Transform parent, int index, int siblingIndex, TMP_Text sample)
        {
            string cellName = UiNames.StallShelfCellPrefix + index;
            Transform cell = parent.Find(cellName);
            if (cell == null)
            {
                GameObject go = new GameObject(cellName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                go.AddComponent<Image>();
                cell = go.transform;
            }

            float step = LayoutConstUi.ShelfCellSize + LayoutConstUi.ShelfCellGap;
            float x = LayoutConstUi.ShelfCellLeft + (index % LayoutConstUi.ShelfCellColumns) * step;
            float y = LayoutConstUi.ShelfCellTop + (index / LayoutConstUi.ShelfCellColumns) * step;
            RectTransform rect = cell as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(LayoutConstUi.ShelfCellSize, LayoutConstUi.ShelfCellSize);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.SetSiblingIndex(siblingIndex);

            Image background = cell.GetComponent<Image>();
            background.color = CellBackground;
            background.raycastTarget = false;

            MakeIcon(cell);
            MakeCountText(cell, sample);

            // 뒤 형제가 위로 온다 — 글자가 그림 위에 보여야 한다.
            cell.Find(UiNames.CakeIcon).SetSiblingIndex(0);
            cell.Find(UiNames.StallShelfCountText).SetSiblingIndex(1);

            // 그림을 끼우는 것은 코드 몫이라 꺼 둔 채로 둔다.
            cell.gameObject.SetActive(false);
        }

        private static void MakeIcon(Transform cell)
        {
            Transform icon = cell.Find(UiNames.CakeIcon);
            if (icon == null)
            {
                GameObject go = new GameObject(UiNames.CakeIcon, typeof(RectTransform));
                go.transform.SetParent(cell, false);
                go.AddComponent<Image>();
                icon = go.transform;
            }

            RectTransform rect = icon as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(LayoutConstUi.CakeIconShelfSize, LayoutConstUi.CakeIconShelfSize);
            rect.anchoredPosition = new Vector2(LayoutConstUi.CakeIconShelfInset, -LayoutConstUi.CakeIconShelfInset);

            Image image = icon.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        // 폰트는 ShelfText 것을 그대로 빌린다 — 프리팹마다 폰트 경로를 따로 들지 않으려는 것이다.
        private static void MakeCountText(Transform cell, TMP_Text sample)
        {
            Transform count = cell.Find(UiNames.StallShelfCountText);
            if (count == null)
            {
                GameObject go = new GameObject(UiNames.StallShelfCountText, typeof(RectTransform));
                go.transform.SetParent(cell, false);
                go.AddComponent<TextMeshProUGUI>();
                count = go.transform;
            }

            RectTransform rect = count as RectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(LayoutConstUi.ShelfCountTextWidth, LayoutConstUi.ShelfCountTextHeight);
            rect.anchoredPosition = new Vector2(-LayoutConstUi.ShelfCountTextInset, LayoutConstUi.ShelfCountTextInset);

            TextMeshProUGUI text = count.GetComponent<TextMeshProUGUI>();
            text.font = sample.font;
            text.fontSize = LayoutConstUi.ShelfCountFontSize;
            text.color = CountColor;
            text.alignment = TextAlignmentOptions.BottomRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = "×0";
        }

        private static void ShiftLowerWidgets(Transform root)
        {
            for (int i = 0; i < QueueCellCount; i++)
            {
                Shift(root, UiNames.StallQueueCellPrefix + i);
            }

            for (int i = 0; i < ShiftNames.Length; i++)
            {
                Shift(root, ShiftNames[i]);
            }

            for (int i = 0; i < UiNames.StallMakeRowCount; i++)
            {
                Shift(root, UiNames.StallMakeRowPrefix + i);
            }

            for (int i = 0; i < ShiftToggleIds.Length; i++)
            {
                Shift(root, UiNames.StallExpTogglePrefix + ShiftToggleIds[i]);
            }
        }

        private static void Shift(Transform root, string childName)
        {
            RectTransform rect = FindDeep(root, childName) as RectTransform;
            if (rect == null)
            {
                Debug.LogError("[ShelfCellSetup] 내릴 위젯이 없다 : " + childName);
                return;
            }

            Vector2 pos = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(pos.x, pos.y - LayoutConstUi.ShelfShiftY);
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform deeper = FindDeep(child, childName);
                if (deeper != null)
                {
                    return deeper;
                }
            }

            return null;
        }
    }
}
