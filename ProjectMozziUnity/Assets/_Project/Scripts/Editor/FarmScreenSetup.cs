using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 농장 화면 프리팹을 540 기준 메인 화면 배치로 놓는 손질기. 자리 값은 정식/메인 README 「코드에 넘길 것」이 정본이다.
    // 부르는 법 : 메뉴 「Mozzi/농장 화면 540 배치」 또는 unity command eval 로 FarmScreenSetup.Apply().
    public static class FarmScreenSetup
    {
        private const string FarmScreenPath = "Assets/_Project/Resources/Prefabs/UI/FarmScreen.prefab";
        private const float ReferenceHeight = LayoutConstUi.ReferenceHeight;

        // 자원바 높이 (540 기준). 옛 56 × 1.5 다. 밤 덮개는 이 아래부터 덮는다.
        private const float ResourceBarHeight = 84f;

        // 메뉴 아이콘 (왼쪽 위 px, 540×960 기준) · 크기.
        private const float IconLeft = 482f;
        private const float IconCodexTop = 768f;
        private const float IconUpgradeTop = 824f;
        private const float IconMenuTop = 880f;
        private const float IconSize = 56f;

        // 아이콘 왼쪽에 뜨는 작은 판. 오른쪽 끝을 아이콘에서 8 띄우고, 아래끝을 누른 아이콘 아래끝에 맞춘다.
        private const float PanelGap = 8f;
        // 강화 판은 다섯 줄. 줄마다 왼쪽에 두 줄 글(이름 · 단계 / 지금 → 다음), 오른쪽에 값 버튼.
        private const int UpgradeRowCount = 5;
        private const float UpgradeRowPitch = 54f;
        private const float UpgradeTextHeight = 48f;
        private const float UpgradeButtonWidth = 104f;
        private const float UpgradeBoxWidth = 380f;
        private const float UpgradeBoxHeight = BoxTitleHeight + 6f + UpgradeRowCount * UpgradeRowPitch + 6f;
        private const float MenuBoxWidth = 200f;
        private const float MenuBoxHeight = 96f;
        private const float BoxPad = 12f;
        private const float BoxTitleHeight = 28f;
        private const float BoxRowHeight = 40f;

        private const float NextDayWidth = 180f;
        private const float NextDayHeight = 60f;

        // 밤 글귀 · 「다음날로」 가운데 y (540×960 위에서 잰 px). 건물 · 농장처럼 아래끝 기준이라 긴 화면에서도 같은 자리다.
        // 길 위 건물 이름표(y 442)에 안 겹치게 길 아래 농장 위쪽에 둔다.
        private const float NightTextY = 494f;
        private const float NextDayY = 556f;

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
        private static readonly Color PanelCream = new Color(1f, 0.965f, 0.9f, 1f);
        private static readonly Color PanelEdge = new Color32(0x6B, 0x45, 0x2E, 0xFF);
        private static readonly Color TextDark = new Color(0.29f, 0.18f, 0.13f, 1f);

        [MenuItem("Mozzi/농장 화면 540 배치")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FarmScreenPath);
            Transform t = root.transform;

            TMP_Text sample = UiBindUtil.FindDeep(t, UiNames.FarmGoldText).GetComponent<TMP_Text>();
            RectTransform nextDay = UiBindUtil.FindDeep(t, UiNames.FarmNextDayButton) as RectTransform;
            PlaceNight(t, nextDay);

            MakeIcon(t, UiNames.FarmCodexButton, SpriteKeys.UiIconCodex, IconCodexTop);
            MakeIcon(t, UiNames.FarmUpgradeButton, SpriteKeys.UiIconUpgrade, IconUpgradeTop);
            MakeIcon(t, UiNames.FarmMenuButton, SpriteKeys.UiIconMenu, IconMenuTop);

            RectTransform upgradeBox = MakePanel(t, UiNames.FarmUpgradePanel, UpgradeBoxWidth, UpgradeBoxHeight, IconUpgradeTop, sample);
            PlaceUpgradeRow(upgradeBox, 0, UiNames.FarmUpgradeFenceText, UiNames.FarmUpgradeFenceButton, nextDay, sample);
            PlaceUpgradeRow(upgradeBox, 1, UiNames.FarmUpgradeFloorText, UiNames.FarmUpgradeFloorButton, nextDay, sample);
            PlaceUpgradeRow(upgradeBox, 2, UiNames.FarmUpgradeCollectorText, UiNames.FarmUpgradeCollectorButton, nextDay, sample);
            PlaceUpgradeRow(upgradeBox, 3, UiNames.FarmUpgradeFeederText, UiNames.FarmUpgradeFeederButton, nextDay, sample);
            PlaceUpgradeRow(upgradeBox, 4, UiNames.FarmUpgradeStorageText, UiNames.FarmUpgradeStorageButton, nextDay, sample);

            RectTransform menuBox = MakePanel(t, UiNames.FarmMenuPanel, MenuBoxWidth, MenuBoxHeight, IconMenuTop, sample);
            RectTransform recipeButton = EnsureButton(menuBox, UiNames.FarmRecipeCodexButton, nextDay, "레시피 도감");
            FormalUiBuild.TopLeft(recipeButton, BoxPad, BoxTitleHeight + 8f, MenuBoxWidth - BoxPad * 2f, BoxRowHeight + 4f);

            // 뒤에서 앞으로 : 밤 덮개 · 자원바 · 「다음날로」 · 아이콘 셋 · 판 둘.
            UiBindUtil.FindDeep(t, UiNames.FarmNightCover).SetAsFirstSibling();
            nextDay.SetAsLastSibling();
            UiBindUtil.FindDeep(t, UiNames.FarmCodexButton).SetAsLastSibling();
            UiBindUtil.FindDeep(t, UiNames.FarmUpgradeButton).SetAsLastSibling();
            UiBindUtil.FindDeep(t, UiNames.FarmMenuButton).SetAsLastSibling();
            UiBindUtil.FindDeep(t, UiNames.FarmUpgradePanel).SetAsLastSibling();
            UiBindUtil.FindDeep(t, UiNames.FarmMenuPanel).SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root, FarmScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[FarmScreenSetup] 끝");
        }

        // 덮개는 자원바 아래 전부와 안전 영역 아래 바깥까지. 글귀와 「다음날로」는 가운데 줄에 위아래로 놓는다.
        private static void PlaceNight(Transform root, RectTransform nextDay)
        {
            RectTransform cover = UiBindUtil.FindDeep(root, UiNames.FarmNightCover) as RectTransform;
            cover.anchorMin = Vector2.zero;
            cover.anchorMax = Vector2.one;
            cover.pivot = new Vector2(0.5f, 0.5f);
            cover.offsetMin = new Vector2(0f, -LayoutConstUi.BarBleedLength);
            cover.offsetMax = new Vector2(0f, -ResourceBarHeight);

            // 글귀는 덮개 자식이고 덮개 아래끝은 화면 아래끝보다 BarBleedLength 아래라 그만큼 더 올린다.
            RectTransform text = UiBindUtil.FindDeep(cover, UiNames.FarmNightText) as RectTransform;
            BottomCenter(text, ReferenceHeight - NightTextY + LayoutConstUi.BarBleedLength, 450f, 60f);
            text.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

            BottomCenter(nextDay, ReferenceHeight - NextDayY, NextDayWidth, NextDayHeight);
            TMP_Text label = nextDay.GetComponentInChildren<TMP_Text>();
            label.fontSize = 24f;
        }

        private static void MakeIcon(Transform root, string name, string spriteKey, float top)
        {
            RectTransform rect = Ensure(root, name);
            FormalUiBuild.BottomLeft(rect, IconLeft, ReferenceHeight - top - IconSize, IconSize, IconSize);

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.sprite = Resources.Load<Sprite>(SpriteKeys.Root + spriteKey);
            image.color = Color.white;
            image.raycastTarget = true;
            if (image.sprite == null)
            {
                Debug.LogError("[FarmScreenSetup] 아이콘 그림이 없다 : " + spriteKey);
            }

            if (rect.GetComponent<Button>() == null)
            {
                rect.gameObject.AddComponent<Button>();
            }
        }

        // 판 뿌리(화면 전체) · 가림막 · 상자 · 제목. 상자는 아이콘 왼쪽에, 아래끝을 그 아이콘 아래끝에 맞춘다. 처음엔 꺼 둔다.
        private static RectTransform MakePanel(Transform root, string name, float width, float height, float iconTop, TMP_Text sample)
        {
            RectTransform panel = Ensure(root, name);
            FormalUiBuild.Stretch(panel, 0f);

            RectTransform blocker = Ensure(panel, UiNames.FarmPanelBlocker);
            FormalUiBuild.Stretch(blocker, -LayoutConstUi.BarBleedLength);
            FormalUiBuild.SetImage(blocker, Clear, true);
            if (blocker.GetComponent<Button>() == null)
            {
                blocker.gameObject.AddComponent<Button>();
            }

            RectTransform edge = Ensure(panel, UiNames.FarmPanelBox);
            float left = IconLeft - PanelGap - width;
            float bottom = ReferenceHeight - iconTop - IconSize;
            FormalUiBuild.BottomLeft(edge, left, bottom, width, height);
            FormalUiBuild.SetImage(edge, PanelEdge, true);

            RectTransform box = Ensure(edge, "Fill");
            FormalUiBuild.Stretch(box, 2f);
            FormalUiBuild.SetImage(box, PanelCream, true);

            TMP_Text title = EnsureText(box, UiNames.FarmPanelTitle, sample);
            FormalUiBuild.TopLeft(title.rectTransform, BoxPad, 4f, width - BoxPad * 2f, BoxTitleHeight);
            StyleText(title, 18f, TextDark, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;

            panel.gameObject.SetActive(false);
            return box;
        }

        // 강화 판 index 번째 줄. 글은 왼쪽 두 줄, 버튼은 오른쪽에 세로 가운데로 붙인다.
        private static void PlaceUpgradeRow(RectTransform box, int index, string textName, string buttonName, RectTransform template,
            TMP_Text sample)
        {
            float top = BoxTitleHeight + 6f + index * UpgradeRowPitch;
            float textWidth = UpgradeBoxWidth - BoxPad * 3f - UpgradeButtonWidth;

            TMP_Text text = EnsureText(box, textName, sample);
            FormalUiBuild.TopLeft(text.rectTransform, BoxPad, top, textWidth, UpgradeTextHeight);
            StyleText(text, 15f, TextDark, TextAlignmentOptions.Left);
            text.lineSpacing = -10f;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform button = EnsureButton(box, buttonName, template, "-");
            FormalUiBuild.TopLeft(button, UpgradeBoxWidth - BoxPad - UpgradeButtonWidth, top + (UpgradeTextHeight - BoxRowHeight) * 0.5f,
                UpgradeButtonWidth, BoxRowHeight);
        }

        // template(「다음날로」)을 복제해 같은 바탕 · 글꼴의 버튼을 만든다. 있으면 그대로 쓴다.
        private static RectTransform EnsureButton(Transform parent, string name, RectTransform template, string labelText)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = Object.Instantiate(template.gameObject, parent, false);
                go.name = name;
                go.SetActive(true);
                child = go.transform;
            }

            TMP_Text label = child.GetComponentInChildren<TMP_Text>();
            label.fontSize = 18f;
            label.text = labelText;
            return child as RectTransform;
        }

        private static void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
        }

        private static RectTransform Ensure(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            return child as RectTransform;
        }

        private static TMP_Text EnsureText(Transform parent, string name, TMP_Text sample)
        {
            RectTransform rect = Ensure(parent, name);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = sample.font;
            text.fontSharedMaterial = sample.fontSharedMaterial;
            return text;
        }

        // 부모 아래끝 가운데에서 위로 centerY 만큼 올라간 자리에 가운데를 둔다.
        private static void BottomCenter(RectTransform rect, float centerY, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, centerY);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
