using Mozzi.Game.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 가판대 손님 줄 칸을 키우고 겹 그림 자리 넷을 넣는 일회성 손질기.
    // 부르는 법 : 메뉴 「Mozzi/손님 겹 자리 만들기」 또는 unity command eval 로 CustomerLayerSetup.Apply().
    public static class CustomerLayerSetup
    {
        private const string StallScreenPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";

        // 손님 줄 칸 수. Stall 표의 queueMax 와 같지만 프리팹에 박힌 칸 수라 여기 둔다.
        private const int QueueCellCount = 5;

        // 그리는 순서대로. 형제 순서가 곧 위아래다 — 뒤가 위로 온다.
        private static readonly string[] LayerNames =
        {
            UiNames.CustomerBody,
            UiNames.CustomerHair,
            UiNames.CustomerShirt,
            UiNames.CustomerFace,
        };

        // 칸이 커진 만큼 아래로 내릴 위젯들. ButtonStrip 은 화면 바닥에 붙어 있어 건드리지 않는다.
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

        [MenuItem("Mozzi/손님 겹 자리 만들기")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StallScreenPath);

            // 이미 키운 프리팹이면 아래 위젯을 또 내리지 않는다 (두 번 돌려도 같은 자리).
            RectTransform firstCell = FindDeep(root.transform, UiNames.StallQueueCellPrefix + 0) as RectTransform;
            if (firstCell == null)
            {
                Debug.LogError("[CustomerLayerSetup] 손님 칸이 없다");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            bool alreadyGrown = Mathf.Approximately(firstCell.sizeDelta.y, LayoutConstUi.QueueCellHeight);

            for (int i = 0; i < QueueCellCount; i++)
            {
                Transform cell = FindDeep(root.transform, UiNames.StallQueueCellPrefix + i);
                if (cell == null)
                {
                    Debug.LogError("[CustomerLayerSetup] 손님 칸이 없다 : " + i);
                    continue;
                }

                RectTransform rect = cell as RectTransform;
                rect.sizeDelta = new Vector2(LayoutConstUi.QueueCellWidth, LayoutConstUi.QueueCellHeight);

                for (int layer = 0; layer < LayerNames.Length; layer++)
                {
                    MakeLayer(cell, LayerNames[layer], layer);
                }

                MoveCakeIcon(cell);
            }

            if (alreadyGrown == false)
            {
                ShiftLowerWidgets(root.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(root, StallScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[CustomerLayerSetup] 손님 칸 " + QueueCellCount + " · 아래 위젯 내림 " + (alreadyGrown == false));
        }

        // 칸 왼쪽 위에 겹치는 48×64 그림 자리. 그림을 끼우는 것은 코드 몫이라 꺼 둔 채로 둔다.
        private static void MakeLayer(Transform cell, string layerName, int siblingIndex)
        {
            Transform layer = cell.Find(layerName);
            if (layer == null)
            {
                GameObject go = new GameObject(layerName, typeof(RectTransform));
                go.transform.SetParent(cell, false);
                go.AddComponent<Image>();
                layer = go.transform;
            }

            RectTransform rect = layer as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(LayoutConstUi.CustomerCanvasWidth, LayoutConstUi.CustomerCanvasHeight);
            rect.anchoredPosition = new Vector2(LayoutConstUi.CustomerLayerLeft, -LayoutConstUi.CustomerLayerTop);
            rect.SetSiblingIndex(siblingIndex);

            Image image = layer.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            layer.gameObject.SetActive(false);
        }

        // 떡 그림은 칸 오른쪽 위 구석으로. 자리 값은 LayoutConstUi 한 곳에서만 갖는다.
        private static void MoveCakeIcon(Transform cell)
        {
            Transform icon = cell.Find(UiNames.CakeIcon);
            if (icon == null)
            {
                Debug.LogError("[CustomerLayerSetup] 칸에 떡 Icon 이 없다 — CakeIconSetup 을 먼저 돌린다");
                return;
            }

            RectTransform rect = icon as RectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(LayoutConstUi.CakeIconQueueSize, LayoutConstUi.CakeIconQueueSize);
            rect.anchoredPosition = new Vector2(-LayoutConstUi.CakeIconQueueRight, -LayoutConstUi.CakeIconQueueTop);

            // 글자보다 뒤 형제라야 떡이 글자 위로 온다.
            icon.SetAsLastSibling();
        }

        private static void ShiftLowerWidgets(Transform root)
        {
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
                Debug.LogError("[CustomerLayerSetup] 내릴 위젯이 없다 : " + childName);
                return;
            }

            Vector2 pos = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(pos.x, pos.y - LayoutConstUi.QueueRowShiftY);
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
