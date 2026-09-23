using Mozzi.Game.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 보관함 화면 머리의 「농장 n/20」 글자 옆에 「담장 늘리기」 버튼을 심는 일회성 손질기.
    // 모양은 줄의 PlaceButton 을 복제해 쓴다 — 색 · 글꼴을 따로 들지 않으려는 것이다. 글자는 코드(StorageScreen)가 채운다.
    // 부르는 법 : 메뉴 「Mozzi/보관함 담장 버튼 만들기」 또는 unity command eval 로 StorageFenceButtonSetup.Apply().
    public static class StorageFenceButtonSetup
    {
        private const string StorageScreenPath = "Assets/_Project/Resources/Prefabs/UI/StorageScreen.prefab";
        private const string SampleButtonRow = UiNames.StorageRowPrefix + "0";

        // FenceText 가 (8, -40) 200×22 이라 그 오른쪽에 붙인다.
        private const float ButtonLeft = 212f;
        private const float ButtonTop = 38f;
        private const float ButtonWidth = 140f;
        private const float ButtonHeight = 26f;

        [MenuItem("Mozzi/보관함 담장 버튼 만들기")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StorageScreenPath);

            Transform fenceText = FindDeep(root.transform, UiNames.StorageFenceText);
            Transform row = FindDeep(root.transform, SampleButtonRow);
            Transform sample = row == null ? null : FindDeep(row, UiNames.StoragePlaceButton);
            if (fenceText == null || sample == null)
            {
                Debug.LogError("[StorageFenceButtonSetup] FenceText 나 Row_0/PlaceButton 이 없다");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            Transform button = fenceText.parent.Find(UiNames.StorageFenceButton);
            if (button == null)
            {
                GameObject go = Object.Instantiate(sample.gameObject, fenceText.parent, false);
                go.name = UiNames.StorageFenceButton;
                button = go.transform;
            }

            button.SetSiblingIndex(fenceText.GetSiblingIndex() + 1);
            button.gameObject.SetActive(true);

            RectTransform rect = button as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rect.anchoredPosition = new Vector2(ButtonLeft, -ButtonTop);

            // 복제한 버튼에 옛 리스너가 딸려 오면 안 된다 — 배선은 코드가 한다.
            button.GetComponent<Button>().onClick.RemoveAllListeners();

            PrefabUtility.SaveAsPrefabAsset(root, StorageScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[StorageFenceButtonSetup] 저장 " + StorageScreenPath);
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
