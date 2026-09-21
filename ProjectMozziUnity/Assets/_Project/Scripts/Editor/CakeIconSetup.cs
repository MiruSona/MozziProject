using Mozzi.Game.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 레시피 도감 칸 · 발견 연출 · 만들기 줄 · 손님 주문 칸에 떡 그림 자식(Icon)을 넣는 일회성 손질기.
    // 부르는 법 : 메뉴 「Mozzi/떡 아이콘 자리 만들기」 또는 unity command eval 로 CakeIconSetup.Apply().
    public static class CakeIconSetup
    {
        private const string RecipeCodexPath = "Assets/_Project/Resources/Prefabs/UI/RecipeCodexScreen.prefab";
        private const string StallScreenPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";

        // 손님 줄 칸 수. Stall 표의 queueMax 와 같지만 프리팹에 박힌 칸 수라 여기 둔다.
        private const int QueueCellCount = 5;

        private static readonly string[] RecipeIds =
        {
            "redbean_cake", "bean_cake", "mugwort_cake", "sesame_cake", "pinenut_cake", "jujube_cake",
            "redbean_bean_cake", "mugwort_sesame_cake", "pinenut_jujube_cake", "redbean_mugwort_sesame_cake",
        };

        private static readonly string[] CellTextNames =
        {
            UiNames.RecipeCodexCellNameText,
            UiNames.RecipeCodexCellIngredientText,
            UiNames.RecipeCodexCellPriceText,
        };

        [MenuItem("Mozzi/떡 아이콘 자리 만들기")]
        public static void Apply()
        {
            ApplyRecipeCodex();
            ApplyStallScreen();
            AssetDatabase.SaveAssets();
            Debug.Log("[CakeIconSetup] 끝");
        }

        // 칸 왼쪽에 64px 그림, 글자 셋은 오른쪽으로 밀고 폭을 줄인다.
        private static void ApplyRecipeCodex()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RecipeCodexPath);
            for (int i = 0; i < RecipeIds.Length; i++)
            {
                Transform cell = FindDeep(root.transform, UiNames.RecipeCodexCellPrefix + RecipeIds[i]);
                if (cell == null)
                {
                    Debug.LogError("[CakeIconSetup] 칸이 없다 : " + RecipeIds[i]);
                    continue;
                }

                MakeIcon(cell, LayoutConstUi.CakeIconCodexSize, new Vector2(0f, 0.5f),
                    new Vector2(LayoutConstUi.CakeIconCodexLeft, 0f));
                MoveCellTexts(cell);
            }

            PrefabUtility.SaveAsPrefabAsset(root, RecipeCodexPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[CakeIconSetup] 도감 칸 " + RecipeIds.Length + " 에 아이콘");
        }

        private static void MoveCellTexts(Transform cell)
        {
            for (int i = 0; i < CellTextNames.Length; i++)
            {
                Transform text = FindDeep(cell, CellTextNames[i]);
                if (text == null)
                {
                    Debug.LogError("[CakeIconSetup] 칸 글자가 없다 : " + CellTextNames[i]);
                    continue;
                }

                RectTransform rect = text as RectTransform;
                rect.anchoredPosition = new Vector2(LayoutConstUi.CodexCellTextLeft, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(LayoutConstUi.CodexCellTextWidth, rect.sizeDelta.y);
            }
        }

        private static void ApplyStallScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StallScreenPath);

            Transform madePanel = FindDeep(root.transform, UiNames.StallMadePanel);
            MakeIcon(madePanel, LayoutConstUi.CakeIconMadeSize, new Vector2(0.5f, 0.5f),
                new Vector2(0f, LayoutConstUi.CakeIconMadeY));

            // 흰 섬광(MadeFlash)보다 먼저 그려지게 맨 앞 형제로 둔다. 뒤에 두면 떡만 섬광 위로 떠오른다.
            madePanel.Find(UiNames.CakeIcon).SetAsFirstSibling();

            // 96px 그림이 들어가 NEW 글자가 겹친다 — 그만큼 위로 올린다.
            RectTransform madeNew = FindDeep(madePanel, UiNames.StallMadeNewText) as RectTransform;
            madeNew.anchoredPosition = new Vector2(madeNew.anchoredPosition.x, LayoutConstUi.MadeNewTextY);

            for (int i = 0; i < UiNames.StallMakeRowCount; i++)
            {
                Transform row = FindDeep(root.transform, UiNames.StallMakeRowPrefix + i);
                MakeIcon(row, LayoutConstUi.CakeIconMakeRowSize, new Vector2(0f, 0.5f),
                    new Vector2(LayoutConstUi.CakeIconMakeRowLeft, 0f));
            }

            for (int i = 0; i < QueueCellCount; i++)
            {
                Transform queueCell = FindDeep(root.transform, UiNames.StallQueueCellPrefix + i);
                MakeIcon(queueCell, LayoutConstUi.CakeIconQueueSize, new Vector2(0.5f, 1f),
                    new Vector2(0f, -LayoutConstUi.CakeIconQueueTop));
            }

            PrefabUtility.SaveAsPrefabAsset(root, StallScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[CakeIconSetup] 발견 연출 · 만들기 줄 " + UiNames.StallMakeRowCount + " · 손님 칸 " + QueueCellCount);
        }

        // 정사각 그림 자리. 그림을 끼우는 것은 코드 몫이라 꺼 둔 채로 둔다.
        private static void MakeIcon(Transform parent, float size, Vector2 anchor, Vector2 anchoredPosition)
        {
            Transform old = parent.Find(UiNames.CakeIcon);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject go = new GameObject(UiNames.CakeIcon, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = anchoredPosition;

            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            go.SetActive(false);
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
