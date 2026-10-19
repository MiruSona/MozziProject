using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 전용 화면 손질기(KitchenScreenSetup 등)가 같이 쓰는 짓기 도움 함수. 빈 뿌리에서 매번 새로 짓는 방식을 전제로 한다.
    // Stage 안 자리는 세계 좌표(왼쪽 위 원점 · y 아래로)를 그대로 받는다 (FormalLayout).
    public static class FormalUiBuild
    {
        public const string FontPath = "Assets/_Project/Fonts/ONEMobilePOP SDF.asset";

        public static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
        public static readonly Color TextDark = new Color(0.29f, 0.18f, 0.13f, 1f);
        public static readonly Color TextLight = new Color(1f, 0.98f, 0.92f, 1f);
        public static readonly Color PlateDark = new Color(0.17f, 0.11f, 0.09f, 0.85f);
        private static readonly Color MenuCream = new Color(1f, 0.965f, 0.9f, 1f);
        private static readonly Color MenuDim = new Color(0f, 0f, 0f, 0.45f);

        public static RectTransform NewNode(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform as RectTransform;
        }

        // 화면 뿌리. 바탕 Image 색은 OverlayScreen 의 Backdrop 이 따라 쓴다.
        public static GameObject NewScreenRoot(string name, Color backColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rect = root.transform as RectTransform;
            Stretch(rect, 0f);
            Image image = root.AddComponent<Image>();
            image.color = backColor;
            return root;
        }

        // 내용 칸 아래끝 가운데에 붙는 540×1200 칸.
        public static RectTransform NewStage(Transform root)
        {
            RectTransform stage = NewNode(root, UiNames.Stage);
            stage.anchorMin = new Vector2(0.5f, 0f);
            stage.anchorMax = new Vector2(0.5f, 0f);
            stage.pivot = new Vector2(0.5f, 0f);
            stage.anchoredPosition = Vector2.zero;
            stage.sizeDelta = new Vector2(FormalLayout.StageWidth, FormalLayout.StageHeight);
            return stage;
        }

        // Stage 위끝보다 위(1200 보다 긴 화면)를 채우는 띠.
        public static void NewFillTop(Transform root, Color color)
        {
            RectTransform top = NewNode(root, UiNames.SceneFillTop);
            top.anchorMin = new Vector2(0f, 0f);
            top.anchorMax = new Vector2(1f, 1f);
            top.offsetMin = new Vector2(0f, FormalLayout.StageHeight - 1f);
            top.offsetMax = new Vector2(0f, LayoutConstUi.BarBleedLength);
            SetImage(top, color, false);
        }

        public static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        // 부모 왼쪽 위에서 (x, y) 들어간 자리. y 는 아래로 양수다.
        public static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 부모 왼쪽 아래에서 (x, y) 들어간 자리. y 는 위로 양수다.
        public static void BottomLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 부모 왼쪽 위 기준 (x, y) 에 rect 의 pivot 점을 둔다.
        public static void PivotAt(RectTransform rect, float x, float y, Vector2 pivot, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = pivot;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static Image SetImage(RectTransform rect, Color color, bool raycast)
        {
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Sprite LoadSprite(string spriteKey)
        {
            Sprite sprite = Resources.Load<Sprite>(SpriteKeys.Root + spriteKey);
            if (sprite == null)
            {
                Debug.LogError("[FormalUiBuild] 그림이 없다 : " + spriteKey + " (FormalArtCopy 를 먼저 돌린다)");
            }

            return sprite;
        }

        // 그림 한 장을 제 크기(1px = UI 1)로 놓는다. pivot 은 그림 안 비율, (x, y) 는 그 점의 세계 자리.
        public static Image PlaceSprite(Transform parent, string name, string spriteKey, float x, float y, Vector2 pivot)
        {
            RectTransform rect = NewNode(parent, name);
            Sprite sprite = LoadSprite(spriteKey);
            Vector2 size = new Vector2(32f, 32f);
            if (sprite != null)
            {
                size = sprite.rect.size;
            }

            PivotAt(rect, x, y, pivot, size.x, size.y);
            Image image = SetImage(rect, Color.white, false);
            image.sprite = sprite;
            return image;
        }

        // 9-slice 틀. 배율을 안 넣으면 테두리가 3배로 두꺼워진다 (FormalLayout.SliceMultiplier).
        public static Image MakeSliced(RectTransform rect, string spriteKey, Color color, bool raycast)
        {
            Image image = SetImage(rect, color, raycast);
            image.sprite = LoadSprite(spriteKey);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = FormalLayout.SliceMultiplier;
            return image;
        }

        public static TextMeshProUGUI MakeText(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewNode(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        // ui_button 틀 버튼 + 가운데 Label. 글자는 화면 코드가 채운다. 리스너는 코드가 건다 (인스펙터 연결 없음).
        public static RectTransform MakeButton(Transform parent, string name, string labelText, float fontSize)
        {
            RectTransform rect = NewNode(parent, name);
            MakeSliced(rect, SpriteKeys.UiButtonFrame, Color.white, true);
            rect.gameObject.AddComponent<Button>();

            TextMeshProUGUI label = MakeText(rect, UiNames.ButtonLabel, fontSize, TextDark, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 6f);
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.6f;
            label.fontSizeMax = fontSize;
            label.text = labelText;
            return rect;
        }

        // 아래 버튼 줄 : 내용 칸 아래끝에 붙는 전체 폭 칸에 버튼을 가운데 정렬로 한 줄 놓는다.
        public static RectTransform MakeBottomRow(Transform root, string[] names, string[] labels, float[] widths)
        {
            RectTransform row = NewNode(root, UiNames.BottomRow);
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, FormalLayout.RowBottom);
            row.sizeDelta = new Vector2(FormalLayout.StageWidth, FormalLayout.RowHeight);

            float[] lefts = RowLefts(widths);
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform button = MakeButton(row, names[i], labels[i], FormalLayout.RowFontSize);
                BottomLeft(button, lefts[i], 0f, widths[i], FormalLayout.RowHeight);
            }

            return row;
        }

        // 폭 목록을 가운데 정렬로 한 줄에 놓는다. 돌려준 x 들은 각 버튼의 왼끝이다.
        public static float[] RowLefts(float[] widths)
        {
            float total = 0f;
            for (int i = 0; i < widths.Length; i++)
            {
                total += widths[i];
            }

            total += FormalLayout.RowGap * (widths.Length - 1);
            float[] lefts = new float[widths.Length];
            float x = (FormalLayout.StageWidth - total) * 0.5f;
            for (int i = 0; i < widths.Length; i++)
            {
                lefts[i] = x;
                x += widths[i] + FormalLayout.RowGap;
            }

            return lefts;
        }

        // 윗줄 골드 판 (내용 칸 왼쪽 위). GoldText 를 안에 둔다.
        public static void MakeTopRow(Transform root)
        {
            MakeTopRow(root, FormalLayout.TopPlateLeft, FormalLayout.TopPlateTop, FormalLayout.TopPlateWidth, FormalLayout.TopPlateHeight);
        }

        // 화면 그림이 왼쪽 위를 가리면 안 될 때 자리 · 크기를 따로 준다 (합성소).
        public static void MakeTopRow(Transform root, float left, float top, float width, float height)
        {
            RectTransform row = NewNode(root, UiNames.TopRow);
            Stretch(row, 0f);

            RectTransform plate = NewNode(row, UiNames.GoldPlate);
            TopLeft(plate, left, top, width, height);
            SetImage(plate, PlateDark, false);

            TextMeshProUGUI gold = MakeText(plate, UiNames.GoldText, 24f, TextLight, TextAlignmentOptions.Center);
            Stretch(gold.rectTransform, 4f);
            gold.text = "0 G";
        }

        // 차림표 판 (주방 · 가판대 같이 씀, 코드는 RecipeMenuView) : 어두운 가림막(누르면 닫힘) + 크림색 상자 + 줄 열둘(2열). 처음엔 꺼 둔다.
        // 판은 화면 뿌리 맨 위에 붙인다. 가림막은 버튼 줄까지 덮지만 상자는 버튼 줄 바로 위(KitchenLayout.MenuBoxBottom)에 아래끝을 붙여 버튼을 안 가린다.
        // 여기서는 줄 열둘이 다 든 높이로 짓고, 열 때 RecipeMenuView 가 찾은 떡 수에 맞춰 높이를 줄인다 (줄은 상자 위끝 기준이라 위로 자란다).
        public static void MakeRecipeMenuPanel(Transform root)
        {
            RectTransform panel = NewNode(root, UiNames.KitchenMenuPanel);
            Stretch(panel, 0f);

            RectTransform blocker = NewNode(panel, UiNames.KitchenMenuBlocker);
            Stretch(blocker, -LayoutConstUi.BarBleedLength);
            SetImage(blocker, MenuDim, true);
            blocker.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float boxWidth = KitchenLayout.MenuBoxPad * 2f + KitchenLayout.MenuRowWidth * KitchenLayout.MenuColumns + KitchenLayout.MenuRowGap;
            float boxHeight = KitchenLayout.MenuBoxHeight(UiNames.KitchenMenuRowCount);

            RectTransform box = NewNode(panel, UiNames.KitchenMenuBox);
            box.anchorMin = new Vector2(0.5f, 0f);
            box.anchorMax = new Vector2(0.5f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, KitchenLayout.MenuBoxBottom);
            box.sizeDelta = new Vector2(boxWidth, boxHeight);
            MakeSliced(box, SpriteKeys.UiButtonFrame, MenuCream, true);

            TextMeshProUGUI title = MakeText(box, UiNames.KitchenMenuTitleText, 24f, TextDark, TextAlignmentOptions.Center);
            TopLeft(title.rectTransform, KitchenLayout.MenuBoxPad, KitchenLayout.MenuBoxPad, boxWidth - KitchenLayout.MenuBoxPad * 2f, KitchenLayout.MenuTitleHeight);
            title.text = "차림표 : 누르면 바로 만든다";

            TextMeshProUGUI empty = MakeText(box, UiNames.KitchenMenuEmptyText, 20f, TextDark, TextAlignmentOptions.Center);
            TopLeft(empty.rectTransform, KitchenLayout.MenuBoxPad, KitchenLayout.MenuBoxPad + KitchenLayout.MenuTitleHeight,
                boxWidth - KitchenLayout.MenuBoxPad * 2f, KitchenLayout.MenuRowHeight * 2f);
            empty.textWrappingMode = TextWrappingModes.Normal;
            empty.text = UiText.StallMakeEmpty;

            for (int i = 0; i < UiNames.KitchenMenuRowCount; i++)
            {
                int column = i % KitchenLayout.MenuColumns;
                int line = i / KitchenLayout.MenuColumns;
                RectTransform row = MakeButton(box, UiNames.KitchenMenuRowPrefix + i, "", 20f);
                TopLeft(row,
                    KitchenLayout.MenuBoxPad + column * (KitchenLayout.MenuRowWidth + KitchenLayout.MenuRowGap),
                    KitchenLayout.MenuBoxPad + KitchenLayout.MenuTitleHeight + line * (KitchenLayout.MenuRowHeight + KitchenLayout.MenuRowGap),
                    KitchenLayout.MenuRowWidth, KitchenLayout.MenuRowHeight);

                RectTransform label = row.Find(UiNames.ButtonLabel) as RectTransform;
                label.offsetMin = new Vector2(52f, 6f);

                RectTransform icon = NewNode(row, UiNames.CakeIcon);
                icon.anchorMin = new Vector2(0f, 0.5f);
                icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                // 떡 그림이 캔버스 바닥에 붙어 있어 그림 가운데가 캔버스 가운데보다 약 5 아래다. 그만큼 올려 줄 가운데에 맞춘다.
                icon.anchoredPosition = new Vector2(12f, 5f);
                icon.sizeDelta = new Vector2(32f, 32f);
                SetImage(icon, Color.white, false).preserveAspect = true;
            }

            panel.gameObject.SetActive(false);
        }

        public static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }
    }
}
