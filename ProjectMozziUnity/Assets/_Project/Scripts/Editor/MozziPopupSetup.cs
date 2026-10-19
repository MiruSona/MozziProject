using Mozzi.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // OfflinePopup 프리팹을 본떠 찹쌀이 팝업(MozziPopup) 프리팹을 만드는 일회성 손질기.
    // 덮개 · 상자 · 글꼴은 OfflinePopup 것을 그대로 빌리고, 이름 글자와 버튼 둘을 새로 놓는다.
    // 부르는 법 : 메뉴 「Mozzi/찹쌀이 팝업 만들기」 또는 unity command eval 로 MozziPopupSetup.Apply().
    public static class MozziPopupSetup
    {
        private const string OfflinePopupPath = "Assets/_Project/Resources/Prefabs/UI/OfflinePopup.prefab";
        private const string MozziPopupPath = "Assets/_Project/Resources/Prefabs/UI/MozziPopup.prefab";

        private const string OldTitle = "OfflineTitleText";
        private const string OldTime = "OfflineTimeText";
        private const string OldBody = "OfflineBodyText";
        private const string OldTip = "OfflineTipText";
        private const string OldBox = "OfflineBox";

        private const float BoxWidth = 260f;
        private const float BoxHeight = 200f;
        private const float NameFontSize = 18f;
        private const float ButtonFontSize = 14f;
        private const float ButtonLeft = 20f;
        private const float ButtonWidth = 220f;
        private const float StoreButtonTop = 100f;
        private const float StoreButtonHeight = 40f;
        private const float CloseButtonTop = 150f;
        private const float CloseButtonHeight = 32f;

        // 보관함 화면 버튼과 같은 색 (StorageScreen PlaceButton).
        private static readonly Color ButtonColor = new Color(0.35f, 0.38f, 0.55f, 1f);

        [MenuItem("Mozzi/찹쌀이 팝업 만들기")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(OfflinePopupPath);
            root.name = "MozziPopup";

            OfflinePopup old = root.GetComponent<OfflinePopup>();
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            if (root.GetComponent<MozziPopup>() == null)
            {
                root.AddComponent<MozziPopup>();
            }

            Transform box = UiBindUtil.FindDeep(root.transform, OldBox);
            if (box == null)
            {
                Debug.LogError("[MozziPopupSetup] OfflineBox 가 없다");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            box.name = UiNames.MozziPopupBox;
            (box as RectTransform).sizeDelta = new Vector2(BoxWidth, BoxHeight);

            TMP_Text title = Rename(box, OldTitle, UiNames.MozziPopupTitleText).GetComponent<TMP_Text>();
            title.alignment = TextAlignmentOptions.Center;
            (title.transform as RectTransform).sizeDelta = new Vector2(BoxWidth, 34f);

            TMP_Text name = Rename(box, OldTime, UiNames.MozziPopupNameText).GetComponent<TMP_Text>();
            name.alignment = TextAlignmentOptions.Center;
            name.fontSize = NameFontSize;
            (name.transform as RectTransform).sizeDelta = new Vector2(BoxWidth, 30f);

            Remove(box, OldBody);
            Remove(root.transform, OldTip);

            MakeButton(box, UiNames.MozziPopupStoreButton, "보관함으로", StoreButtonTop, StoreButtonHeight, title);
            MakeButton(box, UiNames.MozziPopupCloseButton, "닫기", CloseButtonTop, CloseButtonHeight, title);

            PrefabUtility.SaveAsPrefabAsset(root, MozziPopupPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[MozziPopupSetup] 저장 " + MozziPopupPath);
        }

        private static Transform Rename(Transform parent, string oldName, string newName)
        {
            Transform t = UiBindUtil.FindDeep(parent, oldName);
            if (t == null)
            {
                t = UiBindUtil.FindDeep(parent, newName);
            }

            if (t == null)
            {
                throw new System.InvalidOperationException("[MozziPopupSetup] 자식이 없다 : " + oldName);
            }

            t.name = newName;
            return t;
        }

        private static void Remove(Transform parent, string childName)
        {
            Transform t = UiBindUtil.FindDeep(parent, childName);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        // Image + Button + Label(TMP). 폰트는 제목 것을 빌린다 — 프리팹마다 폰트 경로를 따로 들지 않으려는 것이다.
        private static void MakeButton(Transform box, string buttonName, string label, float top, float height, TMP_Text sample)
        {
            Transform button = box.Find(buttonName);
            if (button == null)
            {
                GameObject go = new GameObject(buttonName, typeof(RectTransform));
                go.transform.SetParent(box, false);
                go.AddComponent<Image>();
                go.AddComponent<Button>();
                button = go.transform;
            }

            RectTransform rect = button as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(ButtonWidth, height);
            rect.anchoredPosition = new Vector2(ButtonLeft, -top);

            Image image = button.GetComponent<Image>();
            image.color = ButtonColor;

            Button component = button.GetComponent<Button>();
            ColorBlock colors = component.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            component.colors = colors;

            Transform labelT = button.Find(UiNames.ButtonLabel);
            if (labelT == null)
            {
                GameObject go = new GameObject(UiNames.ButtonLabel, typeof(RectTransform));
                go.transform.SetParent(button, false);
                go.AddComponent<TextMeshProUGUI>();
                labelT = go.transform;
            }

            RectTransform labelRect = labelT as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelT.GetComponent<TextMeshProUGUI>();
            text.font = sample.font;
            text.fontSize = ButtonFontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = label;
        }
    }
}
