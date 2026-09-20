using Mozzi.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 자원바 부재료 칸과 가판대 실험 토글에 그림 자식(Icon)을 넣는 일회성 손질기.
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.IngredientIconSetup.Apply();` 한 줄.
    public static class IngredientIconSetup
    {
        private const string FarmScreenPath = "Assets/_Project/Resources/Prefabs/UI/FarmScreen.prefab";
        private const string StallScreenPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";

        // 「대추 100/100」이 옆 칸에 붙던 것을 글씨 크기로 푼다 (11 → 9 면 58px 칸에 들어간다).
        private const float IngredientFontSize = 9f;

        private static readonly string[] IngredientIds =
        {
            "redbean", "bean", "mugwort", "sesame", "pinenut", "jujube",
        };

        public static void Apply()
        {
            ApplyFarmScreen();
            ApplyStallScreen();
            AssetDatabase.SaveAssets();
            Debug.Log("[IngredientIconSetup] 끝");
        }

        private static void ApplyFarmScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FarmScreenPath);
            for (int i = 0; i < IngredientIds.Length; i++)
            {
                Transform cell = FindDeep(root.transform, UiNames.FarmIngredientPrefix + IngredientIds[i]);
                if (cell == null)
                {
                    Debug.LogError("[IngredientIconSetup] 칸이 없다 : " + IngredientIds[i]);
                    continue;
                }

                TMP_Text text = cell.GetComponent<TMP_Text>();
                text.fontSize = IngredientFontSize;
                MakeIcon(cell, LayoutConstUi.IngredientIconSize, LayoutConstUi.IngredientIconLeft);
            }

            PrefabUtility.SaveAsPrefabAsset(root, FarmScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[IngredientIconSetup] 자원바 칸 " + IngredientIds.Length + " 에 아이콘 · 글씨 " + IngredientFontSize);
        }

        private static void ApplyStallScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StallScreenPath);
            for (int i = 0; i < IngredientIds.Length; i++)
            {
                Transform toggle = FindDeep(root.transform, UiNames.StallExpTogglePrefix + IngredientIds[i]);
                if (toggle == null)
                {
                    Debug.LogError("[IngredientIconSetup] 토글이 없다 : " + IngredientIds[i]);
                    continue;
                }

                MakeIcon(toggle, LayoutConstUi.ToggleIconSize, LayoutConstUi.ToggleIconLeft);
            }

            PrefabUtility.SaveAsPrefabAsset(root, StallScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[IngredientIconSetup] 실험 토글 " + IngredientIds.Length + " 에 아이콘");
        }

        // 왼쪽 가운데에 붙는 정사각 그림 자리. 그림을 끼우는 것은 코드 몫이라 꺼 둔 채로 둔다.
        private static void MakeIcon(Transform parent, float size, float left)
        {
            Transform old = parent.Find(UiNames.IngredientIcon);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject go = new GameObject(UiNames.IngredientIcon, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(left, 0f);

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
